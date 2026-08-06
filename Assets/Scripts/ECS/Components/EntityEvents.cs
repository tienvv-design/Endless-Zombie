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

public struct XPCollectedEvent : IComponentData
{
    public int XPAmount;
}

public struct GoldCollectedEvent : IComponentData
{
    public int Amount;
}
