using System.ComponentModel;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public enum EnemyType : byte
{
    Normal,
    Elite,
    Boss,
}

public struct Mob : IComponentData
{
    public GameObjectType MobTarget;
    public int Health;
    public int TakenDamageAmount;
    public float KnockbackResistance;
    public EnemyType EnemyType;
    public int GoldMultiplier;
    public int XPReward;
    public int GoldReward;
    public int MaxHealth;
    public float CrowdControlResistance;
    public float SpawnTime;
}

public struct BossPhase : IComponentData
{
    public byte CurrentPhase;
    public float PhaseTwoHealthRatio;
    public float PhaseThreeHealthRatio;
    public float SpeedMultiplierPerPhase;
    public float DamageMultiplierPerPhase;
    public float BaseMoveSpeed;
    public int BaseDamage;
}

public struct MobStatusEffects : IComponentData
{
    public float BurnRemaining;
    public float BurnTickTimer;
    public int BurnDamagePerTick;
    public float FrostRemaining;
    public float FrostSpeedMultiplier;
    public float OriginalMoveSpeed;
    public bool FrostApplied;
}

public struct BossShockwave : IComponentData
{
    public float Cooldown;
    public float WarningDuration;
    public float Radius;
    public int Damage;
    public float Timer;
    public byte IsWarning;
}

public struct SpawnEmergence : IComponentData
{
    public float Duration;
    public float Elapsed;
}

public enum EliteModifierKind : byte
{
    None,
    Bulwark,
    Frenzied,
    Colossus,
    Revenant,
}

public struct EliteModifier : IComponentData
{
    public EliteModifierKind Kind;
    public float IncomingDamageMultiplier;
    public float HealthRegenerationPerSecond;
    public float RegenerationAccumulator;
}

public class MobAuthoring : MonoBehaviour
{
    public int Health = 100;
    public GameObjectType targetObjectType;
    [Range(0f, 1f)] public float KnockbackResistance;
    // public int DamageAmount;
    // [Range(UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ, 50f)]
    // public float HitDistance;
    
    public class Baker : Baker<MobAuthoring>
    {
        public override void Bake(MobAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Mob
            {
                MobTarget = authoring.targetObjectType,
                Health = authoring.Health,
                TakenDamageAmount = 0,
                KnockbackResistance = authoring.KnockbackResistance,
                EnemyType = EnemyType.Normal,
                GoldMultiplier = 1,
                XPReward = 5,
                GoldReward = 1,
                MaxHealth = authoring.Health,
                CrowdControlResistance = 0f,
                SpawnTime = 0f,
            });
            AddComponent<MobStatusEffects>(entity);
        }
    }
}
