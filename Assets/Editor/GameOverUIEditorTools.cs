using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class GameOverUIEditorTools
{
    private const string SettingsPath = "Assets/Resources/GameOverUISettings.asset";
    private const string PrefabPath = "Assets/Resources/GameOverMenu.prefab";

    static GameOverUIEditorTools()
    {
        EditorApplication.delayCall += CreatePrefabIfMissing;
    }

    [MenuItem("Tools/Endless Zombie/UI/Edit Lose Screen")]
    private static void EditLoseScreen()
    {
        CreatePrefabIfMissing();
        Object prefab = AssetDatabase.LoadMainAssetAtPath(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        AssetDatabase.OpenAsset(prefab);
    }

    [MenuItem("Tools/Endless Zombie/UI/Edit Lose Layout Defaults")]
    private static void EditLoseLayoutDefaults()
    {
        Object settings = AssetDatabase.LoadMainAssetAtPath(SettingsPath);
        Selection.activeObject = settings;
        EditorGUIUtility.PingObject(settings);
    }

    [MenuItem("Tools/Endless Zombie/UI/Reset Lose Screen Prefab From Defaults")]
    private static void RebuildLoseScreenPrefab()
    {
        CreateOrReplacePrefab();
        EditLoseScreen();
    }

    private static void CreatePrefabIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;
        CreateOrReplacePrefab();
    }

    private static void CreateOrReplacePrefab()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == PrefabPath)
            StageUtility.GoBackToPreviousStage();

        GameOverUISettings layout = AssetDatabase.LoadAssetAtPath<GameOverUISettings>(SettingsPath);
        GameObject root = new("Game Over Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(GameOverMenu));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.localScale = Vector3.one;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = layout != null ? layout.SortingOrder : 160;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = layout != null ? layout.ReferenceResolution : new Vector2(1080f, 1920f);
        rootRect.sizeDelta = scaler.referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = layout != null ? layout.MatchWidthOrHeight : 0.5f;
        root.GetComponent<GameOverMenu>().BuildEditorPreview();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
