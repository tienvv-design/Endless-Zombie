using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class EnemyAnimationPreviewWindow : EditorWindow
{
    private sealed class ClipOption
    {
        public string Label;
        public AnimationClip Clip;
        public bool ProceduralDogBite;
        public float Duration;
    }

    private sealed class EnemyOption
    {
        public string Label;
        public GameObject Prefab;
        public RuntimeAnimatorController Controller;
        public float RuntimeScale = 1f;
        public readonly List<ClipOption> Clips = new();
    }

    private readonly List<EnemyOption> m_Enemies = new();
    private PreviewRenderUtility m_Preview;
    private GameObject m_Instance;
    private Animator m_PreviewAnimator;
    private Bounds m_ModelBounds;
    private int m_EnemyIndex;
    private int m_ClipIndex;
    private bool m_Playing = true;
    private float m_Time;
    private float m_Speed = 1f;
    private float m_Yaw = 150f;
    private float m_Pitch = 12f;
    private float m_Zoom = 1f;
    private double m_LastEditorTime;

    [MenuItem("Tools/Endless Zombie/Enemy Animation Preview")]
    public static void Open()
    {
        GetWindow<EnemyAnimationPreviewWindow>("Enemy Animations");
    }

    private void OnEnable()
    {
        BuildEnemyOptions();
        CreatePreview();
        m_LastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdatePlayback;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePlayback;
        DestroyPreview();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Preview the exact enemy models and animation clips configured by MobVisualSettings. " +
            "Drag inside the preview to rotate and use the mouse wheel to zoom.",
            MessageType.Info);

        if (m_Enemies.Count == 0)
        {
            EditorGUILayout.HelpBox("MobVisualSettings or enemy prefabs could not be found.", MessageType.Error);
            if (GUILayout.Button("Reload Settings")) Reload();
            return;
        }

        string[] enemyNames = m_Enemies.ConvertAll(item => item.Label).ToArray();
        EditorGUI.BeginChangeCheck();
        m_EnemyIndex = EditorGUILayout.Popup("Enemy", Mathf.Clamp(m_EnemyIndex, 0, m_Enemies.Count - 1), enemyNames);
        if (EditorGUI.EndChangeCheck())
        {
            m_ClipIndex = 0;
            m_Time = 0f;
            CreatePreview();
        }

        EnemyOption enemy = CurrentEnemy;
        string[] clipNames = enemy.Clips.ConvertAll(item => item.Label).ToArray();
        EditorGUI.BeginChangeCheck();
        m_ClipIndex = EditorGUILayout.Popup("Animation", Mathf.Clamp(m_ClipIndex, 0, enemy.Clips.Count - 1), clipNames);
        if (EditorGUI.EndChangeCheck())
            m_Time = 0f;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Prefab", enemy.Prefab, typeof(GameObject), false);
            EditorGUILayout.ObjectField("Controller", enemy.Controller, typeof(RuntimeAnimatorController), false);
        }
        EditorGUILayout.LabelField("Runtime Scale", enemy.RuntimeScale.ToString("0.###"));

        Rect previewRect = GUILayoutUtility.GetRect(240f, 10000f, 220f, 10000f, GUILayout.ExpandWidth(true));
        DrawPreview(previewRect);
        HandlePreviewInput(previewRect);

        ClipOption option = CurrentClip;
        float duration = Mathf.Max(0.05f, option.Duration);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(m_Playing ? "Pause" : "Play", GUILayout.Width(70f)))
            m_Playing = !m_Playing;
        if (GUILayout.Button("Restart", GUILayout.Width(70f)))
            m_Time = 0f;
        m_Speed = EditorGUILayout.Slider("Speed", m_Speed, 0.1f, 3f);
        EditorGUILayout.EndHorizontal();

        m_Time = EditorGUILayout.Slider("Timeline", m_Time, 0f, duration);
        EditorGUILayout.LabelField("Time", $"{m_Time:0.00}s / {duration:0.00}s");

        if (option.ProceduralDogBite)
            EditorGUILayout.HelpBox(
                "Dog preview = DogMutant Run controller + the custom procedural bite layer. " +
                "No humanoid attack clip is applied.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Prefab"))
        {
            Selection.activeObject = enemy.Prefab;
            EditorGUIUtility.PingObject(enemy.Prefab);
        }
        if (GUILayout.Button("Open Prefab"))
            AssetDatabase.OpenAsset(enemy.Prefab);
        if (GUILayout.Button("Reload Settings")) Reload();
        EditorGUILayout.EndHorizontal();
    }

    private EnemyOption CurrentEnemy => m_Enemies[Mathf.Clamp(m_EnemyIndex, 0, m_Enemies.Count - 1)];
    private ClipOption CurrentClip => CurrentEnemy.Clips[Mathf.Clamp(m_ClipIndex, 0, CurrentEnemy.Clips.Count - 1)];

    private void BuildEnemyOptions()
    {
        m_Enemies.Clear();
        MobVisualSettings settings = AssetDatabase.LoadAssetAtPath<MobVisualSettings>(
            "Assets/Resources/MobVisualSettings.asset");
        if (settings == null) return;

        if (settings.ZombieVisualPrefabs != null)
        {
            foreach (GameObject prefab in settings.ZombieVisualPrefabs)
            {
                if (prefab == null) continue;
                EnemyOption common = NewEnemy($"Common / {prefab.name}", prefab,
                    settings.AnimatorController, settings.HumanoidScale);
                AddClips(common, "Walk", settings.ZombieLocomotionClips);
                AddClips(common, "Attack", settings.ZombieAttackClips);
                if (common.Clips.Count == 0 && settings.ZombieAttackClip != null)
                    AddClip(common, "Attack", settings.ZombieAttackClip);
                EnsureClip(common);
                m_Enemies.Add(common);
            }
        }

        AddSpecial("Special / Zombie Fat", settings.ZombieFatVisualPrefab,
            settings.ZombieFatLocomotionClip, settings.ZombieFatAttackClip, settings.SpecialZombieScale);
        AddSpecial("Special / Zombie Squat", settings.ZombieSquatVisualPrefab,
            settings.ZombieSquatLocomotionClip, settings.ZombieSquatAttackClip, settings.SpecialZombieScale);
        AddSpecial("Special / Zombie Tank", settings.ZombieTankVisualPrefab,
            settings.ZombieTankLocomotionClip, settings.ZombieTankAttackClip, settings.SpecialZombieScale);
        AddSpecial("Boss / Zombie Witch", settings.ZombieWitchVisualPrefab,
            settings.ZombieWitchLocomotionClip, settings.ZombieWitchAttackClip, settings.SpecialZombieScale);

        if (settings.DogMutantVisualPrefab != null)
        {
            EnemyOption dog = NewEnemy("Dog / Zombie Dog", settings.DogMutantVisualPrefab,
                settings.DogMutantAnimatorController, 1f);
            AnimationClip run = settings.DogMutantAnimatorController != null &&
                                settings.DogMutantAnimatorController.animationClips.Length > 0
                ? settings.DogMutantAnimatorController.animationClips[0]
                : null;
            AddClip(dog, "Run", run, false,
                run != null ? run.length : settings.DogMutantRunLoopDuration);
            AddClip(dog, "Run + Procedural Bite", run, true, 0.7f);
            EnsureClip(dog);
            m_Enemies.Add(dog);
        }
    }

    private void AddSpecial(string label, GameObject prefab, AnimationClip locomotion,
        AnimationClip attack, float scale)
    {
        if (prefab == null) return;
        EnemyOption option = NewEnemy(label, prefab, null, scale);
        AddClip(option, "Movement", locomotion);
        AddClip(option, "Attack", attack);
        EnsureClip(option);
        m_Enemies.Add(option);
    }

    private static EnemyOption NewEnemy(string label, GameObject prefab,
        RuntimeAnimatorController controller, float scale)
    {
        return new EnemyOption
        {
            Label = label,
            Prefab = prefab,
            Controller = controller,
            RuntimeScale = Mathf.Max(0.01f, scale),
        };
    }

    private static void AddClips(EnemyOption enemy, string prefix, AnimationClip[] clips)
    {
        if (clips == null) return;
        foreach (AnimationClip clip in clips)
            AddClip(enemy, prefix, clip);
    }

    private static void AddClip(EnemyOption enemy, string prefix, AnimationClip clip,
        bool proceduralDogBite = false, float duration = 0f)
    {
        if (clip == null && !proceduralDogBite) return;
        enemy.Clips.Add(new ClipOption
        {
            Label = clip != null ? $"{prefix} / {clip.name}" : prefix,
            Clip = clip,
            ProceduralDogBite = proceduralDogBite,
            Duration = duration > 0f ? duration : Mathf.Max(0.05f, clip.length),
        });
    }

    private static void EnsureClip(EnemyOption enemy)
    {
        if (enemy.Clips.Count > 0) return;
        enemy.Clips.Add(new ClipOption { Label = "Bind Pose", Duration = 1f });
    }

    private void CreatePreview()
    {
        DestroyPreview();
        if (m_Enemies.Count == 0 || CurrentEnemy.Prefab == null) return;

        m_Preview = new PreviewRenderUtility();
        m_Preview.camera.fieldOfView = 30f;
        m_Preview.camera.nearClipPlane = 0.01f;
        m_Preview.camera.farClipPlane = 1000f;
        m_Preview.camera.clearFlags = CameraClearFlags.Color;
        m_Preview.camera.backgroundColor = new Color(0.055f, 0.065f, 0.075f, 1f);
        m_Preview.lights[0].intensity = 1.25f;
        m_Preview.lights[0].transform.rotation = Quaternion.Euler(42f, 35f, 0f);
        m_Preview.lights[1].intensity = 0.65f;

        m_Instance = Instantiate(CurrentEnemy.Prefab);
        m_Instance.name = "Enemy Animation Preview (Not Saved)";
        m_Instance.hideFlags = HideFlags.HideAndDontSave;
        m_Instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        m_Instance.transform.localScale = Vector3.one * CurrentEnemy.RuntimeScale;
        foreach (Transform child in m_Instance.GetComponentsInChildren<Transform>(true))
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        m_PreviewAnimator = m_Instance.GetComponentInChildren<Animator>(true);
        if (m_PreviewAnimator == null)
            m_PreviewAnimator = m_Instance.AddComponent<Animator>();
        if (CurrentEnemy.Controller != null)
            m_PreviewAnimator.runtimeAnimatorController = CurrentEnemy.Controller;
        m_PreviewAnimator.applyRootMotion = false;
        m_PreviewAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        m_Preview.AddSingleGO(m_Instance);
        SampleCurrentAnimation();
        m_ModelBounds = CalculateBounds(m_Instance);
    }

    private void DestroyPreview()
    {
        if (m_Preview != null)
            m_Preview.Cleanup();
        else if (m_Instance != null)
            DestroyImmediate(m_Instance);
        m_Preview = null;
        m_Instance = null;
        m_PreviewAnimator = null;
    }

    private void SampleCurrentAnimation()
    {
        if (m_Instance == null || m_Enemies.Count == 0) return;
        ClipOption option = CurrentClip;
        if (option.Clip != null)
        {
            GameObject animationRoot = m_PreviewAnimator != null
                ? m_PreviewAnimator.gameObject
                : m_Instance;
            option.Clip.SampleAnimation(
                animationRoot,
                Mathf.Repeat(m_Time, Mathf.Max(0.05f, option.Clip.length)));
        }
        if (option.ProceduralDogBite)
        {
            DogAttackProceduralAnimator dog = m_Instance.GetComponent<DogAttackProceduralAnimator>();
            if (dog == null) dog = m_Instance.AddComponent<DogAttackProceduralAnimator>();
            dog.EvaluatePreview(m_Time / Mathf.Max(0.05f, option.Duration));
        }
    }

    private void DrawPreview(Rect rect)
    {
        if (m_Preview == null || m_Instance == null)
        {
            EditorGUI.DrawRect(rect, new Color(0.055f, 0.065f, 0.075f));
            return;
        }

        SampleCurrentAnimation();
        float radius = Mathf.Max(0.5f, m_ModelBounds.extents.magnitude);
        Quaternion orbit = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
        Vector3 direction = orbit * Vector3.forward;
        float distance = radius * 3.2f * m_Zoom;
        m_Preview.camera.transform.position = m_ModelBounds.center - direction * distance;
        m_Preview.camera.transform.LookAt(m_ModelBounds.center + Vector3.up * radius * 0.05f);

        m_Preview.BeginPreview(rect, GUIStyle.none);
        m_Preview.camera.Render();
        Texture result = m_Preview.EndPreview();
        GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);
    }

    private void HandlePreviewInput(Rect rect)
    {
        Event current = Event.current;
        if (!rect.Contains(current.mousePosition)) return;
        if (current.type == EventType.MouseDrag && current.button == 0)
        {
            m_Yaw += current.delta.x * 0.7f;
            m_Pitch = Mathf.Clamp(m_Pitch - current.delta.y * 0.5f, -20f, 65f);
            current.Use();
            Repaint();
        }
        else if (current.type == EventType.ScrollWheel)
        {
            m_Zoom = Mathf.Clamp(m_Zoom * (1f + current.delta.y * 0.06f), 0.45f, 2.5f);
            current.Use();
            Repaint();
        }
    }

    private void UpdatePlayback()
    {
        double now = EditorApplication.timeSinceStartup;
        float delta = (float)Math.Min(0.1d, now - m_LastEditorTime);
        m_LastEditorTime = now;
        if (!m_Playing || m_Enemies.Count == 0) return;
        float duration = Mathf.Max(0.05f, CurrentClip.Duration);
        m_Time = Mathf.Repeat(m_Time + delta * m_Speed, duration);
        Repaint();
    }

    private void Reload()
    {
        m_EnemyIndex = 0;
        m_ClipIndex = 0;
        m_Time = 0f;
        BuildEnemyOptions();
        CreatePreview();
        Repaint();
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(Vector3.up, Vector3.one * 2f);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
