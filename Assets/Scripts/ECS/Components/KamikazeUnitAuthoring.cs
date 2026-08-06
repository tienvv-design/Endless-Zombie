using Unity.Entities;
using UnityEngine;

public struct KamikazeUnit : IComponentData
{
    public GameObjectType TargetObjectType;
    public float HitDistanceSq;
    public int Damage;
}

public class KamikazeUnitAuthoring : MonoBehaviour
{
    public GameObjectType TargetObjectType;
    public int Damage;
    
    [Range(UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ, 50f)]
    public float HitDistanceSq;
    
    public class Baker : Baker<KamikazeUnitAuthoring>
    {
        public override void Bake(KamikazeUnitAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new KamikazeUnit
            {
                TargetObjectType = authoring.TargetObjectType,
                HitDistanceSq = authoring.HitDistanceSq,
                Damage = authoring.Damage,
            });
        }
    }
} 
    