using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class MobExplosionBridge : SystemBase
{
    private MobVisualSettings m_Settings;

    protected override void OnStartRunning()
    {
        m_Settings = Resources.Load<MobVisualSettings>("MobVisualSettings");
    }

    protected override void OnUpdate()
    {
        foreach (RefRO<MobExplosionEvent> explosion in SystemAPI.Query<RefRO<MobExplosionEvent>>())
        {
            Vector3 position = explosion.ValueRO.Position;
            if (m_Settings != null && m_Settings.ZombieFatExplosionVfx != null)
            {
                GameObject effect = Object.Instantiate(m_Settings.ZombieFatExplosionVfx, position, Quaternion.identity);
                effect.name = "Zombie Fat Explosion VFX";
                effect.transform.localScale *= Mathf.Max(0.6f, explosion.ValueRO.Radius * 0.8f);
                Object.Destroy(effect, 4f);
            }

            if (m_Settings != null && m_Settings.ZombieFatExplosionSound != null)
                AudioSource.PlayClipAtPoint(m_Settings.ZombieFatExplosionSound, position, 1f);
        }
    }
}
