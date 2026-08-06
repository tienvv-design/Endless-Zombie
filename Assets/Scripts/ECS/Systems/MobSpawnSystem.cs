using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MobSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MobSpawnSettings>();
        state.RequireForUpdate<EntityReferences>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> goInfoBuffer))
            return;

        GameObjectInfo target = default;
        bool targetMatch = false;

        foreach (var goInfo in goInfoBuffer)
        {
            if (goInfo.ObjectType == GameObjectType.Character1)
            {
                target = goInfo;
                targetMatch = true;
                break;
            }
        }
        
        if (!targetMatch) return;
        
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Singletons
        var spawner    = SystemAPI.GetSingleton<MobSpawnSettings>();
        var references = SystemAPI.GetSingleton<EntityReferences>();

        // Time-driven difficulty
        spawner.ElapsedTime += deltaTime;

        float spawnRate = spawner.BaseSpawnRate + (spawner.ElapsedTime * spawner.RateIncrease);
        float interval  = 1f / math.max(spawnRate, 0.25f);

        spawner.Timer += deltaTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        while (spawner.Timer >= interval)
        {
            spawner.Timer -= interval;

            // Update random
            references.Random = Unity.Mathematics.Random.CreateFromIndex(
                references.Random.NextUInt()
            );

            // 50/50 mob type
            bool typeA = references.Random.NextBool();

            Entity prefab = typeA
                ? references.MobPrefabEntity
                : references.MobOrbitingPrefabEntity;

            float angle = references.Random.NextFloat(0, math.PI * 2f);

            float3 spawnPos = target.Position + new float3(
                math.cos(angle) * spawner.SpawnRadius,
                0f,
                math.sin(angle) * spawner.SpawnRadius
            );

            Entity mob = ecb.Instantiate(prefab);

            ecb.SetComponent(mob, new LocalTransform
            {
                Position = spawnPos,
                Rotation = quaternion.identity,
                Scale    = 1f
            });
        }

        // Write back the updated random + timers
        SystemAPI.SetSingleton(references);
        SystemAPI.SetSingleton(spawner);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
