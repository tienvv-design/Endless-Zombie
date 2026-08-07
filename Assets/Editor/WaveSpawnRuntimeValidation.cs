using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WaveSpawnRuntimeValidation
{
    private const double DurationSeconds = 45.0;
    private static readonly string ProjectRoot = Directory.GetParent(Application.dataPath).FullName;
    private static readonly string RequestPath = Path.Combine(ProjectRoot, "outputs", "wave-spawn-roadmap", "run-runtime-tests.request");
    private static readonly string ResultPath = Path.Combine(ProjectRoot, "outputs", "wave-spawn-roadmap", "runtime-results.txt");

    private static double _startedAt;
    private static double _lastSampleAt;
    private static bool _observedStage;
    private static bool _observedEnemies;
    private static bool _fifoValid = true;
    private static bool _maxAliveValid = true;
    private static int _peakAlive;
    private static int _peakQueue;
    private static double _maxEditorFrameMs;
    private static bool _sampling;

    static WaveSpawnRuntimeValidation()
    {
        if (!File.Exists(RequestPath))
            return;

        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying)
                StartSampling();
            else if (!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPlaying = true;
        };
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode)
            return;

        StartSampling();
    }

    private static void StartSampling()
    {
        if (_sampling)
            return;

        _sampling = true;
        _startedAt = EditorApplication.timeSinceStartup;
        _lastSampleAt = _startedAt;
        EditorApplication.update += Sample;
    }

    private static void Sample()
    {
        double now = EditorApplication.timeSinceStartup;
        _maxEditorFrameMs = System.Math.Max(_maxEditorFrameMs, (now - _lastSampleAt) * 1000.0);
        _lastSampleAt = now;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            EntityManager manager = world.EntityManager;
            using EntityQuery stageQuery = manager.CreateEntityQuery(
                ComponentType.ReadOnly<StageRuntime>(),
                ComponentType.ReadOnly<WaveRuntime>(),
                ComponentType.ReadOnly<SpawnRequest>());
            using EntityQuery metricsQuery = manager.CreateEntityQuery(ComponentType.ReadOnly<CombatMetrics>());

            if (stageQuery.CalculateEntityCount() == 1 && metricsQuery.CalculateEntityCount() == 1)
            {
                _observedStage = true;
                Entity stageEntity = stageQuery.GetSingletonEntity();
                StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
                CombatMetrics metrics = metricsQuery.GetSingleton<CombatMetrics>();
                DynamicBuffer<WaveRuntime> waves = manager.GetBuffer<WaveRuntime>(stageEntity, true);
                DynamicBuffer<SpawnRequest> requests = manager.GetBuffer<SpawnRequest>(stageEntity, true);

                _peakAlive = Mathf.Max(_peakAlive, metrics.ActiveEnemies);
                _peakQueue = Mathf.Max(_peakQueue, requests.Length);
                _observedEnemies |= metrics.ActiveEnemies > 0;

                for (int i = 1; i < requests.Length; i++)
                    _fifoValid &= requests[i - 1].Sequence < requests[i].Sequence;

                if (stage.CurrentWaveIndex >= 0 && stage.CurrentWaveIndex < waves.Length)
                    _maxAliveValid &= metrics.ActiveEnemies <= waves[stage.CurrentWaveIndex].MaxAliveEnemies;

                if (stage.State == StageRuntimeState.Completed && _observedEnemies)
                {
                    Finish("stage-completed");
                    return;
                }
            }
        }

        if (now - _startedAt >= DurationSeconds)
            Finish("time-budget-completed");
    }

    private static void Finish(string reason)
    {
        EditorApplication.update -= Sample;
        _sampling = false;
        File.Delete(RequestPath);
        bool passed = _observedStage && _observedEnemies && _fifoValid && _maxAliveValid;
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath,
            $"state={(passed ? "Passed" : "Failed")}\n" +
            $"reason={reason}\n" +
            $"observedStage={_observedStage}\n" +
            $"observedEnemies={_observedEnemies}\n" +
            $"fifoValid={_fifoValid}\n" +
            $"maxAliveValid={_maxAliveValid}\n" +
            $"peakAlive={_peakAlive}\n" +
            $"peakQueue={_peakQueue}\n" +
            $"maxEditorFrameMs={_maxEditorFrameMs:0.000}\n");
        Debug.Log($"[WaveSpawnRuntimeValidation] {(passed ? "PASSED" : "FAILED")}. Peak alive={_peakAlive}, peak queue={_peakQueue}.");
        EditorApplication.isPlaying = false;
    }
}
