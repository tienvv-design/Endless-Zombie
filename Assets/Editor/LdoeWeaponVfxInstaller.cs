using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LdoeWeaponVfxInstaller
{
    private const string Root = "Assets/VFX/LDoE/Weapon";
    private const string MaterialFolder = Root + "/MuzzleFlashAssets/Mats";
    private const string AdditiveShaderPath = Root + "/Shaders/ParticlesAdditive.shader";
    private const string AlphaShaderPath = Root + "/Shaders/ParticlesAlphaBlended.shader";
    private const string LightMuzzlePath = Root + "/Prefabs/MuzzleFlash1.prefab";
    private const string HeavyMuzzlePath = Root + "/Prefabs/MuzzleFlash2.prefab";
    private const string ShotgunMuzzlePath = Root + "/Prefabs/AttackSfx_Shotgun_Default.prefab";

    private static readonly string[] WeaponPrefabPaths =
    {
        "Assets/Weapons/Prefabs/FN_Five_Seven.prefab",
        "Assets/Weapons/Prefabs/SM_Ak47.prefab",
        "Assets/Weapons/Prefabs/SM_Barrett_M82A1.prefab",
        "Assets/Weapons/Prefabs/SM_M16A1.prefab",
        "Assets/Weapons/Prefabs/Flamethrower.prefab",
        "Assets/Weapons/Prefabs/LDoE_Rifle_M32.prefab",
        "Assets/Weapons/Prefabs/LDoE_Minigun.prefab",
        "Assets/Weapons/Prefabs/LDoE_MP5K.prefab",
        "Assets/Weapons/Prefabs/SM_HK_MP5.prefab",
        "Assets/Weapons/Prefabs/LDoE_Harpoon.prefab",
        "Assets/Weapons/Prefabs/LDoE_Armourbreaker_Shotgun.prefab",
        "Assets/Weapons/Prefabs/LDoE_Winchester_Mercenary.prefab",
    };

    private static readonly Dictionary<string, string> GunMuzzleAssignments = new()
    {
        { "AssaultRifle", LightMuzzlePath },
        { "CryoGun", HeavyMuzzlePath },
        { "FlameRifle", HeavyMuzzlePath },
        { "M32GrenadeLauncher", HeavyMuzzlePath },
        { "Minigun", HeavyMuzzlePath },
        { "MP5K", LightMuzzlePath },
        { "Pistol", LightMuzzlePath },
        { "RicochetSMG", LightMuzzlePath },
        { "RocketLauncher", HeavyMuzzlePath },
        { "Shotgun", ShotgunMuzzlePath },
        { "TeslaGun", HeavyMuzzlePath },
        { "WinchesterMercenary", ShotgunMuzzlePath },
    };

    [MenuItem("Tools/Endless Zombie/Install LDoE Weapon VFX")]
    public static void Install()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Shader additiveShader = AssetDatabase.LoadAssetAtPath<Shader>(AdditiveShaderPath);
        Shader alphaShader = AssetDatabase.LoadAssetAtPath<Shader>(AlphaShaderPath);
        GameObject lightMuzzle = AssetDatabase.LoadAssetAtPath<GameObject>(LightMuzzlePath);
        GameObject heavyMuzzle = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyMuzzlePath);
        GameObject shotgunMuzzle = AssetDatabase.LoadAssetAtPath<GameObject>(ShotgunMuzzlePath);

        if (additiveShader == null || alphaShader == null || lightMuzzle == null ||
            heavyMuzzle == null || shotgunMuzzle == null)
        {
            throw new InvalidOperationException(
                "LDoE weapon VFX assets did not import correctly. Check the Console for shader or prefab errors.");
        }

        ConfigureMaterials(additiveShader, alphaShader);
        ConfigureParticleScaling(LightMuzzlePath);
        ConfigureParticleScaling(HeavyMuzzlePath);
        ConfigureParticleScaling(ShotgunMuzzlePath);
        foreach (string prefabPath in WeaponPrefabPaths)
            InstallMuzzleSocket(prefabPath);
        ConfigureGunAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LDoE weapon VFX installed: URP materials, twelve muzzle sockets, and twelve gun configs.");
    }

    private static void ConfigureMaterials(Shader additiveShader, Shader alphaShader)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            material.shader = string.Equals(material.name, "Smoke", StringComparison.OrdinalIgnoreCase)
                ? alphaShader
                : additiveShader;
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
        }
    }

    private static void ConfigureParticleScaling(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void InstallMuzzleSocket(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform muzzle = FindChild(root.transform, "Muzzle");
            if (muzzle != null)
            {
                Debug.Log($"Muzzle socket already exists; preserving manual transform: {prefabPath}.");
                return;
            }

            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);

            if (!TryGetMuzzlePose(
                    root.transform,
                    out Vector3 muzzlePosition,
                    out Quaternion muzzleRotation,
                    out Bounds bounds))
            {
                throw new InvalidOperationException($"Could not calculate a muzzle pose for {prefabPath}.");
            }

            muzzle.localPosition = muzzlePosition;
            muzzle.localRotation = muzzleRotation;
            muzzle.localScale = Vector3.one * 0.08f;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log(
                $"Muzzle socket: {prefabPath} at {muzzle.localPosition}, " +
                $"forward {muzzle.localRotation * Vector3.forward}; bounds {bounds.size}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool TryGetMuzzlePose(
        Transform root,
        out Vector3 muzzlePosition,
        out Quaternion muzzleRotation,
        out Bounds localBounds)
    {
        List<Vector3> vertices = CollectVertices(root, bodyOnly: true);
        if (vertices.Count == 0)
            vertices = CollectVertices(root, bodyOnly: false);

        muzzlePosition = default;
        muzzleRotation = Quaternion.identity;
        localBounds = default;
        if (vertices.Count == 0)
            return false;

        localBounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Count; i++)
            localBounds.Encapsulate(vertices[i]);

        Vector3 size = localBounds.size;
        int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
        float minimum = GetAxis(localBounds.min, axis);
        float maximum = GetAxis(localBounds.max, axis);
        float length = maximum - minimum;
        float slice = Mathf.Max(length * 0.15f, 0.001f);
        List<Vector3> lowEnd = new();
        List<Vector3> highEnd = new();

        foreach (Vector3 vertex in vertices)
        {
            float value = GetAxis(vertex, axis);
            if (value <= minimum + slice)
                lowEnd.Add(vertex);
            if (value >= maximum - slice)
                highEnd.Add(vertex);
        }

        if (lowEnd.Count == 0 || highEnd.Count == 0)
            return false;

        Vector3 lowCenter = Average(lowEnd);
        Vector3 highCenter = Average(highEnd);
        float lowSpread = CrossSectionSpread(lowEnd, lowCenter, axis);
        float highSpread = CrossSectionSpread(highEnd, highCenter, axis);
        // These PolyOne models have a slimmer stock/handle end than the barrel end,
        // so the old "smallest cross section" heuristic selected the back of the gun.
        // Select the opposite end and keep the measured barrel height/centre.
        bool useHighEnd = highSpread > lowSpread;

        Vector3 direction = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
        if (!useHighEnd)
            direction = -direction;

        muzzlePosition = useHighEnd ? highCenter : lowCenter;
        float padding = Mathf.Max(0.002f, length * 0.01f);
        SetAxis(ref muzzlePosition, axis, useHighEnd ? maximum + padding : minimum - padding);
        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.9f
            ? Vector3.forward
            : Vector3.up;
        muzzleRotation = Quaternion.LookRotation(direction, up);
        return true;
    }

    private static List<Vector3> CollectVertices(Transform root, bool bodyOnly)
    {
        List<Vector3> vertices = new();
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;
            if (bodyOnly && filter.name.IndexOf("body", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            foreach (Vector3 vertex in filter.sharedMesh.vertices)
                vertices.Add(root.InverseTransformPoint(filter.transform.TransformPoint(vertex)));
        }
        return vertices;
    }

    private static Vector3 Average(List<Vector3> vertices)
    {
        Vector3 total = Vector3.zero;
        foreach (Vector3 vertex in vertices)
            total += vertex;
        return total / vertices.Count;
    }

    private static float CrossSectionSpread(List<Vector3> vertices, Vector3 center, int axis)
    {
        float total = 0f;
        foreach (Vector3 vertex in vertices)
        {
            Vector3 delta = vertex - center;
            if (axis == 0) delta.x = 0f;
            else if (axis == 1) delta.y = 0f;
            else delta.z = 0f;
            total += delta.sqrMagnitude;
        }
        return total / vertices.Count;
    }

    private static float GetAxis(Vector3 value, int axis)
    {
        return axis == 0 ? value.x : axis == 1 ? value.y : value.z;
    }

    private static void SetAxis(ref Vector3 value, int axis, float component)
    {
        if (axis == 0) value.x = component;
        else if (axis == 1) value.y = component;
        else value.z = component;
    }

    private static void ConfigureGunAssets()
    {
        foreach (KeyValuePair<string, string> assignment in GunMuzzleAssignments)
        {
            string gunName = assignment.Key;
            string muzzlePath = assignment.Value;
            string configPath = $"Assets/ScriptableObjects/Guns/{gunName}.asset";
            GunConfig config = AssetDatabase.LoadAssetAtPath<GunConfig>(configPath);
            GameObject muzzlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(muzzlePath);
            if (config == null || muzzlePrefab == null)
                throw new InvalidOperationException($"Missing gun config or muzzle VFX for {gunName}.");

            SerializedObject serializedConfig = new(config);
            serializedConfig.FindProperty(nameof(GunConfig.MuzzleVfxPrefab)).objectReferenceValue = muzzlePrefab;
            serializedConfig.FindProperty(nameof(GunConfig.VfxLifetime)).floatValue =
                gunName == "Shotgun" ? 1.25f : 0.75f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase))
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }
}
