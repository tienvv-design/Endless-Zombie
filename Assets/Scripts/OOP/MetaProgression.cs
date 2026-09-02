using System;
using UnityEngine;

public enum StageUpgradeType
{
    Health,
    Income
}

public readonly struct StageUpgradeSnapshot
{
    public readonly int Level;
    public readonly int Progress;
    public readonly bool IsBreakthrough;
    public readonly int Cost;
    public readonly float CurrentValue;
    public readonly float NextValue;

    public StageUpgradeSnapshot(int purchases, int cost, float currentValue, float nextValue)
    {
        Level = purchases + 1;
        Progress = purchases % MetaProgression.PurchasesPerRank;
        IsBreakthrough = Progress == MetaProgression.SmallPurchasesPerRank;
        Cost = cost;
        CurrentValue = currentValue;
        NextValue = nextValue;
    }
}

public static class MetaProgression
{
    public const int PurchasesPerRank = 10;
    public const int SmallPurchasesPerRank = 9;
    public const float HealthMarker = 10f;

    private const string ActiveStageKey = "StageUpgrade.ActiveStage";
    private const string WeaponKey = "Meta.SelectedWeapon";
    private const string SelectedWeaponIdKey = "Meta.SelectedWeaponId";
    private const string WeaponMigrationKey = "Meta.WeaponIdMigrationV1";
    private static string s_StageId = "Stage1";
    private static GunConfig[] s_WeaponCatalog;

    // Original scene order, used once to preserve existing unlocks after the catalog is reordered.
    private static readonly string[] LegacyWeaponIds =
    {
        "pistol", "shotgun", "assault_rifle", "rocket_launcher", "tesla_gun", "flame_rifle",
        "cryo_gun", "ldoe_mp5k", "ldoe_winchester_mercenary", "ldoe_m32", "ldoe_minigun"
    };

    public static event Action UpgradesChanged;
    public static event Action<int> SelectedWeaponChanged;
    public static int HealthPurchases => GetPurchases(StageUpgradeType.Health);
    public static int IncomePurchases => GetPurchases(StageUpgradeType.Income);
    public static float HealthBonus => HealthValue(HealthPurchases) - HealthMarker;
    public static float IncomeMultiplier => IncomeValue(IncomePurchases);
    public static int SelectedWeapon
    {
        get
        {
            string selectedId = PlayerPrefs.GetString(SelectedWeaponIdKey, string.Empty);
            int resolved = FindWeaponIndex(selectedId);
            return resolved >= 0 ? resolved : Mathf.Max(0, PlayerPrefs.GetInt(WeaponKey, 0));
        }
    }

    public static void ConfigureWeapons(GunConfig[] configs)
    {
        s_WeaponCatalog = configs;
        if (PlayerPrefs.GetInt(WeaponMigrationKey, 0) == 0)
        {
            int legacySelected = Mathf.Clamp(PlayerPrefs.GetInt(WeaponKey, 0), 0, LegacyWeaponIds.Length - 1);
            PlayerPrefs.SetString(SelectedWeaponIdKey, LegacyWeaponIds[legacySelected]);
            for (int i = 1; i < LegacyWeaponIds.Length; i++)
            {
                if (PlayerPrefs.GetInt($"Meta.Weapon.{i}", 0) == 1)
                    PlayerPrefs.SetInt(WeaponIdUnlockKey(LegacyWeaponIds[i]), 1);
            }
            PlayerPrefs.SetInt(WeaponMigrationKey, 1);
        }

        int selectedIndex = FindWeaponIndex(PlayerPrefs.GetString(SelectedWeaponIdKey, string.Empty));
        if (selectedIndex < 0) selectedIndex = 0;
        PlayerPrefs.SetInt(WeaponKey, selectedIndex);
        PlayerPrefs.Save();
    }

    public static void BeginStageSession(string stageId)
    {
        s_StageId = string.IsNullOrWhiteSpace(stageId) ? "Stage1" : stageId.Trim();
        string previousStage = PlayerPrefs.GetString(ActiveStageKey, string.Empty);
        if (!string.IsNullOrEmpty(previousStage) && previousStage != s_StageId)
        {
            ClearStage(previousStage);
            ClearStage(s_StageId);
        }

        PlayerPrefs.SetString(ActiveStageKey, s_StageId);
        PlayerPrefs.Save();
        UpgradesChanged?.Invoke();
    }

