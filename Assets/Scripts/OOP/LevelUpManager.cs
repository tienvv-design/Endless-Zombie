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
        GunArchetype activeArchetype = GetActiveArchetype();
        List<CharUpgrade> genericUpgrades = new();
        List<CharUpgrade> weaponUpgrades = new();
        AddEligible(m_UpgradeAssets, activeArchetype, genericUpgrades);
        AddEligible(Resources.LoadAll<CharUpgrade>("WeaponUpgrades"), activeArchetype, weaponUpgrades);
        m_CurrentUpgrades.Clear();

        // Make the level-up screen reflect the equipped weapon: two choices are
        // guaranteed to come from its own pool whenever enough are available.
        int weaponChoices = Mathf.Min(2, weaponUpgrades.Count);
        for (int i = 0; i < weaponChoices; i++)
        {
            AddWeightedChoice(weaponUpgrades);
        }

        genericUpgrades.AddRange(weaponUpgrades);
        while (m_CurrentUpgrades.Count < 3 && genericUpgrades.Count > 0)
            AddWeightedChoice(genericUpgrades);

        Debug.Log("Set random upgrades");
        OnUpgradesAssigned?.Invoke(new List<CharUpgrade>(m_CurrentUpgrades));
    }

    private void AddEligible(IEnumerable<CharUpgrade> source, GunArchetype archetype, List<CharUpgrade> target)
    {
        if (source == null) return;
        foreach (CharUpgrade upgrade in source)
            if (upgrade != null && upgrade.GetUpgradeType() != UpgradeTypes.ProjectileSpeed &&
                !target.Contains(upgrade) && upgrade.IsEligible(archetype, GetUpgradeLevel(upgrade)))
                target.Add(upgrade);
    }

    private void AddWeightedChoice(List<CharUpgrade> candidates)
    {
        int randomIndex = GetWeightedRandomIndex(candidates);
        CharUpgrade selectedUpgrade = candidates[randomIndex];
        selectedUpgrade.Init();
        m_CurrentUpgrades.Add(selectedUpgrade);
        candidates.RemoveAt(randomIndex);
    }

    private static GunArchetype GetActiveArchetype()
    {
        if (CharUpgrade.TryGetActiveGunArchetype(out GunArchetype archetype)) return archetype;
        return Mathf.Clamp(MetaProgression.SelectedWeapon, 0, 3) switch
        {
            1 => GunArchetype.Shotgun,
            2 => GunArchetype.AssaultRifle,
            3 => GunArchetype.RocketLauncher,
            _ => GunArchetype.Pistol,
        };
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
