using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateBefore(typeof(SpawnEntrySchedulerSystem))]
public partial struct WaveProgressionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<StageRuntime>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RefRW<StageRuntime> stage = SystemAPI.GetSingletonRW<StageRuntime>();
        DynamicBuffer<WaveRuntime> waves = SystemAPI.GetSingletonBuffer<WaveRuntime>();

        if (stage.ValueRO.State == StageRuntimeState.NotStarted)
        {
            if (waves.IsEmpty)
            {
                stage.ValueRW.State = StageRuntimeState.Stopped;
                return;
            }

            stage.ValueRW.State = StageRuntimeState.Running;
            stage.ValueRW.CurrentWaveIndex = 0;
            WaveRuntime firstWave = waves[0];
            firstWave.State = WaveRuntimeState.Delay;
            firstWave.StateElapsedTime = 0f;
            waves[0] = firstWave;
        }

        if (stage.ValueRO.State != StageRuntimeState.Running)
            return;

        int waveIndex = stage.ValueRO.CurrentWaveIndex;
        if (waveIndex < 0 || waveIndex >= waves.Length)
            return;

        WaveRuntime wave = waves[waveIndex];
        if (wave.State == WaveRuntimeState.Delay)
        {
            wave.StateElapsedTime += SystemAPI.Time.DeltaTime;
            if (wave.StateElapsedTime >= wave.WaveDelay)
            {
                wave.State = WaveRuntimeState.Active;
                wave.StateElapsedTime = 0f;
            }
            waves[waveIndex] = wave;
        }
        else if (wave.State == WaveRuntimeState.Active)
        {
            wave.StateElapsedTime += SystemAPI.Time.DeltaTime;
            waves[waveIndex] = wave;
        }
    }
}
