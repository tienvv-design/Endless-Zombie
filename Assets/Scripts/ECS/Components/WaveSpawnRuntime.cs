using Unity.Collections;
using Unity.Entities;

public struct GameplayStartedTag : IComponentData { }

public enum StageRuntimeState : byte { NotStarted, Running, Completed, Stopped }
public enum WaveRuntimeState : byte { Pending, Delay, Active, Completed }
public enum SpawnEntryRuntimeState : byte { Pending, Active, Completed, Failed }

public struct StageRuntime : IComponentData
{
    public FixedString64Bytes StageId;
    public int StageNumber;
    public float HealthGrowthPerStage;
    public float HealthGrowthPerWave;
    public StageRuntimeState State;
    public int CurrentWaveIndex;
    public float DefaultWaveDelay;
    public float AttackDistance;
    public int MaxAliveEnemies;
    public uint NextRequestSequence;
    public bool EnableEliteModifiers;
    public float RandomEliteChance;
    public int EliteChanceStartsAtWave;
    public float BossHealthMultiplier, BossDamageMultiplier, BossScaleMultiplier;
    public float BossPhaseTwoHealth, BossPhaseThreeHealth, BossSpeedPerPhase, BossDamagePerPhase;
    public float BossShockwaveCooldown, BossShockwaveWarning, BossShockwaveRadius;
    public int BossShockwaveDamage;
    public float SpawnPortalDuration;
    public bool SpawnOutsideCamera;
    public float OffscreenSpawnPadding;
}

[InternalBufferCapacity(8)]
public struct WaveRuntime : IBufferElementData
{
    public FixedString64Bytes WaveId;
    public WaveType WaveType;
    public WaveActivationCondition ActivationCondition;
    public WaveRuntimeState State;
    public float StateElapsedTime;
    public float WaveDelay;
    public int CompletionThreshold;
    public int MaxAliveEnemies;
    public int FirstSpawnEntryIndex;
    public int SpawnEntryCount;
}

[InternalBufferCapacity(16)]
public struct SpawnEntryRuntime : IBufferElementData
{
    public FixedString64Bytes EnemyId;
    public Entity EnemyPrefab;
    public EnemyType EnemyType;
    public MobVisualKind VisualKind;
    public float HealthMultiplier;
    public float DamageMultiplier;
    public float Scale;
    public int XPReward;
    public int GoldReward;
    public FixedString64Bytes SpawnArenaGroupId;
    public SpawnEntryRuntimeState State;
    public int WaveIndex;
    public int Quantity;
    public int EnqueuedCount;
    public int SpawnedCount;
    public float SpawnDelay;
    public float SpawnInterval;
    public float NextSpawnTime;
}

[InternalBufferCapacity(32)]
public struct SpawnRequest : IBufferElementData
{
    public uint Sequence;
    public int WaveIndex;
    public int SpawnEntryIndex;
    public Entity EnemyPrefab;
    public EnemyType EnemyType;
    public MobVisualKind VisualKind;
    public float HealthMultiplier;
    public float DamageMultiplier;
    public float Scale;
    public int XPReward;
    public int GoldReward;
    public FixedString64Bytes SpawnArenaGroupId;
}

[InternalBufferCapacity(8)]
public struct EnemyCatalogRuntime : IBufferElementData
{
    public FixedString64Bytes EnemyId;
    public Entity EnemyPrefab;
    public EnemyType EnemyType;
    public MobVisualKind VisualKind;
    public float HealthMultiplier;
    public float DamageMultiplier;
    public float Scale;
    public int XPReward;
    public int GoldReward;
}
