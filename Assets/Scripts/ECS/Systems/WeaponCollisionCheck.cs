using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;
using Collider = Unity.Physics.Collider;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct WeaponOverlapSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public unsafe void OnUpdate(ref SystemState state)
    {
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = physicsWorldSingleton.PhysicsWorld.CollisionWorld;
        
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        NativeList<DistanceHit> overlapHits = new NativeList<DistanceHit>(Allocator.Temp);
        
        WeaponManager weaponManager = SystemAPI.GetSingleton<WeaponManager>();

        // Iterate weapons
        foreach (var (weaponTransform, collisionLogs,weaponCollider, weaponEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, DynamicBuffer<CollisionLog>,RefRO<PhysicsCollider>>().WithEntityAccess())
        {
            BlobAssetReference<Collider> collider = weaponCollider.ValueRO.Value;
            if (!collider.IsCreated)
                continue;

            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1 << GameAssets.MOB_LAYER,
                GroupIndex = 0,
            };
            
            Collider* colliderPtr = (Collider*)collider.GetUnsafePtr();
            
            if (colliderPtr->Type != ColliderType.Box) return;
            
            NativeList<int> currentHitIds = new NativeList<int>(Allocator.Temp);
            NativeList<int> exitIds = new NativeList<int>(Allocator.Temp);
            
            BoxCollider* boxColliderPtr = (BoxCollider*)colliderPtr;
            
            overlapHits.Clear();
            
            if (collisionWorld.OverlapBox(weaponTransform.ValueRO.Position, quaternion.identity, 
                    boxColliderPtr->Size / 2, ref overlapHits, filter))
            {
                currentHitIds.Clear();
                for (int i = 0; i < overlapHits.Length; i++)
                    currentHitIds.Add(overlapHits[i].Entity.Index);

                // 1. Determine exits
                for (int i = 0; i < collisionLogs.Length; i++)
                {
                    if (!currentHitIds.Contains(collisionLogs[i].CollidedEntityId))
                        exitIds.Add(i);
                }

                // Remove in reverse order to avoid shifting
                for (int i = exitIds.Length - 1; i >= 0; i--)
                    collisionLogs.RemoveAt(exitIds[i]);

                // 2. Determine new entries
                for (int i = 0; i < overlapHits.Length; i++)
                {
                    bool alreadyColliding = false;
                    for (int j = 0; j < collisionLogs.Length; j++)
                    {
                        if (collisionLogs[j].CollidedEntityId == overlapHits[i].Entity.Index)
                        {
                            alreadyColliding = true;
                            break;
                        }
                    }

                    if (!alreadyColliding)
                    {
                        collisionLogs.Add(new CollisionLog { CollidedEntityId = currentHitIds[i] });
                        Entity e = ecb.CreateEntity();
                        ecb.AddComponent(e, new MobDamageTakenEvent
                        {
                            Id = currentHitIds[i],
                            Entity = overlapHits[i].Entity,
                            Amount = weaponManager.DamagePerHit,
                        });
                        // Debug.Log($"Frame: {Time.frameCount}: Weapon {weaponEntity} hit entity {currentHitIds[i]}");
                    }
                }
            }
            else
            {
                collisionLogs.Clear();
            }
            
            //hits.Dispose();
        }
        
        overlapHits.Dispose();
    }
}
