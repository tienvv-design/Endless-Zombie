using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

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
    }

    [BurstCompile]
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
                    out float3 spawnPosition))
            {
                entry.EnqueuedCount = math.max(entry.SpawnedCount, entry.EnqueuedCount - 1);
                entry.State = SpawnEntryRuntimeState.Active;
                entries[request.SpawnEntryIndex] = entry;
                requests.RemoveAt(0);
                continue;
            }

            Entity mob = ecb.Instantiate(request.EnemyPrefab);
            ecb.SetComponent(mob, LocalTransform.FromPositionRotationScale(
                spawnPosition, quaternion.identity, math.max(0.01f, request.Scale)));
            if (SystemAPI.HasComponent<Mob>(request.EnemyPrefab))
            {
                Mob mobData = SystemAPI.GetComponent<Mob>(request.EnemyPrefab);
                mobData.Health = math.max(1, (int)math.round(mobData.Health * request.HealthMultiplier));
                mobData.MaxHealth = mobData.Health;
                mobData.EnemyType = request.EnemyType;
                mobData.XPReward = math.max(1, request.XPReward);
                mobData.GoldReward = math.max(0, request.GoldReward);
                mobData.SpawnTime = (float)SystemAPI.Time.ElapsedTime;
                ecb.SetComponent(mob, mobData);
            }
            if (SystemAPI.HasComponent<KamikazeUnit>(request.EnemyPrefab))
            {
                KamikazeUnit attack = SystemAPI.GetComponent<KamikazeUnit>(request.EnemyPrefab);
                attack.Damage = math.max(1, (int)math.round(attack.Damage * request.DamageMultiplier));
                ecb.SetComponent(mob, attack);
            }

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

    private static bool TryChoosePosition(
        FixedString64Bytes groupId,
        float3 playerPosition,
        DynamicBuffer<SpawnArenaRegion> regions,
        CollisionWorld collisionWorld,
        ref SpawnPositionSettings settings,
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
}

public static class WaveSpawnRuntimeRules
{
    public static int CalculateSpawnBudget(int maxAlive, int alive)
    {
        return math.max(0, maxAlive - alive);
    }
}
