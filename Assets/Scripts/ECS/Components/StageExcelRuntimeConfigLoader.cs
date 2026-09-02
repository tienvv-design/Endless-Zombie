using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public static class StageExcelRuntimeConfigLoader
{
    private const string ResourceFolder = "StageConfigs";

    public static bool TryApplyCurrentStage(EntityManager manager, Entity stageEntity)
    {
        if (!manager.HasBuffer<EnemyCatalogRuntime>(stageEntity))
            return false;

        int stageNumber = StageMapProgression.CurrentStage;
        TextAsset source = Resources.Load<TextAsset>($"{ResourceFolder}/Stage_{stageNumber:00}");
        if (source == null)
        {
            Debug.LogWarning($"No Excel stage config found for stage {stageNumber}; using baked StageConfig fallback.");
            return false;
        }

        RuntimeStageConfig config;
        try
        {
            config = JsonUtility.FromJson<RuntimeStageConfig>(source.text);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not parse {source.name}: {exception.Message}");
            return false;
        }

        if (config == null || config.waves == null || config.waves.Length != 20)
        {
            Debug.LogError($"{source.name} must contain exactly 20 waves.");
            return false;
        }

        DynamicBuffer<EnemyCatalogRuntime> catalog = manager.GetBuffer<EnemyCatalogRuntime>(stageEntity);
        DynamicBuffer<WaveRuntime> waves = manager.GetBuffer<WaveRuntime>(stageEntity);
        DynamicBuffer<SpawnEntryRuntime> entries = manager.GetBuffer<SpawnEntryRuntime>(stageEntity);
        DynamicBuffer<SpawnRequest> requests = manager.GetBuffer<SpawnRequest>(stageEntity);
        waves.Clear();
        entries.Clear();
        requests.Clear();

        StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
        stage.StageId = new FixedString64Bytes(config.stageId ?? $"Stage_{stageNumber:00}");
        stage.DefaultWaveDelay = Mathf.Max(0f, config.defaultWaveDelay);
        stage.AttackDistance = Mathf.Max(0.1f, config.attackDistance);
        stage.MaxAliveEnemies = Mathf.Max(1, config.maxAliveEnemies);
        stage.EnableEliteModifiers = false;
        stage.RandomEliteChance = 0f;
        stage.CurrentWaveIndex = -1;
        stage.NextRequestSequence = 0;
        stage.State = StageRuntimeState.NotStarted;
        manager.SetComponentData(stageEntity, stage);

        for (int waveIndex = 0; waveIndex < config.waves.Length; waveIndex++)
        {
            RuntimeWaveConfig sourceWave = config.waves[waveIndex] ?? new RuntimeWaveConfig();
            int firstEntryIndex = entries.Length;
            RuntimeSpawnEntryConfig[] sourceEntries = sourceWave.entries ?? Array.Empty<RuntimeSpawnEntryConfig>();

            foreach (RuntimeSpawnEntryConfig sourceEntry in sourceEntries)
            {
                RuntimeSpawnEntryConfig item = sourceEntry ?? new RuntimeSpawnEntryConfig();
                bool found = TryFindEnemy(catalog, item.enemyId, out EnemyCatalogRuntime enemy);
                bool validArena = !string.IsNullOrWhiteSpace(item.spawnArenaGroupId);
                entries.Add(new SpawnEntryRuntime
                {
                    EnemyId = new FixedString64Bytes(item.enemyId ?? string.Empty),
                    EnemyPrefab = found ? enemy.EnemyPrefab : Entity.Null,
                    EnemyType = found ? enemy.EnemyType : EnemyType.Normal,
                    VisualKind = found ? enemy.VisualKind : MobVisualKind.Zombie,
                    HealthMultiplier = found ? enemy.HealthMultiplier : 1f,
                    DamageMultiplier = found ? enemy.DamageMultiplier : 1f,
                    Scale = found ? enemy.Scale : 1f,
                    XPReward = found ? enemy.XPReward : 0,
                    GoldReward = found ? enemy.GoldReward : 0,
                    SpawnArenaGroupId = new FixedString64Bytes(item.spawnArenaGroupId ?? string.Empty),
                    State = !found || !validArena
                        ? SpawnEntryRuntimeState.Failed
                        : item.quantity == 0 ? SpawnEntryRuntimeState.Completed : SpawnEntryRuntimeState.Pending,
                    WaveIndex = waveIndex,
                    Quantity = Mathf.Max(0, item.quantity),
                    SpawnDelay = Mathf.Max(0f, item.spawnDelay),
                    SpawnInterval = Mathf.Max(0f, item.spawnInterval),
                    NextSpawnTime = Mathf.Max(0f, item.spawnDelay),
                });

                if (!found)
                    Debug.LogError($"{source.name} wave {waveIndex + 1} references missing enemy ID '{item.enemyId}'.");
            }

            waves.Add(new WaveRuntime
            {
                WaveId = new FixedString64Bytes(sourceWave.waveId ?? $"Wave_{waveIndex + 1:00}"),
                WaveType = (WaveType)Mathf.Clamp(sourceWave.waveType, 0, (int)WaveType.Boss),
                ActivationCondition = waveIndex == 0
                    ? WaveActivationCondition.StageStarted
                    : WaveActivationCondition.PreviousWaveCompleted,
                State = WaveRuntimeState.Pending,
                WaveDelay = Mathf.Max(0f, sourceWave.waveDelay),
                CompletionThreshold = Mathf.Max(0, sourceWave.completionThreshold),
                MaxAliveEnemies = sourceWave.maxAliveEnemies > 0
                    ? sourceWave.maxAliveEnemies
                    : stage.MaxAliveEnemies,
                FirstSpawnEntryIndex = firstEntryIndex,
                SpawnEntryCount = entries.Length - firstEntryIndex,
            });
        }

        Debug.Log($"Loaded {stage.StageId} from Excel runtime config: {waves.Length} waves, {entries.Length} spawn entries.");
        return true;
    }

    private static bool TryFindEnemy(
        DynamicBuffer<EnemyCatalogRuntime> catalog,
        string enemyId,
        out EnemyCatalogRuntime result)
    {
        for (int i = 0; i < catalog.Length; i++)
        {
            EnemyCatalogRuntime candidate = catalog[i];
            if (candidate.EnemyId.ToString() != enemyId) continue;
            result = candidate;
            return candidate.EnemyPrefab != Entity.Null;
        }

        result = default;
        return false;
    }

    [Serializable]
    private sealed class RuntimeStageConfig
    {
        public string stageId = string.Empty;
        public float defaultWaveDelay = 0.5f;
        public float attackDistance = 1.5f;
        public int maxAliveEnemies = 16;
        public RuntimeWaveConfig[] waves = Array.Empty<RuntimeWaveConfig>();
    }

    [Serializable]
    private sealed class RuntimeWaveConfig
    {
        public string waveId = string.Empty;
        public int waveType = 0;
        public float waveDelay = 0f;
        public int completionThreshold = 0;
        public int maxAliveEnemies = 0;
        public RuntimeSpawnEntryConfig[] entries = Array.Empty<RuntimeSpawnEntryConfig>();
    }

    [Serializable]
    private sealed class RuntimeSpawnEntryConfig
    {
        public string enemyId = string.Empty;
        public int quantity = 0;
        public float spawnDelay = 0f;
        public float spawnInterval = 0f;
        public string spawnArenaGroupId = string.Empty;
    }
}

public static class EnemyHealthScalingSettings
{
    private const string ResourceName = "EnemyHealthScalingSettings";
    public const float DefaultHealthGrowthPerStage = 0.2f;
    public const float DefaultHealthGrowthPerWave = 0.1f;

    public static void ApplyTo(ref StageRuntime stage, int stageNumber)
    {
        TextAsset source = Resources.Load<TextAsset>(ResourceName);
        RuntimeConfig config = source != null
            ? JsonUtility.FromJson<RuntimeConfig>(source.text)
            : null;

        stage.StageNumber = Mathf.Max(1, stageNumber);
        stage.HealthGrowthPerStage = config != null
            ? Mathf.Max(0f, config.healthGrowthPerStage)
            : DefaultHealthGrowthPerStage;
        stage.HealthGrowthPerWave = config != null
            ? Mathf.Max(0f, config.healthGrowthPerWave)
            : DefaultHealthGrowthPerWave;
    }

    [Serializable]
    private sealed class RuntimeConfig
    {
        public float healthGrowthPerStage = DefaultHealthGrowthPerStage;
        public float healthGrowthPerWave = DefaultHealthGrowthPerWave;
    }
}
