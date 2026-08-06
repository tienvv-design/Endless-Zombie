using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public struct LevelDefinition
{
    public int LevelNumber;
    public int NeededXPtoComplete;
}

public struct XPGainEventInfo
{
    public int CurrentLevel;
    public int CurrentXP;
    public int GainedXP;
    public int NextLevelRequiredXP;
}

public class CharacterXPManager : MonoBehaviour
{
    public static CharacterXPManager Instance;
    
    [SerializeField] private XPSettings m_XPSettingsAsset;
    private XPSettings m_XPSettings;

    public Action<XPGainEventInfo> OnXPGain;
    public Action<int> OnLevelUp;
    
    private int m_CurrentXP;
    private int m_CharacterLevel;
    
    public int CharacterLevel => m_CharacterLevel;

    private Dictionary<int, LevelDefinition> m_levelDefinitions = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Can not have multiple instances of a singleton class.");
            Destroy(this);
        }
        
        m_XPSettings = Instantiate(m_XPSettingsAsset);
    }
    
    public int GetXPRequiredForLevel(int level)
    {
        // Simple quadratic progression (customize as needed)
        float firsLvlXP = m_XPSettings.FirstLevelNeededXP;
        float quadScale = m_XPSettings.XpGainMultiplier;
        return (int)(firsLvlXP * Mathf.Pow(level, quadScale));
    }

    public void GainXP(int amount)
    {
        // Debug.Log("Gained XP!");
        
        if (amount <= 0)
        {
            Debug.LogWarning("Tried to add non-positive XP.");
            return;
        }

        m_CurrentXP += amount;

        // Handle multiple level-ups in one XP gain
        // while (m_CurrentXP >= GetXPRequiredForLevel(m_CharacterLevel + 1))
        // {
        //     m_CurrentXP -= GetXPRequiredForLevel(m_CharacterLevel + 1);
        //     m_CharacterLevel++;
        //     OnLevelUp?.Invoke(m_CharacterLevel);
        // }
        
        while (true)
        {
            int requiredXP = GetXPRequiredForLevel(m_CharacterLevel + 1);
            if (requiredXP <= 0 || m_CurrentXP < requiredXP)
                break;
    
            m_CurrentXP -= requiredXP;
            m_CharacterLevel++;
            OnLevelUp?.Invoke(m_CharacterLevel);
        }
        
        OnXPGain?.Invoke(new XPGainEventInfo
        {
            CurrentLevel = m_CharacterLevel,
            CurrentXP = m_CurrentXP,
            GainedXP = amount,
            NextLevelRequiredXP = GetXPRequiredForLevel(m_CharacterLevel + 1)
        });
    }
}
