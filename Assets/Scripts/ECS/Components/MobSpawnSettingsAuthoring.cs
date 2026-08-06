using Unity.Entities;
using UnityEngine;

public struct MobSpawnSettings : IComponentData
{
    public float SpawnRadius;

    public float BaseSpawnRate;    // mobs per second at time 0
    public float RateIncrease;     // how much it grows per second

    public float ElapsedTime;
    public float Timer;
    public float WaveDuration;
    public float WaveHealthMultiplier;
    public float WaveDamageMultiplier;
    public int EliteStartWave;
    public float EliteBaseChance;
    public float EliteChancePerWave;
    public float EliteMaxChance;
    public float EliteHealthMultiplier;
    public float EliteDamageMultiplier;
    public float EliteKnockbackResistance;
    public float EliteScale;
    public int EliteGoldMultiplier;
    public int BossWaveInterval;
    public int LastBossWave;
    public float BossHealthMultiplier;
    public float BossDamageMultiplier;
    public float BossScale;
    public int BossGoldMultiplier;
    public float BossKnockbackResistance;
    public float BossCrowdControlResistance;
    public float BossPhaseSpeedMultiplier;
    public float BossPhaseDamageMultiplier;
}

public struct CombatMetrics : IComponentData
{
    public int WindowDamage;
    public float WindowElapsed;
    public float RecentDps;
    public int TotalDamage;
    public int KillCount;
    public float TotalTimeToKill;
    public float AverageTimeToKill;
    public int ActiveEnemies;
    public int NearbyEnemies;
    public float Pressure;
}

public class MobSpawnSettingsAuthoring : MonoBehaviour
{
    public float SpawnRadius = 25;
    public float BaseSpawnRate = 1; 
    public float RateIncrease = 0.15f;  

    public float ElapsedTime = 0;
    public float Timer = 0;
    [Min(1f)] public float WaveDuration = 60f;
    [Min(1f)] public float WaveHealthMultiplier = 1.08f;
    [Min(1f)] public float WaveDamageMultiplier = 1.05f;
    [Header("Elite Enemy")]
    [Min(1)] public int EliteStartWave = 2;
    [Range(0f, 1f)] public float EliteBaseChance = 0.05f;
    [Range(0f, 1f)] public float EliteChancePerWave = 0.025f;
    [Range(0f, 1f)] public float EliteMaxChance = 0.25f;
    [Min(1f)] public float EliteHealthMultiplier = 4f;
    [Min(1f)] public float EliteDamageMultiplier = 2f;
    [Range(0f, 1f)] public float EliteKnockbackResistance = 0.75f;
    [Min(1f)] public float EliteScale = 1.4f;
    [Min(1)] public int EliteGoldMultiplier = 5;
    [Header("Boss")]
    [Min(1)] public int BossWaveInterval = 5;
    [Min(1f)] public float BossHealthMultiplier = 20f;
    [Min(1f)] public float BossDamageMultiplier = 3f;
    [Min(1f)] public float BossScale = 2f;
    [Min(1)] public int BossGoldMultiplier = 25;
    [Range(0f, 1f)] public float BossKnockbackResistance = 0.9f;
    [Range(0f, 1f)] public float BossCrowdControlResistance = 0.85f;
    [Min(1f)] public float BossPhaseSpeedMultiplier = 1.2f;
    [Min(1f)] public float BossPhaseDamageMultiplier = 1.25f;
    
    public class Baker : Baker<MobSpawnSettingsAuthoring>
    {
        public override void Bake(MobSpawnSettingsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new MobSpawnSettings
            {
                SpawnRadius = authoring.SpawnRadius,
                BaseSpawnRate = authoring.BaseSpawnRate, 
                RateIncrease = authoring.RateIncrease,
                ElapsedTime = authoring.ElapsedTime,
                Timer = authoring.Timer,
                WaveDuration = authoring.WaveDuration,
                WaveHealthMultiplier = authoring.WaveHealthMultiplier,
                WaveDamageMultiplier = authoring.WaveDamageMultiplier,
                EliteStartWave = authoring.EliteStartWave,
                EliteBaseChance = authoring.EliteBaseChance,
                EliteChancePerWave = authoring.EliteChancePerWave,
                EliteMaxChance = authoring.EliteMaxChance,
                EliteHealthMultiplier = authoring.EliteHealthMultiplier,
                EliteDamageMultiplier = authoring.EliteDamageMultiplier,
                EliteKnockbackResistance = authoring.EliteKnockbackResistance,
                EliteScale = authoring.EliteScale,
                EliteGoldMultiplier = authoring.EliteGoldMultiplier,
                BossWaveInterval = authoring.BossWaveInterval,
                LastBossWave = 0,
                BossHealthMultiplier = authoring.BossHealthMultiplier,
                BossDamageMultiplier = authoring.BossDamageMultiplier,
                BossScale = authoring.BossScale,
                BossGoldMultiplier = authoring.BossGoldMultiplier,
                BossKnockbackResistance = authoring.BossKnockbackResistance,
                BossCrowdControlResistance = authoring.BossCrowdControlResistance,
                BossPhaseSpeedMultiplier = authoring.BossPhaseSpeedMultiplier,
                BossPhaseDamageMultiplier = authoring.BossPhaseDamageMultiplier,
            });
            AddComponent<CombatMetrics>(entity);
        }
    }
}
