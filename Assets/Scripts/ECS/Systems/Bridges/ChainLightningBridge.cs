using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(EventResetSystem))]
public partial class ChainLightningBridge : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (RefRO<ChainLightningEvent> chainEvent in SystemAPI.Query<RefRO<ChainLightningEvent>>())
        {
            GameObject visual = new GameObject("Chain Lightning");
            LineRenderer line = visual.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, chainEvent.ValueRO.Start);
            line.SetPosition(1, chainEvent.ValueRO.End);
            line.startWidth = 0.12f;
            line.endWidth = 0.04f;
            line.startColor = new Color(0.35f, 0.85f, 1f, 1f);
            line.endColor = new Color(0.8f, 0.95f, 1f, 0.25f);
            Material material = new Material(Shader.Find("Sprites/Default"));
            line.material = material;
            Object.Destroy(material, 0.1f);
            Object.Destroy(visual, 0.08f);
        }
    }
}
