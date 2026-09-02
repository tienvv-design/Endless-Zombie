using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StageConfig", menuName = "Wave Spawn/Stage")]
public sealed class StageConfig : ScriptableObject
{
    public string StageId;
    [Min(0f)] public float DefaultWaveDelay = 3f;
    [Min(0.1f)] public float AttackDistance = 1.2f;
    [Min(1)] public int MaxAliveEnemies = 100;
    [Header("Elite Modifiers")]
    public bool EnableEliteModifiers = true;
    [Tooltip("Chance for a normal enemy to mutate into an Elite. Elite waves always receive a modifier.")]
    [Range(0f, 1f)] public float RandomEliteChance = 0.04f;
    [Min(0)] public int EliteChanceStartsAtWave = 1;
    [Header("Boss Tuning")]
    public BossTuning Boss = new();
    [Header("Spawn Portal")]
    [Min(0.1f)] public float SpawnPortalDuration = 0.7f;
    public bool SpawnOutsideCamera = true;
    [Tooltip("Extra viewport margin beyond the screen edge before a spawn is accepted.")]
    [Range(0f, 0.25f)] public float OffscreenSpawnPadding = 0.12f;
    public WaveDefinition[] Waves = Array.Empty<WaveDefinition>();
}

[Serializable]
public sealed class BossTuning
{
    [Min(1f)] public float HealthMultiplier = 8f;
    [Min(1f)] public float DamageMultiplier = 2f;
    [Min(1f)] public float ScaleMultiplier = 1.8f;
    [Range(0.1f, 0.9f)] public float PhaseTwoHealth = 0.66f;
    [Range(0.05f, 0.8f)] public float PhaseThreeHealth = 0.33f;
    [Min(1f)] public float SpeedPerPhase = 1.18f;
    [Min(1f)] public float DamagePerPhase = 1.3f;
    [Min(1f)] public float ShockwaveCooldown = 7f;
    [Min(0.2f)] public float ShockwaveWarning = 1.2f;
    [Min(1f)] public float ShockwaveRadius = 4.5f;
    [Min(1)] public int ShockwaveDamage = 8;
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
