using UnityEditor;
using UnityEngine;

public sealed class GoldCheatWindow : EditorWindow
{
    private const string BalanceKey = "Meta.Gold";

    [SerializeField] private int customAmount = 10_000;
    [SerializeField] private int setBalance = 1_000_000;
    private double nextRepaint;

    [MenuItem("Tools/Endless Zombie/Gold Cheat Tool")]
    public static void Open()
    {
        GoldCheatWindow window = GetWindow<GoldCheatWindow>("Gold Cheat");
        window.minSize = new Vector2(360f, 330f);
        window.Show();
    }

    private void OnEnable() => EditorApplication.update += Refresh;
    private void OnDisable() => EditorApplication.update -= Refresh;

    private void Refresh()
    {
        if (EditorApplication.timeSinceStartup < nextRepaint)
            return;
        nextRepaint = EditorApplication.timeSinceStartup + 0.25d;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("GOLD CHEAT TOOL", new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
        });
        EditorGUILayout.HelpBox(
            "Editor-only debug tool. Meta Gold is saved immediately; Run Gold only exists during Play Mode.",
            MessageType.Info);

        DrawMetaGold();
        EditorGUILayout.Space(8f);
        DrawRunGold();
    }

    private void DrawMetaGold()
    {
        EditorGUILayout.LabelField("META GOLD", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Current Balance", ReadMetaBalance().ToString("N0"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+1K")) AddMetaGold(1_000);
                if (GUILayout.Button("+10K")) AddMetaGold(10_000);
                if (GUILayout.Button("+100K")) AddMetaGold(100_000);
                if (GUILayout.Button("+1M")) AddMetaGold(1_000_000);
            }

            customAmount = Mathf.Max(0, EditorGUILayout.IntField("Custom Add", customAmount));
            if (GUILayout.Button("Add Custom Gold"))
                AddMetaGold(customAmount);

            setBalance = Mathf.Max(0, EditorGUILayout.IntField("Set Balance", setBalance));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Set Exact Balance"))
                    SetMetaBalance(setBalance);
                if (GUILayout.Button("Reset To 0"))
                    SetMetaBalance(0);
            }
        }
    }

    private static void DrawRunGold()
    {
        EditorGUILayout.LabelField("RUN GOLD", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GoldWallet wallet = GoldWallet.Instance;
            if (!EditorApplication.isPlaying || wallet == null)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to modify Run Gold.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Current Run Reward", wallet.RunReward.ToString("N0"));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+100 Run")) wallet.AddRunReward(100);
                if (GUILayout.Button("+1K Run")) wallet.AddRunReward(1_000);
                if (GUILayout.Button("+10K Run")) wallet.AddRunReward(10_000);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Bank Now")) wallet.BankRunReward();
                if (GUILayout.Button("Reset Run")) wallet.ResetRunReward();
            }
        }
    }

    private static int ReadMetaBalance()
    {
        return EditorApplication.isPlaying && GoldWallet.Instance != null
            ? GoldWallet.Instance.Balance
            : PlayerPrefs.GetInt(BalanceKey, 0);
    }

    private static void AddMetaGold(int amount)
    {
        if (amount <= 0)
            return;

        long target = (long)ReadMetaBalance() + amount;
        SetMetaBalance(target > int.MaxValue ? int.MaxValue : (int)target);
    }

    private static void SetMetaBalance(int balance)
    {
        balance = Mathf.Max(0, balance);
        GoldWallet wallet = GoldWallet.Instance;
        if (EditorApplication.isPlaying && wallet != null)
        {
            int difference = balance - wallet.Balance;
            if (difference > 0)
                wallet.Add(difference);
            else if (difference < 0)
                wallet.TrySpend(-difference);
        }
        else
        {
            PlayerPrefs.SetInt(BalanceKey, balance);
            PlayerPrefs.Save();
        }

        Debug.Log($"Gold cheat set Meta Gold to {balance:N0}.");
    }
}
