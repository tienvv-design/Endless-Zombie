using Unity.Entities;
using UnityEngine;

[UpdateBefore(typeof(GunStatsSystem))]
public partial class MetaWeaponLoadoutSystem : SystemBase
{
    private Entity m_AppliedEntity = Entity.Null;
    private int m_AppliedWeapon = -1;
    private GunConfig m_AppliedConfig;

    protected override void OnCreate()
    {
        RequireForUpdate<WeaponManager>();
    }

    protected override void OnUpdate()
    {
        Entity gunEntity = SystemAPI.GetSingletonEntity<WeaponManager>();
        int selectedWeapon = Mathf.Max(0, MetaProgression.SelectedWeapon);
        GunConfig selectedConfig = WeaponVfxRuntime.CurrentConfig;
        if (m_AppliedEntity == gunEntity && m_AppliedWeapon == selectedWeapon &&
            m_AppliedConfig == selectedConfig)
            return;

        RefRW<WeaponManager> gun = SystemAPI.GetSingletonRW<WeaponManager>();
        if (selectedConfig != null)
            ApplyConfig(ref gun.ValueRW, selectedConfig);
        else
            ApplyProfile(ref gun.ValueRW, Mathf.Clamp(selectedWeapon, 0, 3));

        gun.ValueRW.MagazineSize = gun.ValueRO.BaseMagazineSize;
        gun.ValueRW.AmmoInMagazine = gun.ValueRO.BaseMagazineSize;
        gun.ValueRW.ReloadDuration = gun.ValueRO.BaseReloadDuration;
        gun.ValueRW.Timer = 0f;
        gun.ValueRW.ReloadTimer = 0f;
        gun.ValueRW.IsReloading = false;
        m_AppliedEntity = gunEntity;
        m_AppliedWeapon = selectedWeapon;
        m_AppliedConfig = selectedConfig;
    }

    private static void ApplyConfig(ref WeaponManager gun, GunConfig config)
    {
        gun.Archetype = config.Archetype;
        gun.BaseDamage = config.BaseDamage;
        gun.BaseShotsPerSecond = config.BaseShotsPerSecond;
        gun.BaseProjectileCount = config.BaseProjectileCount;
        gun.BaseCriticalChance = config.BaseCriticalChance;
        gun.BaseCriticalDamage = config.BaseCriticalDamage;
        gun.BaseAttackRange = config.BaseRange;
        gun.BaseProjectileSpeed = config.BaseProjectileSpeed;
        gun.BasePierce = config.BasePierce;
        gun.BaseKnockback = config.BaseKnockback;
        gun.BaseSpreadAngle = config.BaseSpreadAngle;
        gun.MinimumFireInterval = config.MinimumFireInterval;
        gun.BaseMagazineSize = Mathf.Max(1, config.BaseMagazineSize);
        gun.BaseReloadDuration = Mathf.Max(0.05f, config.BaseReloadDuration);

        gun.IsExplosive = config.IsExplosive;
        gun.BaseExplosionRadius = Mathf.Max(0f, config.ExplosionRadius);
        gun.BaseExplosionDamageMultiplier = Mathf.Max(0f, config.ExplosionDamageMultiplier);
        gun.ExplosionKnockbackMultiplier = Mathf.Max(0f, config.ExplosionKnockbackMultiplier);
        gun.BaseRicochetCount = Mathf.Max(0, config.RicochetCount);
        gun.RicochetSearchRange = Mathf.Max(0.1f, config.RicochetSearchRange);
        gun.BaseChainCount = Mathf.Max(0, config.ChainCount);
        gun.ChainRange = Mathf.Max(0.1f, config.ChainRange);
        gun.ChainDamageMultiplier = Mathf.Clamp(config.ChainDamageMultiplier, 0.1f, 1f);
        gun.Element = config.Element;
        gun.BaseElementChance = Mathf.Clamp01(config.ElementChance);
        gun.ElementDuration = Mathf.Max(0f, config.ElementDuration);
        gun.BaseElementMagnitude = Mathf.Max(0f, config.ElementMagnitude);
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
