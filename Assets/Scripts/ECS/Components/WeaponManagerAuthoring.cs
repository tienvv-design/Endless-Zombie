using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct WeaponManager : IComponentData
{
    public GunArchetype Archetype;
    public Entity WeaponEntityPrefab;
    public Entity BulletProjectilePrefab;
    public Entity HarpoonProjectilePrefab;
    public Entity GrenadeProjectilePrefab;
    public Entity IceProjectilePrefab;
    public Entity FireProjectilePrefab;
    public int NumberOfWeapons;
    public int DamagePerHit;

    public float3 Pivot;
    public float Radius;
    public float RotateSpeed;
    public bool ClockWise;
    
    public float ActiveDuration;
    
    public float Cooldown;
    public float Timer;
    public float AttackRange;
    public float ProjectileSpeed;
    public bool EnableOrbitingWeapons;
    
    public bool isActive;

    public int BaseDamage;
    public float BaseShotsPerSecond;
    public int BaseProjectileCount;
    public float BaseCriticalChance;
    public float BaseCriticalDamage;
    public float BaseAttackRange;
    public float BaseProjectileSpeed;
    public int BasePierce;
    public float BaseKnockback;
    public float BaseSpreadAngle;
    public float BaseExplosionRadius;
    public float BaseExplosionDamageMultiplier;
    public int BaseRicochetCount;
    public int BaseChainCount;
    public float BaseElementChance;
    public float BaseElementMagnitude;
    public int BaseMagazineSize;
    public float BaseReloadDuration;

    public float ShotsPerSecond;
    public int ProjectileCount;
    public float CriticalChance;
    public float CriticalDamage;
    public int Pierce;
    public float Knockback;
    public float SpreadAngle;
    public float MinimumFireInterval;
    public int MagazineSize;
    public int AmmoInMagazine;
    public float ReloadDuration;
    public float ReloadTimer;
    public bool IsReloading;
    public bool IsExplosive;
    public float ExplosionRadius;
    public float ExplosionDamageMultiplier;
    public float ExplosionKnockbackMultiplier;
    public int RicochetCount;
    public float RicochetSearchRange;
    public int ChainCount;
    public float ChainRange;
    public float ChainDamageMultiplier;
    public ElementType Element;
    public float ElementChance;
    public float ElementDuration;
    public float ElementMagnitude;
    public Unity.Mathematics.Random Random;
}

public struct GunModifiers : IComponentData
{
    public float DamageBonusPercent;
    public float FireRateBonusPercent;
    public float RangeBonusPercent;
    public float ProjectileSpeedBonusPercent;
    public int AdditionalProjectiles;
    public float CriticalChanceBonus;
    public float CriticalDamageBonus;
    public int AdditionalPierce;
    public float KnockbackBonus;
    public int SynergyLevel;
    public int AdditionalMagazineSize;
    public float ReloadSpeedBonusPercent;
    public float SpreadReductionPercent;
    public float ExplosionRadiusBonusPercent;
    public float ExplosionDamageBonus;
    public int AdditionalRicochets;
    public int AdditionalChains;
    public float ElementChanceBonus;
    public float ElementMagnitudeBonusPercent;
}

public class WeaponManagerAuthoring : MonoBehaviour
{
    [Header("Equipped Gun")]
    public GunConfig GunConfig;

    public GameObject WeaponObjectPrefab;
    [Header("Projectile Visual Prefabs")]
    public GameObject BulletProjectilePrefab;
    public GameObject HarpoonProjectilePrefab;
    public GameObject GrenadeProjectilePrefab;
    public GameObject IceProjectilePrefab;
    public GameObject FireProjectilePrefab;
    public int NumberOfWeapons;
    public int DamagePerHit;
    public float Cooldown;
    public float ActiveDuration;
    [Min(0.1f)] public float AttackRange = 6f;
    [Min(0.1f)] public float ProjectileSpeed = 12f;
    public bool EnableOrbitingWeapons;

