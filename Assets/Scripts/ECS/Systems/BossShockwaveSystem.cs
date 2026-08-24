using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BossShockwaveSystem : ISystem
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
        if (!SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> objects)) return;
        GameObjectInfo player = default;
        bool found = false;
        foreach (GameObjectInfo info in objects)
            if (info.ObjectType == GameObjectType.Character1) { player = info; found = true; break; }
        if (!found) return;

        float dt = SystemAPI.Time.DeltaTime;
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        foreach (var (transform, shockwave, phase) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRW<BossShockwave>, RefRO<BossPhase>>())
        {
            shockwave.ValueRW.Timer -= dt;
            if (shockwave.ValueRO.Timer > 0f) continue;
            if (shockwave.ValueRO.IsWarning == 0)
            {
                shockwave.ValueRW.IsWarning = 1;
                shockwave.ValueRW.Timer = shockwave.ValueRO.WarningDuration;
                continue;
            }
            float phaseRadius = shockwave.ValueRO.Radius;
            if (math.distancesq(transform.ValueRO.Position.xz, player.Position.xz) <= phaseRadius * phaseRadius)
            {
                Entity hit = ecb.CreateEntity();
                ecb.AddComponent(hit, new MobDamageGivenEvent
                {
                    Id = player.ID,
                    Amount = math.max(1, (int)math.ceil(shockwave.ValueRO.Damage *
                        (1f + (phase.ValueRO.CurrentPhase - 1) * 0.35f))),
                });
            }
            shockwave.ValueRW.IsWarning = 0;
            shockwave.ValueRW.Timer = shockwave.ValueRO.Cooldown / (1f + (phase.ValueRO.CurrentPhase - 1) * 0.2f);
        }
    }
}
