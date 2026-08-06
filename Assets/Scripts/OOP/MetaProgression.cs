using UnityEngine;

public static class MetaProgression
{
    private const string HealthLevelKey = "Meta.HealthLevel";
    private const string ArmorLevelKey = "Meta.ArmorLevel";
    private const string WeaponKey = "Meta.SelectedWeapon";

    public static int HealthLevel => PlayerPrefs.GetInt(HealthLevelKey, 0);
    public static int ArmorLevel => PlayerPrefs.GetInt(ArmorLevelKey, 0);
    public static int HealthBonus => HealthLevel * 10;
    public static int Armor => ArmorLevel;
    public static int SelectedWeapon => PlayerPrefs.GetInt(WeaponKey, 0);
    public static int HealthCost => 50 + HealthLevel * 35;
    public static int ArmorCost => 75 + ArmorLevel * 50;

    public static bool BuyHealth()
    {
        if (!GoldWallet.Instance || !GoldWallet.Instance.TrySpend(HealthCost)) return false;
        PlayerPrefs.SetInt(HealthLevelKey, HealthLevel + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool BuyArmor()
    {
        if (!GoldWallet.Instance || !GoldWallet.Instance.TrySpend(ArmorCost)) return false;
        PlayerPrefs.SetInt(ArmorLevelKey, ArmorLevel + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static int WeaponCost(int weaponIndex) => weaponIndex == 0 ? 0 : 150 * weaponIndex;
    public static bool IsWeaponUnlocked(int index) => index == 0 || PlayerPrefs.GetInt($"Meta.Weapon.{index}", 0) == 1;

    public static bool BuyOrSelectWeapon(int index)
    {
        if (!IsWeaponUnlocked(index))
        {
            if (!GoldWallet.Instance || !GoldWallet.Instance.TrySpend(WeaponCost(index))) return false;
            PlayerPrefs.SetInt($"Meta.Weapon.{index}", 1);
        }
        PlayerPrefs.SetInt(WeaponKey, index);
        PlayerPrefs.Save();
        return true;
    }
}

public class MainMenuMetaShop : MonoBehaviour
{
    private readonly string[] m_Weapons = { "Pistol", "Shotgun", "Assault Rifle", "Rocket Launcher" };
    private GUIStyle m_Title;
    private GUIStyle m_Label;

    private void OnGUI()
    {
        m_Title ??= new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        m_Label ??= new GUIStyle(GUI.skin.label) { fontSize = 17 };
        Rect safe = Screen.safeArea;
        float panelWidth = Mathf.Min(420f, safe.width - 24f);
        float guiSafeTop = Screen.height - safe.yMax;
        Rect panel = new Rect(safe.xMax - panelWidth - 12f, guiSafeTop + 12f, panelWidth, Mathf.Min(500f, safe.height - 24f));
        GUI.Box(panel, GUIContent.none);
        GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, panel.height - 24f));
        GUILayout.Label("ARMORY & SURVIVOR", m_Title, GUILayout.Height(38f));
        GUILayout.Label($"Gold: {(GoldWallet.Instance ? GoldWallet.Instance.Balance : 0)}", m_Label);
        if (GUILayout.Button($"Upgrade Health  +10 HP   ({MetaProgression.HealthCost} Gold)", GUILayout.Height(48f)))
            MetaProgression.BuyHealth();
        if (GUILayout.Button($"Upgrade Armor  +1 reduction   ({MetaProgression.ArmorCost} Gold)", GUILayout.Height(48f)))
            MetaProgression.BuyArmor();
        GUILayout.Label($"Health bonus: +{MetaProgression.HealthBonus}   Armor: {MetaProgression.Armor}", m_Label);
        GUILayout.Space(8f);
        GUILayout.Label("Weapons", m_Label);
        for (int i = 0; i < m_Weapons.Length; i++)
        {
            bool unlocked = MetaProgression.IsWeaponUnlocked(i);
            string state = MetaProgression.SelectedWeapon == i ? "EQUIPPED" : unlocked ? "Select" : $"Buy {MetaProgression.WeaponCost(i)} Gold";
            if (GUILayout.Button($"{m_Weapons[i]}  -  {state}", GUILayout.Height(44f)))
                MetaProgression.BuyOrSelectWeapon(i);
        }
        GUILayout.EndArea();
    }
}
