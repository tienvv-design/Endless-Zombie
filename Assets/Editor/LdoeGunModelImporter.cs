using System;
using UnityEditor;
using UnityEngine;

public static class LdoeGunModelImporter
{
    private const string Root = "Assets/Weapons/LDoE";
    private const string Meshes = Root + "/Meshes";
    private const string Textures = Root + "/Textures";
    private const string Materials = Root + "/Materials";
    private const string Prefabs = "Assets/Weapons/Prefabs";
    private const string BaseMaterialPath = Materials + "/Weapons_URP.mat";
    private const string EmissionMaterialPath = Materials + "/Weapons_Emission_URP.mat";

    private readonly struct WeaponDefinition
    {
        public readonly string Name;
        public readonly string MeshPath;
        public readonly Vector3 MuzzlePosition;
        public readonly Vector3 LeftHandPosition;

        public WeaponDefinition(
            string name,
            string meshPath,
            Vector3 muzzlePosition,
            Vector3 leftHandPosition)
        {
            Name = name;
            MeshPath = meshPath;
            MuzzlePosition = muzzlePosition;
            LeftHandPosition = leftHandPosition;
        }
    }

    private static readonly WeaponDefinition[] Weapons =
    {
        new("LDoE_MP5K", Meshes + "/MP5K_Main.asset",
            new Vector3(0f, 0.08f, 0.34f), new Vector3(0f, -0.055f, 0.06f)),
        new("LDoE_Armourbreaker_Shotgun", Meshes + "/Armourbreaker_Shotgun.asset",
            new Vector3(0f, -0.014f, 0.691f), new Vector3(0f, -0.075f, 0.18f)),
        new("LDoE_Winchester_Mercenary", Meshes + "/Winchester_Mercenary.asset",
            new Vector3(0f, 0.055f, 0.86f), new Vector3(0f, -0.065f, 0.2f)),
        new("LDoE_Rifle_M32", Meshes + "/Rifle_M32.asset",
            new Vector3(0f, 0.077f, 0.511f), new Vector3(0f, -0.075f, 0.12f)),
    };

    [InitializeOnLoadMethod]
    private static void BuildAutomaticallyWhenImported()
    {
        EditorApplication.delayCall += () =>
        {
            if (!AllPrefabsExist() && AssetDatabase.LoadAssetAtPath<Mesh>(Weapons[0].MeshPath) != null)
                Build();
        };
    }

    [MenuItem("Tools/Endless Zombie/Build LDoE Gun Models")]
    public static void Build()
    {
        EnsureFolder("Assets/Weapons");
        EnsureFolder(Root);
        EnsureFolder(Materials);
        EnsureFolder(Prefabs);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("URP Lit shader was not found.");

        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Weapons.png");
        Texture2D minigunEmission =
            AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Minigun_Emission.png");
        if (atlas == null)
            throw new InvalidOperationException("The LDoE Weapons texture atlas did not import.");

        Material baseMaterial = GetOrCreateMaterial(BaseMaterialPath, shader);
        baseMaterial.SetTexture("_BaseMap", atlas);
        baseMaterial.SetTexture("_MainTex", atlas);
        baseMaterial.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(baseMaterial);

        Material emissionMaterial = GetOrCreateMaterial(EmissionMaterialPath, shader);
        emissionMaterial.SetTexture("_BaseMap", atlas);
        emissionMaterial.SetTexture("_MainTex", atlas);
        emissionMaterial.SetTexture("_EmissionMap", minigunEmission);
        emissionMaterial.SetColor("_EmissionColor", Color.white);
        emissionMaterial.EnableKeyword("_EMISSION");
        emissionMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(emissionMaterial);

        foreach (WeaponDefinition weapon in Weapons)
            BuildSingleMeshWeapon(weapon, baseMaterial);
        BuildMinigun(baseMaterial, emissionMaterial);
        BuildHarpoon(baseMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built six clean LDoE gun prefabs under Assets/Weapons/Prefabs.");
    }

    private static void BuildSingleMeshWeapon(WeaponDefinition definition, Material material)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(definition.MeshPath);
        if (mesh == null)
            throw new InvalidOperationException($"Missing LDoE mesh: {definition.MeshPath}");

        GameObject root = new(definition.Name);
        try
        {
            AddMesh(root.transform, "Mesh", mesh, material, Vector3.zero);
            AddSocket(root.transform, "Muzzle", definition.MuzzlePosition, 0.08f);
            AddSocket(root.transform, "LeftHandGrip", definition.LeftHandPosition, 1f);
            PrefabUtility.SaveAsPrefabAsset(root, $"{Prefabs}/{definition.Name}.prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildMinigun(Material baseMaterial, Material emissionMaterial)
    {
        Mesh main = AssetDatabase.LoadAssetAtPath<Mesh>(Meshes + "/Minigun_Main.asset");
        Mesh barrel = AssetDatabase.LoadAssetAtPath<Mesh>(Meshes + "/Minigun_Barrel.asset");
        if (main == null || barrel == null)
            throw new InvalidOperationException("Missing one or more LDoE Minigun meshes.");

        GameObject root = new("LDoE_Minigun");
        try
        {
            AddMesh(root.transform, "Minigun_Main", main, baseMaterial, Vector3.zero);
            AddMesh(root.transform, "Minigun_Barrel", barrel, emissionMaterial,
                new Vector3(0f, 0f, 0.28f));
            AddSocket(root.transform, "Muzzle", new Vector3(0f, -0.014f, 0.691f), 0.1f);
            AddSocket(root.transform, "LeftHandGrip", new Vector3(0f, -0.12f, 0.05f), 1f);
            PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/LDoE_Minigun.prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildHarpoon(Material material)
    {
        Mesh body = AssetDatabase.LoadAssetAtPath<Mesh>(Meshes + "/Harpoon_gun.asset");
        Mesh arrow = AssetDatabase.LoadAssetAtPath<Mesh>(Meshes + "/Arrow.asset");
        if (body == null || arrow == null)
            throw new InvalidOperationException("Missing one or more LDoE Harpoon meshes.");

        GameObject root = new("LDoE_Harpoon");
        try
        {
            AddMesh(root.transform, "Harpoon_Body", body, material, Vector3.zero);
            AddMesh(root.transform, "Arrow", arrow, material, Vector3.zero);
            AddSocket(root.transform, "Muzzle", new Vector3(0f, 0.06f, 0.95f), 0.08f);
            AddSocket(root.transform, "LeftHandGrip", new Vector3(0f, -0.08f, 0.2f), 1f);
            PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/LDoE_Harpoon.prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AddMesh(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        Vector3 localPosition)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void AddSocket(
        Transform parent,
        string name,
        Vector3 localPosition,
        float localScale)
    {
        Transform socket = new GameObject(name).transform;
        socket.SetParent(parent, false);
        socket.localPosition = localPosition;
        socket.localScale = Vector3.one * localScale;
    }

    private static Material GetOrCreateMaterial(string path, Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            material.shader = shader;
            return material;
        }

        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static bool AllPrefabsExist()
    {
        foreach (WeaponDefinition weapon in Weapons)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{weapon.Name}.prefab") == null)
                return false;
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/LDoE_Minigun.prefab") != null &&
               AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/LDoE_Harpoon.prefab") != null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string name = path.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
