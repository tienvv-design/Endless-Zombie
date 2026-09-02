using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MapVersion1Installer
{
    private const string SourcePath = "Assets/Map/source/ВЕРСИЯ1.fbx";
    private const string GeneratedFolder = "Assets/Map/Generated";
    private const string MaterialFolder = GeneratedFolder + "/Materials";
    private const string RuntimeMapFolder = "Assets/Resources/StageMaps";
    private const string PrefabPath = RuntimeMapFolder + "/Map_VERSION1.prefab";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string SceneObjectName = "Stage Map - VERSION1";
    private const float TargetMapSize = 60f;

    static MapVersion1Installer()
    {
        EditorApplication.delayCall += InstallIfNeeded;
    }

    [MenuItem("Tools/Endless Zombie/Map/Install VERSION1 Map")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play Mode before installing the map.");
            return;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null)
        {
            Debug.LogError($"Map source was not found at '{SourcePath}'.");
            return;
        }

        EnsureFolder("Assets/Map", "Generated");
        EnsureFolder(GeneratedFolder, "Materials");
        EnsureFolder("Assets/Resources", "StageMaps");
        GameObject prefab = BuildPrefab(source);
        if (prefab == null) return;
        InstallIntoGameScene(prefab);
        AssetDatabase.SaveAssets();
        EditorPrefs.SetString("EndlessZombie.VERSION1MapHash",
            AssetDatabase.GetAssetDependencyHash(SourcePath).ToString());
        Debug.Log($"Installed {SceneObjectName} from {SourcePath}.");
    }

    private static void InstallIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null) return;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        string sourceDependency = AssetDatabase.GetAssetDependencyHash(SourcePath).ToString();
        string installedDependency = EditorPrefs.GetString("EndlessZombie.VERSION1MapHash", string.Empty);
        if (prefab == null || prefab.GetComponent<FlowFieldNavigationSurface>() == null ||
            !HasNavigationColliders(prefab) || sourceDependency != installedDependency)
        {
            Install();
            EditorPrefs.SetString("EndlessZombie.VERSION1MapHash", sourceDependency);
        }
    }

    private static GameObject BuildPrefab(GameObject source)
    {
        GameObject root = new(SceneObjectName);
        GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (model == null)
        {
            Object.DestroyImmediate(root);
            Debug.LogError("Unity could not instantiate the VERSION1 FBX.");
            return null;
        }

        model.name = "Map Geometry";
        model.transform.SetParent(root.transform, false);
        RemoveImportedSceneObjects(model);
        ConfigureRenderersAndMaterials(model);
        FitMapToGameplayArea(model);
        AddGroundColliders(model);
        AddNavigationColliders(model);
        root.AddComponent<FlowFieldNavigationSurface>();
        SetStaticRecursively(root);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        return prefab;
    }

    private static void ConfigureRenderersAndMaterials(GameObject model)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Dictionary<Material, Material> converted = new();
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material sourceMaterial = materials[i];
                if (sourceMaterial == null) continue;
                if (!converted.TryGetValue(sourceMaterial, out Material material))
                {
                    string sourceName = MakeSafeFileName(string.IsNullOrWhiteSpace(sourceMaterial.name)
                        ? $"Map Material {converted.Count + 1:00}"
                        : sourceMaterial.name);
                    string safeName = $"{converted.Count:00}_{sourceName}";
                    string path = $"{MaterialFolder}/{safeName}.mat";
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    bool createAsset = material == null;
                    if (createAsset)
                        material = new Material(urpLit != null ? urpLit : sourceMaterial.shader);
                    else if (urpLit != null)
                        material.shader = urpLit;
                    material.name = safeName;
                    material.color = sourceMaterial.HasProperty("_Color") ? sourceMaterial.color : Color.white;
                    if (sourceMaterial.mainTexture != null)
                        material.mainTexture = sourceMaterial.mainTexture;
                    CopyTexture(sourceMaterial, material, "_BumpMap", "_BumpMap");
                    CopyTexture(sourceMaterial, material, "_MetallicGlossMap", "_MetallicGlossMap");
                    CopyTexture(sourceMaterial, material, "_OcclusionMap", "_OcclusionMap");
                    if (material.GetTexture("_BumpMap") != null)
                        material.EnableKeyword("_NORMALMAP");
                    if (createAsset) AssetDatabase.CreateAsset(material, path);
                    else EditorUtility.SetDirty(material);
                    converted.Add(sourceMaterial, material);
                }
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static void CopyTexture(Material source, Material destination, string sourceProperty, string destinationProperty)
    {
        if (!source.HasProperty(sourceProperty) || !destination.HasProperty(destinationProperty)) return;
        Texture texture = source.GetTexture(sourceProperty);
        if (texture != null) destination.SetTexture(destinationProperty, texture);
    }

    private static void FitMapToGameplayArea(GameObject model)
    {
        if (!TryGetBounds(model, out Bounds bounds)) return;
        float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
        if (horizontalSize > 0.001f)
            model.transform.localScale = Vector3.one * (TargetMapSize / horizontalSize);

        if (!TryGetBounds(model, out bounds)) return;
        model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
    }

    private static void AddGroundColliders(GameObject model)
    {
        string[] groundWords = { "plane", "ground", "road", "floor", "terrain", "street", "pavement", "дорог" };
        int added = 0;
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            string objectName = filter.name.ToLowerInvariant();
            if (!groundWords.Any(objectName.Contains)) continue;
            if (filter.GetComponent<Collider>() != null) continue;
            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            added++;
        }

        if (added == 0 && TryGetBounds(model, out Bounds bounds))
        {
            GameObject fallback = new("Gameplay Ground Collider");
            fallback.transform.SetParent(model.transform.parent, false);
            BoxCollider collider = fallback.AddComponent<BoxCollider>();
            Vector3 localCenter = model.transform.parent.InverseTransformPoint(bounds.center);
            collider.center = new Vector3(localCenter.x, -0.25f, localCenter.z);
            collider.size = new Vector3(bounds.size.x, 0.5f, bounds.size.z);
        }
    }

    private static void AddNavigationColliders(GameObject model)
    {
        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<MeshCollider>() != null) continue;
            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
        }
    }

    private static bool HasNavigationColliders(GameObject prefab)
    {
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        return filters.Length > 0 &&
               filters.All(filter => filter.sharedMesh == null || filter.GetComponent<MeshCollider>() != null);
    }

    private static void InstallIntoGameScene(GameObject prefab)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
        if (openedTemporarily)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject[] existingMaps = scene.GetRootGameObjects()
            .Where(item => item.name == SceneObjectName).ToArray();
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        Vector3 scale = Vector3.one;
        if (existingMaps.Length > 0)
        {
            position = existingMaps[0].transform.position;
            rotation = existingMaps[0].transform.rotation;
            scale = existingMaps[0].transform.localScale;
        }

        foreach (GameObject existing in existingMaps)
            Object.DestroyImmediate(existing);

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance != null)
        {
            instance.name = SceneObjectName;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.localScale = scale;
            instance.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (openedTemporarily)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void RemoveImportedSceneObjects(GameObject model)
    {
        foreach (Camera camera in model.GetComponentsInChildren<Camera>(true))
            Object.DestroyImmediate(camera.gameObject);
        foreach (Light light in model.GetComponentsInChildren<Light>(true))
            Object.DestroyImmediate(light.gameObject);
        foreach (AudioListener listener in model.GetComponentsInChildren<AudioListener>(true))
            Object.DestroyImmediate(listener);
    }

    private static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static void SetStaticRecursively(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.isStatic = true;
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char character in Path.GetInvalidFileNameChars()) value = value.Replace(character, '_');
        return value;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
