using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateAfter(typeof(SpawnEntrySchedulerSystem))]
public partial struct WaveCompletionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StageRuntime>();
        state.RequireForUpdate<CombatMetrics>();
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
        int aliveEnemies = SystemAPI.GetSingleton<CombatMetrics>().ActiveEnemies;
        int waveIndex = stage.ValueRO.CurrentWaveIndex;
        if (waveIndex < 0 || waveIndex >= waves.Length)
            return;

        WaveRuntime wave = waves[waveIndex];
        if (waveIndex == waves.Length - 1 && wave.State == WaveRuntimeState.Completed &&
            requests.IsEmpty && aliveEnemies == 0)
        {
            stage.ValueRW.State = StageRuntimeState.Completed;
            return;
        }
        if (wave.State != WaveRuntimeState.Active)
            return;

        bool entriesTerminal = true;
        int end = wave.FirstSpawnEntryIndex + wave.SpawnEntryCount;
        for (int i = wave.FirstSpawnEntryIndex; i < end; i++)
        {
            SpawnEntryRuntimeState entryState = entries[i].State;
            if (entryState != SpawnEntryRuntimeState.Completed &&
                entryState != SpawnEntryRuntimeState.Failed)
            {
                entriesTerminal = false;
                break;
            }
        }

        bool waveHasQueuedRequest = false;
        for (int i = 0; i < requests.Length; i++)
        {
            if (requests[i].WaveIndex == waveIndex)
            {
                waveHasQueuedRequest = true;
                break;
            }
        }

        if (!entriesTerminal || waveHasQueuedRequest || aliveEnemies > wave.CompletionThreshold)
            return;

        wave.State = WaveRuntimeState.Completed;
        waves[waveIndex] = wave;

        int nextWaveIndex = waveIndex + 1;
        if (nextWaveIndex < waves.Length)
        {
            stage.ValueRW.CurrentWaveIndex = nextWaveIndex;
            WaveRuntime next = waves[nextWaveIndex];
            next.State = WaveRuntimeState.Delay;
            next.StateElapsedTime = 0f;
            waves[nextWaveIndex] = next;
        }
        else if (requests.IsEmpty && aliveEnemies == 0)
        {
            stage.ValueRW.State = StageRuntimeState.Completed;
        }
    }
}
