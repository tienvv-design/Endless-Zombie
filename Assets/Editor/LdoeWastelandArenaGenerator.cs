using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LdoeWastelandArenaGenerator
{
    private const string SourceRoot = "Assets/LDoE/WastelandSmall";
    private const string SourceAssetRoot = SourceRoot + "/Assets";
    private const string SourceMaterialRoot = SourceRoot + "/RuntimeMaterials";
    private const string OutputRoot = "Assets/LDoE/WastelandArena";
    private const string TextureRoot = OutputRoot + "/Textures";
    private const string GeneratedRoot = OutputRoot + "/Generated";
    private const string MaterialRoot = GeneratedRoot + "/Materials";
    private const string PrefabPath = GeneratedRoot + "/WastelandArena_Endless.prefab";
    private const string RuntimeResourceRoot = "Assets/Resources/StageMaps";
    private const string RuntimePrefabPath = RuntimeResourceRoot + "/WastelandArena_Endless.prefab";
    private const string PreviewImagePath = GeneratedRoot + "/WastelandArena_Preview.png";
    private const string AutoBuildSessionKey = "EndlessZombie.LdoeWastelandArenaGenerator.V2";

    private static readonly string[] RoadMeshPaths =
    {
        SourceAssetRoot + "/Road01.asset",
        SourceAssetRoot + "/Road02.asset",
        SourceAssetRoot + "/Road03.asset",
        SourceAssetRoot + "/Road04.asset",
        SourceAssetRoot + "/Road05.asset",
        SourceAssetRoot + "/Road06.asset",
        SourceAssetRoot + "/Road11.asset",
        SourceAssetRoot + "/Road22.asset",
        SourceAssetRoot + "/Road33.asset",
        SourceAssetRoot + "/Road44.asset",
        SourceAssetRoot + "/Road55.asset",
        SourceAssetRoot + "/Road66.asset"
    };

    private readonly struct Placement
    {
        public readonly Vector3 Position;
        public readonly float Yaw;
        public readonly float Scale;

        public Placement(float x, float z, float yaw, float scale = 1f)
        {
            Position = new Vector3(x, 0f, z);
            Yaw = yaw;
            Scale = scale;
        }
    }

    [InitializeOnLoadMethod]
    private static void ScheduleAutoBuild()
    {
        if (SessionState.GetBool(AutoBuildSessionKey, false)) return;
        EditorApplication.delayCall += TryAutoBuild;
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            BuildMap();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.delayCall += TryAutoBuild;
    }

    [MenuItem("Tools/Endless Zombie/Maps/Build New LDoE Wasteland Arena")]
    public static void BuildMap()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before generating the Wasteland arena.");

        EnsureFolder(OutputRoot);
        EnsureFolder(GeneratedRoot);
        EnsureFolder(MaterialRoot);
        EnsureFolder(RuntimeResourceRoot);

        Material groundMaterial = CreateTexturedMaterial(
            "ArenaWastelandGround", SourceAssetRoot + "/Mats/wasteland_sand.png",
            Color.white, new Vector2(6f, 6f), 0.08f, 0f, false);
        Material roadMaterial = CreateTexturedMaterial(
            "ArenaWastelandRoad", TextureRoot + "/road.png",
            Color.white, Vector2.one, 0.12f, 0f, false);
        Material roadMaterial2 = CreateTexturedMaterial(
            "ArenaWastelandRoad2", TextureRoot + "/roadM2.png",
            Color.white, Vector2.one, 0.12f, 0f, false);
        Material rockMaterial = CreateTexturedMaterial(
            "ArenaWastelandRock", TextureRoot + "/Wasteland_Decor_Rock02.png",
            new Color(0.8f, 0.75f, 0.68f), Vector2.one, 0.18f, 0f, false);
        Material rockMaterial2 = CreateTexturedMaterial(
            "ArenaWastelandRock3", TextureRoot + "/Wasteland_Decor_Rock03.png",
            new Color(0.8f, 0.75f, 0.68f), Vector2.one, 0.18f, 0f, false);
        Material bushMaterial = CreateTexturedMaterial(
            "ArenaWastelandBush", TextureRoot + "/Wasteland_Bush3.png",
            Color.white, Vector2.one, 0.08f, 0f, true);
        Material treeMaterial = CreateTexturedMaterial(
            "ArenaWastelandSmallTrees", TextureRoot + "/wasteland_small_trees.png",
            Color.white, Vector2.one, 0.08f, 0f, true);
        Material truckMaterial = CreateTexturedMaterial(
            "ArenaWastelandTruck", TextureRoot + "/wasteland_broken_truck.png",
            Color.white, Vector2.one, 0.24f, 0.18f, false);
        Material puddleMaterial = CreateTexturedMaterial(
            "ArenaWastelandPuddle", TextureRoot + "/wasteland_puddle_1.png",
            new Color(0.62f, 0.66f, 0.61f), Vector2.one, 0.86f, 0.05f, false);

        GameObject root = new("Wasteland Arena - LDoE");
        try
        {
            CreateGround(root.transform, groundMaterial);
            CreateRoadLoop(root.transform, roadMaterial, roadMaterial2);
            CreateCenterLandmark(root.transform, truckMaterial, rockMaterial, rockMaterial2);
            CreateRockFormations(root.transform, rockMaterial, rockMaterial2);
            CreateVegetation(root.transform, bushMaterial, treeMaterial);
            CreatePuddles(root.transform, puddleMaterial);
            CreateGameplayMarkers(root.transform);
            CreateBoundaries(root.transform);

            AddNavigationSurfaceIfAvailable(root);
            SetStaticExceptMarkers(root.transform);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Unity could not save the generated Wasteland arena prefab.");
            GameObject runtimePrefab = PrefabUtility.SaveAsPrefabAsset(root, RuntimePrefabPath);
            if (runtimePrefab == null)
                throw new InvalidOperationException("Unity could not save the Stage 2 runtime map prefab.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(RuntimePrefabPath, ImportAssetOptions.ForceUpdate);
        BuildPreviewSceneAndImage();
        Debug.Log($"Built new LDoE Wasteland arena at {PrefabPath}.");
    }

    [MenuItem("Tools/Endless Zombie/Maps/Validate LDoE Wasteland Arena Materials")]
    public static void ValidateMaterials()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new MissingReferenceException($"Missing generated map: {PrefabPath}");

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        HashSet<Material> materials = new();
        List<string> errors = new();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterials.Length == 0)
                errors.Add($"{renderer.name}: no material slots");
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    errors.Add($"{renderer.name}: null material");
                    continue;
                }
                materials.Add(material);
                if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") == null)
                    errors.Add($"{renderer.name}/{material.name}: missing Base Map texture");
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("Wasteland material validation failed:\n" + string.Join("\n", errors));

        Debug.Log($"Wasteland material validation passed: {renderers.Length} renderers, " +
                  $"{materials.Count} textured materials, 0 missing assignments.");
    }

    [MenuItem("Tools/Endless Zombie/Maps/Validate Stage 2 Runtime Map")]
    public static void ValidateStage2RuntimeMap()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject prefab = Resources.Load<GameObject>("StageMaps/WastelandArena_Endless");
        if (prefab == null) throw new MissingReferenceException("Stage 2 Resources map cannot be loaded.");

        Transform playerSpawn = FindChild(prefab.transform, "Spawn_Player");
        if (playerSpawn == null) throw new MissingReferenceException("Stage 2 map has no Spawn_Player marker.");
        int zombieSpawns = 0;
        foreach (Transform item in prefab.GetComponentsInChildren<Transform>(true))
            if (item.name.StartsWith("Spawn_Zombie_", StringComparison.Ordinal)) zombieSpawns++;
        if (zombieSpawns != 8)
            throw new InvalidOperationException($"Stage 2 map needs 8 zombie spawns, found {zombieSpawns}.");

        Type navigationType = Type.GetType("FlowFieldNavigationSurface, Assembly-CSharp");
        Component navigation = navigationType != null ? prefab.GetComponent(navigationType) : null;
        if (navigation == null) throw new MissingReferenceException("Stage 2 map has no flow-field navigation surface.");
        SerializedObject serialized = new(navigation);
        string walkableName = serialized.FindProperty("m_WalkableSurfaceName")?.stringValue;
        if (walkableName != "Wasteland_Ground_60x60")
            throw new InvalidOperationException($"Stage 2 flow-field ground is '{walkableName}'.");

        Debug.Log("Stage 2 runtime map validation passed: Resources load, player spawn, " +
                  "8 zombie spawns and Wasteland flow-field ground are ready.");
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item;
        return null;
    }

    private static void CreateGround(Transform parent, Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Wasteland_Ground_60x60";
        ground.transform.SetParent(parent, false);
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void CreateRoadLoop(Transform parent, Material material, Material material2)
    {
        Transform group = CreateGroup(parent, "Road Loop");
        Placement[] placements =
        {
            new(-22f, -17f, 0f), new(-12f, -18f, 8f), new(-2f, -17f, 18f),
            new(8f, -14f, 35f), new(16f, -8f, 60f), new(20f, 1f, 88f),
            new(19f, 11f, 112f), new(12f, 18f, 145f), new(2f, 21f, 175f),
            new(-9f, 20f, 196f), new(-18f, 15f, 225f), new(-22f, 7f, 258f),
            new(-23f, -3f, 275f), new(-20f, -11f, 305f)
        };

        for (int i = 0; i < placements.Length; i++)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RoadMeshPaths[i % RoadMeshPaths.Length]);
            if (mesh == null) continue;
            Placement placement = placements[i];
            Material roadMaterial = i % RoadMeshPaths.Length < 6 ? material : material2;
            GameObject road = CreateMeshObject($"Road_{i + 1:00}", mesh, roadMaterial, group);
            road.transform.localPosition = placement.Position + Vector3.up * 0.018f;
            road.transform.localRotation = Quaternion.Euler(0f, placement.Yaw, 0f);
            float horizontalSize = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z);
            float normalizedScale = horizontalSize > 0.001f ? 10f / horizontalSize : 1f;
            road.transform.localScale = Vector3.one * normalizedScale;
            road.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private static void CreateCenterLandmark(
        Transform parent, Material truckMaterial, Material rockMaterial, Material rockMaterial2)
    {
        Transform group = CreateGroup(parent, "Central Wreck");
        GameObject truck = InstantiateAsset(
            SourceAssetRoot + "/wasteland_broken_truck.prefab",
            "Broken Truck", group, new Placement(1.5f, 2.5f, -22f, 1.35f), truckMaterial, true);
        if (truck != null) truck.transform.localPosition += Vector3.up * 0.04f;

        GameObject westRocks = InstantiateAsset(SourceAssetRoot + "/Wasteland_Decor_Rock.prefab",
            "Wreck Rocks West", group, new Placement(-5.5f, 1f, 32f, 0.75f), rockMaterial, true);
        ApplyRockMaterials(westRocks, rockMaterial, rockMaterial2);
        GameObject eastRocks = InstantiateAsset(SourceAssetRoot + "/Wasteland_Decor_Rock.prefab",
            "Wreck Rocks East", group, new Placement(7f, 4f, 210f, 0.6f), rockMaterial, true);
        ApplyRockMaterials(eastRocks, rockMaterial, rockMaterial2);
    }

    private static void CreateRockFormations(Transform parent, Material material, Material material2)
    {
        Transform group = CreateGroup(parent, "Rock Formations");
        Placement[] placements =
        {
            new(-25f, -24f, 18f, 0.9f), new(-11f, -25f, 74f, 0.7f),
            new(12f, -25f, 150f, 0.65f), new(25f, -20f, 210f, 0.85f),
            new(25f, 20f, 280f, 0.75f), new(10f, 26f, 10f, 0.8f),
            new(-12f, 26f, 90f, 0.65f), new(-25f, 21f, 185f, 0.85f),
            new(-12f, -7f, 28f, 0.55f), new(12f, 10f, 165f, 0.55f)
        };

        for (int i = 0; i < placements.Length; i++)
        {
            GameObject rocks = InstantiateAsset(SourceAssetRoot + "/Wasteland_Decor_Rock.prefab",
                $"Rock Cluster {i + 1:00}", group, placements[i], material, true);
            ApplyRockMaterials(rocks, material, material2);
        }
    }

    private static void CreateVegetation(Transform parent, Material bushMaterial, Material treeMaterial)
    {
        Transform trees = CreateGroup(parent, "Dead Trees");
        Placement[] treePlacements =
        {
            new(-27f, -17f, 12f, 1.2f), new(-27f, 2f, 85f, 0.9f),
            new(-23f, 26f, 155f, 1.1f), new(-4f, 27f, 220f, 0.8f),
            new(19f, 25f, 300f, 1.15f), new(27f, 8f, 34f, 0.95f),
            new(26f, -12f, 110f, 1.1f), new(8f, -27f, 190f, 0.85f)
        };
        for (int i = 0; i < treePlacements.Length; i++)
            InstantiateAsset(SourceAssetRoot + "/wasteland_small_trees.prefab",
                $"Dead Trees {i + 1:00}", trees, treePlacements[i], treeMaterial, false);

        Transform bushes = CreateGroup(parent, "Scrub");
        Placement[] bushPlacements =
        {
            new(-25f, -8f, 40f, 1.1f), new(-19f, -25f, 95f, 0.8f),
            new(-4f, -26f, 170f, 1f), new(18f, -24f, 240f, 0.9f),
            new(27f, -3f, 300f, 1.15f), new(26f, 15f, 22f, 0.85f),
            new(16f, 27f, 75f, 1f), new(-2f, 26f, 145f, 1.1f),
            new(-18f, 24f, 200f, 0.9f), new(-27f, 12f, 270f, 1.05f),
            new(-14f, 8f, 30f, 0.75f), new(14f, -5f, 130f, 0.7f)
        };
        for (int i = 0; i < bushPlacements.Length; i++)
        {
            string prefab = i % 2 == 0 ? "/Wasteland_Bush.prefab" : "/Wasteland_Bush2.prefab";
            InstantiateAsset(SourceAssetRoot + prefab, $"Scrub {i + 1:00}", bushes,
                bushPlacements[i], bushMaterial, false);
        }
    }

    private static void CreatePuddles(Transform parent, Material material)
    {
        Transform group = CreateGroup(parent, "Puddles");
        Placement[] placements =
        {
            new(-15f, -17f, 8f, 1.3f), new(7f, -15f, 28f, 0.9f),
            new(19f, 8f, 88f, 1.15f), new(-8f, 20f, 195f, 1f),
            new(-21f, 3f, 265f, 0.85f)
        };
        for (int i = 0; i < placements.Length; i++)
        {
            GameObject puddle = InstantiateAsset(SourceAssetRoot + "/wasteland_puddle.prefab",
                $"Puddle {i + 1:00}", group, placements[i], material, false);
            if (puddle != null) puddle.transform.localPosition += Vector3.up * 0.025f;
        }
    }

    private static void CreateGameplayMarkers(Transform parent)
    {
        Transform group = CreateGroup(parent, "Gameplay Markers");
        CreateMarker(group, "Spawn_Player", new Vector3(0f, 0.05f, -8f), Color.cyan);
        Vector3[] zombieSpawns =
        {
            new(-25f, 0.05f, -25f), new(0f, 0.05f, -27f), new(25f, 0.05f, -25f),
            new(27f, 0.05f, 0f), new(25f, 0.05f, 25f), new(0f, 0.05f, 27f),
            new(-25f, 0.05f, 25f), new(-27f, 0.05f, 0f)
        };
        for (int i = 0; i < zombieSpawns.Length; i++)
            CreateMarker(group, $"Spawn_Zombie_{i + 1:00}", zombieSpawns[i], new Color(0.75f, 0.1f, 0.08f));
    }

    private static void CreateBoundaries(Transform parent)
    {
        Transform group = CreateGroup(parent, "Boundary Colliders");
        CreateBoundary(group, "Boundary_North", new Vector3(0f, 1.5f, 30.5f), new Vector3(62f, 3f, 1f));
        CreateBoundary(group, "Boundary_South", new Vector3(0f, 1.5f, -30.5f), new Vector3(62f, 3f, 1f));
        CreateBoundary(group, "Boundary_East", new Vector3(30.5f, 1.5f, 0f), new Vector3(1f, 3f, 62f));
        CreateBoundary(group, "Boundary_West", new Vector3(-30.5f, 1.5f, 0f), new Vector3(1f, 3f, 62f));
    }

    private static GameObject InstantiateAsset(
        string assetPath, string name, Transform parent, Placement placement,
        Material material, bool addColliders)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
        {
            Debug.LogWarning($"Wasteland source asset was not found: {assetPath}");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null) return null;
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = placement.Position;
        instance.transform.localRotation = Quaternion.Euler(0f, placement.Yaw, 0f);
        instance.transform.localScale = Vector3.one * placement.Scale;
        ApplyMaterial(instance, material);
        if (addColliders) AddMeshColliders(instance);
        return instance;
    }

    private static GameObject CreateMeshObject(string name, Mesh mesh, Material material, Transform parent)
    {
        GameObject instance = new(name);
        instance.transform.SetParent(parent, false);
        instance.AddComponent<MeshFilter>().sharedMesh = mesh;
        instance.AddComponent<MeshRenderer>().sharedMaterial = material;
        return instance;
    }

    private static void ApplyMaterial(GameObject root, Material material)
    {
        if (root == null) return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            int slots = Mathf.Max(1, renderer.sharedMaterials.Length);
            Material[] materials = new Material[slots];
            for (int i = 0; i < slots; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private static void ApplyRockMaterials(GameObject root, Material rock02, Material rock03)
    {
        if (root == null) return;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            string name = renderer.name.ToLowerInvariant();
            Material material = name.EndsWith("2", StringComparison.Ordinal) ||
                                name.EndsWith("4", StringComparison.Ordinal)
                ? rock03
                : rock02;
            int slots = Mathf.Max(1, renderer.sharedMaterials.Length);
            Material[] materials = new Material[slots];
            for (int i = 0; i < slots; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private static void AddMeshColliders(GameObject root)
    {
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
        }
    }

    private static void CreateMarker(Transform parent, string name, Vector3 position, Color color)
    {
        GameObject marker = new(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = position;
        marker.tag = "Untagged";
        marker.isStatic = false;
    }

    private static void CreateBoundary(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject boundary = new(name);
        boundary.transform.SetParent(parent, false);
        boundary.transform.localPosition = center;
        BoxCollider collider = boundary.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static Material CreateTexturedMaterial(
        string name,
        string texturePath,
        Color color,
        Vector2 tiling,
        float smoothness,
        float metallic,
        bool doubleSided)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new MissingReferenceException("Universal Render Pipeline/Lit shader is unavailable.");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null) throw new MissingReferenceException($"Missing Wasteland texture: {texturePath}");

        string destinationPath = $"{MaterialRoot}/{name}.mat";
        Material destination = AssetDatabase.LoadAssetAtPath<Material>(destinationPath);
        if (destination == null)
        {
            destination = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(destination, destinationPath);
        }

        destination.shader = shader;
        destination.name = name;
        destination.SetTexture("_BaseMap", texture);
        destination.SetTextureScale("_BaseMap", tiling);
        destination.SetColor("_BaseColor", color);
        destination.SetFloat("_Smoothness", smoothness);
        destination.SetFloat("_Metallic", metallic);
        destination.SetFloat("_Cull", doubleSided ? 0f : 2f);
        destination.doubleSidedGI = doubleSided;
        destination.enableInstancing = true;
        EditorUtility.SetDirty(destination);
        return destination;
    }

    private static void SetStaticExceptMarkers(Transform root)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            item.gameObject.isStatic = !item.name.StartsWith("Spawn_", StringComparison.Ordinal);
    }

    private static void AddNavigationSurfaceIfAvailable(GameObject root)
    {
        Type navigationType = Type.GetType("FlowFieldNavigationSurface, Assembly-CSharp");
        if (navigationType != null && typeof(Component).IsAssignableFrom(navigationType))
        {
            Component navigation = root.AddComponent(navigationType);
            SerializedObject serialized = new(navigation);
            SerializedProperty walkableName = serialized.FindProperty("m_WalkableSurfaceName");
            if (walkableName != null) walkableName.stringValue = "Wasteland_Ground_60x60";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void BuildPreviewSceneAndImage()
    {
        const int previewLayer = 31;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return;

        bool batchMode = Application.isBatchMode;
        Scene previewScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            batchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        try
        {
            GameObject map = PrefabUtility.InstantiatePrefab(prefab, previewScene) as GameObject;
            if (map != null)
            {
                map.name = "Wasteland Arena - LDoE";
                foreach (Transform item in map.GetComponentsInChildren<Transform>(true))
                    item.gameObject.layer = previewLayer;
            }

            GameObject lightObject = new("Preview Sun");
            SceneManager.MoveGameObjectToScene(lightObject, previewScene);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.83f, 0.63f);
            light.cullingMask = 1 << previewLayer;
            lightObject.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

            GameObject cameraObject = new("Preview Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(38f, 48f, -42f);
            camera.transform.LookAt(new Vector3(0f, 0f, 0f));
            camera.orthographic = true;
            camera.orthographicSize = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.075f, 0.055f);
            camera.cullingMask = 1 << previewLayer;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;

            RenderCamera(camera, PreviewImagePath);
        }
        finally
        {
            if (!batchMode) EditorSceneManager.CloseScene(previewScene, true);
        }
    }

    private static void RenderCamera(Camera camera, string assetPath)
    {
        const int size = 1024;
        RenderTexture texture = new(size, size, 24, RenderTextureFormat.ARGB32);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            Texture2D image = new(size, size, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            image.Apply();
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(absolutePath, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
