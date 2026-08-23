using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MainMenuCanvasPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/MainMenuCanvas.prefab";

    static MainMenuCanvasPrefabBuilder()
    {
        EditorApplication.delayCall += CreateIfMissing;
    }

    [MenuItem("Tools/Endless Zombie/Rebuild Main Menu Canvas Prefab")]
    public static void Rebuild()
    {
        Build(true);
    }

    private static void CreateIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            Build(false);
    }

    private static void Build(bool overwrite)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        MainMenuManager manager = Object.FindFirstObjectByType<MainMenuManager>();
        if (manager == null)
        {
            if (overwrite)
                Debug.LogWarning("Open GameScene or MainMenu before rebuilding the Main Menu Canvas prefab.");
            return;
        }

        MethodInfo build = typeof(MainMenuManager).GetMethod("BuildMenuInCode", BindingFlags.Instance | BindingFlags.NonPublic);
        build?.Invoke(manager, null);
        GameObject canvas = GameObject.Find("Battle Main Menu");
        if (canvas == null)
            return;

        Object.DestroyImmediate(canvas.transform.Find("Weapon Window")?.gameObject);
        Object.DestroyImmediate(canvas.transform.Find("Feature Window")?.gameObject);
        MainMenuCanvasView view = canvas.GetComponent<MainMenuCanvasView>() ?? canvas.AddComponent<MainMenuCanvasView>();
        view.CaptureReferences();
        PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath);
        Object.DestroyImmediate(canvas);
        AssetDatabase.SaveAssets();
        Debug.Log($"Main Menu Canvas prefab created at {PrefabPath}. Edit this prefab to change button layout.");
    }
}
