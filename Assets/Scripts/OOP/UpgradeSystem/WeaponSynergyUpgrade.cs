using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSynergyUpgrade", menuName = "Settings-Configs/Upgrades/WeaponSynergyUpgrade")]
public class WeaponSynergyUpgrade : CharUpgrade
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
        modifiers.SynergyLevel++;
        m_EntityManager.SetComponentData(entity, modifiers);
    }

    public override UpgradeTypes GetUpgradeType()
    {
        return UpgradeTypes.WeaponSynergy;
    }
}
