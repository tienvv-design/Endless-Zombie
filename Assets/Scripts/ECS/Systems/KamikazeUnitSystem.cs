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
        
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (localTransform, kamikaze, entity)
                 in SystemAPI.Query<RefRO<LocalTransform>, RefRW<KamikazeUnit>>().WithEntityAccess())
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
                if (SystemAPI.HasComponent<UnitMover>(entity) && SystemAPI.IsComponentEnabled<UnitMover>(entity))
                    SystemAPI.SetComponentEnabled<UnitMover>(entity, false);

                kamikaze.ValueRW.AttackTimer -= deltaTime;
                if (kamikaze.ValueRO.AttackTimer <= 0f)
                {
                    Entity attackEvent = ecb.CreateEntity();
                    ecb.AddComponent(attackEvent, new MobDamageGivenEvent
                    {
                        Id = target.ID,
                        Amount = kamikaze.ValueRO.Damage,
                    });
                    kamikaze.ValueRW.AttackTimer = math.max(0.05f, kamikaze.ValueRO.AttackInterval);
                }
            }
            else
            {
                if (SystemAPI.HasComponent<UnitMover>(entity) && !SystemAPI.IsComponentEnabled<UnitMover>(entity))
                    SystemAPI.SetComponentEnabled<UnitMover>(entity, true);
            }
            
        }
    }
}
