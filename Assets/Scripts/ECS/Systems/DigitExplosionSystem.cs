
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

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
                    Entity mobDamageEventEntity = ecb.CreateEntity();
                    
                    ecb.AddComponent(mobDamageEventEntity, new MobDamageTakenEvent
                    {
                        Entity = hit.Entity,
                        Id = hit.Entity.Index,
                        Amount = explosionEvent.ValueRO.Damage,
                    });
                }
            }

        }
    }
}
