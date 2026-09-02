using System;
using Unity.Entities;

public static class WaveSpawnLifecycle
{
    public static event Action StageCompleted;
    private static uint _runSeedSequence;

    public static void StopStage()
    {
        RemoveGameplayStartedTag();
        if (!TryGetStage(out EntityManager manager, out Entity stageEntity)) return;
        StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
        stage.State = StageRuntimeState.Stopped;
        manager.SetComponentData(stageEntity, stage);
        manager.GetBuffer<SpawnRequest>(stageEntity).Clear();
    }

    public static void ResetStage()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        EntityManager manager = world.EntityManager;
        EntityQuery startGate = manager.CreateEntityQuery(typeof(GameplayStartedTag));
        manager.DestroyEntity(startGate);
        startGate.Dispose();
        EntityQuery mobs = manager.CreateEntityQuery(typeof(Mob));
        manager.DestroyEntity(mobs);
        mobs.Dispose();

        if (!TryGetStage(out manager, out Entity stageEntity)) return;
        StageExcelRuntimeConfigLoader.TryApplyCurrentStage(manager, stageEntity);
        StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
        stage.State = StageRuntimeState.NotStarted;
        stage.CurrentWaveIndex = -1;
        stage.NextRequestSequence = 0;
        manager.SetComponentData(stageEntity, stage);
        manager.GetBuffer<SpawnRequest>(stageEntity).Clear();
        DynamicBuffer<WaveRuntime> waves = manager.GetBuffer<WaveRuntime>(stageEntity);
        for (int i = 0; i < waves.Length; i++)
        {
            WaveRuntime wave = waves[i];
            wave.State = WaveRuntimeState.Pending;
            wave.StateElapsedTime = 0f;
            waves[i] = wave;
        }
        DynamicBuffer<SpawnEntryRuntime> entries = manager.GetBuffer<SpawnEntryRuntime>(stageEntity);
        for (int i = 0; i < entries.Length; i++)
        {
            SpawnEntryRuntime entry = entries[i];
            entry.State = entry.EnemyPrefab == Entity.Null
                ? SpawnEntryRuntimeState.Failed
                : entry.Quantity == 0 ? SpawnEntryRuntimeState.Completed : SpawnEntryRuntimeState.Pending;
            entry.EnqueuedCount = 0;
            entry.SpawnedCount = 0;
            entry.NextSpawnTime = entry.SpawnDelay;
            entries[i] = entry;
        }
    }

    public static void BeginStage()
    {
        ResetStage();
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        EntityManager manager = world.EntityManager;
        ReseedSpawnPositions(manager);
        EntityQuery query = manager.CreateEntityQuery(typeof(GameplayStartedTag));
        bool exists = !query.IsEmptyIgnoreFilter;
        query.Dispose();
        if (!exists)
            manager.CreateEntity(typeof(GameplayStartedTag));
    }

    private static void ReseedSpawnPositions(EntityManager manager)
    {
        EntityQuery query = manager.CreateEntityQuery(typeof(SpawnPositionSettings));
        if (query.CalculateEntityCount() == 1)
        {
            Entity settingsEntity = query.GetSingletonEntity();
            SpawnPositionSettings settings = manager.GetComponentData<SpawnPositionSettings>(settingsEntity);
            settings.Random = Unity.Mathematics.Random.CreateFromIndex(CreateRunSeed());
            manager.SetComponentData(settingsEntity, settings);
        }
        query.Dispose();
    }

    private static uint CreateRunSeed()
    {
        ulong ticks = (ulong)DateTime.UtcNow.Ticks;
        uint seed = (uint)ticks ^ (uint)(ticks >> 32) ^ ++_runSeedSequence * 0x9E3779B9u;
        return seed == 0u ? 1u : seed;
    }

    private static void RemoveGameplayStartedTag()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        EntityQuery query = world.EntityManager.CreateEntityQuery(typeof(GameplayStartedTag));
        world.EntityManager.DestroyEntity(query);
        query.Dispose();
    }

    private static bool TryGetStage(out EntityManager manager, out Entity stageEntity)
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            manager = default;
            stageEntity = Entity.Null;
            return false;
        }
        manager = world.EntityManager;
        EntityQuery query = manager.CreateEntityQuery(typeof(StageRuntime));
        bool found = query.CalculateEntityCount() == 1;
        stageEntity = found ? query.GetSingletonEntity() : Entity.Null;
        query.Dispose();
        return found;
    }

    internal static void RaiseStageCompleted()
    {
        MetaProgression.CompleteCurrentStage();
        StageCompleted?.Invoke();
    }
}

[UpdateAfter(typeof(WaveCompletionSystem))]
public partial class StageCompletionBridge : SystemBase
{
    private bool _raised;
    protected override void OnCreate() => RequireForUpdate<StageRuntime>();

    protected override void OnUpdate()
    {
        StageRuntime stage = SystemAPI.GetSingleton<StageRuntime>();
        if (stage.State == StageRuntimeState.Completed && !_raised)
        {
            _raised = true;
            WaveSpawnLifecycle.RaiseStageCompleted();
        }
        else if (stage.State != StageRuntimeState.Completed)
            _raised = false;
    }
}
