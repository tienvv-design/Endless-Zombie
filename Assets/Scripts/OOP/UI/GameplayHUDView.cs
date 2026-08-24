using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayHUDView : MonoBehaviour
{
    public RectTransform BottomCombatPanel;
    public Image HealthFill;
    public Image XPFill;
    public TMP_Text HealthText;
    public TMP_Text LevelText;
    public TMP_Text GoldText;
    public TMP_Text WaveText;
    public TMP_Text KillProgressText;
    public TMP_Text AmmoText;
    public Image AmmoIcon;
    public Button SettingsButton;
    public TMP_Text[] WeaponStatValues = new TMP_Text[4];

    public void CaptureReferences()
    {
        BottomCombatPanel = transform.Find("BottomCombatHUD") as RectTransform;
        HealthFill = FindComponent<Image>("BottomCombatHUD/HealthBar/Fill");
        XPFill = FindComponent<Image>("BottomCombatHUD/XPBar/Fill");
        HealthText = FindComponent<TMP_Text>("BottomCombatHUD/HealthValue");
        LevelText = FindComponent<TMP_Text>("BottomCombatHUD/PlayerLevel");
        GoldText = FindComponent<TMP_Text>("GoldCounter");
        WaveText = FindComponent<TMP_Text>("WaveStatus");
        KillProgressText = FindComponent<TMP_Text>("KillProgress");
        AmmoText = FindComponent<TMP_Text>("AmmoDisplay/AmmoCount");
        AmmoIcon = FindComponent<Image>("AmmoDisplay/AmmoIcon");
        SettingsButton = FindComponent<Button>("SettingButton");

        RemoveRangeUIAndReflow();
        string[] names = { "DamageValue", "FireRateValue", "CritChanceValue", "CritDamageValue" };
        if (WeaponStatValues == null || WeaponStatValues.Length != names.Length)
            WeaponStatValues = new TMP_Text[names.Length];
        for (int i = 0; i < names.Length; i++)
            WeaponStatValues[i] = FindComponent<TMP_Text>("BottomCombatHUD/" + names[i]);
    }

    private void RemoveRangeUIAndReflow()
    {
        Transform panel = transform.Find("BottomCombatHUD");
        if (panel == null) return;
        foreach (string item in new[] { "Range", "RangeIcon", "RangeValue" })
        {
            Transform rangeItem = panel.Find(item);
            if (rangeItem != null) rangeItem.gameObject.SetActive(false);
        }

        string[][] columns =
        {
            new[] { "Damage", "DamageIcon", "DamageValue" },
            new[] { "FireRate", "FireRateIcon", "FireRateValue" },
            new[] { "CritChance", "CritChanceIcon", "CritChanceValue" },
            new[] { "CritDamage", "CritDamageIcon", "CritDamageValue" },
        };
        // Reuse the original stats strip width (roughly x=35..396) and divide
        // it evenly between the four remaining stats.
        float left = 35f;
        float spacing = 120f;
        for (int column = 0; column < columns.Length; column++)
        foreach (string item in columns[column])
        {
            if (panel.Find(item) is not RectTransform rect) continue;
            Vector2 position = rect.anchoredPosition;
            position.x = left + spacing * column;
            rect.anchoredPosition = position;
        }
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform item = transform.Find(path);
        return item != null ? item.GetComponent<T>() : null;
    }
}
