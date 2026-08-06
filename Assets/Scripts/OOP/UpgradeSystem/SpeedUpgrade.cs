using UnityEngine;

[CreateAssetMenu(fileName = "SpeedUpgrade", menuName = "Settings-Configs/Upgrades/SpeedUpgrade")]
public class SpeedUpgrade : CharUpgrade
{
    [Range(0, 1)] 
    public float FirstLevelBuff;
    [Tooltip("The value to add to the multiplier per level, for example if value is 0.2 and upgrade level is 2 move speed will increase by 0.4")]
    public float MultiplierPerLevel;
    
    private int m_UpgradeLevel = 1; 
    
    public override UpgradeTypes GetUpgradeType()
    {
        return UpgradeTypes.ProjectileSpeed;
    }

    public override void Init() { }

    public override void ApplyUpgrade()
    {
        Unity.Entities.EntityManager entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
        Unity.Entities.Entity entity = entityManager.CreateEntityQuery(typeof(GunModifiers)).GetSingletonEntity();
        GunModifiers modifiers = entityManager.GetComponentData<GunModifiers>(entity);
        modifiers.ProjectileSpeedBonusPercent += MultiplierPerLevel * 100f;
        entityManager.SetComponentData(entity, modifiers);
    }
    
}
