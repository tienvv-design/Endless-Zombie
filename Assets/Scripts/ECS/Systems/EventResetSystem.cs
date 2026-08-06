using Unity.Burst;
using Unity.Entities;
using UnityEngine.EventSystems;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
internal partial struct EventResetSystem : ISystem
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
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach (var (damageTakenEvent, entity) in SystemAPI.Query<RefRW<MobDamageTakenEvent>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }
        
        foreach (var (digitExplosionEvent, entity) in SystemAPI.Query<RefRW<DigitExplosionEvent>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }
        
        foreach (var (deathEvent, entity) in SystemAPI.Query<RefRW<MobDeathEvent>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }
        
        foreach (var (xpCollectedEvent, entity) in SystemAPI.Query<RefRW<XPCollectedEvent>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }
        
        foreach (var (mobDamageGiven, entity) in SystemAPI.Query<RefRW<MobDamageGivenEvent>>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }
    }
}
    