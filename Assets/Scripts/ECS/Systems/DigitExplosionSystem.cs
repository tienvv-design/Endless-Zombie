
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Transforms;

internal partial struct DigitExplosionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<GameRunningTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = physicsWorldSingleton.PhysicsWorld.CollisionWorld;
        
        NativeList<DistanceHit> overlapHits = new NativeList<DistanceHit>(Allocator.Temp);
        
        foreach (var explosionEvent in SystemAPI.Query<RefRO<DigitExplosionEvent>>())
        {
            overlapHits.Clear();

            CollisionFilter colFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1 << GameAssets.MOB_LAYER,
                GroupIndex = 0,
            };

            if (collisionWorld.OverlapSphere(explosionEvent.ValueRO.Position, explosionEvent.ValueRO.Radius,
                    ref overlapHits, colFilter))
            {
                foreach (DistanceHit hit in overlapHits)
                {
                    float3 knockbackDirection = float3.zero;
                    if (SystemAPI.HasComponent<LocalTransform>(hit.Entity))
                    {
                        float3 mobPosition = SystemAPI.GetComponent<LocalTransform>(hit.Entity).Position;
                        knockbackDirection = math.normalizesafe(mobPosition - explosionEvent.ValueRO.Position);
                    }

                    Entity mobDamageEventEntity = ecb.CreateEntity();
                    
                    ecb.AddComponent(mobDamageEventEntity, new MobDamageTakenEvent
                    {
                        Entity = hit.Entity,
                        Id = hit.Entity.Index,
                        Amount = explosionEvent.ValueRO.Damage,
                        KnockbackDirection = knockbackDirection,
                        KnockbackDistance = explosionEvent.ValueRO.Knockback,
                        IsCritical = explosionEvent.ValueRO.IsCritical,
                    });
                }
            }

        }
    }
}
