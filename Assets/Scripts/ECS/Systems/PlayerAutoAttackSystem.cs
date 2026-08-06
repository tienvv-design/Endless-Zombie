using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
internal partial struct PlayerAutoAttackSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WeaponManager>();
        state.RequireForUpdate<GameRunningTag>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
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
                attack.ValueRW.AmmoInMagazine = math.max(1, attack.ValueRO.MagazineSize);
                attack.ValueRW.ReloadTimer = 0f;
                attack.ValueRW.IsReloading = false;
            }
            return;
        }

        if (attack.ValueRO.AmmoInMagazine <= 0)
        {
            attack.ValueRW.IsReloading = true;
            attack.ValueRW.ReloadTimer = math.max(0.05f, attack.ValueRO.ReloadDuration);
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

        Entity closestMob = Entity.Null;
        float3 closestPosition = float3.zero;
        float closestDistanceSq = attack.ValueRO.AttackRange * attack.ValueRO.AttackRange;

        foreach (var (transform, mob, entity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Mob>>().WithEntityAccess())
        {
            float distanceSq = math.distancesq(playerPosition, transform.ValueRO.Position);
            if (distanceSq > closestDistanceSq)
                continue;

            closestDistanceSq = distanceSq;
            closestMob = entity;
            closestPosition = transform.ValueRO.Position;
        }

        if (closestMob == Entity.Null)
            return;

        EntityCommandBuffer ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        float3 targetDirection = math.normalizesafe(closestPosition - playerPosition, new float3(0f, 0f, 1f));
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
                ? math.max(1, (int)math.round(attack.ValueRO.DamagePerHit * attack.ValueRO.CriticalDamage))
                : attack.ValueRO.DamagePerHit;

            Entity projectile = ecb.Instantiate(attack.ValueRO.WeaponEntityPrefab);
            ecb.SetComponent(projectile, new LocalTransform
            {
                Position = playerPosition + direction * 0.75f + new float3(0f, 0.75f, 0f),
                Rotation = quaternion.LookRotationSafe(direction, math.up()),
                Scale = 0.35f,
            });
            ecb.AddComponent(projectile, new PlayerProjectile
            {
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
            attack.ValueRW.ReloadTimer = math.max(0.05f, attack.ValueRO.ReloadDuration);
        }
    }
}