    [Header("Gun Base Stats")]
    [Min(1)] public int ProjectileCount = 1;
    [Range(0f, 1f)] public float CriticalChance = 0.1f;
    [Min(1f)] public float CriticalDamage = 1.5f;
    [Min(0)] public int Pierce;
    [Min(0f)] public float Knockback = 0.5f;
    [Min(0f)] public float SpreadAngle = 10f;
    [Min(0.01f)] public float MinimumFireInterval = 0.1f;
    [Header("Automatic Magazine")]
    [Min(1)] public int MagazineSize = 12;
    [Min(0.05f)] public float ReloadDuration = 1.2f;
    [Header("Explosive Projectile")]
    public bool IsExplosive;
    [Min(0f)] public float ExplosionRadius;
    [Min(0f)] public float ExplosionDamageMultiplier = 1f;
    [Min(0f)] public float ExplosionKnockbackMultiplier = 1f;
    [Header("Ricochet")]
    [Min(0)] public int RicochetCount;
    [Min(0.1f)] public float RicochetSearchRange = 5f;
    [Header("Chain Lightning")]
    [Min(0)] public int ChainCount;
    [Min(0.1f)] public float ChainRange = 4f;
    [Range(0.1f, 1f)] public float ChainDamageMultiplier = 0.75f;
    [Header("Elemental Effect")]
    public ElementType Element;
    [Range(0f, 1f)] public float ElementChance;
    [Min(0f)] public float ElementDuration;
    [Min(0f)] public float ElementMagnitude;
    
    public float Radius;
    public float RotateSpeed;
    public bool ClockWise;
    
