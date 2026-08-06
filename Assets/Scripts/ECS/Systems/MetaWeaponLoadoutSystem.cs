using Unity.Entities;
using UnityEngine;

[UpdateBefore(typeof(GunStatsSystem))]
public partial class MetaWeaponLoadoutSystem : SystemBase
{
    private bool m_Applied;

    protected override void OnCreate()
    {
        RequireForUpdate<WeaponManager>();
    }

    protected override void OnUpdate()
    {
        if (m_Applied) return;
        RefRW<WeaponManager> gun = SystemAPI.GetSingletonRW<WeaponManager>();
        ApplyProfile(ref gun.ValueRW, Mathf.Clamp(MetaProgression.SelectedWeapon, 0, 3));
        gun.ValueRW.AmmoInMagazine = gun.ValueRO.MagazineSize;
        m_Applied = true;
    }

    private static void ApplyProfile(ref WeaponManager gun, int profile)
    {
        switch (profile)
        {
            case 1: // Shotgun
                DisableExplosion(ref gun);
                gun.BaseDamage = 1; gun.BaseShotsPerSecond = 0.8f; gun.BaseProjectileCount = 5;
                gun.BaseAttackRange = 5f; gun.BaseProjectileSpeed = 12f; gun.BaseSpreadAngle = 24f;
                gun.MagazineSize = 6; gun.ReloadDuration = 1.8f; gun.BaseKnockback = 1.2f;
                break;
            case 2: // Assault Rifle
                DisableExplosion(ref gun);
                gun.BaseDamage = 1; gun.BaseShotsPerSecond = 5f; gun.BaseProjectileCount = 1;
                gun.BaseAttackRange = 9f; gun.BaseProjectileSpeed = 18f; gun.BaseSpreadAngle = 4f;
                gun.MagazineSize = 30; gun.ReloadDuration = 1.6f; gun.BaseKnockback = 0.25f;
                break;
            case 3: // Rocket Launcher
                gun.BaseDamage = 4; gun.BaseShotsPerSecond = 0.6f; gun.BaseProjectileCount = 1;
                gun.BaseAttackRange = 10f; gun.BaseProjectileSpeed = 9f; gun.BaseSpreadAngle = 0f;
                gun.MagazineSize = 3; gun.ReloadDuration = 2.4f; gun.BaseKnockback = 1.5f;
                gun.IsExplosive = true; gun.BaseExplosionRadius = 2.5f;
                gun.BaseExplosionDamageMultiplier = 0.75f; gun.ExplosionKnockbackMultiplier = 1.5f;
                break;
            default: // Pistol
                DisableExplosion(ref gun);
                gun.BaseDamage = 2; gun.BaseShotsPerSecond = 1.5f; gun.BaseProjectileCount = 1;
                gun.BaseAttackRange = 8f; gun.BaseProjectileSpeed = 14f; gun.BaseSpreadAngle = 0f;
                gun.MagazineSize = 12; gun.ReloadDuration = 1.2f; gun.BaseKnockback = 0.5f;
                break;
        }
    }

    private static void DisableExplosion(ref WeaponManager gun)
    {
        gun.IsExplosive = false;
        gun.BaseExplosionRadius = 0f;
        gun.BaseExplosionDamageMultiplier = 0f;
        gun.ExplosionRadius = 0f;
        gun.ExplosionDamageMultiplier = 0f;
        gun.ExplosionKnockbackMultiplier = 0f;
    }
}
