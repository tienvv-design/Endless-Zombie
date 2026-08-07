using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StageConfig", menuName = "Wave Spawn/Stage")]
public sealed class StageConfig : ScriptableObject
{
    public string StageId;
    [Min(0f)] public float DefaultWaveDelay = 3f;
    [Min(1)] public int MaxAliveEnemies = 100;
    public WaveDefinition[] Waves = Array.Empty<WaveDefinition>();
}

[Serializable]
public sealed class WaveDefinition
{
    public string WaveId;
    public WaveType WaveType;
    public WaveActivationCondition ActivationCondition = WaveActivationCondition.PreviousWaveCompleted;
    [Tooltip("Negative value uses the Stage default wave delay.")]
    public float WaveDelay = -1f;
    [Min(0)] public int CompletionThreshold = 4;
    [Tooltip("Zero or negative uses the Stage Max Alive Enemy value.")]
    public int MaxAliveEnemyOverride;
    public SpawnEntryDefinition[] SpawnEntries = Array.Empty<SpawnEntryDefinition>();
}

[Serializable]
public sealed class SpawnEntryDefinition
{
    public string EnemyId;
    [Min(0)] public int Quantity;
    [Min(0f)] public float SpawnDelay;
    [Min(0f)] public float SpawnInterval;
    public string SpawnArenaGroupId;
}
