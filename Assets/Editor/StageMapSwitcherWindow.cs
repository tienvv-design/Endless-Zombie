using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public sealed class StageMapSwitcherWindow : EditorWindow
{
    private const string GameSceneName = "GameScene";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

    private int selectedStage;
    private int loadedSettingsStage;
    private string status;
    private Vector3 mapPosition;
    private Vector3 mapRotation;
    private Vector3 mapScale = Vector3.one;
    private Vector3 playerSpawnPosition;
    private Vector3 playerSpawnRotation;

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
        window.minSize = new Vector2(380f, 500f);
        window.Show();
        return window;
    }

    private void OnEnable()
    {
        selectedStage = StageMapProgression.CurrentStage;
        LoadMapSettings();
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
        int nextStage = EditorGUILayout.IntPopup(
            "Target map",
            selectedStage,
            new[] { "Stage 1 - City", "Stage 2 - Wasteland" },
            new[] { 1, 2 });
        if (nextStage != selectedStage)
        {
            selectedStage = nextStage;
            LoadMapSettings();
        }

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

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("MAP TRANSFORM & PLAYER SPAWN", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Spawn values are local to the map root. Apply writes directly to the selected map prefab.",
            MessageType.Info);
        mapPosition = EditorGUILayout.Vector3Field("Map Position", mapPosition);
        mapRotation = EditorGUILayout.Vector3Field("Map Rotation", mapRotation);
        mapScale = EditorGUILayout.Vector3Field("Map Scale", mapScale);
        playerSpawnPosition = EditorGUILayout.Vector3Field("Player Spawn Position", playerSpawnPosition);
        playerSpawnRotation = EditorGUILayout.Vector3Field("Player Spawn Rotation", playerSpawnRotation);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Apply Transform + Spawn To Prefab", GUILayout.Height(32f)))
                ApplyMapSettings();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload Values")) LoadMapSettings();
            if (GUILayout.Button("Select Spawn In Scene")) SelectSpawnInScene();
        }

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
        if (loadedSettingsStage != selectedStage) LoadMapSettings();
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

        if (!Application.isPlaying)
            EnsureEditablePrefabInstance(gameScene, selectedStage);

        StageMapRuntimeLoader.ApplyMapForCurrentStage(gameScene, Application.isPlaying);
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(gameScene);

        SceneView.RepaintAll();
        status = $"Switched to Stage {selectedStage}: {(selectedStage == 1 ? "City" : "Wasteland")}.";
        Debug.Log(status);
        Repaint();
    }

    private static void EnsureEditablePrefabInstance(Scene scene, int stage)
    {
        string prefabPath = stage == 1
            ? "Assets/Resources/StageMaps/Map_VERSION1.prefab"
            : "Assets/Resources/StageMaps/WastelandArena_Endless.prefab";
        string sceneObjectName = stage == 1
            ? "Stage Map - VERSION1"
            : "Stage Map - Wasteland";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        GameObject[] existingMaps = scene.GetRootGameObjects()
            .Where(item => item.name == sceneObjectName || item.name == prefab.name)
            .ToArray();
        GameObject existing = existingMaps.FirstOrDefault();
        if (existing != null &&
            PrefabUtility.GetPrefabInstanceStatus(existing) == PrefabInstanceStatus.Connected &&
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(existing) == prefabPath)
            return;

        Vector3 position = existing != null ? existing.transform.position : Vector3.zero;
        Quaternion rotation = existing != null ? existing.transform.rotation : Quaternion.identity;
        Vector3 scale = existing != null ? existing.transform.localScale : Vector3.one;
        foreach (GameObject item in existingMaps)
            Object.DestroyImmediate(item);

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null) return;
        instance.name = sceneObjectName;
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = scale;
    }

    private void LoadMapSettings()
    {
        loadedSettingsStage = selectedStage;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(selectedStage));
        if (prefab == null) return;

        mapPosition = prefab.transform.localPosition;
        mapRotation = prefab.transform.localEulerAngles;
        mapScale = prefab.transform.localScale;
        Transform spawn = FindDescendant(prefab.transform, "Spawn_Player");
        if (spawn == null)
        {
            playerSpawnPosition = Vector3.zero;
            playerSpawnRotation = Vector3.zero;
            return;
        }

        playerSpawnPosition = prefab.transform.InverseTransformPoint(spawn.position);
        playerSpawnRotation = (Quaternion.Inverse(prefab.transform.rotation) * spawn.rotation).eulerAngles;
    }

    private void ApplyMapSettings()
    {
        string prefabPath = GetPrefabPath(selectedStage);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            status = $"Cannot load map prefab: {prefabPath}";
            return;
        }

        try
        {
            root.transform.localPosition = mapPosition;
            root.transform.localEulerAngles = mapRotation;
            root.transform.localScale = new Vector3(
                Mathf.Max(0.01f, mapScale.x),
                Mathf.Max(0.01f, mapScale.y),
                Mathf.Max(0.01f, mapScale.z));

            Transform spawn = FindDescendant(root.transform, "Spawn_Player");
            if (spawn == null)
            {
                spawn = new GameObject("Spawn_Player").transform;
                spawn.SetParent(root.transform, false);
            }

            Vector3 desiredSpawnPosition = root.transform.TransformPoint(playerSpawnPosition);
            Quaternion desiredSpawnRotation = root.transform.rotation * Quaternion.Euler(playerSpawnRotation);
            if ((spawn.position - desiredSpawnPosition).sqrMagnitude > 0.000001f)
                spawn.position = desiredSpawnPosition;
            if (Quaternion.Angle(spawn.rotation, desiredSpawnRotation) > 0.001f)
                spawn.rotation = desiredSpawnRotation;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Scene gameScene = SceneManager.GetSceneByName(GameSceneName);
        if (gameScene.IsValid() && gameScene.isLoaded)
        {
            EnsureEditablePrefabInstance(gameScene, selectedStage);
            GameObject map = FindMapRoot(gameScene, selectedStage);
            if (map != null && PrefabUtility.IsPartOfPrefabInstance(map))
                PrefabUtility.RevertObjectOverride(map.transform, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(gameScene);
        }

        AssetDatabase.SaveAssets();
        LoadMapSettings();
        SceneView.RepaintAll();
        status = $"Saved map transform and Spawn_Player to Stage {selectedStage} prefab.";
        Debug.Log(status);
    }

    private void SelectSpawnInScene()
    {
        Scene gameScene = SceneManager.GetSceneByName(GameSceneName);
        GameObject map = gameScene.IsValid() && gameScene.isLoaded
            ? FindMapRoot(gameScene, selectedStage)
            : null;
        Transform spawn = map != null ? FindDescendant(map.transform, "Spawn_Player") : null;
        if (spawn == null)
        {
            status = "Apply the settings first to create Spawn_Player, then select it again.";
            return;
        }

        Selection.activeTransform = spawn;
        SceneView.lastActiveSceneView?.FrameSelected();
        status = $"Selected Stage {selectedStage} Spawn_Player in the scene.";
    }

    private static string GetPrefabPath(int stage)
    {
        return stage == 1
            ? "Assets/Resources/StageMaps/Map_VERSION1.prefab"
            : "Assets/Resources/StageMaps/WastelandArena_Endless.prefab";
    }

    private static GameObject FindMapRoot(Scene scene, int stage)
    {
        string sceneObjectName = stage == 1 ? "Stage Map - VERSION1" : "Stage Map - Wasteland";
        string prefabName = stage == 1 ? "Map_VERSION1" : "WastelandArena_Endless";
        return scene.GetRootGameObjects()
            .FirstOrDefault(item => item.name == sceneObjectName || item.name == prefabName);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == objectName);
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
