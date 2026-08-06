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
        return UpgradeTypes.Speed;
    }

    public override void Init() { }

    public override void ApplyUpgrade()
    {
        var playerCharacter = GameObject.FindGameObjectWithTag("Player");

        if (playerCharacter.TryGetComponent(out CharacterLogic characterLogic))
        {
            if (!characterLogic.CharacterStats)
            {
                Debug.Log("Say something!");
            }
            characterLogic.CharacterStats.MoveSpeed += characterLogic.CharacterStats.MoveSpeed * MultiplierPerLevel * m_UpgradeLevel;   
        }
        else
        {
            Debug.LogWarning("Couldn't find player character");
        }
    }
    
}