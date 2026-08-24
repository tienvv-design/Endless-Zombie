using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct DamageDigit : IComponentData
{
    public bool AlwaysFaceCamera;
    public bool IsExplosive;
    
    public float ExplosionRadius;
    public int DamageValue;
    public bool IsCritical;
    public float BaseScale;
    public float FeedbackTime;
    
    public float ExplosionDelay;
    public float ExplosionTimer;

    public Unity.Mathematics.Random Random;
}

public class DamageDigitAuthoring : MonoBehaviour
{
    public bool AlwaysFaceCamera = true; 
    public float ExplosionDelay;
    public float ExplosionRadius;
    
    public class Baker : Baker<DamageDigitAuthoring>
    {
        public override void Bake(DamageDigitAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DamageDigit
            {
                AlwaysFaceCamera = authoring.AlwaysFaceCamera,
                ExplosionDelay = authoring.ExplosionDelay,
                ExplosionRadius = authoring.ExplosionRadius,
                Random = new Random(1),
            });
        }
    }
}
