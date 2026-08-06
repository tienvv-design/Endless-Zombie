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
}

public struct MobDeathEvent : IComponentData
{
    public LocalTransform LocalTransform;
}
