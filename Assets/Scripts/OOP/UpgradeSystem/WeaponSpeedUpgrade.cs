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
            Entity weaponManagerEntity = m_EntityManager.CreateEntityQuery(typeof(WeaponManager)).GetSingletonEntity();
            WeaponManager weaponManager = m_EntityManager.GetComponentData<WeaponManager>(weaponManagerEntity);

            weaponManager.RotateSpeed += 1;
        
            m_EntityManager.SetComponentData(weaponManagerEntity, weaponManager);
        }

        public override UpgradeTypes GetUpgradeType()
        {
            return UpgradeTypes.WeaponSpeed;
        }
    }
}