using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct KamikazeUnitSystem : ISystem
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
        
        foreach (var (localTransform, kamikaze, entity)
                 in SystemAPI.Query<RefRW<LocalTransform>, RefRO<KamikazeUnit>>().WithEntityAccess())
        {
            if (!SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> goInfoBuffer))
                return;

            GameObjectInfo target = default;
            bool targetMatch = false;

            foreach (var goInfo in goInfoBuffer)
            {
                if (kamikaze.ValueRO.TargetObjectType == goInfo.ObjectType)
                {
                    target = goInfo;
                    targetMatch = true;
                    break;
                }
            }
            
            if(!targetMatch) continue;
            
            float3 direction = target.Position - localTransform.ValueRO.Position;
            float distanceSq = math.lengthsq(direction);

            if (distanceSq <= kamikaze.ValueRO.HitDistanceSq)
            {
                Entity e = ecb.CreateEntity();
                ecb.AddComponent(e, new MobDamageGivenEvent
                {
                    Id = target.ID,
                    Amount = kamikaze.ValueRO.Damage,
                });

                ecb.DestroyEntity(entity);
            }
            
        }
    }
}
    