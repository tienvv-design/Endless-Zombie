using Unity.Entities;
using UnityEngine;

public enum MobVisualKind : byte
{
    Zombie,
    DogMutant,
}

public struct MobVisualVariant : IComponentData
{
    public MobVisualKind Kind;
}

public sealed class MobVisualVariantAuthoring : MonoBehaviour
{
    public MobVisualKind Kind;

    private sealed class Baker : Baker<MobVisualVariantAuthoring>
    {
        public override void Bake(MobVisualVariantAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new MobVisualVariant
            {
                Kind = authoring.Kind,
            });
        }
    }
}
