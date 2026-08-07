#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class WaveSpawnConfigTests
{
    // Kept in the Editor assembly so Unity can validate authoring data without entering Play Mode.
    [Test]
    public void StageWithoutWaves_IsRejected()
    {
        StageConfig stage = ScriptableObject.CreateInstance<StageConfig>();
        stage.StageId = "Empty";
        EnemyCatalog catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
        Assert.That(WaveSpawnConfigValidator.IsValid(stage, catalog), Is.False);
        Object.DestroyImmediate(stage);
        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void FirstWaveMustUseStageStarted()
    {
        StageConfig stage = ScriptableObject.CreateInstance<StageConfig>();
        WaveDefinition wave = new();
        EnemyCatalog catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
        stage.StageId = "Stage";
        stage.Waves = new[] { wave };
        wave.WaveId = "Wave";
        wave.ActivationCondition = WaveActivationCondition.PreviousWaveCompleted;
        Assert.That(WaveSpawnConfigValidator.IsValid(stage, catalog), Is.False);
        Object.DestroyImmediate(stage);
        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void QuantityZero_IsValidAndTerminal()
    {
        StageConfig stage = ScriptableObject.CreateInstance<StageConfig>();
        WaveDefinition wave = new();
        SpawnEntryDefinition entry = new();
        EnemyCatalog catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
        var prefab = new GameObject("EnemyPrefab");
        stage.StageId = "Stage";
        stage.Waves = new[] { wave };
        wave.WaveId = "Wave";
        wave.ActivationCondition = WaveActivationCondition.StageStarted;
        wave.SpawnEntries = new[] { entry };
        entry.EnemyId = "Enemy";
        entry.Quantity = 0;
        entry.SpawnArenaGroupId = "Outer";
        catalog.Enemies = new[] { new EnemyCatalogEntry { EnemyId = "Enemy", Prefab = prefab } };
        Assert.That(WaveSpawnConfigValidator.IsValid(stage, catalog), Is.True);
        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(stage);
        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void MaxAliveBudget_NeverExceedsCapacity()
    {
        Assert.That(WaveSpawnRuntimeRules.CalculateSpawnBudget(20, 7), Is.EqualTo(13));
        Assert.That(WaveSpawnRuntimeRules.CalculateSpawnBudget(20, 20), Is.Zero);
        Assert.That(WaveSpawnRuntimeRules.CalculateSpawnBudget(20, 25), Is.Zero);
    }

    [Test]
    public void Scheduler_PausesWithoutRunningTag_ThenQueuesEntriesInFifoOrder()
    {
        using var world = new World("WaveSpawnSchedulerTest");
        EntityManager manager = world.EntityManager;
        Entity stageEntity = CreateRuntimeStage(manager, entryCount: 2, quantityPerEntry: 3);
        SystemHandle scheduler = world.CreateSystem<SpawnEntrySchedulerSystem>();
        SimulationSystemGroup simulation = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
        simulation.AddSystemToUpdateList(scheduler);

        simulation.Update();
        Assert.That(manager.GetBuffer<SpawnRequest>(stageEntity).Length, Is.Zero);

        Entity gate = manager.CreateEntity();
        manager.AddComponent<GameRunningTag>(gate);
        simulation.Update();

        DynamicBuffer<SpawnRequest> requests = manager.GetBuffer<SpawnRequest>(stageEntity);
        Assert.That(requests.Length, Is.EqualTo(6));
        for (int i = 0; i < requests.Length; i++)
            Assert.That(requests[i].Sequence, Is.EqualTo(i));
        Assert.That(requests[0].SpawnEntryIndex, Is.EqualTo(0));
        Assert.That(requests[3].SpawnEntryIndex, Is.EqualTo(1));
    }

    [Test]
    public void Scheduler_StressQueuesTenThousandRequestsWithinTwoSeconds()
    {
        using var world = new World("WaveSpawnStressTest");
        EntityManager manager = world.EntityManager;
        Entity stageEntity = CreateRuntimeStage(manager, entryCount: 100, quantityPerEntry: 100);
        manager.AddComponent<GameRunningTag>(manager.CreateEntity());
        SystemHandle scheduler = world.CreateSystem<SpawnEntrySchedulerSystem>();
        SimulationSystemGroup simulation = world.GetOrCreateSystemManaged<SimulationSystemGroup>();
        simulation.AddSystemToUpdateList(scheduler);
        var timer = Stopwatch.StartNew();

        simulation.Update();
        timer.Stop();

        DynamicBuffer<SpawnRequest> requests = manager.GetBuffer<SpawnRequest>(stageEntity);
        Assert.That(requests.Length, Is.EqualTo(10_000));
        Assert.That(requests[0].Sequence, Is.Zero);
        Assert.That(requests[9_999].Sequence, Is.EqualTo(9_999));
        Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(2.0));
    }

    [TestCase(-1, 0f, 0f, "Outer")]
    [TestCase(1, -1f, 0f, "Outer")]
    [TestCase(1, 0f, -1f, "Outer")]
    [TestCase(1, 0f, 0f, "")]
    public void InvalidEntryFields_AreRejected(int quantity, float delay, float interval, string arena)
    {
        CreateValidConfig(out StageConfig stage, out WaveDefinition wave, out SpawnEntryDefinition entry, out EnemyCatalog catalog, out GameObject prefab);
        entry.Quantity = quantity;
        entry.SpawnDelay = delay;
        entry.SpawnInterval = interval;
        entry.SpawnArenaGroupId = arena;
        Assert.That(WaveSpawnConfigValidator.IsValid(stage, catalog), Is.False);
        DestroyConfig(stage, catalog, prefab);
    }

    [Test]
    public void UnknownEnemy_IsRejectedAsTerminalConfigurationFailure()
    {
        CreateValidConfig(out StageConfig stage, out WaveDefinition wave, out SpawnEntryDefinition entry, out EnemyCatalog catalog, out GameObject prefab);
        entry.EnemyId = "Missing";
        Assert.That(WaveSpawnConfigValidator.IsValid(stage, catalog), Is.False);
        DestroyConfig(stage, catalog, prefab);
    }

    private static Entity CreateRuntimeStage(EntityManager manager, int entryCount, int quantityPerEntry)
    {
        Entity entity = manager.CreateEntity();
        manager.AddComponentData(entity, new StageRuntime
        {
            State = StageRuntimeState.Running,
            CurrentWaveIndex = 0,
        });
        DynamicBuffer<WaveRuntime> waves = manager.AddBuffer<WaveRuntime>(entity);
        waves.Add(new WaveRuntime
        {
            State = WaveRuntimeState.Active,
            FirstSpawnEntryIndex = 0,
            SpawnEntryCount = entryCount,
        });
        DynamicBuffer<SpawnEntryRuntime> entries = manager.AddBuffer<SpawnEntryRuntime>(entity);
        for (int i = 0; i < entryCount; i++)
        {
            entries.Add(new SpawnEntryRuntime
            {
                State = SpawnEntryRuntimeState.Active,
                WaveIndex = 0,
                Quantity = quantityPerEntry,
                SpawnInterval = 0f,
            });
        }
        manager.AddBuffer<SpawnRequest>(entity);
        return entity;
    }

    private static void CreateValidConfig(out StageConfig stage, out WaveDefinition wave, out SpawnEntryDefinition entry, out EnemyCatalog catalog, out GameObject prefab)
    {
        stage = ScriptableObject.CreateInstance<StageConfig>();
        wave = new WaveDefinition();
        entry = new SpawnEntryDefinition();
        catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
        prefab = new GameObject("EnemyPrefab");
        stage.StageId = "Stage";
        stage.MaxAliveEnemies = 20;
        stage.Waves = new[] { wave };
        wave.WaveId = "Wave";
        wave.ActivationCondition = WaveActivationCondition.StageStarted;
        wave.SpawnEntries = new[] { entry };
        entry.EnemyId = "Enemy";
        entry.Quantity = 1;
        entry.SpawnArenaGroupId = "Outer";
        catalog.Enemies = new[] { new EnemyCatalogEntry { EnemyId = "Enemy", Prefab = prefab } };
    }

    private static void DestroyConfig(StageConfig stage, EnemyCatalog catalog, GameObject prefab)
    {
        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(stage);
        Object.DestroyImmediate(catalog);
    }
}
#endif
