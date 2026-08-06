using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[UpdateAfter(typeof(MobHealthManager))]
internal partial struct CombatMetricsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CombatMetrics>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RefRW<CombatMetrics> metrics = SystemAPI.GetSingletonRW<CombatMetrics>();
        metrics.ValueRW.WindowElapsed += SystemAPI.Time.DeltaTime;
        if (metrics.ValueRO.WindowElapsed >= 1f)
        {
            metrics.ValueRW.RecentDps = metrics.ValueRO.WindowDamage /
                                        math.max(0.001f, metrics.ValueRO.WindowElapsed);
            metrics.ValueRW.WindowDamage = 0;
            metrics.ValueRW.WindowElapsed = 0f;
        }

        float3 playerPosition = float3.zero;
        bool playerFound = false;
        if (SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> objects))
        {
            foreach (GameObjectInfo objectInfo in objects)
            {
                if (objectInfo.ObjectType != GameObjectType.Character1)
                    continue;
                playerPosition = objectInfo.Position;
                playerFound = true;
                break;
            }
        }

        int activeEnemies = 0;
        int nearbyEnemies = 0;
        float pressure = 0f;
        foreach (var (mob, transform) in SystemAPI.Query<RefRO<Mob>, RefRO<LocalTransform>>())
        {
            if (mob.ValueRO.Health <= 0)
                continue;

            activeEnemies++;
            if (!playerFound)
                continue;

            float distance = math.distance(playerPosition, transform.ValueRO.Position);
            if (distance <= 8f)
            {
                nearbyEnemies++;
                float typeWeight = mob.ValueRO.EnemyType == EnemyType.Boss
                    ? 5f
                    : mob.ValueRO.EnemyType == EnemyType.Elite ? 2f : 1f;
                pressure += typeWeight * math.saturate(1f - distance / 8f);
            }
        }

        metrics.ValueRW.ActiveEnemies = activeEnemies;
        metrics.ValueRW.NearbyEnemies = nearbyEnemies;
        metrics.ValueRW.Pressure = pressure;
    }
}
