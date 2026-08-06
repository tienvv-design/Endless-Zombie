using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelUpManager : MonoBehaviour, IGameLevelUp
{
    public Action<List<CharUpgrade>> OnUpgradesAssigned;
    public Action<CharUpgrade> OnUpgradeApplied;
    
    public static LevelUpManager Instance;
    
    [SerializeField] private List<CharUpgrade> m_UpgradeAssets;
    private List<CharUpgrade> m_CurrentUpgrades = new();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        m_UpgradeAssets[0].ApplyUpgrade();
    }

    public void UpgradeChosenCallback(CharUpgrade upgrade)
    {
        OnUpgradeApplied?.Invoke(upgrade);
        upgrade.ApplyUpgrade();
    }

    public void SetRandomUpgrades()
    {
        List<CharUpgrade> charUpgradesCopy = new List<CharUpgrade>(m_UpgradeAssets);
        m_CurrentUpgrades.Clear();

        int numberOfUpgrades = Mathf.Clamp(3, 0, charUpgradesCopy.Count);
        
        for (int i = 0; i < numberOfUpgrades; i++)
        {
            int randomIndex = Random.Range(0, charUpgradesCopy.Count);
            charUpgradesCopy[randomIndex].Init();
            m_CurrentUpgrades.Add(charUpgradesCopy[randomIndex]);
            charUpgradesCopy.RemoveAt(randomIndex);
        }

        Debug.Log("Set random upgrades");
        OnUpgradesAssigned?.Invoke(new List<CharUpgrade>(m_CurrentUpgrades));
    }

    // public List<CharUpgrade> GetUpgrades()
    // {
    //     return new List<CharUpgrade>(m_CurrentUpgrades);
    // }

    public void OnStateEnable()
    {
        SetRandomUpgrades();
    }

    public void OnStateDisable() { }
}
