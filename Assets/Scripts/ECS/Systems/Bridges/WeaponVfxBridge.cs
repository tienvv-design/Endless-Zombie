using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(EventResetSystem))]
public partial class WeaponVfxBridge : SystemBase
{
    private PlayerGunplayAnimator m_PlayerGunplayAnimator;
    private CameraHitFeedback m_CameraFeedback;

    protected override void OnUpdate()
    {
        GunConfig config = WeaponVfxRuntime.CurrentConfig;
        if (config == null) return;

        foreach (RefRO<WeaponFiredVfxEvent> fired in SystemAPI.Query<RefRO<WeaponFiredVfxEvent>>())
        {
            AudioManager.Instance?.PlayWeapon(config.Archetype);
            if (m_PlayerGunplayAnimator == null)
                m_PlayerGunplayAnimator = Object.FindFirstObjectByType<PlayerGunplayAnimator>();
            m_PlayerGunplayAnimator?.PlayShot();

            Transform muzzle = WeaponVfxRuntime.CurrentMuzzle;
            Vector3 position = muzzle != null
                ? muzzle.TransformPoint(config.MuzzleEffectLocalOffset)
                : (Vector3)fired.ValueRO.Position;
            Vector3 shotDirection = (Vector3)math.normalizesafe(fired.ValueRO.Direction, math.forward());
            Quaternion rotation = Quaternion.LookRotation(shotDirection);
            WeaponVfxRuntime.Play(
                config.MuzzleVfxPrefab,
                position,
                rotation,
                config.VfxLifetime,
                muzzle != null
                    ? muzzle.localScale * config.MuzzleEffectScale
                    : Vector3.one * config.MuzzleEffectScale);
        }

        foreach (RefRO<WeaponImpactVfxEvent> impact in SystemAPI.Query<RefRO<WeaponImpactVfxEvent>>())
        {
            GameObject prefab = impact.ValueRO.IsExplosion && config.ExplosionVfxPrefab != null
                ? config.ExplosionVfxPrefab
                : config.ImpactVfxPrefab;
            Quaternion rotation = Quaternion.LookRotation(-(Vector3)math.normalizesafe(
                impact.ValueRO.Direction, math.forward()));
            WeaponVfxRuntime.Play(prefab, impact.ValueRO.Position, rotation, config.VfxLifetime);
            if (impact.ValueRO.IsExplosion)
            {
                GetCameraFeedback()?.AddImpulse(0.09f, 0.16f);
                AudioManager.Instance?.Play(SoundLabel.DigitExplosionSound);
            }
        }

        bool criticalThisFrame = false;
        bool damagedThisFrame = false;
        foreach (RefRO<MobDamageTakenEvent> damage in SystemAPI.Query<RefRO<MobDamageTakenEvent>>())
        {
            criticalThisFrame |= damage.ValueRO.IsCritical;
            damagedThisFrame = true;
        }
        if (damagedThisFrame)
            AudioManager.Instance?.Play(SoundLabel.MobDamageSound);
        if (criticalThisFrame)
            GetCameraFeedback()?.AddImpulse(0.055f, 0.1f);

        foreach (RefRO<WeaponReloadVfxEvent> reload in SystemAPI.Query<RefRO<WeaponReloadVfxEvent>>())
        {
            AudioManager.Instance?.PlayWeaponReload(config.Archetype);
            Transform muzzle = WeaponVfxRuntime.CurrentMuzzle;
            WeaponVfxRuntime.Play(
                config.ReloadVfxPrefab,
                muzzle != null ? muzzle.position : (Vector3)reload.ValueRO.Position,
                muzzle != null ? muzzle.rotation : Quaternion.identity,
                config.VfxLifetime);
        }
    }

    private CameraHitFeedback GetCameraFeedback()
    {
        if (m_CameraFeedback != null) return m_CameraFeedback;
        Camera camera = Camera.main;
        if (camera == null) return null;
        m_CameraFeedback = camera.GetComponent<CameraHitFeedback>();
        if (m_CameraFeedback == null)
            m_CameraFeedback = camera.gameObject.AddComponent<CameraHitFeedback>();
        return m_CameraFeedback;
    }
}
