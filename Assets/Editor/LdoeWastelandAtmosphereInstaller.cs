using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class LdoeWastelandAtmosphereInstaller
{
    private const string LdoeTextureRoot =
        "D:/Project/LDoE/Assets/ContentAddressable/Content/Visual Effects/_Textures";
    private const string OutputRoot = "Assets/LDoE/EnvironmentVFX";
    private const string TextureRoot = OutputRoot + "/Textures";
    private const string MaterialRoot = OutputRoot + "/Materials";
    private const string DustTexturePath = TextureRoot + "/particle_dust.png";
    private const string SmokeTexturePath = TextureRoot + "/smoke_gray.png";
    private const string DustMaterialPath = MaterialRoot + "/LDoE_Wasteland_Dust_URP.mat";
    private const string SmokeMaterialPath = MaterialRoot + "/LDoE_Wasteland_Fog_URP.mat";
    private const string PlayerVisualPath =
        "Assets/Models/LDoE Survivor/Survivor_character_fixed.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string WastelandPrefabPath =
        "Assets/LDoE/WastelandArena/Generated/WastelandArena_Endless.prefab";
    private const string AtmosphereName = "LDoE Wasteland Atmosphere";

    [MenuItem("Tools/Endless Zombie/Install Player Shadows + LDoE Wasteland Atmosphere")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before installing shadows and atmosphere VFX.");

        EnsureAssets();
        FixPrefabShadows(PlayerVisualPath);
        InstallPlayerShadowEnforcer();

        PrefabStage openStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (openStage != null && openStage.assetPath == WastelandPrefabPath)
        {
            // Modify the active prefab stage itself. Loading a second prefab copy
            // while this asset is open leaves stale stage data that Auto Save can
            // write back over the newly generated atmosphere.
            AddAtmosphere(openStage.prefabContentsRoot.transform);
            EditorSceneManager.MarkSceneDirty(openStage.scene);
            PrefabUtility.SaveAsPrefabAsset(openStage.prefabContentsRoot, WastelandPrefabPath);
        }
        else
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(WastelandPrefabPath);
            try
            {
                AddAtmosphere(contents.transform);
                PrefabUtility.SaveAsPrefabAsset(contents, WastelandPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(WastelandPrefabPath, ImportAssetOptions.ForceUpdate);
        LdoeWastelandArenaGenerator.SyncEditedPrefabToRuntime();
        Debug.Log("Player shadows enabled and LDoE dust/fog atmosphere installed on the Wasteland map.");
    }

    [MenuItem("Tools/Endless Zombie/Validate Player Shadows + Wasteland Atmosphere")]
    public static void Validate()
    {
        GameObject playerVisual = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisualPath);
        if (playerVisual == null)
            throw new MissingReferenceException($"Missing player visual: {PlayerVisualPath}");

        Renderer[] playerRenderers = playerVisual.GetComponentsInChildren<Renderer>(true);
        if (playerRenderers.Length == 0)
            throw new MissingComponentException("The player visual contains no renderers.");
        foreach (Renderer renderer in playerRenderers)
        {
            if (renderer is ParticleSystemRenderer or TrailRenderer or LineRenderer)
                continue;
            if (renderer.shadowCastingMode == ShadowCastingMode.Off || !renderer.receiveShadows)
                throw new InvalidOperationException($"Player renderer '{renderer.name}' still has shadows disabled.");
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null || playerPrefab.GetComponent<PlayerShadowEnforcer>() == null)
            throw new MissingComponentException("The runtime Player prefab is missing PlayerShadowEnforcer.");

        GameObject wasteland = AssetDatabase.LoadAssetAtPath<GameObject>(WastelandPrefabPath);
        Transform atmosphere = wasteland != null ? wasteland.transform.Find(AtmosphereName) : null;
        ParticleSystem[] systems = atmosphere != null
            ? atmosphere.GetComponentsInChildren<ParticleSystem>(true)
            : Array.Empty<ParticleSystem>();
        if (systems.Length != 2)
            throw new InvalidOperationException("Wasteland atmosphere must contain exactly two particle systems.");
        foreach (ParticleSystem system in systems)
        {
            Material material = system.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            if (material == null || material.shader == null || !material.shader.name.Contains("Universal Render Pipeline"))
                throw new InvalidOperationException($"Atmosphere particle '{system.name}' is missing its URP material.");
        }

        Debug.Log($"Shadow/VFX validation passed: {playerRenderers.Length} player renderers and " +
                  $"{systems.Length} lightweight LDoE atmosphere systems are ready.");
    }

    private static void InstallPlayerShadowEnforcer()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (contents.GetComponent<PlayerShadowEnforcer>() == null)
                contents.AddComponent<PlayerShadowEnforcer>();
            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    public static void AddAtmosphere(Transform mapRoot)
    {
        EnsureAssets();
        Transform previous = mapRoot.Find(AtmosphereName);
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous.gameObject);

        Material dustMaterial = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        Material smokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath);
        GameObject atmosphere = new(AtmosphereName);
        atmosphere.transform.SetParent(mapRoot, false);

        CreateDust(atmosphere.transform, dustMaterial);
        CreateFog(atmosphere.transform, smokeMaterial);
    }

    private static void EnsureAssets()
    {
        EnsureFolder(OutputRoot);
        EnsureFolder(TextureRoot);
        EnsureFolder(MaterialRoot);
        BakeOpacityTexture("particle_dust.png", DustTexturePath, 0.02f, 0.55f);
        BakeOpacityTexture("smoke_gray.png", SmokeTexturePath, 0.48f, 0.42f);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureTexture(DustTexturePath, false);
        ConfigureTexture(SmokeTexturePath, false);
        CreateParticleMaterial(DustMaterialPath, DustTexturePath);
        CreateParticleMaterial(SmokeMaterialPath, SmokeTexturePath);
    }

    private static void BakeOpacityTexture(
        string sourceName,
        string destination,
        float backgroundCutoff,
        float opacityRange)
    {
        string source = Path.Combine(LdoeTextureRoot, sourceName).Replace('\\', '/');
        if (!File.Exists(source))
            throw new FileNotFoundException($"LDoE environment VFX texture was not found: {source}");

        Texture2D sourceTexture = new(2, 2, TextureFormat.RGBA32, false);
        if (!sourceTexture.LoadImage(File.ReadAllBytes(source)))
            throw new InvalidOperationException($"Could not read LDoE VFX texture: {source}");

        Color[] pixels = sourceTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float luminance = pixels[i].grayscale;
            float alpha = Mathf.Clamp01(
                (luminance - backgroundCutoff) / Mathf.Max(0.01f, opacityRange));
            alpha = Mathf.SmoothStep(0f, 1f, alpha);
            pixels[i] = new Color(1f, 1f, 1f, alpha);
        }

        Texture2D baked = new(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        baked.SetPixels(pixels);
        baked.Apply(false, false);
        File.WriteAllBytes(destination, baked.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(sourceTexture);
        UnityEngine.Object.DestroyImmediate(baked);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureTexture(string assetPath, bool alphaFromGrayscale)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = alphaFromGrayscale
            ? TextureImporterAlphaSource.FromGrayScale
            : TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static Material CreateParticleMaterial(string materialPath, string texturePath)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            throw new MissingReferenceException("URP Particles/Unlit shader was not found.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
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

    private static void CreateDust(Transform parent, Material material)
    {
        ParticleSystem particles = CreateParticleObject("Windblown Dust", parent, new Vector3(0f, 5.5f, 0f));
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.42f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.44f, 0.35f, 0.23f, 0.24f),
            new Color(0.82f, 0.68f, 0.43f, 0.48f));
        main.maxParticles = 130;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 15f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(56f, 10f, 56f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateFadeGradient(0.18f, 0.75f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        noise.frequency = 0.18f;
        noise.scrollSpeed = 0.08f;
        noise.damping = true;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 1.4f;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void CreateFog(Transform parent, Material material)
    {
        ParticleSystem particles = CreateParticleObject("Low Fog Wisps", parent, new Vector3(0f, 0.45f, 0f));
        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(15f, 22f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(7f, 14f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(2.2f, 4.5f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(1f, 1.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.28f, 0.25f, 0.21f, 0.09f),
            new Color(0.58f, 0.53f, 0.44f, 0.18f));
        main.maxParticles = 24;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0.9f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(48f, 0.8f, 48f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.strength = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        noise.frequency = 0.08f;
        noise.scrollSpeed = 0.04f;
        noise.damping = true;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = CreateFadeGradient(0.2f, 0.78f);

        ParticleSystem.TextureSheetAnimationModule sheet = particles.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Grid;
        sheet.numTilesX = 5;
        sheet.numTilesY = 5;
        sheet.animation = ParticleSystemAnimationType.WholeSheet;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
        renderer.sortingFudge = 0.25f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Gradient CreateFadeGradient(float fadeInEnd, float fadeOutStart)
    {
        Gradient fade = new();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, fadeInEnd),
                new GradientAlphaKey(1f, fadeOutStart),
                new GradientAlphaKey(0f, 1f)
            });
        return fade;
    }

    private static ParticleSystem CreateParticleObject(string name, Transform parent, Vector3 position)
    {
        GameObject item = new(name);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        ParticleSystem particles = item.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        return particles;
    }

    private static void FixPrefabShadows(string prefabPath)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer or TrailRenderer or LineRenderer) continue;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
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
