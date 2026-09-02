using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal partial struct PlayerAutoAttackSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WeaponManager>();
        state.RequireForUpdate<GameRunningTag>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        RefRW<WeaponManager> attack = SystemAPI.GetSingletonRW<WeaponManager>();
        if (attack.ValueRO.EnableOrbitingWeapons)
            return;

        float deltaTime = SystemAPI.Time.DeltaTime;
        attack.ValueRW.Timer -= deltaTime;

        if (attack.ValueRO.IsReloading)
        {
            attack.ValueRW.ReloadTimer -= deltaTime;
            if (attack.ValueRO.ReloadTimer <= 0f)
            {
                attack.ValueRW.AmmoInMagazine = attack.ValueRO.MagazineSize;
                attack.ValueRW.ReloadTimer = 0f;
                attack.ValueRW.IsReloading = false;
            }
            return;
        }

        if (attack.ValueRO.AmmoInMagazine <= 0)
        {
            attack.ValueRW.IsReloading = true;
            attack.ValueRW.ReloadTimer = attack.ValueRO.ReloadDuration;
            EntityCommandBuffer reloadEcb = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            Entity reloadEvent = reloadEcb.CreateEntity();
            reloadEcb.AddComponent(reloadEvent, new WeaponReloadVfxEvent
            {
                Position = float3.zero,
                Duration = attack.ValueRO.ReloadDuration,
            });
            return;
        }

        if (attack.ValueRO.Timer > 0f)
            return;

        if (!SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> objects))
            return;

        float3 playerPosition = float3.zero;
        bool playerFound = false;
        foreach (GameObjectInfo objectInfo in objects)
        {
            if (objectInfo.ObjectType != GameObjectType.Character1)
                continue;

            playerPosition = objectInfo.Position;
            playerFound = true;
            break;
        }

        if (!playerFound)
            return;

        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
            return;

        Entity closestMob = Entity.Null;
        float3 closestPosition = float3.zero;
        float closestDistanceSq = attack.ValueRO.AttackRange * attack.ValueRO.AttackRange;

        foreach (var (transform, mob, entity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Mob>>().WithEntityAccess())
        {
            if (!GameplayCameraVisibility.Contains(gameplayCamera, transform.ValueRO.Position))
                continue;

            float distanceSq = math.distancesq(playerPosition, transform.ValueRO.Position);
            if (distanceSq > closestDistanceSq)
                continue;
            closestDistanceSq = distanceSq;
            closestMob = entity;
            closestPosition = transform.ValueRO.Position;
        }

        if (closestMob == Entity.Null)
            return;

        float3 playerToTargetDirection = math.normalizesafe(
            closestPosition - playerPosition, new float3(0f, 0f, 1f));
        if (!IsPlayerFacingTarget(playerToTargetDirection))
            return;

        // Spawn both the gameplay projectile and its tracer at the exact same point
        // used by the equipped gun's muzzle VFX. Fall back to the old player-based
        // origin only while the held-weapon presenter is not ready yet.
        float3 shotOrigin = playerPosition + playerToTargetDirection * 0.75f +
                            new float3(0f, 0.75f, 0f);
        Transform muzzle = WeaponVfxRuntime.CurrentBulletSpawn ?? WeaponVfxRuntime.CurrentMuzzle;
        GunConfig vfxConfig = WeaponVfxRuntime.CurrentConfig;
        if (muzzle != null && vfxConfig != null && vfxConfig.Archetype == attack.ValueRO.Archetype)
            shotOrigin = muzzle.position;

        float3 targetPosition = closestPosition + new float3(0f, 0.75f, 0f);
        float3 targetDirection = math.normalizesafe(
            targetPosition - shotOrigin, playerToTargetDirection);

        EntityCommandBuffer ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        Entity firedEvent = ecb.CreateEntity();
        ecb.AddComponent(firedEvent, new WeaponFiredVfxEvent
        {
            Position = shotOrigin,
            Direction = targetDirection,
            TargetPosition = targetPosition,
            Target = closestMob,
        });
        int projectileCount = attack.ValueRO.ProjectileCount;
        float totalSpreadRadians = math.radians(attack.ValueRO.SpreadAngle);
        float angleStep = projectileCount > 1 ? totalSpreadRadians / (projectileCount - 1) : 0f;
        Unity.Mathematics.Random random = attack.ValueRO.Random;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = projectileCount > 1
                ? -totalSpreadRadians * 0.5f + angleStep * i
                : 0f;
            float3 direction = math.mul(quaternion.RotateY(angle), targetDirection);
            bool isCritical = random.NextFloat() < attack.ValueRO.CriticalChance;
            bool appliesElement = attack.ValueRO.Element != ElementType.None &&
                                  random.NextFloat() < attack.ValueRO.ElementChance;
            int damage = isCritical
                // Critical damage must never lose part of its multiplier because of
                // midpoint rounding (for example 7 x 1.5 must be 11, not 10).
                ? math.max(1, (int)math.ceil(attack.ValueRO.DamagePerHit * attack.ValueRO.CriticalDamage))
                : attack.ValueRO.DamagePerHit;

            Entity projectile = ecb.Instantiate(attack.ValueRO.WeaponEntityPrefab);
            float projectileVisualScale = attack.ValueRO.Archetype switch
            {
                GunArchetype.FlameRifle => 0.01f,
                GunArchetype.RocketLauncher or GunArchetype.GrenadeLauncher => 0.55f,
                _ => 0.48f,
            };
            ecb.SetComponent(projectile, new LocalTransform
            {
                Position = shotOrigin,
                Rotation = quaternion.LookRotationSafe(direction, math.up()),
                Scale = projectileVisualScale,
            });
            ecb.AddComponent(projectile, new PlayerProjectile
            {
                Archetype = attack.ValueRO.Archetype,
                Direction = direction,
                Damage = damage,
                Speed = attack.ValueRO.ProjectileSpeed,
                RemainingRange = attack.ValueRO.AttackRange,
                RemainingPierce = attack.ValueRO.Pierce,
                Knockback = attack.ValueRO.Knockback,
                IsCritical = isCritical,
                HitDistanceSq = 0.25f,
                IsExplosive = attack.ValueRO.IsExplosive,
                ExplosionRadius = attack.ValueRO.ExplosionRadius,
                ExplosionDamageMultiplier = attack.ValueRO.ExplosionDamageMultiplier,
                ExplosionKnockback = attack.ValueRO.Knockback * attack.ValueRO.ExplosionKnockbackMultiplier,
                RemainingRicochets = attack.ValueRO.RicochetCount,
                RicochetSearchRange = attack.ValueRO.RicochetSearchRange,
                RemainingChains = attack.ValueRO.ChainCount,
                ChainRange = attack.ValueRO.ChainRange,
                ChainDamageMultiplier = attack.ValueRO.ChainDamageMultiplier,
                Element = appliesElement ? attack.ValueRO.Element : ElementType.None,
                ElementDuration = attack.ValueRO.ElementDuration,
                ElementMagnitude = attack.ValueRO.ElementMagnitude,
            });
            ecb.AddBuffer<PlayerProjectileHit>(projectile);
        }

        attack.ValueRW.Random = random;
        attack.ValueRW.AmmoInMagazine--;
        attack.ValueRW.Timer = math.max(attack.ValueRO.Cooldown, 0.05f);

        if (attack.ValueRO.AmmoInMagazine <= 0)
        {
            attack.ValueRW.IsReloading = true;
            attack.ValueRW.ReloadTimer = attack.ValueRO.ReloadDuration;
            Entity reloadEvent = ecb.CreateEntity();
            ecb.AddComponent(reloadEvent, new WeaponReloadVfxEvent
            {
                Position = playerPosition,
                Duration = attack.ValueRO.ReloadDuration,
            });
        }
    }

    private static bool IsPlayerFacingTarget(float3 targetDirection)
    {
        CharacterLogic player = UnityEngine.Object.FindFirstObjectByType<CharacterLogic>();
        if (player == null || player.AimTransform == null)
            return true;

        Transform bulletSpawn = WeaponVfxRuntime.CurrentBulletSpawn;
        Vector3 forward = bulletSpawn != null
            ? bulletSpawn.forward
            : player.AimTransform.forward;
        forward.y = 0f;
        Vector3 desired = (Vector3)targetDirection;
        desired.y = 0f;
        if (forward.sqrMagnitude < 0.0001f || desired.sqrMagnitude < 0.0001f)
            return true;

        const float aimToleranceDegrees = 4f;
        return Vector3.Dot(forward.normalized, desired.normalized) >=
               Mathf.Cos(aimToleranceDegrees * Mathf.Deg2Rad);
    }
}
