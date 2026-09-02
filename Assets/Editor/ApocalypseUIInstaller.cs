#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ApocalypseUIInstaller
{
    private const string Root = "Assets/Art/UI/ApocalypseGenerated/";
    private const string InstallVersion = "2026-08-29-icons-v1";
    private static readonly string[] Prefabs =
    {
        "Assets/Resources/MainMenuCanvas.prefab",
        "Assets/Resources/GameplayHUDLayout.prefab"
    };

    private static readonly string[] Scenes = Array.Empty<string>();

    private static Dictionary<string, Sprite> sprites;

    static ApocalypseUIInstaller()
    {
        EditorApplication.delayCall += AutoInstall;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += AutoInstall;
    }

    [MenuItem("Tools/Endless Zombie/UI/Install Apocalypse Sprite Set")]
    public static void InstallFromMenu()
    {
        Install(true);
    }

    [MenuItem("Tools/Endless Zombie/UI/Validate Apocalypse Sprite Set")]
    public static void ValidateFromMenu()
    {
        int missing = 0;
        foreach (string prefabPath in Prefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (IsIconTarget(image.transform) && image.sprite == null) missing++;
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log($"[Apocalypse UI] Validation complete. Missing prefab Image sprites: {missing}.");
    }

    private static void AutoInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        string key = $"EndlessZombie.ApocalypseUI.{Application.dataPath.GetHashCode()}";
        if (EditorPrefs.GetString(key) == InstallVersion)
            return;

        Install(false);
        EditorPrefs.SetString(key, InstallVersion);
    }

    private static void Install(bool verbose)
    {
        ConfigureGeneratedTextures();
        LoadSprites();

        int changed = 0;
        foreach (string prefabPath in Prefabs)
            changed += ProcessPrefab(prefabPath);
        foreach (string scenePath in Scenes)
            changed += ProcessScene(scenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (verbose || changed > 0)
            Debug.Log($"[Apocalypse UI] Installed generated sprite set on {changed} UI Image components.");
    }

    private static void ConfigureGeneratedTextures()
    {
        foreach (string path in AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(path);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            importer.spriteBorder = Vector4.zero;
            importer.SaveAndReimport();
        }
    }

    private static void LoadSprites()
    {
        sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        string[] names =
        {
            "icon_health", "icon_ammo", "icon_damage", "icon_firerate", "icon_range", "icon_crit_chance",
            "icon_crit_damage", "icon_gold", "icon_settings", "icon_inventory", "icon_pet", "icon_battle", "icon_shop",
        };
        foreach (string name in names)
            sprites[name] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + name + ".png");
    }

    private static int ProcessPrefab(string path)
    {
        if (!File.Exists(path)) return 0;
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        int changed = ProcessImages(root.GetComponentsInChildren<Image>(true), path);
        if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        return changed;
    }

    private static int ProcessScene(string path)
    {
        if (!File.Exists(path)) return 0;
        Scene scene = SceneManager.GetSceneByPath(path);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        int changed = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            changed += ProcessImages(root.GetComponentsInChildren<Image>(true), path);
        if (changed > 0) EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
        return changed;
    }

    private static int ProcessImages(Image[] images, string assetPath)
    {
        int changed = 0;
        foreach (Image image in images)
        {
            string hierarchy = HierarchyPath(image.transform).ToLowerInvariant();
            if (assetPath.EndsWith("LoadingScreen.unity", StringComparison.OrdinalIgnoreCase) &&
                hierarchy.Contains("background") && image.sprite != null)
                continue;

            Sprite replacement = Choose(hierarchy);
            if (replacement == null || image.sprite == replacement) continue;
            image.sprite = replacement;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            EditorUtility.SetDirty(image);
            changed++;
        }
        return changed;
    }

    private static Sprite Choose(string path)
    {
        string leaf = path[(path.LastIndexOf('/') + 1)..];
        if (!leaf.Contains("icon")) return null;
        if (leaf.Contains("ammo")) return S("icon_ammo");
        if (leaf.Contains("critdamage") || leaf.Contains("crit damage")) return S("icon_crit_damage");
        if (leaf.Contains("critchance") || leaf.Contains("crit chance")) return S("icon_crit_chance");
        if (leaf.Contains("firerate") || leaf.Contains("fire rate")) return S("icon_firerate");
        if (leaf.Contains("damage")) return S("icon_damage");
        if (leaf.Contains("range")) return S("icon_range");
        if (leaf.Contains("health") || leaf.Contains("max hp") || leaf.Contains("heart")) return S("icon_health");
        if (leaf.Contains("gold") || leaf.Contains("income")) return S("icon_gold");
        if (leaf.Contains("shop")) return S("icon_shop");
        if (leaf.Contains("inventory")) return S("icon_inventory");
        if (leaf.Contains("pet")) return S("icon_pet");
        if (leaf.Contains("weapon") || leaf.Contains("battle") || leaf.Contains("boss")) return S("icon_battle");
        if (leaf.Contains("setting")) return S("icon_settings");
        return null;
    }

    private static bool IsIconTarget(Transform transform) =>
        transform.name.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0;

    private static Sprite S(string key) => sprites.TryGetValue(key, out Sprite sprite) ? sprite : null;

    private static string HierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
#endif
