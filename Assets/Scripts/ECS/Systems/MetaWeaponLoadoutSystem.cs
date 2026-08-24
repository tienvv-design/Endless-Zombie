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
        gun.ValueRW.MagazineSize = gun.ValueRO.BaseMagazineSize;
        gun.ValueRW.AmmoInMagazine = gun.ValueRO.BaseMagazineSize;
        gun.ValueRW.ReloadDuration = gun.ValueRO.BaseReloadDuration;
        m_Applied = true;
    }

    private static void ApplyProfile(ref WeaponManager gun, int profile)
    {
        switch (profile)
        {
            case 1: // Shotgun
                gun.Archetype = GunArchetype.Shotgun;
                DisableExplosion(ref gun);
                gun.BaseDamage = 4; gun.BaseShotsPerSecond = 0.9f; gun.BaseProjectileCount = 6;
                gun.BaseCriticalChance = 0.08f; gun.BaseCriticalDamage = 1.5f;
                gun.BaseAttackRange = 5f; gun.BaseProjectileSpeed = 12f; gun.BaseSpreadAngle = 24f;
                gun.BaseKnockback = 1.2f;
                gun.BaseMagazineSize = 6; gun.BaseReloadDuration = 1.75f;
                break;
            case 2: // Assault Rifle
                gun.Archetype = GunArchetype.AssaultRifle;
                DisableExplosion(ref gun);
                gun.BaseDamage = 5; gun.BaseShotsPerSecond = 5.5f; gun.BaseProjectileCount = 1;
                gun.BaseCriticalChance = 0.08f; gun.BaseCriticalDamage = 1.6f;
                gun.BaseAttackRange = 9f; gun.BaseProjectileSpeed = 18f; gun.BaseSpreadAngle = 4f;
                gun.BaseKnockback = 0.25f;
                gun.BaseMagazineSize = 30; gun.BaseReloadDuration = 1.6f;
                break;
            case 3: // Rocket Launcher
                gun.Archetype = GunArchetype.RocketLauncher;
                gun.BaseDamage = 38; gun.BaseShotsPerSecond = 0.55f; gun.BaseProjectileCount = 1;
                gun.BaseCriticalChance = 0.05f; gun.BaseCriticalDamage = 1.5f;
                gun.BaseAttackRange = 10f; gun.BaseProjectileSpeed = 9f; gun.BaseSpreadAngle = 0f;
                gun.BaseKnockback = 1.5f;
                gun.BaseMagazineSize = 3; gun.BaseReloadDuration = 2.4f;
                gun.IsExplosive = true; gun.BaseExplosionRadius = 2.5f;
                gun.BaseExplosionDamageMultiplier = 1f; gun.ExplosionKnockbackMultiplier = 1.5f;
                break;
            default: // Pistol
                gun.Archetype = GunArchetype.Pistol;
                DisableExplosion(ref gun);
                gun.BaseDamage = 8; gun.BaseShotsPerSecond = 2f; gun.BaseProjectileCount = 1;
                gun.BaseCriticalChance = 0.15f; gun.BaseCriticalDamage = 1.75f;
                gun.BaseAttackRange = 8f; gun.BaseProjectileSpeed = 14f; gun.BaseSpreadAngle = 0f;
                gun.BaseKnockback = 0.5f;
                gun.BaseMagazineSize = 12; gun.BaseReloadDuration = 1.1f;
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
