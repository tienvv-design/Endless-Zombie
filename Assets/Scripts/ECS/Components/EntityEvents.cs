using Unity.Entities;
using Unity.Mathematics;

public struct DigitExplosionEvent : IComponentData
{
    public float3 Position;
    public float Radius;
    public int Damage;
}

public struct XPCollectedEvent : IComponentData
{
    public int XPAmount;
} 
