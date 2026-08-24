using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "GunPoolUpgrade", menuName = "Settings-Configs/Upgrades/Gun Pool Upgrade")]
public class GunPoolUpgrade : CharUpgrade
{
    [SerializeField] private string m_DisplayName = "Weapon Upgrade";
    [SerializeField] private GunArchetype m_Archetype;
    [SerializeField, Min(1)] private int m_MaxLevel = 5;
    [SerializeField] private GunUpgradeEffect m_Effect;

    public override string DisplayName => m_DisplayName;
    public override UpgradeTypes GetUpgradeType() => UpgradeTypes.WeaponSynergy;
    public override bool IsEligible(GunArchetype archetype, int currentLevel) =>
        archetype == m_Archetype && currentLevel < m_MaxLevel;

    public override void Init() { }

    public override void ApplyUpgrade()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        EntityQuery query = world.EntityManager.CreateEntityQuery(typeof(GunModifiers));
        if (query.CalculateEntityCount() != 1) { query.Dispose(); return; }
        Entity entity = query.GetSingletonEntity();
        GunModifiers value = world.EntityManager.GetComponentData<GunModifiers>(entity);
        value.DamageBonusPercent += m_Effect.DamagePercent;
        value.FireRateBonusPercent += m_Effect.FireRatePercent;
        value.RangeBonusPercent += m_Effect.RangePercent;
        value.ProjectileSpeedBonusPercent += m_Effect.ProjectileSpeedPercent;
        value.AdditionalProjectiles += m_Effect.Projectiles;
        value.CriticalChanceBonus += m_Effect.CriticalChance;
        value.CriticalDamageBonus += m_Effect.CriticalDamage;
        value.AdditionalPierce += m_Effect.Pierce;
        value.KnockbackBonus += m_Effect.Knockback;
        value.AdditionalMagazineSize += m_Effect.Magazine;
        value.ReloadSpeedBonusPercent += m_Effect.ReloadSpeedPercent;
        value.SpreadReductionPercent += m_Effect.SpreadReductionPercent;
        value.ExplosionRadiusBonusPercent += m_Effect.ExplosionRadiusPercent;
        value.ExplosionDamageBonus += m_Effect.ExplosionDamage;
        value.AdditionalRicochets += m_Effect.Ricochets;
        value.AdditionalChains += m_Effect.Chains;
        value.ElementChanceBonus += m_Effect.ElementChance;
        value.ElementMagnitudeBonusPercent += m_Effect.ElementMagnitudePercent;
        world.EntityManager.SetComponentData(entity, value);
        query.Dispose();
    }

    public override string GetValuePreview(int currentLevel)
    {
        if (!TryGetGunStats(out WeaponManager gun, out GunModifiers modifiers))
            return $"LV {currentLevel} → {currentLevel + 1}";
        if (m_Effect.Projectiles != 0) return $"PROJECTILES  {gun.ProjectileCount} → {gun.ProjectileCount + m_Effect.Projectiles}";
        if (m_Effect.Ricochets != 0) return $"RICOCHET  {gun.RicochetCount} → {gun.RicochetCount + m_Effect.Ricochets}";
        if (m_Effect.Chains != 0) return $"CHAINS  {gun.ChainCount} → {gun.ChainCount + m_Effect.Chains}";
        if (m_Effect.Magazine != 0) return $"MAGAZINE  {gun.MagazineSize} → {gun.MagazineSize + m_Effect.Magazine}";
        if (m_Effect.Pierce != 0) return $"PIERCE  {gun.Pierce} → {gun.Pierce + m_Effect.Pierce}";
        if (m_Effect.CriticalChance != 0f) return $"CRIT  {FormatStat(gun.CriticalChance * 100f)}% → {FormatStat((gun.CriticalChance + m_Effect.CriticalChance) * 100f)}%";
        if (m_Effect.FireRatePercent != 0f) return $"FIRE RATE  {FormatStat(gun.ShotsPerSecond)} → {FormatStat(gun.ShotsPerSecond * (1f + m_Effect.FireRatePercent / 100f))}/s";
        if (m_Effect.DamagePercent != 0f) return $"DMG  {gun.DamagePerHit} → {Mathf.Max(1, Mathf.CeilToInt(gun.DamagePerHit * (1f + m_Effect.DamagePercent / 100f)))}";
        if (m_Effect.ReloadSpeedPercent != 0f) return $"RELOAD  {FormatStat(gun.ReloadDuration)}s → {FormatStat(gun.ReloadDuration / (1f + m_Effect.ReloadSpeedPercent / 100f))}s";
        if (m_Effect.ExplosionRadiusPercent != 0f) return $"BLAST  {FormatStat(gun.ExplosionRadius)} → {FormatStat(gun.ExplosionRadius * (1f + m_Effect.ExplosionRadiusPercent / 100f))}";
        if (m_Effect.ElementChance != 0f) return $"ELEMENT  {FormatStat(gun.ElementChance * 100f)}% → {FormatStat((gun.ElementChance + m_Effect.ElementChance) * 100f)}%";
        return $"LV {currentLevel} → {currentLevel + 1}";
    }

    public void Configure(string displayName, GunArchetype archetype, int maxLevel, GunUpgradeEffect effect)
    {
        m_DisplayName = displayName; m_Archetype = archetype; m_MaxLevel = maxLevel; m_Effect = effect;
    }
}

[System.Serializable]
public struct GunUpgradeEffect
{
    public float DamagePercent, FireRatePercent, RangePercent, ProjectileSpeedPercent;
    public int Projectiles, Pierce, Magazine, Ricochets, Chains;
    public float CriticalChance, CriticalDamage, Knockback, ReloadSpeedPercent, SpreadReductionPercent;
    public float ExplosionRadiusPercent, ExplosionDamage, ElementChance, ElementMagnitudePercent;
}
