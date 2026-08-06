using System.ComponentModel;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public struct Mob : IComponentData
{
    public GameObjectType MobTarget;
    public int Health;
    public int TakenDamageAmount;
}

public class MobAuthoring : MonoBehaviour
{
    public int Health = 100;
    public GameObjectType targetObjectType;
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
            });
        }
    }
}
