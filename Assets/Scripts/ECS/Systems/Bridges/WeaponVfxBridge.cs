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
        // Reload audio is gameplay feedback and must not depend on the held model/VFX
        // having finished binding. Previously a missing CurrentConfig skipped the whole
        // bridge, which made reload events silent during loadout/model transitions.
        foreach (RefRO<WeaponReloadVfxEvent> reload in SystemAPI.Query<RefRO<WeaponReloadVfxEvent>>())
            AudioManager.Instance?.PlayWeaponReload(reload.ValueRO.Duration);

        GunConfig config = WeaponVfxRuntime.CurrentConfig;
        if (config == null) return;

        foreach (RefRO<WeaponFiredVfxEvent> fired in SystemAPI.Query<RefRO<WeaponFiredVfxEvent>>())
        {
            AudioManager.Instance?.PlayWeapon(config.Archetype);
            if (m_PlayerGunplayAnimator == null)
                m_PlayerGunplayAnimator = Object.FindFirstObjectByType<PlayerGunplayAnimator>();
            m_PlayerGunplayAnimator?.PlayShot();

            Transform muzzle = WeaponVfxRuntime.CurrentMuzzleVfxSocket ??
                               WeaponVfxRuntime.CurrentMuzzle;
            Vector3 position = muzzle != null
                ? muzzle.position
                : (Vector3)fired.ValueRO.Position;
            Vector3 shotDirection = (Vector3)math.normalizesafe(fired.ValueRO.Direction, math.forward());
            Quaternion rotation = muzzle != null
                ? muzzle.rotation
                : Quaternion.LookRotation(shotDirection);
            if (config.Archetype == GunArchetype.FlameRifle && muzzle != null)
            {
                Entity target = fired.ValueRO.Target;
                long targetKey = ((long)(uint)target.Version << 32) | (uint)target.Index;
                Vector3 targetDirection = (Vector3)fired.ValueRO.TargetPosition - muzzle.position;
                if (targetDirection.sqrMagnitude < 0.0001f)
                    targetDirection = shotDirection;
                WeaponVfxRuntime.PlayContinuous(
                    config.MuzzleVfxPrefab, muzzle, Vector3.zero,
                    Quaternion.LookRotation(targetDirection.normalized, Vector3.up), targetKey,
                    config.VfxLifetime, config.MuzzleEffectScale);
            }
            else
            {
                WeaponVfxRuntime.Play(
                    config.MuzzleVfxPrefab,
                    position,
                    rotation,
                    config.VfxLifetime,
                    muzzle != null
                        ? muzzle.localScale * config.MuzzleEffectScale
                        : Vector3.one * config.MuzzleEffectScale);
            }
            AddWeaponRecoil(config.Archetype);
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
                GetCameraFeedback()?.AddImpulse(0.18f, 0.24f, 17f, 1.5f);
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
        {
            AudioManager.Instance?.Play(SoundLabel.MobDamageSound);
            GetCameraFeedback()?.AddImpulse(0.014f, 0.055f, 31f, 0.22f);
        }
        if (criticalThisFrame)
            GetCameraFeedback()?.AddImpulse(0.045f, 0.11f, 24f, 0.65f);

        foreach (RefRO<WeaponReloadVfxEvent> reload in SystemAPI.Query<RefRO<WeaponReloadVfxEvent>>())
        {
            Transform muzzle = WeaponVfxRuntime.CurrentMuzzleVfxSocket ??
                               WeaponVfxRuntime.CurrentMuzzle;
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

    private void AddWeaponRecoil(GunArchetype archetype)
    {
        CameraHitFeedback feedback = GetCameraFeedback();
        if (feedback == null) return;

        switch (archetype)
        {
            case GunArchetype.Shotgun:
                feedback.AddImpulse(0.06f, 0.13f, 20f, 0.9f);
                break;
            case GunArchetype.SniperRifle:
                feedback.AddImpulse(0.052f, 0.12f, 19f, 0.8f);
                break;
            case GunArchetype.RocketLauncher:
            case GunArchetype.GrenadeLauncher:
                feedback.AddImpulse(0.072f, 0.15f, 17f, 1.05f);
                break;
            case GunArchetype.Pistol:
                feedback.AddImpulse(0.018f, 0.065f, 29f, 0.3f);
                break;
            case GunArchetype.AssaultRifle:
                feedback.AddImpulse(0.014f, 0.055f, 30f, 0.26f);
                break;
            case GunArchetype.SMG:
                feedback.AddImpulse(0.009f, 0.042f, 34f, 0.18f);
                break;
            case GunArchetype.Minigun:
                feedback.AddImpulse(0.006f, 0.036f, 36f, 0.13f);
                break;
            case GunArchetype.TeslaGun:
                feedback.AddImpulse(0.022f, 0.075f, 25f, 0.38f);
                break;
            case GunArchetype.CryoGun:
                feedback.AddImpulse(0.017f, 0.065f, 27f, 0.3f);
                break;
            case GunArchetype.FlameRifle:
                feedback.AddImpulse(0.007f, 0.04f, 32f, 0.14f);
                break;
        }
    }
}
