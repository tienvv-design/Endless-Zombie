using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class StageMapSwitcherWindow : EditorWindow
{
    private const string GameSceneName = "GameScene";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

    private int selectedStage;
    private string status;

    [MenuItem("Tools/Endless Zombie/Maps/Quick Map Switcher %#m")]
    private static void Open()
    {
        GetOrOpen();
    }

    [MenuItem("Tools/Endless Zombie/Maps/Switch to Stage 1 - City %&1")]
    private static void SwitchToCityShortcut()
    {
        GetOrOpen().SwitchTo(1, false);
    }

    [MenuItem("Tools/Endless Zombie/Maps/Switch to Stage 2 - Wasteland %&2")]
    private static void SwitchToWastelandShortcut()
    {
        GetOrOpen().SwitchTo(2, false);
    }

    private static StageMapSwitcherWindow GetOrOpen()
    {
        StageMapSwitcherWindow window = GetWindow<StageMapSwitcherWindow>();
        window.titleContent = new GUIContent("Map Switcher");
        window.minSize = new Vector2(360f, 245f);
        window.Show();
        return window;
    }

    private void OnEnable()
    {
        selectedStage = StageMapProgression.CurrentStage;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("ENDLESS ZOMBIE - QUICK MAP SWITCHER", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUILayout.HelpBox(
            Application.isPlaying
                ? "Play Mode: map is switched immediately without reloading GameScene."
                : "Edit Mode: the selected map is previewed when GameScene is open. The stage is also used for the next Play.",
            MessageType.Info);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Current gameplay stage", $"STAGE {StageMapProgression.CurrentStage}");
        selectedStage = EditorGUILayout.IntPopup(
            "Target map",
            selectedStage,
            new[] { "Stage 1 - City", "Stage 2 - Wasteland" },
            new[] { 1, 2 });

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("STAGE 1\nCITY", GUILayout.Height(48f)))
                SwitchTo(1, false);
            if (GUILayout.Button("STAGE 2\nWASTELAND", GUILayout.Height(48f)))
                SwitchTo(2, false);
        }

        EditorGUILayout.Space(6f);
        if (GUILayout.Button("Open GameScene + Apply Selected Map", GUILayout.Height(30f)))
            SwitchTo(selectedStage, true);

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            "Shortcuts: Ctrl+Shift+M (window)  |  Ctrl+Alt+1 (City)  |  Ctrl+Alt+2 (Wasteland)",
            EditorStyles.miniLabel);
    }

    private void SwitchTo(int stage, bool openSceneWhenNeeded)
    {
        selectedStage = Mathf.Clamp(stage, StageMapProgression.FirstStage, StageMapProgression.LastConfiguredStage);
        StageMapProgression.SelectStage(selectedStage);

        Scene gameScene = SceneManager.GetSceneByName(GameSceneName);
        if (!gameScene.IsValid() || !gameScene.isLoaded)
        {
            if (!openSceneWhenNeeded)
            {
                status = $"Stage {selectedStage} selected for the next Play. Open GameScene to preview it now.";
                Repaint();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                status = "Opening GameScene was cancelled. The gameplay stage selection was still saved.";
                Repaint();
                return;
            }

            gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        }

        StageMapRuntimeLoader.ApplyMapForCurrentStage(gameScene, Application.isPlaying);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(gameScene);

        SceneView.RepaintAll();
        status = $"Switched to Stage {selectedStage}: {(selectedStage == 1 ? "City" : "Wasteland")}.";
        Debug.Log(status);
        Repaint();
    }

    private void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Scene gameScene = SceneManager.GetSceneByName(GameSceneName);
            if (gameScene.IsValid() && gameScene.isLoaded)
                StageMapRuntimeLoader.ApplyMapForCurrentStage(gameScene);
        }

        Repaint();
    }
}
