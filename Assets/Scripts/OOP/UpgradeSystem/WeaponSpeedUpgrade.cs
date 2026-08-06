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
    }
}
