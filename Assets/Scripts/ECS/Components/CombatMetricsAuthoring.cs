using Unity.Entities;
using UnityEngine;

public struct CombatMetrics : IComponentData
{
    public int WindowDamage;
    public float WindowElapsed;
    public float RecentDps;
    public int TotalDamage;
    public int KillCount;
    public float TotalTimeToKill;
    public float AverageTimeToKill;
    public int ActiveEnemies;
    public int NearbyEnemies;
    public float Pressure;
}

public sealed class CombatMetricsAuthoring : MonoBehaviour
{
    private sealed class Baker : Baker<CombatMetricsAuthoring>
    {
        public override void Bake(CombatMetricsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent<CombatMetrics>(entity);
        }
    }
}
