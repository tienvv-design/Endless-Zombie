using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class WinUIEditorTools
{
    private const string PrefabPath = "Assets/Resources/WinMenu.prefab";

    static WinUIEditorTools() => EditorApplication.delayCall += CreatePrefabIfMissing;

    [MenuItem("Tools/Endless Zombie/UI/Edit Win Screen")]
    private static void EditWinScreen()
    {
        CreatePrefabIfMissing();
        Object prefab = AssetDatabase.LoadMainAssetAtPath(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        AssetDatabase.OpenAsset(prefab);
    }

    [MenuItem("Tools/Endless Zombie/UI/Reset Win Screen Prefab From Code")]
    private static void RebuildWinScreenPrefab()
    {
        CreateOrReplacePrefab();
        EditWinScreen();
    }

    private static void CreatePrefabIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
        CreateOrReplacePrefab();
    }

    private static void CreateOrReplacePrefab()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage()?.assetPath == PrefabPath)
            StageUtility.GoBackToPreviousStage();
        GameObject root = WinMenu.CreateCanvasRoot();
        root.GetComponent<WinMenu>().BuildEditorPreview();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
