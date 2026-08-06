using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
internal partial struct PlayerProjectileSystem : ISystem
{
    private EntityQuery _mobQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameRunningTag>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _mobQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<Mob>(),
            ComponentType.ReadOnly<LocalTransform>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        NativeArray<Entity> mobEntities = _mobQuery.ToEntityArray(Allocator.Temp);
        ComponentLookup<LocalTransform> transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
        ComponentLookup<Mob> mobs = SystemAPI.GetComponentLookup<Mob>(true);
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, projectile, hitList, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRW<PlayerProjectile>, DynamicBuffer<PlayerProjectileHit>>()
                     .WithEntityAccess())
        {
            float travelDistance = math.min(
                projectile.ValueRO.Speed * deltaTime,
                projectile.ValueRO.RemainingRange);
            if (travelDistance <= 0f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            float3 start = transform.ValueRO.Position;
            float3 end = start + projectile.ValueRO.Direction * travelDistance;
            float lastHitT = -1f;
            bool destroyProjectile = false;
            bool ricocheted = false;

            while (true)
            {
                Entity closestHit = Entity.Null;
                float closestT = float.MaxValue;

                for (int i = 0; i < mobEntities.Length; i++)
                {
                    Entity mobEntity = mobEntities[i];
                    if (!mobs.HasComponent(mobEntity) || mobs[mobEntity].Health <= 0 ||
                        WasAlreadyHit(hitList, mobEntity))
                        continue;

                    float t;
                    float distanceSq = DistanceToSegmentSq(
                        transforms[mobEntity].Position + new float3(0f, 0.75f, 0f), start, end, out t);
                    if (t <= lastHitT || distanceSq > projectile.ValueRO.HitDistanceSq)
                        continue;

                    if (t < closestT || (math.abs(t - closestT) < 0.0001f && mobEntity.Index < closestHit.Index))
                    {
                        closestT = t;
                        closestHit = mobEntity;
                    }
                }

                if (closestHit == Entity.Null)
                    break;

                if (projectile.ValueRO.IsExplosive && projectile.ValueRO.ExplosionRadius > 0f)
                {
                    Entity explosionEvent = ecb.CreateEntity();
                    ecb.AddComponent(explosionEvent, new DigitExplosionEvent
                    {
                        Position = math.lerp(start, end, closestT),
                        Radius = projectile.ValueRO.ExplosionRadius,
                        Damage = math.max(1, (int)math.round(
                            projectile.ValueRO.Damage * projectile.ValueRO.ExplosionDamageMultiplier)),
                        Knockback = projectile.ValueRO.ExplosionKnockback,
                        IsCritical = projectile.ValueRO.IsCritical,
                    });
                }
                else
                {
                    Entity damageEvent = ecb.CreateEntity();
                    ecb.AddComponent(damageEvent, new MobDamageTakenEvent
                    {
                        Id = closestHit.Index,
                        Entity = closestHit,
                        Amount = projectile.ValueRO.Damage,
                        KnockbackDirection = projectile.ValueRO.Direction,
                        KnockbackDistance = projectile.ValueRO.Knockback,
                        IsCritical = projectile.ValueRO.IsCritical,
                        Element = projectile.ValueRO.Element,
                        ElementDuration = projectile.ValueRO.ElementDuration,
                        ElementMagnitude = projectile.ValueRO.ElementMagnitude,
                    });
                }
                hitList.Add(new PlayerProjectileHit { Entity = closestHit });
                lastHitT = closestT;

                if (!projectile.ValueRO.IsExplosive && projectile.ValueRO.RemainingChains > 0)
                {
                    ApplyChainLightning(
                        ecb,
                        mobEntities,
                        transforms,
                        mobs,
                        hitList,
                        closestHit,
                        projectile.ValueRO.Damage,
                        projectile.ValueRO.RemainingChains,
                        projectile.ValueRO.ChainRange,
                        projectile.ValueRO.ChainDamageMultiplier,
                        projectile.ValueRO.IsCritical);
                    ecb.DestroyEntity(entity);
                    destroyProjectile = true;
                    break;
                }

                if (!projectile.ValueRO.IsExplosive && projectile.ValueRO.RemainingRicochets > 0)
                {
                    float3 impactPosition = math.lerp(start, end, closestT);
                    Entity ricochetTarget = FindClosestRicochetTarget(
                        mobEntities,
                        transforms,
                        mobs,
                        hitList,
                        impactPosition,
                        projectile.ValueRO.RicochetSearchRange);

                    if (ricochetTarget != Entity.Null)
                    {
                        float distanceTravelled = travelDistance * closestT;
                        projectile.ValueRW.RemainingRange = math.max(0f,
                            projectile.ValueRO.RemainingRange - distanceTravelled);
                        projectile.ValueRW.RemainingRicochets--;
                        projectile.ValueRW.Direction = math.normalizesafe(
                            transforms[ricochetTarget].Position + new float3(0f, 0.75f, 0f) - impactPosition,
                            projectile.ValueRO.Direction);
                        transform.ValueRW.Position = impactPosition;
                        transform.ValueRW.Rotation = quaternion.LookRotationSafe(
                            projectile.ValueRO.Direction,
                            math.up());
                        ricocheted = true;
                        break;
                    }
                }

                if (projectile.ValueRO.IsExplosive || projectile.ValueRO.RemainingPierce <= 0)
                {
                    ecb.DestroyEntity(entity);
                    destroyProjectile = true;
                    break;
                }

                projectile.ValueRW.RemainingPierce--;
            }

            if (destroyProjectile)
                continue;
            if (ricocheted)
                continue;

            transform.ValueRW.Position = end;
            projectile.ValueRW.RemainingRange -= travelDistance;
            if (projectile.ValueRO.RemainingRange <= 0f)
                ecb.DestroyEntity(entity);
        }

        mobEntities.Dispose();
    }

    private static bool WasAlreadyHit(DynamicBuffer<PlayerProjectileHit> hitList, Entity entity)
    {
        for (int i = 0; i < hitList.Length; i++)
            if (hitList[i].Entity == entity)
                return true;
        return false;
    }

    private static Entity FindClosestRicochetTarget(
        NativeArray<Entity> mobEntities,
        ComponentLookup<LocalTransform> transforms,
        ComponentLookup<Mob> mobs,
        DynamicBuffer<PlayerProjectileHit> hitList,
        float3 impactPosition,
        float searchRange)
    {
        Entity closest = Entity.Null;
        float closestDistanceSq = searchRange * searchRange;

        for (int i = 0; i < mobEntities.Length; i++)
        {
            Entity candidate = mobEntities[i];
            if (!mobs.HasComponent(candidate) || mobs[candidate].Health <= 0 ||
                !transforms.HasComponent(candidate) || WasAlreadyHit(hitList, candidate))
                continue;

            float distanceSq = math.distancesq(impactPosition, transforms[candidate].Position);
            if (distanceSq > closestDistanceSq)
                continue;

            if (closest == Entity.Null || distanceSq < closestDistanceSq ||
                (math.abs(distanceSq - closestDistanceSq) < 0.0001f && candidate.Index < closest.Index))
            {
                closest = candidate;
                closestDistanceSq = distanceSq;
            }
        }

        return closest;
    }

    private static void ApplyChainLightning(
        EntityCommandBuffer ecb,
        NativeArray<Entity> mobEntities,
        ComponentLookup<LocalTransform> transforms,
        ComponentLookup<Mob> mobs,
        DynamicBuffer<PlayerProjectileHit> hitList,
        Entity firstTarget,
        int baseDamage,
        int chainCount,
        float chainRange,
        float damageMultiplier,
        bool isCritical)
    {
        float3 chainStart = transforms[firstTarget].Position;
        int chainDamage = baseDamage;

        for (int chainIndex = 0; chainIndex < chainCount; chainIndex++)
        {
            Entity nextTarget = FindClosestRicochetTarget(
                mobEntities, transforms, mobs, hitList, chainStart, chainRange);
            if (nextTarget == Entity.Null)
                break;

            float3 chainEnd = transforms[nextTarget].Position;
            chainDamage = math.max(1, (int)math.round(chainDamage * damageMultiplier));

            Entity damageEvent = ecb.CreateEntity();
            ecb.AddComponent(damageEvent, new MobDamageTakenEvent
            {
                Id = nextTarget.Index,
                Entity = nextTarget,
                Amount = chainDamage,
                KnockbackDirection = float3.zero,
                KnockbackDistance = 0f,
                IsCritical = isCritical,
            });

            Entity visualEvent = ecb.CreateEntity();
            ecb.AddComponent(visualEvent, new ChainLightningEvent
            {
                Start = chainStart + new float3(0f, 0.75f, 0f),
                End = chainEnd + new float3(0f, 0.75f, 0f),
            });

            hitList.Add(new PlayerProjectileHit { Entity = nextTarget });
            chainStart = chainEnd;
        }
    }

    private static float DistanceToSegmentSq(float3 point, float3 start, float3 end, out float t)
    {
        float3 segment = end - start;
        float lengthSq = math.lengthsq(segment);
        t = lengthSq > 0f ? math.clamp(math.dot(point - start, segment) / lengthSq, 0f, 1f) : 0f;
        return math.distancesq(point, start + segment * t);
    }
}
