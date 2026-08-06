using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class MobDeathBridge : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        foreach (var mobDeathEvent in SystemAPI.Query<RefRO<MobDeathEvent>>())
        {
            AudioManager.Instance.Play(SoundLabel.MobDeathSound);
        }
    }
}