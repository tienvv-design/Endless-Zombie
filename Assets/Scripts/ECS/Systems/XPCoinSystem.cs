using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal partial struct XPCoinSystem : ISystem
{

    public const float XP_COLLECT_DISTANCE_SQ = UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer xpSpawnEcb = new EntityCommandBuffer(Allocator.Temp);
        EntityCommandBuffer esEcb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        SystemAPI.TryGetSingleton(out EntityReferences entityReferences);
        
        foreach (var mobDeathEvent in SystemAPI.Query<RefRO<MobDeathEvent>>())
        {
            // Debug.Log("Mob is dead alright");
            Entity xpCollectible = xpSpawnEcb.Instantiate(entityReferences.XPCollectable);

            LocalTransform localTransform = SystemAPI.GetComponent<LocalTransform>(entityReferences.XPCollectable);
            localTransform.Position = mobDeathEvent.ValueRO.LocalTransform.Position;
            xpSpawnEcb.SetComponent(xpCollectible, localTransform);
        }
        
        xpSpawnEcb.Playback(state.EntityManager);
        
        SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> goInfoBuffer);
        
        foreach (var (localTransform, xpCoin, unitMover, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRW<XPCoin>, RefRW<UnitMover>>().WithEntityAccess()
                     .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
        {
            // Find the target object and get the necessary info.
            for (int i = 0; i < goInfoBuffer.Length; i++)
            {
                if (xpCoin.ValueRO.TargetType == goInfoBuffer[i].ObjectType)
                {
                    xpCoin.ValueRW.TargetPosition = goInfoBuffer[i].Position;
                    unitMover.ValueRW.targetPosition = xpCoin.ValueRO.TargetPosition;
                }
            }

            // Set UnitMover component enabled if the target has gotten within range. 
            if (math.distancesq(localTransform.ValueRO.Position, xpCoin.ValueRO.TargetPosition) <=
                xpCoin.ValueRO.MagnetPullDistanceSq)
            {
                if (!SystemAPI.IsComponentEnabled<UnitMover>(entity))
                {
                    SystemAPI.SetComponentEnabled<UnitMover>(entity, true);   
                }
            }

            // If the coin is close enough, create an event and destroy the entity.
            if (math.distancesq(localTransform.ValueRO.Position, xpCoin.ValueRO.TargetPosition) <=
                XP_COLLECT_DISTANCE_SQ)
            {
                Entity xpCollectedEvent = esEcb.CreateEntity();
                esEcb.AddComponent(xpCollectedEvent, new XPCollectedEvent
                {
                    XPAmount = xpCoin.ValueRO.XPAmount,
                });
                
                esEcb.DestroyEntity(entity);
            }
        }
    }
}

