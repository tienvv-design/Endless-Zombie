using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class LdoeCombatImpactVfxInstaller
{
    private const string SourceRoot = "D:/Project/LDoE/Assets/Content/Models/SFX/Blood/Mats";
    private const string OutputRoot = "Assets/VFX/LDoE/CombatImpact";
    private const string TextureRoot = OutputRoot + "/Textures";
    private const string MaterialRoot = OutputRoot + "/Materials";
    private const string PrefabRoot = OutputRoot + "/Prefabs";
    private const string SplatterTexture = TextureRoot + "/T_Blood_SubUV_2x2_01.png";
    private const string MistTexture = TextureRoot + "/T_Blood_Mist_04.png";
    private const string SplatterMaterial = MaterialRoot + "/LDoE_Blood_Splatter_URP.mat";
    private const string MistMaterial = MaterialRoot + "/LDoE_Blood_Mist_URP.mat";
    private const string HitPrefab = PrefabRoot + "/LDoE_Zombie_BulletHit.prefab";
    private const string GunConfigRoot = "Assets/ScriptableObjects/Guns";

    [MenuItem("Tools/Endless Zombie/Install LDoE Zombie Hit VFX")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before installing combat VFX.");

        EnsureFolder(TextureRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(PrefabRoot);
        CopyTexture("T_Blood_SubUV_2x2_01.png", SplatterTexture);
        CopyTexture("T_Blood_Mist_04.png", MistTexture);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureGrayscaleAlpha(SplatterTexture);
        ConfigureGrayscaleAlpha(MistTexture);

        Material splatter = CreateMaterial(SplatterMaterial, SplatterTexture);
        Material mist = CreateMaterial(MistMaterial, MistTexture);
        CreateHitPrefab(splatter, mist);

        GameObject hitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HitPrefab);
        string[] configGuids = AssetDatabase.FindAssets("t:GunConfig", new[] { GunConfigRoot });
        foreach (string guid in configGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GunConfig config = AssetDatabase.LoadAssetAtPath<GunConfig>(path);
            if (config == null) continue;
            SerializedObject serialized = new(config);
            serialized.FindProperty(nameof(GunConfig.ImpactVfxPrefab)).objectReferenceValue = hitPrefab;
            serialized.FindProperty(nameof(GunConfig.VfxLifetime)).floatValue = 0.85f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"LDoE zombie hit VFX installed and assigned to {configGuids.Length} gun configs.");
    }

    [MenuItem("Tools/Endless Zombie/Validate LDoE Zombie Hit VFX")]
    public static void Validate()
    {
        GameObject hit = AssetDatabase.LoadAssetAtPath<GameObject>(HitPrefab);
        if (hit == null || hit.GetComponentsInChildren<ParticleSystem>(true).Length != 2)
            throw new InvalidOperationException("Zombie hit VFX prefab is missing or incomplete.");

        string[] configGuids = AssetDatabase.FindAssets("t:GunConfig", new[] { GunConfigRoot });
        foreach (string guid in configGuids)
        {
            GunConfig config = AssetDatabase.LoadAssetAtPath<GunConfig>(AssetDatabase.GUIDToAssetPath(guid));
            if (config == null || config.ImpactVfxPrefab != hit)
                throw new InvalidOperationException($"Gun config '{config?.name}' is missing the zombie hit VFX.");
        }
        Debug.Log($"Combat VFX validation passed: blood impact is assigned to {configGuids.Length} guns.");
    }

    private static void CreateHitPrefab(Material splatterMaterial, Material mistMaterial)
    {
        GameObject root = new("LDoE Zombie Bullet Hit");
        try
        {
            CreateSplatter(root.transform, splatterMaterial);
            CreateMist(root.transform, mistMaterial);
            PrefabUtility.SaveAsPrefabAsset(root, HitPrefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void CreateSplatter(Transform parent, Material material)
    {
        ParticleSystem particles = CreateParticle("Blood Splatter", parent);
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.68f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.52f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.3f, 0.002f, 0.001f, 0.95f),
            new Color(0.95f, 0.018f, 0.006f, 1f));
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.3f, 0.75f);
        main.maxParticles = 22;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 16) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = FadeOut(0.58f);
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1f));

        ParticleSystem.TextureSheetAnimationModule sheet = particles.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = 2;
        sheet.numTilesY = 2;
        sheet.animation = ParticleSystemAnimationType.SingleRow;
        sheet.rowMode = ParticleSystemAnimationRowMode.Random;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void CreateMist(Transform parent, Material material)
    {
        ParticleSystem particles = CreateParticle("Blood Mist", parent);
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.95f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.68f, 1.25f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.35f, 0.002f, 0.001f, 0.68f),
            new Color(0.9f, 0.012f, 0.004f, 0.88f));
        main.maxParticles = 7;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 5) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = FadeOut(0.42f);
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.35f));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static ParticleSystem CreateParticle(string name, Transform parent)
    {
        GameObject item = new(name);
        item.transform.SetParent(parent, false);
        ParticleSystem particles = item.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;
        return particles;
    }

    private static Gradient FadeOut(float fadeStart)
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, fadeStart),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Material CreateMaterial(string path, string texturePath)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) throw new MissingReferenceException("URP Particles/Unlit shader was not found.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureGrayscaleAlpha(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static void CopyTexture(string sourceName, string destination)
    {
        string source = Path.Combine(SourceRoot, sourceName).Replace('\\', '/');
        if (!File.Exists(source)) throw new FileNotFoundException($"Missing LDoE blood texture: {source}");
        if (!File.Exists(destination)) File.Copy(source, destination);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
