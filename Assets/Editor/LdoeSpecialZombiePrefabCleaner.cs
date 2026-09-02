using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class LdoeSpecialZombiePrefabCleaner
{
    private const string SessionKey = "EndlessZombie.LdoeSpecialZombiePrefabCleaner.v1";
    private static readonly string[] PrefabPaths =
    {
        "Assets/LDoE/SpecialZombies/Imported/Generated/CleanCharacterModels/ZombieFat_ZombieFat.prefab",
        "Assets/LDoE/SpecialZombies/Imported/Generated/CleanCharacterModels/ZombieSquat_Zombie_Squat.prefab",
        "Assets/LDoE/SpecialZombies/Imported/Generated/CleanCharacterModels/ZombieTank_ZombieTank.prefab",
        "Assets/LDoE/SpecialZombies/Imported/Generated/CleanCharacterModels/ZombieWitch_Zombie_Witch.prefab",
    };

    static LdoeSpecialZombiePrefabCleaner()
    {
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += CleanImportedPrefabs;
    }

    [MenuItem("Tools/Endless Zombie/Zombies/Clean LDoE Special Zombie Prefabs")]
    public static void CleanImportedPrefabs()
    {
        int removed = 0;
        foreach (string path in PrefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
                for (int i = descendants.Length - 1; i >= 0; i--)
                {
                    Transform item = descendants[i];
                    if (item == root.transform || !IsHelper(item.name)) continue;
                    Object.DestroyImmediate(item.gameObject);
                    removed++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Cleaned LDoE special zombie prefabs. Removed {removed} helper/minimap objects.");
    }

    private static bool IsHelper(string objectName) => objectName == "CharacterTrigger" ||
        objectName == "Prefab_Minimap_Enemy" || objectName == "Prefab_Minimap_Enemy_Corpse";
}
