using Unity.Entities;
using UnityEngine;

public struct MobSpawnSettings : IComponentData
{
    public float SpawnRadius;

    public float BaseSpawnRate;    // mobs per second at time 0
    public float RateIncrease;     // how much it grows per second

    public float ElapsedTime;
    public float Timer;
}

public class MobSpawnSettingsAuthoring : MonoBehaviour
{
    public float SpawnRadius = 25;
    public float BaseSpawnRate = 1; 
    public float RateIncrease = 0.15f;  

    public float ElapsedTime = 0;
    public float Timer = 0;
    
    public class Baker : Baker<MobSpawnSettingsAuthoring>
    {
        public override void Bake(MobSpawnSettingsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new MobSpawnSettings
            {
                SpawnRadius = authoring.SpawnRadius,
                BaseSpawnRate = authoring.BaseSpawnRate, 
                RateIncrease = authoring.RateIncrease,
                ElapsedTime = authoring.ElapsedTime,
                Timer = authoring.Timer,
            });
        }
    }
}
