using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageDigitUpgrade", menuName = "Settings-Configs/Upgrades/DamageDigitUpgrade")]
public class DamageDigitUpgrade : CharUpgrade
{
    [Header("The ratio of increase in the possibility of a damage digit turning into a bomb")]
    [SerializeField] private float m_ExplosionChangeIncrease;
    
    private EntityManager m_EntityManager;

    public override void Init()
    {
        m_EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }
    
    public override UpgradeTypes GetUpgradeType()
    {
        return UpgradeTypes.DigitBomb;
    }

    public override void ApplyUpgrade()
    {
        Entity characterStatsEntity = m_EntityManager.CreateEntityQuery(typeof(CharacterStatsComponent)).GetSingletonEntity();
        CharacterStatsComponent characterStats = m_EntityManager.GetComponentData<CharacterStatsComponent>(characterStatsEntity);

        float increaseAmount = characterStats.DamageDigitExplosionChance * m_ExplosionChangeIncrease;
        characterStats.DamageDigitExplosionChance += increaseAmount;
        Debug.Log("Chance is: " + characterStats.DamageDigitExplosionChance);
        
        m_EntityManager.SetComponentData(characterStatsEntity, characterStats);
    }
}
    