    public static StageUpgradeSnapshot GetSnapshot(StageUpgradeType type)
    {
        int purchases = GetPurchases(type);
        float current = type == StageUpgradeType.Health ? HealthValue(purchases) : IncomeValue(purchases);
        float next = type == StageUpgradeType.Health ? HealthValue(purchases + 1) : IncomeValue(purchases + 1);
        return new StageUpgradeSnapshot(purchases, UpgradeCost(purchases), current, next);
    }

    public static bool TryPurchase(StageUpgradeType type)
    {
        int purchases = GetPurchases(type);
        if (GoldWallet.Instance == null || !GoldWallet.Instance.TrySpend(UpgradeCost(purchases)))
            return false;

        PlayerPrefs.SetInt(PurchaseKey(s_StageId, type), purchases + 1);
        PlayerPrefs.Save();
        UpgradesChanged?.Invoke();
        return true;
    }

    public static void CompleteCurrentStage()
    {
        ClearStage(s_StageId);
        UpgradesChanged?.Invoke();
    }

    public static int UpgradeCost(int purchases) => Mathf.Max(1, Mathf.RoundToInt(20f * Mathf.Pow(1.2f, purchases)));

    public static float HealthValue(int purchases)
    {
        int rank = purchases / PurchasesPerRank;
        int progress = purchases % PurchasesPerRank;
        return HealthMarker * Mathf.Pow(5f, rank) * (1f + 0.25f * progress);
    }

    public static float IncomeValue(int purchases)
    {
        int rank = purchases / PurchasesPerRank;
        int progress = purchases % PurchasesPerRank;
        return Mathf.Pow(2f, rank) * (1f + 0.05f * progress);
    }

    private static int GetPurchases(StageUpgradeType type) => PlayerPrefs.GetInt(PurchaseKey(s_StageId, type), 0);
    private static string PurchaseKey(string stageId, StageUpgradeType type) => $"StageUpgrade.{stageId}.{type}";

    private static void ClearStage(string stageId)
    {
        PlayerPrefs.DeleteKey(PurchaseKey(stageId, StageUpgradeType.Health));
        PlayerPrefs.DeleteKey(PurchaseKey(stageId, StageUpgradeType.Income));
        PlayerPrefs.Save();
    }

    public static int WeaponCost(int weaponIndex) => WeaponCost(weaponIndex, GetWeapon(weaponIndex));

    public static int WeaponCost(int weaponIndex, GunConfig gun) =>
        weaponIndex == 0 ? 0 : gun != null && gun.UnlockCost > 0 ? gun.UnlockCost : 150 * weaponIndex;

    public static bool IsWeaponUnlocked(int index)
    {
        if (index == 0) return true;
        GunConfig gun = GetWeapon(index);
        return gun != null && !string.IsNullOrWhiteSpace(gun.WeaponId)
            ? PlayerPrefs.GetInt(WeaponIdUnlockKey(gun.WeaponId), 0) == 1
            : PlayerPrefs.GetInt($"Meta.Weapon.{index}", 0) == 1;
    }

    public static bool BuyOrSelectWeapon(int index)
    {
        return BuyOrSelectWeapon(index, GetWeapon(index));
    }

    public static bool BuyOrSelectWeapon(int index, GunConfig gun)
    {
        if (!IsWeaponUnlocked(index))
        {
            if (GoldWallet.Instance == null || !GoldWallet.Instance.TrySpend(WeaponCost(index, gun))) return false;
            PlayerPrefs.SetInt($"Meta.Weapon.{index}", 1);
            if (gun != null && !string.IsNullOrWhiteSpace(gun.WeaponId))
                PlayerPrefs.SetInt(WeaponIdUnlockKey(gun.WeaponId), 1);
        }
        PlayerPrefs.SetInt(WeaponKey, index);
        if (gun != null && !string.IsNullOrWhiteSpace(gun.WeaponId))
            PlayerPrefs.SetString(SelectedWeaponIdKey, gun.WeaponId);
        PlayerPrefs.Save();
        SelectedWeaponChanged?.Invoke(index);
        return true;
    }

    private static GunConfig GetWeapon(int index) =>
        s_WeaponCatalog != null && index >= 0 && index < s_WeaponCatalog.Length ? s_WeaponCatalog[index] : null;

    private static int FindWeaponIndex(string weaponId)
    {
        if (s_WeaponCatalog == null || string.IsNullOrWhiteSpace(weaponId)) return -1;
        for (int i = 0; i < s_WeaponCatalog.Length; i++)
        {
            GunConfig gun = s_WeaponCatalog[i];
            if (gun != null && string.Equals(gun.WeaponId, weaponId, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static string WeaponIdUnlockKey(string weaponId) => $"Meta.WeaponId.{weaponId}";
}
