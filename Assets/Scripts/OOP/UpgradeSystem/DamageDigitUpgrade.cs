using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageDigitUpgrade", menuName = "Settings-Configs/Upgrades/DamageDigitUpgrade")]
public class DamageDigitUpgrade : CharUpgrade
{
    [SerializeField, Min(0.1f)] private float m_RangeIncrease = 1f;
    
    private EntityManager m_EntityManager;

    public override void Init()
    {
        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    
    public override UpgradeTypes GetUpgradeType()
    {
        return UpgradeTypes.AttackRange;
    }

    public override void ApplyUpgrade()
    {
        Entity entity = m_EntityManager.CreateEntityQuery(typeof(GunModifiers)).GetSingletonEntity();
        GunModifiers modifiers = m_EntityManager.GetComponentData<GunModifiers>(entity);
        modifiers.RangeBonusPercent += m_RangeIncrease * 10f;
        m_EntityManager.SetComponentData(entity, modifiers);
    }

    public override string GetValuePreview(int currentLevel)
    {
        if (!TryGetGunStats(out WeaponManager gun, out GunModifiers modifiers))
            return base.GetValuePreview(currentLevel);
        float nextRange = gun.BaseAttackRange *
            (1f + (modifiers.RangeBonusPercent + m_RangeIncrease * 10f) / 100f);
        return $"RANGE  {FormatStat(gun.AttackRange)} → {FormatStat(nextRange)}";
    }
}
