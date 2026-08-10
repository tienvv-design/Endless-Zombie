using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(EventResetSystem))]
public partial class WeaponVfxBridge : SystemBase
{
    private PlayerGunplayAnimator m_PlayerGunplayAnimator;

    protected override void OnUpdate()
    {
        GunConfig config = WeaponVfxRuntime.CurrentConfig;
        if (config == null) return;

        foreach (RefRO<WeaponFiredVfxEvent> fired in SystemAPI.Query<RefRO<WeaponFiredVfxEvent>>())
        {
            if (m_PlayerGunplayAnimator == null)
                m_PlayerGunplayAnimator = Object.FindFirstObjectByType<PlayerGunplayAnimator>();
            m_PlayerGunplayAnimator?.PlayShot();

            Transform muzzle = WeaponVfxRuntime.CurrentMuzzle;
            Vector3 position = muzzle != null ? muzzle.position : (Vector3)fired.ValueRO.Position;
            Quaternion rotation = muzzle != null
                ? muzzle.rotation
                : Quaternion.LookRotation((Vector3)math.normalizesafe(fired.ValueRO.Direction, math.forward()));
            WeaponVfxRuntime.Play(config.MuzzleVfxPrefab, position, rotation, config.VfxLifetime);
        }

        foreach (RefRO<WeaponImpactVfxEvent> impact in SystemAPI.Query<RefRO<WeaponImpactVfxEvent>>())
        {
            GameObject prefab = impact.ValueRO.IsExplosion && config.ExplosionVfxPrefab != null
                ? config.ExplosionVfxPrefab
                : config.ImpactVfxPrefab;
            Quaternion rotation = Quaternion.LookRotation(-(Vector3)math.normalizesafe(
                impact.ValueRO.Direction, math.forward()));
            WeaponVfxRuntime.Play(prefab, impact.ValueRO.Position, rotation, config.VfxLifetime);
        }

        foreach (RefRO<WeaponReloadVfxEvent> reload in SystemAPI.Query<RefRO<WeaponReloadVfxEvent>>())
        {
            Transform muzzle = WeaponVfxRuntime.CurrentMuzzle;
            WeaponVfxRuntime.Play(
                config.ReloadVfxPrefab,
                muzzle != null ? muzzle.position : (Vector3)reload.ValueRO.Position,
                muzzle != null ? muzzle.rotation : Quaternion.identity,
                config.VfxLifetime);
        }
    }
}
