using Unity.Entities;
using Unity.Transforms;

public struct MobDamageGivenEvent : IComponentData
{
    public int Id;
    public int Amount;
}

public struct MobDamageTakenEvent : IComponentData
{
    public int Id;
    public Entity Entity;
    public int Amount;
    public Unity.Mathematics.float3 KnockbackDirection;
    public float KnockbackDistance;
    public bool IsCritical;
    public ElementType Element;
    public float ElementDuration;
    public float ElementMagnitude;
}

public struct MobDeathEvent : IComponentData
{
    public LocalTransform LocalTransform;
    public int GoldMultiplier;
    public int XPReward;
    public int GoldReward;
}

public struct MobExplosionEvent : IComponentData
{
    public Unity.Mathematics.float3 Position;
    public float Radius;
}
