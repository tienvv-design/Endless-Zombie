using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LdoeWastelandMapInstaller
{
    private const string RootPath = "Assets/LDoE/WastelandSmall";
    private const string SourcePrefabPath = RootPath + "/Maps/SmallWastelandMap_Content.prefab";
    private const string ReadyPrefabPath = RootPath + "/Maps/SmallWastelandMap_Endless.prefab";
    private const string GroundTexturePath = RootPath + "/Assets/Mats/wasteland_sand.png";
    private const string MaterialFolder = RootPath + "/RuntimeMaterials";
    private const string AutoInstallSessionKey = "EndlessZombie.LdoeWastelandMapInstaller.V1";

    [InitializeOnLoadMethod]
    private static void ScheduleAutoInstall()
    {
        if (SessionState.GetBool(AutoInstallSessionKey, false))
            return;

        EditorApplication.delayCall += TryAutoInstall;
    }

    private static void TryAutoInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        SessionState.SetBool(AutoInstallSessionKey, true);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(ReadyPrefabPath) == null)
            BuildEndlessReadyPrefab();
    }

    [MenuItem("Tools/Endless Zombie/Maps/Build LDoE Small Wasteland Map")]
    public static void BuildEndlessReadyPrefab()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (source == null)
            throw new MissingReferenceException($"Could not load {SourcePrefabPath}.");

        EnsureFolder(MaterialFolder);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new MissingReferenceException("Universal Render Pipeline/Lit shader is unavailable.");

        Texture2D groundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexturePath);
        Material ground = CreateOrUpdateMaterial(
            MaterialFolder + "/WastelandGround.mat", shader,
            new Color(0.33f, 0.27f, 0.19f), groundTexture, 0.08f, 0f);
        Material road = CreateOrUpdateMaterial(
            MaterialFolder + "/WastelandRoad.mat", shader,
            new Color(0.22f, 0.19f, 0.15f), null, 0.12f, 0f);
        Material rock = CreateOrUpdateMaterial(
            MaterialFolder + "/WastelandRock.mat", shader,
            new Color(0.39f, 0.36f, 0.31f), null, 0.18f, 0f);
        Material vegetation = CreateOrUpdateMaterial(
            MaterialFolder + "/WastelandVegetation.mat", shader,
            new Color(0.28f, 0.29f, 0.16f), null, 0.08f, 0f);
        Material truck = CreateOrUpdateMaterial(
            MaterialFolder + "/WastelandTruck.mat", shader,
            new Color(0.31f, 0.19f, 0.12f), null, 0.24f, 0.35f);
        Material puddle = CreateOrUpdateMaterial(
            MaterialFolder + "/WastelandPuddle.mat", shader,
            new Color(0.16f, 0.17f, 0.15f), null, 0.72f, 0f);

        GameObject root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        try
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material material = SelectMaterial(
                    renderer.transform, ground, road, rock, vegetation, truck, puddle);
                int materialCount = Math.Max(1, renderer.sharedMaterials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(material, materialCount).ToArray();
            }

            PrefabUtility.SaveAsPrefabAsset(root, ReadyPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Built Endless-ready LDoE Wasteland map at {ReadyPrefabPath}.");
    }

    private static Material SelectMaterial(
        Transform transform,
        Material ground,
        Material road,
        Material rock,
        Material vegetation,
        Material truck,
        Material puddle)
    {
        string hierarchy = GetHierarchyName(transform).ToLowerInvariant();
        if (hierarchy.Contains("ground_60x60")) return ground;
        if (hierarchy.Contains("puddle")) return puddle;
        if (hierarchy.Contains("truck")) return truck;
        if (hierarchy.Contains("rock") || hierarchy.Contains("stone")) return rock;
        if (hierarchy.Contains("bush") || hierarchy.Contains("tree")) return vegetation;
        if (hierarchy.Contains("road")) return road;
        return rock;
    }

    private static string GetHierarchyName(Transform transform)
    {
        string value = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            value = parent.name + "/" + value;
            parent = parent.parent;
        }
        return value;
    }

    private static Material CreateOrUpdateMaterial(
        string path,
        Shader shader,
        Color color,
        Texture texture,
        float smoothness,
        float metallic)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.shader = shader;
        material.SetColor("_BaseColor", color);
        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
