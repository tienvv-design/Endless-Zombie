using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class WaveSpawnAuthoring : MonoBehaviour
{
    [SerializeField] private StageConfig _stage;
    [SerializeField] private EnemyCatalog _enemyCatalog;

    private void OnValidate()
    {
        WaveSpawnConfigValidator.LogProblems(_stage, _enemyCatalog, this);
    }

    private sealed class Baker : Baker<WaveSpawnAuthoring>
    {
        public override void Bake(WaveSpawnAuthoring authoring)
        {
            StageConfig stage = authoring._stage;
            EnemyCatalog catalog = authoring._enemyCatalog;
            if (!WaveSpawnConfigValidator.CanBake(stage, catalog))
            {
                Debug.LogError("Wave Spawn authoring was not baked because its configuration is invalid.", authoring);
                return;
            }

            DependsOn(stage);
            DependsOn(catalog);
            BossTuning boss = stage.Boss ?? new BossTuning();

            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new StageRuntime
            {
                StageId = new FixedString64Bytes(stage.StageId ?? string.Empty),
                State = StageRuntimeState.NotStarted,
                CurrentWaveIndex = -1,
                DefaultWaveDelay = stage.DefaultWaveDelay,
                AttackDistance = Mathf.Max(0.1f, stage.AttackDistance),
                MaxAliveEnemies = stage.MaxAliveEnemies,
                NextRequestSequence = 0,
                EnableEliteModifiers = stage.EnableEliteModifiers,
                RandomEliteChance = Mathf.Clamp01(stage.RandomEliteChance),
                EliteChanceStartsAtWave = Mathf.Max(0, stage.EliteChanceStartsAtWave),
                BossHealthMultiplier = Mathf.Max(1f, boss.HealthMultiplier),
                BossDamageMultiplier = Mathf.Max(1f, boss.DamageMultiplier),
                BossScaleMultiplier = Mathf.Max(1f, boss.ScaleMultiplier),
                BossPhaseTwoHealth = boss.PhaseTwoHealth,
                BossPhaseThreeHealth = boss.PhaseThreeHealth,
                BossSpeedPerPhase = Mathf.Max(1f, boss.SpeedPerPhase),
                BossDamagePerPhase = Mathf.Max(1f, boss.DamagePerPhase),
                BossShockwaveCooldown = Mathf.Max(1f, boss.ShockwaveCooldown),
                BossShockwaveWarning = Mathf.Max(0.2f, boss.ShockwaveWarning),
                BossShockwaveRadius = Mathf.Max(1f, boss.ShockwaveRadius),
                BossShockwaveDamage = Mathf.Max(1, boss.ShockwaveDamage),
                SpawnPortalDuration = Mathf.Max(0.1f, stage.SpawnPortalDuration),
                SpawnOutsideCamera = stage.SpawnOutsideCamera,
                OffscreenSpawnPadding = Mathf.Clamp(stage.OffscreenSpawnPadding, 0f, 0.25f),
            });

            DynamicBuffer<WaveRuntime> waves = AddBuffer<WaveRuntime>(entity);
            DynamicBuffer<SpawnEntryRuntime> entries = AddBuffer<SpawnEntryRuntime>(entity);
            AddBuffer<SpawnRequest>(entity);
            DynamicBuffer<EnemyCatalogRuntime> runtimeCatalog = AddBuffer<EnemyCatalogRuntime>(entity);

            foreach (EnemyCatalogEntry enemy in catalog.Enemies)
            {
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.EnemyId) || enemy.Prefab == null)
                    continue;
                DependsOn(enemy.Prefab);
                runtimeCatalog.Add(new EnemyCatalogRuntime
                {
                    EnemyId = new FixedString64Bytes(enemy.EnemyId),
                    EnemyPrefab = GetEntity(enemy.Prefab, TransformUsageFlags.Dynamic),
                    EnemyType = enemy.EnemyType,
                    VisualKind = enemy.VisualKind,
                    HealthMultiplier = enemy.HealthMultiplier,
                    DamageMultiplier = enemy.DamageMultiplier,
                    Scale = enemy.Scale,
                    XPReward = enemy.XPReward,
                    GoldReward = enemy.GoldReward,
                });
            }

            for (int waveIndex = 0; waveIndex < stage.Waves.Length; waveIndex++)
            {
                WaveDefinition wave = stage.Waves[waveIndex];
                int firstEntryIndex = entries.Length;

                foreach (SpawnEntryDefinition entry in wave.SpawnEntries)
                {
                    bool validEnemy = catalog.TryGet(entry.EnemyId, out EnemyCatalogEntry enemy) &&
                                      enemy.Prefab != null;
                    bool validArena = !string.IsNullOrWhiteSpace(entry.SpawnArenaGroupId);
                    if (validEnemy)
                        DependsOn(enemy.Prefab);
                    entries.Add(new SpawnEntryRuntime
                    {
                        EnemyId = new FixedString64Bytes(entry.EnemyId ?? string.Empty),
                        EnemyPrefab = validEnemy
                            ? GetEntity(enemy.Prefab, TransformUsageFlags.Dynamic)
                            : Entity.Null,
                        EnemyType = validEnemy ? enemy.EnemyType : EnemyType.Normal,
                        VisualKind = validEnemy ? enemy.VisualKind : MobVisualKind.Zombie,
                        HealthMultiplier = validEnemy ? enemy.HealthMultiplier : 1f,
                        DamageMultiplier = validEnemy ? enemy.DamageMultiplier : 1f,
                        Scale = validEnemy ? enemy.Scale : 1f,
                        XPReward = validEnemy ? enemy.XPReward : 0,
                        GoldReward = validEnemy ? enemy.GoldReward : 0,
                        SpawnArenaGroupId = new FixedString64Bytes(entry.SpawnArenaGroupId ?? string.Empty),
                        State = !validEnemy || !validArena
                            ? SpawnEntryRuntimeState.Failed
                            : entry.Quantity == 0
                                ? SpawnEntryRuntimeState.Completed
                                : SpawnEntryRuntimeState.Pending,
                        WaveIndex = waveIndex,
                        Quantity = entry.Quantity,
                        EnqueuedCount = 0,
                        SpawnedCount = 0,
                        SpawnDelay = entry.SpawnDelay,
                        SpawnInterval = entry.SpawnInterval,
                        NextSpawnTime = entry.SpawnDelay,
                    });
                }

                waves.Add(new WaveRuntime
                {
                    WaveId = new FixedString64Bytes(wave.WaveId ?? string.Empty),
                    WaveType = wave.WaveType,
                    ActivationCondition = wave.ActivationCondition,
                    State = WaveRuntimeState.Pending,
                    StateElapsedTime = 0f,
                    WaveDelay = wave.WaveDelay >= 0f ? wave.WaveDelay : stage.DefaultWaveDelay,
                    CompletionThreshold = wave.CompletionThreshold,
                    MaxAliveEnemies = wave.MaxAliveEnemyOverride > 0
                        ? wave.MaxAliveEnemyOverride
                        : stage.MaxAliveEnemies,
                    FirstSpawnEntryIndex = firstEntryIndex,
                    SpawnEntryCount = entries.Length - firstEntryIndex,
                });
            }
        }
    }
}

