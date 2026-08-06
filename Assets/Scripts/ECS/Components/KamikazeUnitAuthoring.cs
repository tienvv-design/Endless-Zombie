using Unity.Entities;
using UnityEngine;

public struct KamikazeUnit : IComponentData
{
    public GameObjectType TargetObjectType;
    public float HitDistanceSq;
    public int Damage;
    public float AttackInterval;
    public float AttackTimer;
}
public class KamikazeUnitAuthoring : MonoBehaviour
{
    public GameObjectType TargetObjectType;
    public int Damage;
    [Min(0.05f)] public float AttackInterval = 1f;
    
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
                AttackInterval = authoring.AttackInterval,
                AttackTimer = 0f,
            });
        }
    }
}
