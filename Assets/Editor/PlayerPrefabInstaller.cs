using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerPrefabInstaller
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string PrefabFolder = "Assets/Prefabs/Player";
    private const string PrefabPath = PrefabFolder + "/Player.prefab";
    private const string AutoInstallSessionKey = "EndlessZombie.PlayerPrefabInstaller.AutoInstall";

    [InitializeOnLoadMethod]
    private static void ScheduleAutoInstall()
    {
        if (SessionState.GetBool(AutoInstallSessionKey, false) ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        SessionState.SetBool(AutoInstallSessionKey, true);
        EditorApplication.delayCall += TryAutoInstallFromLoadedScene;
    }

    [MenuItem("Tools/Endless Zombie/Player/Create or Update Player Prefab")]
    public static void CreateOrUpdateFromMenu()
    {
        CreateOrUpdatePlayerPrefab();
    }

    // Public entry point used by Unity batch mode and project automation.
    public static void CreateOrUpdatePlayerPrefab()
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        GameObject character = FindRootObject(scene, "Character");

        if (character == null)
        {
            throw new MissingReferenceException(
                $"Could not find a root GameObject named 'Character' in {GameScenePath}.");
        }

        EnsureFolder(PrefabFolder);

        GameObject prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
            character,
            PrefabPath,
            InteractionMode.AutomatedAction,
            out bool success);

        if (!success || prefabInstance == null)
        {
            throw new UnityException($"Failed to create Player prefab at {PrefabPath}.");
        }

        prefabInstance.name = "Character";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Player prefab created and connected successfully: {PrefabPath}");
    }

    private static void TryAutoInstallFromLoadedScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool(AutoInstallSessionKey, false);
            return;
        }

        Scene gameScene = SceneManager.GetSceneByPath(GameScenePath);
        if (!gameScene.IsValid() || !gameScene.isLoaded)
        {
            Debug.Log(
                "Player prefab is ready to generate. Open GameScene and use " +
                "Tools > Endless Zombie > Player > Create or Update Player Prefab.");
            SessionState.SetBool(AutoInstallSessionKey, false);
            return;
        }

        GameObject character = FindRootObject(gameScene, "Character");
        if (character == null || PrefabUtility.IsPartOfPrefabInstance(character))
        {
            return;
        }

        EnsureFolder(PrefabFolder);
        GameObject prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
            character,
            PrefabPath,
            InteractionMode.AutomatedAction,
            out bool success);

        if (success && prefabInstance != null)
        {
            prefabInstance.name = "Character";
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Player prefab created and connected successfully: {PrefabPath}");
        }
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root;
            }
        }

        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
