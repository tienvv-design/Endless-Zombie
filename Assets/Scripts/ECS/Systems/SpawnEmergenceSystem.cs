using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateBefore(typeof(UnitMoverSystem))]
public partial struct SpawnEmergenceSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) =>
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (emergence, entity) in SystemAPI.Query<RefRW<SpawnEmergence>>().WithEntityAccess())
        {
            emergence.ValueRW.Elapsed += dt;
            if (emergence.ValueRO.Elapsed < emergence.ValueRO.Duration) continue;
            if (SystemAPI.HasComponent<UnitMover>(entity))
                SystemAPI.SetComponentEnabled<UnitMover>(entity, true);
            ecb.RemoveComponent<SpawnEmergence>(entity);
        }
    }
}
