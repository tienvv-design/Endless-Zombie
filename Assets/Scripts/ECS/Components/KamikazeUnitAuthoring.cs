using Unity.Entities;
using UnityEngine;

public struct KamikazeUnit : IComponentData
{
    public GameObjectType TargetObjectType;
    public float HitDistanceSq;
    public int Damage;
    public float AttackInterval;
    public float AttackTimer;
    public float AttackImpactNormalizedTime;
    public byte HasExploded;
}
public class KamikazeUnitAuthoring : MonoBehaviour
{
    public GameObjectType TargetObjectType;
    public int Damage;
    [Min(0.05f)] public float AttackInterval = 1f;
    [Range(0.05f, 0.95f)] public float AttackImpactNormalizedTime = 0.45f;
    
    [Range(UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ, 50f)]
    public float HitDistanceSq;
    
    public class Baker : Baker<KamikazeUnitAuthoring>
    {
        public override void Bake(KamikazeUnitAuthoring authoring)
        {
            float impactTime = authoring.AttackImpactNormalizedTime > 0f
                ? authoring.AttackImpactNormalizedTime
                : 0.45f;
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new KamikazeUnit
            {
                TargetObjectType = authoring.TargetObjectType,
                HitDistanceSq = authoring.HitDistanceSq,
                Damage = authoring.Damage,
                AttackInterval = authoring.AttackInterval,
                AttackTimer = 0f,
                AttackImpactNormalizedTime = impactTime,
                HasExploded = 0,
            });
        }
    }
}