public static class WaveSpawnConfigValidator
{
    public static bool CanBake(StageConfig stage, EnemyCatalog catalog)
    {
        if (stage == null || catalog == null || stage.Waves == null || stage.Waves.Length == 0)
            return false;

        foreach (WaveDefinition wave in stage.Waves)
        {
            if (wave == null || wave.SpawnEntries == null)
                return false;
            foreach (SpawnEntryDefinition entry in wave.SpawnEntries)
                if (entry == null)
                    return false;
        }

        return true;
    }

    public static bool IsValid(StageConfig stage, EnemyCatalog catalog)
    {
        return CollectProblems(stage, catalog).Count == 0;
    }

    public static void LogProblems(StageConfig stage, EnemyCatalog catalog, Object context)
    {
        foreach (string problem in CollectProblems(stage, catalog))
            Debug.LogError($"Wave Spawn config: {problem}", context);
    }

    public static List<string> CollectProblems(StageConfig stage, EnemyCatalog catalog)
    {
        var problems = new List<string>();
        if (stage == null)
        {
            problems.Add("StageConfig is missing.");
            return problems;
        }

        if (string.IsNullOrWhiteSpace(stage.StageId))
            problems.Add("Stage ID is empty.");
        if (stage.MaxAliveEnemies < 1)
            problems.Add("Stage Max Alive Enemies must be at least 1.");
        if (stage.Waves == null || stage.Waves.Length == 0)
            problems.Add("Stage must contain at least one Wave.");
        if (catalog == null)
            problems.Add("EnemyCatalog is missing.");
        if (problems.Count > 0)
            return problems;

        var waveIds = new HashSet<string>();
        for (int waveIndex = 0; waveIndex < stage.Waves.Length; waveIndex++)
        {
            WaveDefinition wave = stage.Waves[waveIndex];
            if (wave == null)
            {
                problems.Add($"Wave at index {waveIndex} is missing.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(wave.WaveId) || !waveIds.Add(wave.WaveId))
                problems.Add($"Wave at index {waveIndex} has an empty or duplicate Wave ID.");
            if (waveIndex == 0 && wave.ActivationCondition != WaveActivationCondition.StageStarted)
                problems.Add("The first Wave must use StageStarted activation.");
            if (waveIndex > 0 && wave.ActivationCondition != WaveActivationCondition.PreviousWaveCompleted)
                problems.Add($"Wave '{wave.WaveId}' must use PreviousWaveCompleted in the MVP.");

            if (wave.SpawnEntries == null)
                continue;

            for (int entryIndex = 0; entryIndex < wave.SpawnEntries.Length; entryIndex++)
            {
                SpawnEntryDefinition entry = wave.SpawnEntries[entryIndex];
                string label = $"Wave '{wave.WaveId}' entry {entryIndex}";
                if (entry == null)
                {
                    problems.Add($"{label} is missing.");
                    continue;
                }

                if (entry.Quantity < 0)
                    problems.Add($"{label} has a negative Quantity.");
                if (entry.SpawnDelay < 0f || entry.SpawnInterval < 0f)
                    problems.Add($"{label} has a negative delay or interval.");
                if (string.IsNullOrWhiteSpace(entry.SpawnArenaGroupId))
                    problems.Add($"{label} has no Spawn Arena Group.");
                if (catalog == null || !catalog.TryGet(entry.EnemyId, out EnemyCatalogEntry enemy) || enemy.Prefab == null)
                    problems.Add($"{label} references unknown Enemy ID '{entry.EnemyId}'. Invalid entries are terminal failures and never block Stage completion.");
            }
        }

        return problems;
    }
}

