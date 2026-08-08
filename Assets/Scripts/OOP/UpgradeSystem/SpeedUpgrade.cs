using UnityEngine;

[CreateAssetMenu(fileName = "SpeedUpgrade", menuName = "Settings-Configs/Upgrades/SpeedUpgrade")]
public class SpeedUpgrade : CharUpgrade
{
    [Range(0, 1)] 
    public float FirstLevelBuff;
    [Tooltip("The value to add to the multiplier per level, for example if value is 0.2 and upgrade level is 2 move speed will increase by 0.4")]
    public float MultiplierPerLevel;
    
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

    public override string GetValuePreview(int currentLevel)
    {
        if (!TryGetGunStats(out WeaponManager gun, out GunModifiers modifiers))
            return base.GetValuePreview(currentLevel);
        float nextSpeed = gun.BaseProjectileSpeed *
            (1f + (modifiers.ProjectileSpeedBonusPercent + MultiplierPerLevel * 100f) / 100f);
        return $"PROJECTILE SPEED  {FormatStat(gun.ProjectileSpeed)} → {FormatStat(nextSpeed)}";
    }
    
}
