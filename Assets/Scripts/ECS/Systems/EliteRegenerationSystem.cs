using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateBefore(typeof(MobHealthManager))]
public partial struct EliteRegenerationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state) => state.RequireForUpdate<GameRunningTag>();

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (mob, modifier) in SystemAPI.Query<RefRW<Mob>, RefRW<EliteModifier>>())
        {
            if (modifier.ValueRO.HealthRegenerationPerSecond <= 0f || mob.ValueRO.Health <= 0 ||
                mob.ValueRO.Health >= mob.ValueRO.MaxHealth) continue;
            modifier.ValueRW.RegenerationAccumulator += modifier.ValueRO.HealthRegenerationPerSecond * deltaTime;
            int healing = (int)math.floor(modifier.ValueRO.RegenerationAccumulator);
            if (healing <= 0) continue;
            mob.ValueRW.Health = math.min(mob.ValueRO.MaxHealth, mob.ValueRO.Health + healing);
            modifier.ValueRW.RegenerationAccumulator -= healing;
        }
    }
}
