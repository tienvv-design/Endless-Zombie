using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct WeaponManager : IComponentData
{
    public Entity WeaponEntityPrefab;
    public int NumberOfWeapons;
    public int DamagePerHit;

    public float3 Pivot;
    public float Radius;
    public float RotateSpeed;
    public bool ClockWise;
    
    public float ActiveDuration;
    
    public float Cooldown;
    public float Timer;
    
    public bool isActive;
}

public class WeaponManagerAuthoring : MonoBehaviour
{
    public GameObject WeaponObjectPrefab;
    public int NumberOfWeapons;
    public int DamagePerHit;
    public float Cooldown;
    public float ActiveDuration;
    
    public float Radius;
    public float RotateSpeed;
    public bool ClockWise;
    
    public class Baker : Baker<WeaponManagerAuthoring>
    {
        public override void Bake(WeaponManagerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new WeaponManager
            {
                WeaponEntityPrefab = GetEntity(authoring.WeaponObjectPrefab, TransformUsageFlags.Dynamic),
                NumberOfWeapons = authoring.NumberOfWeapons,
                DamagePerHit = authoring.DamagePerHit,
                Radius = authoring.Radius,
                RotateSpeed = authoring.RotateSpeed,
                ClockWise = authoring.ClockWise,
                Cooldown = authoring.Cooldown,
                ActiveDuration = authoring.ActiveDuration,
            });
        }
    }
    
}
