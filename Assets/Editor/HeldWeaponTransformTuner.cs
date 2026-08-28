using UnityEditor;
using UnityEngine;

public sealed class HeldWeaponTransformTuner : EditorWindow
{
    private HeldWeaponPresenter m_Presenter;
    private int m_SelectedIndex;

    [MenuItem("Tools/Endless Zombie/Weapon Transform Tuner")]
    private static void OpenWindow()
    {
        GetWindow<HeldWeaponTransformTuner>("Weapon Tuner");
    }

    [MenuItem("Tools/Endless Zombie/Select Held Weapon")]
    private static void SelectHeldWeapon()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Select Held Weapon",
                "Enter Play Mode and equip the weapon you want to adjust first.",
                "OK");
            return;
        }

        HeldWeaponPresenter presenter = FindActivePresenter();
        if (presenter != null && presenter.EditorCurrentWeapon != null)
        {
            FocusWeapon(presenter.EditorCurrentWeapon);
            return;
        }

        EditorUtility.DisplayDialog(
            "Select Held Weapon",
            "No active held weapon was found. Make sure the player is active and has a weapon equipped.",
            "OK");
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange _)
    {
        m_Presenter = null;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Held Weapon Transform Tuner", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode, then use this window to equip and select any configured gun. Unlock status is ignored.",
                MessageType.Info);
            return;
        }

        if (m_Presenter == null || !m_Presenter.gameObject.activeInHierarchy)
            m_Presenter = FindActivePresenter();

        if (m_Presenter == null)
        {
            EditorGUILayout.HelpBox("No active player weapon presenter was found.", MessageType.Warning);
            if (GUILayout.Button("Find Player Again"))
                m_Presenter = FindActivePresenter();
            return;
        }

        GunConfig[] configs = m_Presenter.EditorGunConfigs;
        if (configs == null || configs.Length == 0)
        {
            EditorGUILayout.HelpBox("The player has no gun configs yet.", MessageType.Warning);
            return;
        }

        string[] names = new string[configs.Length];
        for (int i = 0; i < configs.Length; i++)
        {
            GunConfig config = configs[i];
            names[i] = config == null
                ? $"{i + 1}. Missing Config"
                : $"{i + 1}. {(string.IsNullOrWhiteSpace(config.DisplayName) ? config.name : config.DisplayName)}";
            if (config == m_Presenter.EditorCurrentConfig)
                m_SelectedIndex = i;
        }

        m_SelectedIndex = Mathf.Clamp(m_SelectedIndex, 0, configs.Length - 1);
        EditorGUI.BeginChangeCheck();
        m_SelectedIndex = EditorGUILayout.Popup("Gun", m_SelectedIndex, names);
        if (EditorGUI.EndChangeCheck() && configs[m_SelectedIndex] != null)
        {
            m_Presenter.ShowWeapon(m_SelectedIndex);
            FocusWeapon(m_Presenter.EditorCurrentWeapon);
        }

        using (new EditorGUI.DisabledScope(configs[m_SelectedIndex] == null))
        {
            if (GUILayout.Button("Equip & Select", GUILayout.Height(30f)))
            {
                m_Presenter.ShowWeapon(m_SelectedIndex);
                FocusWeapon(m_Presenter.EditorCurrentWeapon);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Use W / E / R in the Scene view to edit position, rotation, and scale. The values are saved to the selected GunConfig.",
            MessageType.None);
    }

    private static HeldWeaponPresenter FindActivePresenter()
    {
        HeldWeaponPresenter[] presenters = Object.FindObjectsOfType<HeldWeaponPresenter>();
        foreach (HeldWeaponPresenter presenter in presenters)
        {
            if (presenter.gameObject.activeInHierarchy && presenter.EditorGunConfigs != null)
                return presenter;
        }

        return null;
    }

    private static void FocusWeapon(GameObject weapon)
    {
        if (weapon == null)
            return;
        Selection.activeGameObject = weapon;
        EditorGUIUtility.PingObject(weapon);
        SceneView.lastActiveSceneView?.FrameSelected();
    }
}
