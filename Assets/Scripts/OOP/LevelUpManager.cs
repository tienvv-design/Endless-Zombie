using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using OOP.GameStates;

public class LevelUpManager : MonoBehaviour, IGameLevelUp
{
    public Action<List<CharUpgrade>> OnUpgradesAssigned;
    public Action<CharUpgrade> OnUpgradeApplied;
    
    public static LevelUpManager Instance;
    
    [SerializeField] private List<CharUpgrade> m_UpgradeAssets;
    private List<CharUpgrade> m_CurrentUpgrades = new();
    private readonly Dictionary<CharUpgrade, int> m_UpgradeLevels = new();
    
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

    public void UpgradeChosenCallback(CharUpgrade upgrade)
    {
        if (upgrade == null) return;
        upgrade.ApplyUpgrade();
        m_UpgradeLevels[upgrade] = GetUpgradeLevel(upgrade) + 1;
        OnUpgradeApplied?.Invoke(upgrade);
    }

    public void SetRandomUpgrades()
    {
        List<CharUpgrade> charUpgradesCopy = new List<CharUpgrade>(m_UpgradeAssets);
        m_CurrentUpgrades.Clear();

        int numberOfUpgrades = Mathf.Clamp(3, 0, charUpgradesCopy.Count);
        
        for (int i = 0; i < numberOfUpgrades; i++)
        {
            int randomIndex = GetWeightedRandomIndex(charUpgradesCopy);
            CharUpgrade selectedUpgrade = charUpgradesCopy[randomIndex];
            selectedUpgrade.Init();
            m_CurrentUpgrades.Add(selectedUpgrade);
            charUpgradesCopy.RemoveAt(randomIndex);
        }

        Debug.Log("Set random upgrades");
        OnUpgradesAssigned?.Invoke(new List<CharUpgrade>(m_CurrentUpgrades));
    }

    private static int GetWeightedRandomIndex(IReadOnlyList<CharUpgrade> upgrades)
    {
        float totalWeight = 0f;
        for (int i = 0; i < upgrades.Count; i++)
            if (upgrades[i] != null)
                totalWeight += upgrades[i].RollWeight;

        if (totalWeight <= 0f)
            return Random.Range(0, upgrades.Count);

        float roll = Random.value * totalWeight;
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i] == null)
                continue;

            roll -= upgrades[i].RollWeight;
            if (roll <= 0f)
                return i;
        }

        return upgrades.Count - 1;
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

    public int GetUpgradeLevel(CharUpgrade upgrade)
    {
        return upgrade != null && m_UpgradeLevels.TryGetValue(upgrade, out int level) ? level : 0;
    }

    public int GetUpgradeCost(CharUpgrade upgrade)
    {
        return upgrade == null ? int.MaxValue : upgrade.GetCost(GetUpgradeLevel(upgrade));
    }

    public bool CanAfford(CharUpgrade upgrade)
    {
        return upgrade != null;
    }
}
