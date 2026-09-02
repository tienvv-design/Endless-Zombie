using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[UpdateAfter(typeof(SpawnEntrySchedulerSystem))]
[UpdateBefore(typeof(WaveCompletionSystem))]
public partial struct SpawnRequestProcessingSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StageRuntime>();
        state.RequireForUpdate<SpawnPositionSettings>();
        state.RequireForUpdate<CombatMetrics>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<GameRunningTag>();
        state.RequireForUpdate<GameplayStartedTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        StageRuntime stage = SystemAPI.GetSingleton<StageRuntime>();
        if (stage.State != StageRuntimeState.Running)
            return;

        DynamicBuffer<WaveRuntime> waves = SystemAPI.GetSingletonBuffer<WaveRuntime>();
        DynamicBuffer<SpawnEntryRuntime> entries = SystemAPI.GetSingletonBuffer<SpawnEntryRuntime>();
        DynamicBuffer<SpawnRequest> requests = SystemAPI.GetSingletonBuffer<SpawnRequest>();
        DynamicBuffer<SpawnArenaRegion> regions = SystemAPI.GetSingletonBuffer<SpawnArenaRegion>();
        RefRW<SpawnPositionSettings> settings = SystemAPI.GetSingletonRW<SpawnPositionSettings>();
        int alive = SystemAPI.GetSingleton<CombatMetrics>().ActiveEnemies;
        int waveIndex = stage.CurrentWaveIndex;
        if (waveIndex < 0 || waveIndex >= waves.Length || requests.IsEmpty)
            return;

        int maxAlive = waves[waveIndex].MaxAliveEnemies;
        int spawnBudget = WaveSpawnRuntimeRules.CalculateSpawnBudget(maxAlive, alive);
        if (spawnBudget == 0)
            return;

        float3 playerPosition = float3.zero;
        bool playerFound = false;
        if (SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> objects))
        {
            foreach (GameObjectInfo info in objects)
            {
                if (info.ObjectType != GameObjectType.Character1) continue;
                playerPosition = info.Position;
                playerFound = true;
                break;
            }
        }
        if (!playerFound)
            return;

        PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        EntityCommandBuffer ecb = new(Allocator.Temp);
        int spawnedThisFrame = 0;
        Camera gameplayCamera = stage.SpawnOutsideCamera ? Camera.main : null;

        while (!requests.IsEmpty && spawnedThisFrame < spawnBudget)
        {
            SpawnRequest request = requests[0];
            SpawnEntryRuntime entry = entries[request.SpawnEntryIndex];
            if (entry.State == SpawnEntryRuntimeState.Failed || request.EnemyPrefab == Entity.Null)
            {
                entry.State = SpawnEntryRuntimeState.Failed;
                entries[request.SpawnEntryIndex] = entry;
                requests.RemoveAt(0);
                continue;
            }

            if (!TryChoosePosition(
                    request.SpawnArenaGroupId,
                    playerPosition,
                    regions,
                    physicsWorld.CollisionWorld,
                    ref settings.ValueRW,
                    gameplayCamera,
                    stage.OffscreenSpawnPadding,
                    out float3 spawnPosition))
            {
                entry.EnqueuedCount = math.max(entry.SpawnedCount, entry.EnqueuedCount - 1);
                entry.State = SpawnEntryRuntimeState.Active;
                entries[request.SpawnEntryIndex] = entry;
                requests.RemoveAt(0);
                continue;
            }

            FlowFieldNavigationSurface navigation = FlowFieldNavigationSurface.Active;
            if (navigation != null && navigation.IsReady &&
                !navigation.TryProjectToWalkable(spawnPosition, out spawnPosition))
            {
                entry.EnqueuedCount = math.max(entry.SpawnedCount, entry.EnqueuedCount - 1);
                entry.State = SpawnEntryRuntimeState.Active;
                entries[request.SpawnEntryIndex] = entry;
                requests.RemoveAt(0);
                continue;
            }
            // Projection can move a candidate to the nearest walkable cell,
            // occasionally pushing an originally off-screen point back into
            // view. Validate the final position that will actually be used.
            if (gameplayCamera != null && IsInsideCamera(
                    gameplayCamera, spawnPosition, math.max(0.12f, stage.OffscreenSpawnPadding)))
            {
                entry.EnqueuedCount = math.max(entry.SpawnedCount, entry.EnqueuedCount - 1);
                entry.State = SpawnEntryRuntimeState.Active;
                entries[request.SpawnEntryIndex] = entry;
                requests.RemoveAt(0);
                continue;
            }

            Entity mob = ecb.Instantiate(request.EnemyPrefab);
            if (SystemAPI.HasComponent<MobVisualVariant>(request.EnemyPrefab))
                ecb.SetComponent(mob, new MobVisualVariant { Kind = request.VisualKind });
            else
                ecb.AddComponent(mob, new MobVisualVariant { Kind = request.VisualKind });
            float spawnScale = math.max(0.01f, request.Scale);
            bool boss = request.EnemyType == EnemyType.Boss || waves[request.WaveIndex].WaveType == WaveType.Boss;
            bool eliteWave = !boss && waves[request.WaveIndex].WaveType == WaveType.Elite;
            bool forceElite = request.EnemyType == EnemyType.Elite || eliteWave;
            bool randomElite = !boss && stage.EnableEliteModifiers && request.EnemyType == EnemyType.Normal &&
                               request.WaveIndex >= stage.EliteChanceStartsAtWave &&
                               settings.ValueRW.Random.NextFloat() < stage.RandomEliteChance;
            EliteModifierKind eliteKind = stage.EnableEliteModifiers && (forceElite || randomElite)
                ? (EliteModifierKind)settings.ValueRW.Random.NextInt(1, 5)
                : EliteModifierKind.None;
            if (SystemAPI.HasComponent<Mob>(request.EnemyPrefab))
            {
                Mob mobData = SystemAPI.GetComponent<Mob>(request.EnemyPrefab);
                mobData.Health = math.max(1, (int)math.round(mobData.Health * request.HealthMultiplier));
                if (boss)
                {
                    mobData.Health = math.max(1, (int)math.ceil(mobData.Health * stage.BossHealthMultiplier));
                    mobData.EnemyType = EnemyType.Boss;
                    mobData.KnockbackResistance = 1f;
                    mobData.CrowdControlResistance = 0.8f;
                    mobData.GoldMultiplier = math.max(5, mobData.GoldMultiplier);
                    mobData.XPReward = math.max(1, (int)math.ceil(request.XPReward * 5f));
                    mobData.GoldReward = math.max(1, (int)math.ceil(request.GoldReward * 5f));
                    spawnScale *= stage.BossScaleMultiplier;
                }
                else mobData.EnemyType = eliteKind != EliteModifierKind.None ? EnemyType.Elite : request.EnemyType;
                if (!boss)
                {
                    mobData.XPReward = math.max(1, request.XPReward);
                    mobData.GoldReward = math.max(0, request.GoldReward);
                }
                mobData.SpawnTime = (float)SystemAPI.Time.ElapsedTime;
                EliteModifier modifier = default;
                if (eliteKind != EliteModifierKind.None)
                    modifier = ApplyEliteModifier(eliteKind, ref mobData, ref spawnScale);
                mobData.MaxHealth = mobData.Health;
                ecb.SetComponent(mob, mobData);
                if (eliteKind != EliteModifierKind.None)
                    ecb.AddComponent(mob, modifier);
            }
            if (SystemAPI.HasComponent<KamikazeUnit>(request.EnemyPrefab))
            {
                KamikazeUnit attack = SystemAPI.GetComponent<KamikazeUnit>(request.EnemyPrefab);
                attack.HitDistanceSq = math.max(0.01f, stage.AttackDistance * stage.AttackDistance);
                attack.Damage = math.max(1, (int)math.round(attack.Damage * request.DamageMultiplier));
                if (boss) attack.Damage = math.max(1, (int)math.ceil(attack.Damage * stage.BossDamageMultiplier));
                if (eliteKind == EliteModifierKind.Frenzied)
                {
                    attack.Damage = math.max(1, (int)math.ceil(attack.Damage * 1.35f));
                    attack.AttackInterval = math.max(0.15f, attack.AttackInterval * 0.75f);
                }
                else if (eliteKind == EliteModifierKind.Colossus)
                    attack.Damage = math.max(1, (int)math.ceil(attack.Damage * 1.2f));
                if (request.VisualKind == MobVisualKind.ZombieFat)
                {
                    attack.AttackInterval = math.max(1.25f, attack.AttackInterval);
                    attack.AttackImpactNormalizedTime = 0.9f;
                    attack.HasExploded = 0;
                }
                ecb.SetComponent(mob, attack);
            }
            if (SystemAPI.HasComponent<UnitMover>(request.EnemyPrefab) && eliteKind == EliteModifierKind.Frenzied)
            {
                UnitMover mover = SystemAPI.GetComponent<UnitMover>(request.EnemyPrefab);
                mover.moveSpeed *= 1.35f;
                mover.rotationSpeed *= 1.2f;
                ecb.SetComponent(mob, mover);
            }
            if (boss)
            {
                float baseSpeed = SystemAPI.HasComponent<UnitMover>(request.EnemyPrefab)
                    ? SystemAPI.GetComponent<UnitMover>(request.EnemyPrefab).moveSpeed : 1f;
                int baseDamage = SystemAPI.HasComponent<KamikazeUnit>(request.EnemyPrefab)
                    ? math.max(1, (int)math.ceil(SystemAPI.GetComponent<KamikazeUnit>(request.EnemyPrefab).Damage * request.DamageMultiplier * stage.BossDamageMultiplier)) : 1;
                ecb.AddComponent(mob, new BossPhase
                {
                    CurrentPhase = 1,
                    PhaseTwoHealthRatio = stage.BossPhaseTwoHealth,
                    PhaseThreeHealthRatio = stage.BossPhaseThreeHealth,
                    SpeedMultiplierPerPhase = stage.BossSpeedPerPhase,
                    DamageMultiplierPerPhase = stage.BossDamagePerPhase,
                    BaseMoveSpeed = baseSpeed,
                    BaseDamage = baseDamage,
                });
                ecb.AddComponent(mob, new BossShockwave
                {
                    Cooldown = stage.BossShockwaveCooldown,
                    WarningDuration = stage.BossShockwaveWarning,
                    Radius = stage.BossShockwaveRadius,
                    Damage = stage.BossShockwaveDamage,
                    Timer = stage.BossShockwaveCooldown,
                });
            }
            ecb.SetComponent(mob, LocalTransform.FromPositionRotationScale(
                spawnPosition, quaternion.identity, spawnScale));
            ecb.AddComponent(mob, new SpawnEmergence { Duration = stage.SpawnPortalDuration });
            if (SystemAPI.HasComponent<UnitMover>(request.EnemyPrefab))
                ecb.SetComponentEnabled<UnitMover>(mob, false);

            entry.SpawnedCount++;
            if (entry.SpawnedCount >= entry.Quantity)
                entry.State = SpawnEntryRuntimeState.Completed;
            entries[request.SpawnEntryIndex] = entry;
            requests.RemoveAt(0);
            spawnedThisFrame++;
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private static EliteModifier ApplyEliteModifier(EliteModifierKind kind, ref Mob mob, ref float scale)
    {
        EliteModifier modifier = new()
        {
            Kind = kind,
            IncomingDamageMultiplier = 1f,
        };
        mob.GoldMultiplier = math.max(2, mob.GoldMultiplier);
        mob.XPReward = math.max(1, (int)math.ceil(mob.XPReward * 1.75f));
        mob.GoldReward = math.max(1, (int)math.ceil(mob.GoldReward * 1.5f));
        switch (kind)
        {
            case EliteModifierKind.Bulwark:
                mob.Health = math.max(1, (int)math.ceil(mob.Health * 1.45f));
                mob.KnockbackResistance = math.max(mob.KnockbackResistance, 0.7f);
                mob.CrowdControlResistance = math.max(mob.CrowdControlResistance, 0.45f);
                modifier.IncomingDamageMultiplier = 0.6f;
                scale *= 1.1f;
                break;
            case EliteModifierKind.Frenzied:
                mob.Health = math.max(1, (int)math.ceil(mob.Health * 1.2f));
                scale *= 0.95f;
                break;
            case EliteModifierKind.Colossus:
                mob.Health = math.max(1, (int)math.ceil(mob.Health * 2.25f));
                mob.KnockbackResistance = math.max(mob.KnockbackResistance, 0.9f);
                mob.CrowdControlResistance = math.max(mob.CrowdControlResistance, 0.65f);
                scale *= 1.35f;
                break;
            case EliteModifierKind.Revenant:
                mob.Health = math.max(1, (int)math.ceil(mob.Health * 1.55f));
                modifier.HealthRegenerationPerSecond = math.max(1f, mob.Health * 0.018f);
                scale *= 1.08f;
                break;
        }
        return modifier;
    }

    private static bool TryChoosePosition(
        FixedString64Bytes groupId,
        float3 playerPosition,
        DynamicBuffer<SpawnArenaRegion> regions,
        CollisionWorld collisionWorld,
        ref SpawnPositionSettings settings,
        Camera gameplayCamera,
        float offscreenPadding,
        out float3 position)
    {
        int matchingRegions = 0;
        for (int i = 0; i < regions.Length; i++)
            if (regions[i].GroupId.Equals(groupId)) matchingRegions++;
        if (matchingRegions == 0)
        {
            position = float3.zero;
            return false;
        }

        for (int attempt = 0; attempt < settings.MaxPositionAttempts; attempt++)
        {
            int selected = settings.Random.NextInt(matchingRegions);
            SpawnArenaRegion region = default;
            for (int i = 0, match = 0; i < regions.Length; i++)
            {
                if (!regions[i].GroupId.Equals(groupId)) continue;
                if (match++ == selected) { region = regions[i]; break; }
            }

            float3 candidate = region.Center + new float3(
                settings.Random.NextFloat(-region.HalfExtents.x, region.HalfExtents.x),
                0f,
                settings.Random.NextFloat(-region.HalfExtents.z, region.HalfExtents.z));
            if (math.distance(candidate.xz, playerPosition.xz) < settings.MinPlayerDistance)
                continue;
            if (math.length(candidate.xz - playerPosition.xz) > settings.GameplayRadius)
                continue;
            float3 deadDelta = math.abs(candidate - settings.DeadZoneCenter);
            if (math.all(deadDelta <= settings.DeadZoneHalfExtents))
                continue;
            if (gameplayCamera != null && IsInsideCamera(gameplayCamera, candidate, offscreenPadding))
                continue;

            var input = new PointDistanceInput
            {
                Position = candidate,
                MaxDistance = settings.MinClearance,
                Filter = CollisionFilter.Default,
            };
            if (collisionWorld.CalculateDistance(input, out DistanceHit _))
                continue;

            position = candidate;
            return true;
        }

        position = float3.zero;
        return false;
    }

    private static bool IsInsideCamera(Camera camera, float3 worldPosition, float padding)
    {
        Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
        if (viewport.z <= 0f) return false;
        // Keep the model, not only its pivot, outside the visible edge.
        float margin = Mathf.Clamp(Mathf.Max(0.12f, padding), 0f, 0.25f);
        return viewport.x >= -margin && viewport.x <= 1f + margin &&
               viewport.y >= -margin && viewport.y <= 1f + margin;
    }
}

public static class WaveSpawnRuntimeRules
{
    public static int CalculateSpawnBudget(int maxAlive, int alive)
    {
        return math.max(0, maxAlive - alive);
    }
}
