using Unity.Entities;

public struct PlayerProjectile : IComponentData
{
    public GunArchetype Archetype;
    public Unity.Mathematics.float3 Direction;
    public int Damage;
    public float Speed;
    public float RemainingRange;
    public int RemainingPierce;
    public float Knockback;
    public bool IsCritical;
    public float HitDistanceSq;
    public bool IsExplosive;
    public float ExplosionRadius;
    public float ExplosionDamageMultiplier;
    public float ExplosionKnockback;
    public int RemainingRicochets;
    public float RicochetSearchRange;
    public int RemainingChains;
    public float ChainRange;
    public float ChainDamageMultiplier;
    public ElementType Element;
    public float ElementDuration;
    public float ElementMagnitude;
}

[InternalBufferCapacity(4)]
public struct PlayerProjectileHit : IBufferElementData
{
    public Entity Entity;
}
