using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateAfter(typeof(WaveProgressionSystem))]
[UpdateBefore(typeof(WaveCompletionSystem))]
public partial struct SpawnEntrySchedulerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StageRuntime>();
        state.RequireForUpdate<GameRunningTag>();
        state.RequireForUpdate<GameplayStartedTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RefRW<StageRuntime> stage = SystemAPI.GetSingletonRW<StageRuntime>();
        if (stage.ValueRO.State != StageRuntimeState.Running)
            return;

        DynamicBuffer<WaveRuntime> waves = SystemAPI.GetSingletonBuffer<WaveRuntime>();
        DynamicBuffer<SpawnEntryRuntime> entries = SystemAPI.GetSingletonBuffer<SpawnEntryRuntime>();
        DynamicBuffer<SpawnRequest> requests = SystemAPI.GetSingletonBuffer<SpawnRequest>();
        int waveIndex = stage.ValueRO.CurrentWaveIndex;
        if (waveIndex < 0 || waveIndex >= waves.Length)
            return;

        WaveRuntime wave = waves[waveIndex];
        if (wave.State != WaveRuntimeState.Active)
            return;

        int end = wave.FirstSpawnEntryIndex + wave.SpawnEntryCount;
        for (int entryIndex = wave.FirstSpawnEntryIndex; entryIndex < end; entryIndex++)
        {
            SpawnEntryRuntime entry = entries[entryIndex];
            if (entry.State == SpawnEntryRuntimeState.Completed ||
                entry.State == SpawnEntryRuntimeState.Failed)
                continue;

            if (entry.State == SpawnEntryRuntimeState.Pending &&
                wave.StateElapsedTime >= entry.SpawnDelay)
                entry.State = SpawnEntryRuntimeState.Active;

            if (entry.State != SpawnEntryRuntimeState.Active)
            {
                entries[entryIndex] = entry;
                continue;
            }

            if (entry.SpawnInterval <= 0f)
            {
                while (entry.EnqueuedCount < entry.Quantity)
                    Enqueue(ref stage.ValueRW, requests, ref entry, entryIndex);
            }
            else
            {
                while (entry.EnqueuedCount < entry.Quantity &&
                       wave.StateElapsedTime >= entry.NextSpawnTime)
                {
                    Enqueue(ref stage.ValueRW, requests, ref entry, entryIndex);
                    entry.NextSpawnTime += math.max(0.001f, entry.SpawnInterval);
                }
            }

            entries[entryIndex] = entry;
        }
    }

    private static void Enqueue(
        ref StageRuntime stage,
        DynamicBuffer<SpawnRequest> requests,
        ref SpawnEntryRuntime entry,
        int entryIndex)
    {
        requests.Add(new SpawnRequest
        {
            Sequence = stage.NextRequestSequence++,
            WaveIndex = entry.WaveIndex,
            SpawnEntryIndex = entryIndex,
            EnemyPrefab = entry.EnemyPrefab,
            EnemyType = entry.EnemyType,
            VisualKind = entry.VisualKind,
            HealthMultiplier = entry.HealthMultiplier,
            DamageMultiplier = entry.DamageMultiplier,
            Scale = entry.Scale,
            XPReward = entry.XPReward,
            GoldReward = entry.GoldReward,
            SpawnArenaGroupId = entry.SpawnArenaGroupId,
        });
        entry.EnqueuedCount++;
    }
}
