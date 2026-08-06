using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class XPBrigde : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        foreach (var xpCollectedEvent in SystemAPI.Query<RefRO<XPCollectedEvent>>())
        {
            if(CharacterXPManager.Instance) CharacterXPManager.Instance.GainXP(xpCollectedEvent.ValueRO.XPAmount);
        }
    }
}