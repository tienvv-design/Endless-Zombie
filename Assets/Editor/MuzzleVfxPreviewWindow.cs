using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class MuzzleVfxPreviewWindow : EditorWindow
{
    private const string LightVfxPath =
        "Assets/VFX/LDoE/Weapon/Prefabs/MuzzleFlash1.prefab";
    private const string HeavyVfxPath =
        "Assets/VFX/LDoE/Weapon/Prefabs/MuzzleFlash2.prefab";
    private const string ShotgunVfxPath =
        "Assets/VFX/LDoE/Weapon/Prefabs/AttackSfx_Shotgun_Default.prefab";

    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private GunConfig targetGunConfig;
    [SerializeField] private bool previewProjectile = true;
    [SerializeField, Min(0.5f)] private float projectilePreviewDistance = 4f;
    [SerializeField, Min(0.1f)] private float projectilePreviewSpeed = 3f;
    private GameObject previewInstance;
    private GameObject projectilePreviewInstance;
    private Transform previewMuzzle;
    private Transform previewMuzzleVfxSocket;
    private Transform previewBulletSpawn;
    private double previewStartedAt;
    private string currentContextKey;

    [MenuItem("Tools/Endless Zombie/Muzzle VFX Preview")]
    public static void Open()
    {
        GetWindow<MuzzleVfxPreviewWindow>("Muzzle VFX Preview");
    }

    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
        Selection.selectionChanged += HandleSelectionChanged;
        SceneView.duringSceneGui += DrawProjectilePath;
        RefreshTargetFromContext(true);
        if (vfxPrefab == null && targetGunConfig == null)
            vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightVfxPath);
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        Selection.selectionChanged -= HandleSelectionChanged;
        SceneView.duringSceneGui -= DrawProjectilePath;
        StopPreview();
    }

    private void OnGUI()
    {
        RefreshTargetFromContext(false);

        EditorGUILayout.HelpBox(
            "Muzzle is the shared parent. Move/rotate BulletSpawn for the bullet/tracer, and MuzzleVfxSocket for the muzzle flash. " +
            "Saved prefab changes are also synchronized to the equipped gun during Play Mode. " +
            "Choose a Gun Config and press Equip to save this VFX for runtime firing.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        GunConfig selectedConfig = (GunConfig)EditorGUILayout.ObjectField(
            "Gun Config", targetGunConfig, typeof(GunConfig), false);
        if (EditorGUI.EndChangeCheck())
            SetTargetGunConfig(selectedConfig);

        if (targetGunConfig == null)
        {
            GunConfig detected = FindTargetGunConfig(out int matchCount);
            if (detected != null)
                SetTargetGunConfig(detected);
            else if (matchCount > 1)
                EditorGUILayout.HelpBox(
                    "This weapon prefab is shared by multiple Gun Config assets. Select the exact Gun Config above.",
                    MessageType.Warning);
        }

        vfxPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Muzzle VFX Prefab", vfxPrefab, typeof(GameObject), false);

        if (targetGunConfig != null)
        {
            string equippedName = targetGunConfig.MuzzleVfxPrefab != null
                ? targetGunConfig.MuzzleVfxPrefab.name
                : "None";
            EditorGUILayout.LabelField("Currently Equipped", equippedName);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(
                    "Bullet Prefab", targetGunConfig.ProjectilePrefab, typeof(GameObject), false);

            using (new EditorGUI.DisabledScope(targetGunConfig.HeldWeaponPrefab == null))
            {
                if (GUILayout.Button("Open Weapon Prefab & Select Bullet Spawn"))
                    OpenWeaponPrefab();
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Light"))
            SetVfx(LightVfxPath);
        if (GUILayout.Button("Heavy"))
            SetVfx(HeavyVfxPath);
        if (GUILayout.Button("Shotgun"))
            SetVfx(ShotgunVfxPath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        previewProjectile = EditorGUILayout.Toggle("Preview Bullet + Tracer", previewProjectile);
        if (previewProjectile)
        {
            projectilePreviewDistance = EditorGUILayout.Slider(
                "Preview Distance", projectilePreviewDistance, 0.5f, 12f);
            projectilePreviewSpeed = EditorGUILayout.Slider(
                "Preview Speed", projectilePreviewSpeed, 0.1f, 12f);
        }
        if (EditorGUI.EndChangeCheck() && previewMuzzle != null)
            StartPreview();

        EditorGUILayout.Space();
        if (previewMuzzle == null)
        {
            bool hasAnythingToPreview = vfxPrefab != null ||
                                        (previewProjectile && targetGunConfig != null &&
                                         targetGunConfig.ProjectilePrefab != null);
            using (new EditorGUI.DisabledScope(!hasAnythingToPreview))
            {
                if (GUILayout.Button("Show Muzzle + Bullet Preview"))
                    StartPreview();
            }
        }
        else
        {
            EditorGUILayout.LabelField(
                "Previewing", previewMuzzle != null ? previewMuzzle.root.name : "Muzzle");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Bullet Spawn") && previewBulletSpawn != null)
                SelectAndFrame(previewBulletSpawn);
            if (GUILayout.Button("Select Muzzle VFX") && previewMuzzleVfxSocket != null)
                SelectAndFrame(previewMuzzleVfxSocket);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Hide Preview"))
                StopPreview();
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(targetGunConfig == null || vfxPrefab == null))
        {
            if (GUILayout.Button("Equip VFX To Gun Config"))
                EquipVfx();
        }
    }

    private void EquipVfx()
    {
        if (targetGunConfig == null || vfxPrefab == null)
            return;

        Undo.RecordObject(targetGunConfig, "Equip Muzzle VFX");
        targetGunConfig.MuzzleVfxPrefab = vfxPrefab;
        EditorUtility.SetDirty(targetGunConfig);
        AssetDatabase.SaveAssetIfDirty(targetGunConfig);

        if (previewMuzzle != null)
            StartPreview();

        ShowNotification(new GUIContent($"Equipped {vfxPrefab.name} to {targetGunConfig.name}"));
        Debug.Log($"Equipped muzzle VFX '{vfxPrefab.name}' to GunConfig '{targetGunConfig.name}'.", targetGunConfig);
    }

    private void OpenWeaponPrefab()
    {
        if (targetGunConfig == null || targetGunConfig.HeldWeaponPrefab == null)
            return;

        string prefabPath = AssetDatabase.GetAssetPath(targetGunConfig.HeldWeaponPrefab);
        EnsureSeparateSocketsInPrefab(prefabPath);
        PrefabStage stage = PrefabStageUtility.OpenPrefab(prefabPath);
        Transform muzzle = stage != null ? FindChild(stage.prefabContentsRoot.transform, "Muzzle") : null;
        if (muzzle == null) return;
        SelectAndFrame(FindChild(muzzle, "BulletSpawn") ?? muzzle);
    }

    private void HandleSelectionChanged()
    {
        RefreshTargetFromContext(false);
        Repaint();
    }

    private void RefreshTargetFromContext(bool force)
    {
        string contextKey;
        GunConfig detected;

        if (Selection.activeObject is GunConfig selectedConfig)
        {
            detected = selectedConfig;
            contextKey = "config:" + AssetDatabase.GetAssetPath(selectedConfig);
        }
        else
        {
            string prefabPath = GetCurrentWeaponPrefabPath();
            contextKey = "prefab:" + prefabPath;
            detected = FindTargetGunConfig(out _);
        }

        if (!force && string.Equals(currentContextKey, contextKey, StringComparison.Ordinal))
            return;

        currentContextKey = contextKey;
        SetTargetGunConfig(detected);
    }

    private void SetTargetGunConfig(GunConfig config)
    {
        targetGunConfig = config;
        vfxPrefab = config != null ? config.MuzzleVfxPrefab : null;

        if (previewMuzzle != null)
        {
            if (vfxPrefab != null ||
                (previewProjectile && config != null && config.ProjectilePrefab != null))
                StartPreview();
            else
                StopPreview();
        }
    }

    private void SetVfx(string path)
    {
        vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (previewMuzzle != null)
            StartPreview();
    }

    private void StartPreview()
    {
        StopPreview();
        if (vfxPrefab == null &&
            (!previewProjectile || targetGunConfig == null || targetGunConfig.ProjectilePrefab == null))
            return;

        Transform muzzle = FindCurrentMuzzle();
        if (muzzle == null)
        {
            ShowNotification(new GUIContent("Open a gun prefab containing a Muzzle child first."));
            return;
        }

        previewMuzzle = muzzle;
        previewMuzzleVfxSocket = EnsureStageSocket(muzzle, "MuzzleVfxSocket");
        previewBulletSpawn = EnsureStageSocket(muzzle, "BulletSpawn");
        previewStartedAt = EditorApplication.timeSinceStartup;

        if (vfxPrefab != null)
        {
            previewInstance = Instantiate(vfxPrefab);
            previewInstance.name = "Muzzle VFX Preview (not saved)";
            previewInstance.hideFlags = HideFlags.HideAndDontSave;
            previewInstance.transform.SetParent(previewMuzzleVfxSocket, false);
            previewInstance.transform.localPosition = Vector3.zero;
            previewInstance.transform.localRotation = Quaternion.identity;
            previewInstance.transform.localScale = Vector3.one;

            foreach (ParticleSystem particles in previewInstance.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.Play(true);
            }
        }

        if (previewProjectile && targetGunConfig != null && targetGunConfig.ProjectilePrefab != null)
        {
            projectilePreviewInstance = Instantiate(targetGunConfig.ProjectilePrefab);
            projectilePreviewInstance.name = "Bullet Preview (not saved)";
            projectilePreviewInstance.hideFlags = HideFlags.HideAndDontSave;
            projectilePreviewInstance.transform.SetParent(previewBulletSpawn, false);
            SetProjectilePreviewTransform(0f);
        }

        SelectAndFrame(previewBulletSpawn);
        SceneView.RepaintAll();
        Repaint();
    }

    private void StopPreview()
    {
        if (previewInstance != null)
            DestroyImmediate(previewInstance);
        if (projectilePreviewInstance != null)
            DestroyImmediate(projectilePreviewInstance);
        previewInstance = null;
        projectilePreviewInstance = null;
        previewMuzzle = null;
        previewMuzzleVfxSocket = null;
        previewBulletSpawn = null;
        SceneView.RepaintAll();
        Repaint();
    }

    private void UpdatePreview()
    {
        if (previewMuzzle == null)
            return;

        float previewTime = (float)((EditorApplication.timeSinceStartup - previewStartedAt) % 1.5d);
        if (previewInstance != null)
            foreach (ParticleSystem particles in previewInstance.GetComponentsInChildren<ParticleSystem>(true))
                particles.Simulate(previewTime, true, true, false);

        if (projectilePreviewInstance != null)
        {
            float elapsed = (float)(EditorApplication.timeSinceStartup - previewStartedAt);
            float travel = Mathf.Repeat(elapsed * projectilePreviewSpeed, projectilePreviewDistance);
            SetProjectilePreviewTransform(travel);
        }
        SceneView.RepaintAll();
    }

    private void SetProjectilePreviewTransform(float distance)
    {
        if (projectilePreviewInstance == null || previewBulletSpawn == null) return;
        Vector3 direction = previewBulletSpawn.forward.normalized;
        projectilePreviewInstance.transform.SetPositionAndRotation(
            previewBulletSpawn.position + direction * distance,
            Quaternion.LookRotation(direction, previewBulletSpawn.up));
    }

    private void DrawProjectilePath(SceneView _)
    {
        if (previewBulletSpawn == null || !previewProjectile || targetGunConfig == null)
            return;

        Vector3 start = previewBulletSpawn.position;
        Vector3 direction = previewBulletSpawn.forward.normalized;
        Vector3 head = projectilePreviewInstance != null
            ? projectilePreviewInstance.transform.position
            : start;
        float tailLength = Mathf.Min(1.2f, Vector3.Distance(start, head));

        Handles.color = GetTracerColor(targetGunConfig.Archetype);
        Handles.DrawAAPolyLine(5f * targetGunConfig.ProjectileTracerScale,
            head - direction * tailLength, head);
        Handles.color = new Color(1f, 0.75f, 0.2f, 0.35f);
        Handles.DrawDottedLine(start, start + direction * projectilePreviewDistance, 5f);
    }

    private static Color GetTracerColor(GunArchetype archetype) => archetype switch
    {
        GunArchetype.RocketLauncher => new Color(1f, 0.26f, 0.035f, 1f),
        GunArchetype.GrenadeLauncher => new Color(1f, 0.48f, 0.08f, 1f),
        GunArchetype.TeslaGun => new Color(0.18f, 0.86f, 1f, 1f),
        GunArchetype.CryoGun => new Color(0.36f, 0.8f, 1f, 1f),
        GunArchetype.FlameRifle => new Color(1f, 0.23f, 0.025f, 1f),
        GunArchetype.SniperRifle => new Color(1f, 0.9f, 0.48f, 1f),
        GunArchetype.Shotgun => new Color(1f, 0.65f, 0.22f, 1f),
        _ => new Color(1f, 0.82f, 0.32f, 1f),
    };

    private static void EnsureSeparateSocketsInPrefab(string prefabPath)
    {
        if (string.IsNullOrEmpty(prefabPath)) return;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform muzzle = FindChild(root.transform, "Muzzle");
            if (muzzle == null) return;
            EnsureChild(muzzle, "MuzzleVfxSocket");
            EnsureChild(muzzle, "BulletSpawn");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Transform EnsureStageSocket(Transform muzzle, string socketName)
    {
        Transform existing = FindChild(muzzle, socketName);
        if (existing != null) return existing;
        Transform socket = new GameObject(socketName).transform;
        Undo.RegisterCreatedObjectUndo(socket.gameObject, $"Create {socketName}");
        socket.SetParent(muzzle, false);
        EditorSceneManager.MarkSceneDirty(muzzle.gameObject.scene);
        return socket;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform existing = FindChild(parent, childName);
        if (existing != null) return existing;
        Transform child = new GameObject(childName).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void SelectAndFrame(Transform target)
    {
        if (target == null) return;
        Selection.activeTransform = target;
        EditorGUIUtility.PingObject(target);
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static Transform FindCurrentMuzzle()
    {
        if (Selection.activeTransform != null)
        {
            Transform selectedMuzzle = FindChild(Selection.activeTransform, "Muzzle");
            if (selectedMuzzle != null)
                return selectedMuzzle;

            Transform selectedRoot = Selection.activeTransform.root;
            selectedMuzzle = FindChild(selectedRoot, "Muzzle");
            if (selectedMuzzle != null)
                return selectedMuzzle;
        }

        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        return stage != null ? FindChild(stage.prefabContentsRoot.transform, "Muzzle") : null;
    }

    private static GunConfig FindTargetGunConfig(out int matchCount)
    {
        matchCount = 0;
        if (Selection.activeObject is GunConfig selectedConfig)
        {
            matchCount = 1;
            return selectedConfig;
        }

        string weaponPrefabPath = GetCurrentWeaponPrefabPath();
        if (string.IsNullOrEmpty(weaponPrefabPath))
            return null;

        GunConfig result = null;
        foreach (string guid in AssetDatabase.FindAssets("t:GunConfig"))
        {
            string configPath = AssetDatabase.GUIDToAssetPath(guid);
            GunConfig config = AssetDatabase.LoadAssetAtPath<GunConfig>(configPath);
            if (config == null || config.HeldWeaponPrefab == null)
                continue;
            if (!string.Equals(
                    AssetDatabase.GetAssetPath(config.HeldWeaponPrefab),
                    weaponPrefabPath,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            matchCount++;
            result = config;
        }

        return matchCount == 1 ? result : null;
    }

    private static string GetCurrentWeaponPrefabPath()
    {
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null)
            return stage.assetPath;

        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
            return string.Empty;

        return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedObject);
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
