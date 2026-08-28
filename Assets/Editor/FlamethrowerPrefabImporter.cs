using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class FlamethrowerPrefabImporter
{
    private const string ModelPath = "Assets/Models/Flamethrower/Flamethrower.fbx";
    private const string PrefabPath = "Assets/Weapons/Prefabs/Flamethrower.prefab";
    private const string MaterialFolder = "Assets/Weapons/LDoE/Materials/Flamethrower";
    private const string ConfigPath = "Assets/ScriptableObjects/Guns/FlameRifle.asset";

    [InitializeOnLoadMethod]
    private static void BuildAutomaticallyWhenImported()
    {
        EditorApplication.delayCall += () =>
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                Build();
            else
                AssignToFlameRifle(prefab);
        };
    }

    [MenuItem("Tools/Endless Zombie/Build Flamethrower Prefab")]
    public static void Build()
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
            throw new InvalidOperationException($"Flamethrower model has not imported: {ModelPath}");

        EnsureFolder(MaterialFolder);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP Lit shader was not found.");

        GameObject root = new("Flamethrower");
        try
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (visual == null)
                throw new InvalidOperationException("Could not instantiate the Flamethrower model.");

            visual.name = "Model";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            ConvertMaterials(visual, shader);

            AddSocket(root.transform, "Muzzle", new Vector3(0f, 0.05f, 0.63f), 0.08f);
            AddSocket(root.transform, "LeftHandGrip", new Vector3(0f, -0.06f, 0.12f), 1f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssignToFlameRifle(prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Built Flamethrower prefab and assigned it to Flame Rifle: {PrefabPath}");
    }

    private static void ConvertMaterials(GameObject visual, Shader shader)
    {
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] converted = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                string sourceName = source != null ? source.name : $"Material_{i}";
                string safeName = MakeSafeFileName(sourceName);
                string path = $"{MaterialFolder}/{safeName}_URP.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = safeName + "_URP" };
                    AssetDatabase.CreateAsset(material, path);
                }

                Color color = source != null && source.HasProperty("_Color")
                    ? source.color
                    : Color.gray;
                Texture texture = null;
                if (source != null)
                {
                    if (source.HasProperty("_BaseMap")) texture = source.GetTexture("_BaseMap");
                    if (texture == null && source.HasProperty("_MainTex")) texture = source.mainTexture;
                }

                material.shader = shader;
                material.SetColor("_BaseColor", color);
                material.SetColor("_Color", color);
                material.SetTexture("_BaseMap", texture);
                material.SetTexture("_MainTex", texture);
                material.SetFloat("_Smoothness", sourceName.IndexOf("Tank", StringComparison.OrdinalIgnoreCase) >= 0
                    ? 0.15f
                    : 0.3f);
                EditorUtility.SetDirty(material);
                converted[i] = material;
            }
            renderer.sharedMaterials = converted;
        }
    }

    private static void AssignToFlameRifle(GameObject prefab)
    {
        GunConfig config = AssetDatabase.LoadAssetAtPath<GunConfig>(ConfigPath);
        if (config == null || prefab == null || config.HeldWeaponPrefab == prefab)
            return;

        config.HeldWeaponPrefab = prefab;
        config.HeldLocalPosition = new Vector3(0.1f, 0.04f, -0.03f);
        config.HeldLocalEulerAngles = new Vector3(0f, 90f, -90f);
        config.HeldLocalScale = Vector3.one;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(value) ? "Material" : value;
    }

    private static void AddSocket(Transform parent, string name, Vector3 position, float scale)
    {
        Transform socket = new GameObject(name).transform;
        socket.SetParent(parent, false);
        socket.localPosition = position;
        socket.localScale = Vector3.one * scale;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        int separator = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, separator));
        AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
    }
}
