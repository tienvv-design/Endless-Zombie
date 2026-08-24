using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateBefore(typeof(PlayerAutoAttackSystem))]
internal partial struct GunStatsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WeaponManager>();
        state.RequireForUpdate<GunModifiers>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RefRW<WeaponManager> gun = SystemAPI.GetSingletonRW<WeaponManager>();
        GunModifiers modifiers = SystemAPI.GetSingleton<GunModifiers>();

        int synergyLevel = math.max(0, modifiers.SynergyLevel);
        float effectiveDamageBonus = modifiers.DamageBonusPercent + synergyLevel * 5f;
        float effectiveFireRateBonus = modifiers.FireRateBonusPercent + synergyLevel * 5f;
        // Ceil ensures a percentage damage upgrade has an immediate effect on low
        // integer base damage weapons (for example Pistol 2 * 1.2 becomes 3, not 2).
        gun.ValueRW.DamagePerHit = math.max(1,
            (int)math.ceil(gun.ValueRO.BaseDamage * (1f + effectiveDamageBonus / 100f)));
        gun.ValueRW.ShotsPerSecond = math.max(0.01f,
            gun.ValueRO.BaseShotsPerSecond * (1f + effectiveFireRateBonus / 100f));
        gun.ValueRW.Cooldown = math.max(gun.ValueRO.MinimumFireInterval, 1f / gun.ValueRO.ShotsPerSecond);
        gun.ValueRW.ProjectileCount = math.max(1,
            gun.ValueRO.BaseProjectileCount + modifiers.AdditionalProjectiles);
        gun.ValueRW.CriticalChance = math.clamp(
            gun.ValueRO.BaseCriticalChance + modifiers.CriticalChanceBonus, 0f, 1f);
        gun.ValueRW.CriticalDamage = math.max(1f,
            gun.ValueRO.BaseCriticalDamage + modifiers.CriticalDamageBonus);
        gun.ValueRW.AttackRange = math.max(0.1f,
            gun.ValueRO.BaseAttackRange * (1f + modifiers.RangeBonusPercent / 100f));
        gun.ValueRW.ProjectileSpeed = math.max(0.1f,
            gun.ValueRO.BaseProjectileSpeed * (1f + modifiers.ProjectileSpeedBonusPercent / 100f));
        gun.ValueRW.Pierce = math.max(0, gun.ValueRO.BasePierce + modifiers.AdditionalPierce);
        gun.ValueRW.Knockback = math.max(0f, gun.ValueRO.BaseKnockback + modifiers.KnockbackBonus);
        gun.ValueRW.SpreadAngle = math.max(0f,
            gun.ValueRO.BaseSpreadAngle * (1f - math.clamp(modifiers.SpreadReductionPercent, 0f, 100f) / 100f));
        int previousMagazineSize = math.max(1, gun.ValueRO.MagazineSize);
        gun.ValueRW.MagazineSize = math.max(1,
            gun.ValueRO.BaseMagazineSize + modifiers.AdditionalMagazineSize);
        if (gun.ValueRO.MagazineSize > previousMagazineSize && !gun.ValueRO.IsReloading)
            gun.ValueRW.AmmoInMagazine += gun.ValueRO.MagazineSize - previousMagazineSize;
        gun.ValueRW.AmmoInMagazine = math.clamp(
            gun.ValueRO.AmmoInMagazine, 0, gun.ValueRO.MagazineSize);
        gun.ValueRW.ReloadDuration = math.max(0.05f,
            gun.ValueRO.BaseReloadDuration / (1f + math.max(0f, modifiers.ReloadSpeedBonusPercent) / 100f));

        gun.ValueRW.ExplosionRadius = gun.ValueRO.BaseExplosionRadius > 0f
            ? gun.ValueRO.BaseExplosionRadius * (1f + synergyLevel * 0.15f + modifiers.ExplosionRadiusBonusPercent / 100f)
            : 0f;
        gun.ValueRW.ExplosionDamageMultiplier = gun.ValueRO.BaseExplosionDamageMultiplier > 0f
            ? gun.ValueRO.BaseExplosionDamageMultiplier + synergyLevel * 0.1f + modifiers.ExplosionDamageBonus
            : 0f;
        gun.ValueRW.RicochetCount = math.max(0, gun.ValueRO.BaseRicochetCount + modifiers.AdditionalRicochets
            + (gun.ValueRO.BaseRicochetCount > 0 ? synergyLevel : 0));
        gun.ValueRW.ChainCount = math.max(0, gun.ValueRO.BaseChainCount + modifiers.AdditionalChains
            + (gun.ValueRO.BaseChainCount > 0 ? synergyLevel : 0));
        gun.ValueRW.ElementChance = gun.ValueRO.BaseElementChance > 0f
            ? math.saturate(gun.ValueRO.BaseElementChance + synergyLevel * 0.1f + modifiers.ElementChanceBonus)
            : 0f;
        gun.ValueRW.ElementMagnitude = gun.ValueRO.BaseElementMagnitude > 0f
            ? gun.ValueRO.BaseElementMagnitude * (1f + synergyLevel * 0.15f + modifiers.ElementMagnitudeBonusPercent / 100f)
            : 0f;
    }
}
