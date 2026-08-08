using Unity.Entities;
using Unity.Mathematics;

public struct DigitExplosionEvent : IComponentData
{
    public float3 Position;
    public float Radius;
    public int Damage;
    public float Knockback;
    public bool IsCritical;
}

public struct ChainLightningEvent : IComponentData
{
    public float3 Start;
    public float3 End;
}

public struct WeaponFiredVfxEvent : IComponentData
{
    public float3 Position;
    public float3 Direction;
}

public struct WeaponImpactVfxEvent : IComponentData
{
    public float3 Position;
    public float3 Direction;
    public bool IsExplosion;
}

public struct WeaponReloadVfxEvent : IComponentData
{
    public float3 Position;
}

public struct XPCollectedEvent : IComponentData
{
    public int XPAmount;
}

public struct GoldCollectedEvent : IComponentData
{
    public int Amount;
}
