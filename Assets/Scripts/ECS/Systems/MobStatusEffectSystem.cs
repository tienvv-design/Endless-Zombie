using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateBefore(typeof(MobHealthManager))]
internal partial struct MobStatusEffectSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameRunningTag>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        EntityCommandBuffer ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (status, mover, entity) in
                 SystemAPI.Query<RefRW<MobStatusEffects>, RefRW<UnitMover>>().WithEntityAccess())
        {
            if (status.ValueRO.BurnRemaining > 0f)
            {
                status.ValueRW.BurnRemaining = math.max(0f, status.ValueRO.BurnRemaining - deltaTime);
                status.ValueRW.BurnTickTimer -= deltaTime;
                if (status.ValueRO.BurnTickTimer <= 0f)
                {
                    Entity damageEvent = ecb.CreateEntity();
                    ecb.AddComponent(damageEvent, new MobDamageTakenEvent
                    {
                        Id = entity.Index,
                        Entity = entity,
                        Amount = math.max(1, status.ValueRO.BurnDamagePerTick),
                        Element = ElementType.None,
                    });
                    status.ValueRW.BurnTickTimer = 1f;
                }
            }
            else
            {
                status.ValueRW.BurnTickTimer = 0f;
                status.ValueRW.BurnDamagePerTick = 0;
            }

            if (status.ValueRO.FrostRemaining > 0f)
            {
                status.ValueRW.FrostRemaining = math.max(0f, status.ValueRO.FrostRemaining - deltaTime);
                if (!status.ValueRO.FrostApplied)
                {
                    status.ValueRW.OriginalMoveSpeed = mover.ValueRO.moveSpeed;
                    mover.ValueRW.moveSpeed *= math.clamp(status.ValueRO.FrostSpeedMultiplier, 0.1f, 1f);
                    status.ValueRW.FrostApplied = true;
                }
            }
            else if (status.ValueRO.FrostApplied)
            {
                mover.ValueRW.moveSpeed = status.ValueRO.OriginalMoveSpeed;
                status.ValueRW.FrostApplied = false;
                status.ValueRW.OriginalMoveSpeed = 0f;
            }
        }
    }
}
