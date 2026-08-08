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
    private static string s_StageId = "Stage1";

    public static event Action UpgradesChanged;
    public static event Action<int> SelectedWeaponChanged;
    public static int HealthPurchases => GetPurchases(StageUpgradeType.Health);
    public static int IncomePurchases => GetPurchases(StageUpgradeType.Income);
    public static float HealthBonus => HealthValue(HealthPurchases) - HealthMarker;
    public static float IncomeMultiplier => IncomeValue(IncomePurchases);
    public static int SelectedWeapon => PlayerPrefs.GetInt(WeaponKey, 0);

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

    public static int WeaponCost(int weaponIndex) => weaponIndex == 0 ? 0 : 150 * weaponIndex;
    public static bool IsWeaponUnlocked(int index) => index == 0 || PlayerPrefs.GetInt($"Meta.Weapon.{index}", 0) == 1;

    public static bool BuyOrSelectWeapon(int index)
    {
        if (!IsWeaponUnlocked(index))
        {
            if (GoldWallet.Instance == null || !GoldWallet.Instance.TrySpend(WeaponCost(index))) return false;
            PlayerPrefs.SetInt($"Meta.Weapon.{index}", 1);
        }
        PlayerPrefs.SetInt(WeaponKey, index);
        PlayerPrefs.Save();
        SelectedWeaponChanged?.Invoke(index);
        return true;
    }
}