    public class Baker : Baker<WeaponManagerAuthoring>
    {
        public override void Bake(WeaponManagerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            GunConfig config = authoring.GunConfig;
            int baseDamage = config != null ? config.BaseDamage : authoring.DamagePerHit;
            float shotsPerSecond = config != null
                ? config.BaseShotsPerSecond
                : 1f / Mathf.Max(authoring.Cooldown, authoring.MinimumFireInterval);
            int projectileCount = config != null ? config.BaseProjectileCount : authoring.ProjectileCount;
            float criticalChance = config != null ? config.BaseCriticalChance : authoring.CriticalChance;
            float criticalDamage = config != null ? config.BaseCriticalDamage : authoring.CriticalDamage;
            float attackRange = config != null ? config.BaseRange : authoring.AttackRange;
            float projectileSpeed = config != null ? config.BaseProjectileSpeed : authoring.ProjectileSpeed;
            int pierce = config != null ? config.BasePierce : authoring.Pierce;
            float knockback = config != null ? config.BaseKnockback : authoring.Knockback;
            float spreadAngle = config != null ? config.BaseSpreadAngle : authoring.SpreadAngle;
            float minimumFireInterval = config != null ? config.MinimumFireInterval : authoring.MinimumFireInterval;
            int magazineSize = config != null ? config.BaseMagazineSize : authoring.MagazineSize;
            float reloadDuration = config != null ? config.BaseReloadDuration : authoring.ReloadDuration;
            bool isExplosive = config != null ? config.IsExplosive : authoring.IsExplosive;
            float explosionRadius = config != null ? config.ExplosionRadius : authoring.ExplosionRadius;
            float explosionDamageMultiplier = config != null
                ? config.ExplosionDamageMultiplier
                : authoring.ExplosionDamageMultiplier;
            float explosionKnockbackMultiplier = config != null
                ? config.ExplosionKnockbackMultiplier
                : authoring.ExplosionKnockbackMultiplier;
            int ricochetCount = config != null ? config.RicochetCount : authoring.RicochetCount;
            float ricochetSearchRange = config != null
                ? config.RicochetSearchRange
                : authoring.RicochetSearchRange;
            int chainCount = config != null ? config.ChainCount : authoring.ChainCount;
            float chainRange = config != null ? config.ChainRange : authoring.ChainRange;
            float chainDamageMultiplier = config != null
                ? config.ChainDamageMultiplier
                : authoring.ChainDamageMultiplier;
            ElementType element = config != null ? config.Element : authoring.Element;
            float elementChance = config != null ? config.ElementChance : authoring.ElementChance;
            float elementDuration = config != null ? config.ElementDuration : authoring.ElementDuration;
            float elementMagnitude = config != null ? config.ElementMagnitude : authoring.ElementMagnitude;
            AddComponent(entity, new WeaponManager
            {
                Archetype = config != null ? config.Archetype : GunArchetype.Pistol,
                WeaponEntityPrefab = GetEntity(
                    config != null && config.ProjectilePrefab != null
                        ? config.ProjectilePrefab
                        : authoring.WeaponObjectPrefab,
                    TransformUsageFlags.Dynamic),
                BulletProjectilePrefab = GetEntity(authoring.BulletProjectilePrefab, TransformUsageFlags.Dynamic),
                HarpoonProjectilePrefab = GetEntity(authoring.HarpoonProjectilePrefab, TransformUsageFlags.Dynamic),
                GrenadeProjectilePrefab = GetEntity(authoring.GrenadeProjectilePrefab, TransformUsageFlags.Dynamic),
                IceProjectilePrefab = GetEntity(authoring.IceProjectilePrefab, TransformUsageFlags.Dynamic),
                FireProjectilePrefab = GetEntity(authoring.FireProjectilePrefab, TransformUsageFlags.Dynamic),
                NumberOfWeapons = authoring.NumberOfWeapons,
                DamagePerHit = baseDamage,
                Radius = authoring.Radius,
                RotateSpeed = authoring.RotateSpeed,
                ClockWise = authoring.ClockWise,
                Cooldown = authoring.Cooldown,
                ActiveDuration = authoring.ActiveDuration,
                AttackRange = attackRange,
                ProjectileSpeed = projectileSpeed,
                EnableOrbitingWeapons = authoring.EnableOrbitingWeapons,
                BaseDamage = baseDamage,
                BaseShotsPerSecond = shotsPerSecond,
                BaseProjectileCount = projectileCount,
                BaseCriticalChance = criticalChance,
                BaseCriticalDamage = criticalDamage,
                BaseAttackRange = attackRange,
                BaseProjectileSpeed = projectileSpeed,
                BasePierce = pierce,
                BaseKnockback = knockback,
                BaseSpreadAngle = spreadAngle,
                BaseExplosionRadius = Mathf.Max(0f, explosionRadius),
                BaseExplosionDamageMultiplier = Mathf.Max(0f, explosionDamageMultiplier),
                BaseRicochetCount = Mathf.Max(0, ricochetCount),
                BaseChainCount = Mathf.Max(0, chainCount),
                BaseElementChance = Mathf.Clamp01(elementChance),
                BaseElementMagnitude = Mathf.Max(0f, elementMagnitude),
                BaseMagazineSize = Mathf.Max(1, magazineSize),
                BaseReloadDuration = Mathf.Max(0.05f, reloadDuration),
                ShotsPerSecond = shotsPerSecond,
                ProjectileCount = projectileCount,
                CriticalChance = criticalChance,
                CriticalDamage = criticalDamage,
                Pierce = pierce,
                Knockback = knockback,
                SpreadAngle = spreadAngle,
                MinimumFireInterval = minimumFireInterval,
                MagazineSize = Mathf.Max(1, magazineSize),
                AmmoInMagazine = Mathf.Max(1, magazineSize),
                ReloadDuration = Mathf.Max(0.05f, reloadDuration),
                ReloadTimer = 0f,
                IsReloading = false,
                IsExplosive = isExplosive,
                ExplosionRadius = Mathf.Max(0f, explosionRadius),
                ExplosionDamageMultiplier = Mathf.Max(0f, explosionDamageMultiplier),
                ExplosionKnockbackMultiplier = Mathf.Max(0f, explosionKnockbackMultiplier),
                RicochetCount = Mathf.Max(0, ricochetCount),
                RicochetSearchRange = Mathf.Max(0.1f, ricochetSearchRange),
                ChainCount = Mathf.Max(0, chainCount),
                ChainRange = Mathf.Max(0.1f, chainRange),
                ChainDamageMultiplier = Mathf.Clamp(chainDamageMultiplier, 0.1f, 1f),
                Element = element,
                ElementChance = Mathf.Clamp01(elementChance),
                ElementDuration = Mathf.Max(0f, elementDuration),
                ElementMagnitude = Mathf.Max(0f, elementMagnitude),
                Random = Unity.Mathematics.Random.CreateFromIndex(0x6E624EB7u),
            });
            AddComponent<GunModifiers>(entity);
        }
    }
    
}
