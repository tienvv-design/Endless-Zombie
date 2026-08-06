using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDamageUpgrade", menuName = "Settings-Configs/Upgrades/WeaponDamageUpgrade")]
public class WeaponDamageUpgrade : CharUpgrade
{
    private EntityManager m_EntityManager;

    public override void Init()
    {
        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    
    public override UpgradeTypes GetUpgradeType()
    {
        return UpgradeTypes.WeaponDamage;
    }

    public override void ApplyUpgrade()
    {
        Entity entity = m_EntityManager.CreateEntityQuery(typeof(GunModifiers)).GetSingletonEntity();
        GunModifiers modifiers = m_EntityManager.GetComponentData<GunModifiers>(entity);
        modifiers.DamageBonusPercent += 20f;
        m_EntityManager.SetComponentData(entity, modifiers);
    }
}
