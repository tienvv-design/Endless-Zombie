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
    private GameObject previewInstance;
    private Transform previewMuzzle;
    private double previewStartedAt;

    [MenuItem("Tools/Endless Zombie/Muzzle VFX Preview")]
    public static void Open()
    {
        GetWindow<MuzzleVfxPreviewWindow>("Muzzle VFX Preview");
    }

    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
        if (vfxPrefab == null)
            vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LightVfxPath);
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        StopPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Open a gun prefab, select its Muzzle child, then preview the effect while editing the Muzzle Transform.",
            MessageType.Info);

        vfxPrefab = (GameObject)EditorGUILayout.ObjectField(
            "VFX Prefab", vfxPrefab, typeof(GameObject), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Light"))
            SetVfx(LightVfxPath);
        if (GUILayout.Button("Heavy"))
            SetVfx(HeavyVfxPath);
        if (GUILayout.Button("Shotgun"))
            SetVfx(ShotgunVfxPath);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        if (previewInstance == null)
        {
            using (new EditorGUI.DisabledScope(vfxPrefab == null))
            {
                if (GUILayout.Button("Show Preview"))
                    StartPreview();
            }
        }
        else
        {
            EditorGUILayout.LabelField(
                "Previewing", previewMuzzle != null ? previewMuzzle.root.name : "Muzzle");
            if (GUILayout.Button("Hide Preview"))
                StopPreview();
        }
    }

    private void SetVfx(string path)
    {
        vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (previewInstance != null)
            StartPreview();
    }

    private void StartPreview()
    {
        StopPreview();
        if (vfxPrefab == null)
            return;

        Transform muzzle = FindCurrentMuzzle();
        if (muzzle == null)
        {
            ShowNotification(new GUIContent("Open a gun prefab containing a Muzzle child first."));
            return;
        }

        previewInstance = Instantiate(vfxPrefab);
        previewInstance.name = "Muzzle VFX Preview (not saved)";
        previewInstance.hideFlags = HideFlags.HideAndDontSave;
        previewInstance.transform.SetParent(muzzle, false);
        previewInstance.transform.localPosition = Vector3.zero;
        previewInstance.transform.localRotation = Quaternion.identity;
        previewInstance.transform.localScale = Vector3.one;
        previewMuzzle = muzzle;
        previewStartedAt = EditorApplication.timeSinceStartup;

        foreach (ParticleSystem particles in previewInstance.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }

        Selection.activeTransform = muzzle;
        SceneView.lastActiveSceneView?.FrameSelected();
        SceneView.RepaintAll();
        Repaint();
    }

    private void StopPreview()
    {
        if (previewInstance != null)
            DestroyImmediate(previewInstance);
        previewInstance = null;
        previewMuzzle = null;
        SceneView.RepaintAll();
        Repaint();
    }

    private void UpdatePreview()
    {
        if (previewInstance == null || previewMuzzle == null)
            return;

        float previewTime = (float)((EditorApplication.timeSinceStartup - previewStartedAt) % 1.5d);
        foreach (ParticleSystem particles in previewInstance.GetComponentsInChildren<ParticleSystem>(true))
            particles.Simulate(previewTime, true, true, false);
        SceneView.RepaintAll();
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
