using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class MainMenuPlayModeStart
{
    public const string MainMenuPath = "Assets/Scenes/GameScene.unity";

    static MainMenuPlayModeStart()
    {
        UseMainMenu();
    }

    public static void UseMainMenu()
    {
        SceneAsset mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (mainMenu != null && EditorSceneManager.playModeStartScene != mainMenu)
            EditorSceneManager.playModeStartScene = mainMenu;
    }

    public static void UseCurrentScene()
    {
        EditorSceneManager.playModeStartScene = null;
    }
}
