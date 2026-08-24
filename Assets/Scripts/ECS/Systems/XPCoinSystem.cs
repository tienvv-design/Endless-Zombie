using Unity.Burst;
using Unity.Entities;

internal partial struct XPCoinSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer esEcb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach (var mobDeathEvent in SystemAPI.Query<RefRO<MobDeathEvent>>())
        {
            Entity xpCollectedEvent = esEcb.CreateEntity();
            esEcb.AddComponent(xpCollectedEvent, new XPCollectedEvent
            {
                XPAmount = Unity.Mathematics.math.max(1, mobDeathEvent.ValueRO.XPReward),
            });
        }

        // Clean up pickups left from an older play session/domain reload. New
        // kills never instantiate a pickup GameObject/entity anymore.
        foreach (var (_, entity) in SystemAPI.Query<RefRO<XPCoin>>().WithEntityAccess())
            esEcb.DestroyEntity(entity);
    }
}

