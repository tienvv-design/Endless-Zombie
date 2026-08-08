using Unity.Entities;
using UnityEngine;

namespace OOP.UpgradeSystem
{
    [CreateAssetMenu(fileName = "WeaponSpeedUpgrade", menuName = "Settings-Configs/Upgrades/WeaponSpeedUpgrade")]
    public class WeaponSpeedUpgrade : CharUpgrade
    {
        private EntityManager m_EntityManager;

        public override void Init()
        {
            m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public override void ApplyUpgrade()
        {
            Entity entity = m_EntityManager.CreateEntityQuery(typeof(GunModifiers)).GetSingletonEntity();
            GunModifiers modifiers = m_EntityManager.GetComponentData<GunModifiers>(entity);
            modifiers.FireRateBonusPercent += 15f;
            m_EntityManager.SetComponentData(entity, modifiers);
        }

        public override UpgradeTypes GetUpgradeType()
        {
            return UpgradeTypes.WeaponSpeed;
        }

        public override string GetValuePreview(int currentLevel)
        {
            if (!TryGetGunStats(out WeaponManager gun, out GunModifiers modifiers))
                return base.GetValuePreview(currentLevel);
            float nextBonus = modifiers.FireRateBonusPercent + 15f + Mathf.Max(0, modifiers.SynergyLevel) * 5f;
            float nextValue = gun.BaseShotsPerSecond * (1f + nextBonus / 100f);
            return $"FIRE RATE  {FormatStat(gun.ShotsPerSecond)} → {FormatStat(nextValue)} /s";
        }
    }
}
