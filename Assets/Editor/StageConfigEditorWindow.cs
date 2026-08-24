using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class StageConfigEditorWindow : EditorWindow
{
    private const string StageFolder = "Assets/ScriptableObjects/WaveSpawn";

    [SerializeField] private StageConfig m_Stage;
    [SerializeField] private EnemyCatalog m_Catalog;
    private SerializedObject m_SerializedStage;
    private Vector2 m_Scroll;
    private readonly List<bool> m_WaveFoldouts = new();

    [MenuItem("Tools/Endless Zombie/Stage Config Editor")]
    public static void Open()
    {
        StageConfigEditorWindow window = GetWindow<StageConfigEditorWindow>("Stage Config");
        window.minSize = new Vector2(720f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        if (m_Stage == null) m_Stage = FindFirstAsset<StageConfig>();
        if (m_Catalog == null) m_Catalog = FindFirstAsset<EnemyCatalog>();
        RebuildSerializedStage();
    }

    private void OnGUI()
    {
        DrawToolbar();
        if (m_Stage == null)
        {
            EditorGUILayout.HelpBox("Chọn hoặc tạo một Stage Config để bắt đầu.", MessageType.Info);
            return;
        }

        if (m_SerializedStage == null || m_SerializedStage.targetObject != m_Stage)
            RebuildSerializedStage();
        m_SerializedStage.Update();

        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
        DrawStageSettings();
        EditorGUILayout.Space(8f);
        DrawWaves();
        EditorGUILayout.Space(8f);
        DrawValidation();
        EditorGUILayout.EndScrollView();

        if (m_SerializedStage.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(m_Stage);
            Repaint();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            StageConfig stage = (StageConfig)EditorGUILayout.ObjectField(m_Stage, typeof(StageConfig), false,
                GUILayout.MinWidth(180f));
            if (EditorGUI.EndChangeCheck())
            {
                m_Stage = stage;
                RebuildSerializedStage();
            }

            EditorGUI.BeginChangeCheck();
            m_Catalog = (EnemyCatalog)EditorGUILayout.ObjectField(m_Catalog, typeof(EnemyCatalog), false,
                GUILayout.MinWidth(180f));
            if (EditorGUI.EndChangeCheck()) Repaint();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("New Stage", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                CreateStageAsset();
            using (new EditorGUI.DisabledScope(m_Stage == null))
            {
                if (GUILayout.Button("Ping Stage", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    EditorGUIUtility.PingObject(m_Stage);
            }
            using (new EditorGUI.DisabledScope(m_Catalog == null))
            {
                if (GUILayout.Button("Open Catalog", EditorStyles.toolbarButton, GUILayout.Width(95f)))
                {
                    Selection.activeObject = m_Catalog;
                    EditorGUIUtility.PingObject(m_Catalog);
                }
            }
        }
    }

    private void DrawStageSettings()
    {
        EditorGUILayout.LabelField("STAGE SETTINGS", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("StageId"), new GUIContent("Stage ID"));
            EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("DefaultWaveDelay"),
                new GUIContent("Default Wave Delay", "Thời gian nghỉ mặc định giữa hai wave."));
            EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("MaxAliveEnemies"),
                new GUIContent("Max Alive Enemies", "Giới hạn quái sống đồng thời của stage."));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Elite Modifiers", EditorStyles.boldLabel);
            SerializedProperty enableElite = m_SerializedStage.FindProperty("EnableEliteModifiers");
            EditorGUILayout.PropertyField(enableElite, new GUIContent("Enable Elite Modifiers"));
            using (new EditorGUI.DisabledScope(!enableElite.boolValue))
            {
                EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("RandomEliteChance"),
                    new GUIContent("Mutation Chance", "Tỉ lệ quái thường nhận một Elite Modifier. Elite wave luôn được roll."));
                EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("EliteChanceStartsAtWave"),
                    new GUIContent("Starts At Wave", "Wave index bắt đầu cho phép elite xuất hiện ngẫu nhiên."));
            }
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("Boss"), new GUIContent("Boss Tuning"), true);
            EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("SpawnPortalDuration"),
                new GUIContent("Spawn Portal Duration", "Thời gian quái trồi lên khỏi portal trước khi bắt đầu di chuyển."));
            SerializedProperty outsideCamera = m_SerializedStage.FindProperty("SpawnOutsideCamera");
            EditorGUILayout.PropertyField(outsideCamera, new GUIContent("Spawn Outside Camera"));
            using (new EditorGUI.DisabledScope(!outsideCamera.boolValue))
                EditorGUILayout.PropertyField(m_SerializedStage.FindProperty("OffscreenSpawnPadding"),
                    new GUIContent("Offscreen Padding", "Khoảng đệm ngoài viền màn hình để portal không bị nhìn thấy."));

            SerializedProperty waves = m_SerializedStage.FindProperty("Waves");
            int totalEnemies = 0;
            float estimatedDuration = 0f;
            for (int i = 0; i < waves.arraySize; i++)
            {
                SerializedProperty wave = waves.GetArrayElementAtIndex(i);
                SerializedProperty entries = wave.FindPropertyRelative("SpawnEntries");
                estimatedDuration += Mathf.Max(0f, wave.FindPropertyRelative("WaveDelay").floatValue);
                for (int j = 0; j < entries.arraySize; j++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(j);
                    int quantity = Mathf.Max(0, entry.FindPropertyRelative("Quantity").intValue);
                    totalEnemies += quantity;
                    estimatedDuration += entry.FindPropertyRelative("SpawnDelay").floatValue;
                    estimatedDuration += Mathf.Max(0, quantity - 1) * entry.FindPropertyRelative("SpawnInterval").floatValue;
                }
            }
            EditorGUILayout.LabelField($"Overview: {waves.arraySize} waves  •  {totalEnemies} enemies  •  ~{estimatedDuration:0.0}s spawn timeline",
                EditorStyles.miniLabel);
        }
    }

    private void DrawWaves()
    {
        SerializedProperty waves = m_SerializedStage.FindProperty("Waves");
        EnsureFoldoutCount(waves.arraySize);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"WAVES ({waves.arraySize})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Wave", GUILayout.Width(105f)))
            {
                AddWave(waves);
                return;
            }
        }

        for (int i = 0; i < waves.arraySize; i++)
        {
            SerializedProperty wave = waves.GetArrayElementAtIndex(i);
            string waveId = wave.FindPropertyRelative("WaveId").stringValue;
            string title = string.IsNullOrWhiteSpace(waveId) ? $"Wave {i + 1}" : waveId;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_WaveFoldouts[i] = EditorGUILayout.Foldout(m_WaveFoldouts[i],
                        $"{i + 1}. {title}", true, EditorStyles.foldoutHeader);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button("▲", GUILayout.Width(28f))) { waves.MoveArrayElement(i, i - 1); return; }
                    using (new EditorGUI.DisabledScope(i == waves.arraySize - 1))
                        if (GUILayout.Button("▼", GUILayout.Width(28f))) { waves.MoveArrayElement(i, i + 1); return; }
                    if (GUILayout.Button("Duplicate", GUILayout.Width(72f))) { DuplicateElement(waves, i); return; }
                    if (GUILayout.Button("Remove", GUILayout.Width(62f)) &&
                        EditorUtility.DisplayDialog("Remove Wave", $"Xóa '{title}'?", "Remove", "Cancel"))
                    {
                        waves.DeleteArrayElementAtIndex(i);
                        return;
                    }
                }

                if (!m_WaveFoldouts[i]) continue;
                DrawWaveFields(wave, i);
            }
        }
    }

    private void DrawWaveFields(SerializedProperty wave, int waveIndex)
    {
        EditorGUILayout.PropertyField(wave.FindPropertyRelative("WaveId"), new GUIContent("Wave ID"));
        EditorGUILayout.PropertyField(wave.FindPropertyRelative("WaveType"));
        EditorGUILayout.PropertyField(wave.FindPropertyRelative("ActivationCondition"), new GUIContent("Activation"));

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("WaveDelay"), new GUIContent("Wave Delay"));
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("CompletionThreshold"), new GUIContent("Completion Threshold"));
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("MaxAliveEnemyOverride"), new GUIContent("Max Alive Override"));
        }

        SerializedProperty entries = wave.FindPropertyRelative("SpawnEntries");
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Spawn Entries ({entries.arraySize})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ Add Enemy", GUILayout.Width(105f)))
            {
                AddEntry(entries);
                return;
            }
        }

        for (int entryIndex = 0; entryIndex < entries.arraySize; entryIndex++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(entryIndex);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Entry {entryIndex + 1}", EditorStyles.miniBoldLabel, GUILayout.Width(58f));
                    DrawEnemyPopup(entry.FindPropertyRelative("EnemyId"));
                    if (GUILayout.Button("Duplicate", GUILayout.Width(70f))) { DuplicateElement(entries, entryIndex); return; }
                    if (GUILayout.Button("×", GUILayout.Width(26f))) { entries.DeleteArrayElementAtIndex(entryIndex); return; }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("Quantity"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("SpawnDelay"), new GUIContent("Start Delay"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("SpawnInterval"), new GUIContent("Interval"));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("SpawnArenaGroupId"), new GUIContent("Arena Group"));
                }
            }
        }

        if (entries.arraySize == 0)
            EditorGUILayout.HelpBox($"Wave {waveIndex + 1} chưa có enemy nào.", MessageType.Warning);
    }

    private void DrawEnemyPopup(SerializedProperty enemyId)
    {
        string[] ids = m_Catalog != null && m_Catalog.Enemies != null
            ? m_Catalog.Enemies.Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.EnemyId))
                .Select(entry => entry.EnemyId).Distinct().ToArray()
            : Array.Empty<string>();
        if (ids.Length == 0)
        {
            EditorGUILayout.PropertyField(enemyId, GUIContent.none, GUILayout.MinWidth(130f));
            return;
        }

        int current = Array.IndexOf(ids, enemyId.stringValue);
        int selected = EditorGUILayout.Popup(Mathf.Max(0, current), ids, GUILayout.MinWidth(130f));
        if (selected >= 0 && selected < ids.Length) enemyId.stringValue = ids[selected];
    }

    private void DrawValidation()
    {
        m_SerializedStage.ApplyModifiedProperties();
        List<string> problems = WaveSpawnConfigValidator.CollectProblems(m_Stage, m_Catalog);
        EditorGUILayout.LabelField("VALIDATION", EditorStyles.boldLabel);
        if (problems.Count == 0)
            EditorGUILayout.HelpBox("Stage config hợp lệ và sẵn sàng để bake.", MessageType.Info);
        else
            foreach (string problem in problems)
                EditorGUILayout.HelpBox(problem, MessageType.Error);
    }

    private void AddWave(SerializedProperty waves)
    {
        int index = waves.arraySize;
        waves.InsertArrayElementAtIndex(index);
        SerializedProperty wave = waves.GetArrayElementAtIndex(index);
        wave.FindPropertyRelative("WaveId").stringValue = $"Wave_{index + 1:00}";
        wave.FindPropertyRelative("WaveType").enumValueIndex = 0;
        wave.FindPropertyRelative("ActivationCondition").enumValueIndex = index == 0 ? 0 : 1;
        wave.FindPropertyRelative("WaveDelay").floatValue = -1f;
        wave.FindPropertyRelative("CompletionThreshold").intValue = 0;
        wave.FindPropertyRelative("MaxAliveEnemyOverride").intValue = 0;
        wave.FindPropertyRelative("SpawnEntries").arraySize = 0;
        EnsureFoldoutCount(waves.arraySize);
        m_WaveFoldouts[index] = true;
    }

    private void AddEntry(SerializedProperty entries)
    {
        int index = entries.arraySize;
        entries.InsertArrayElementAtIndex(index);
        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
        string defaultEnemy = m_Catalog != null && m_Catalog.Enemies != null
            ? m_Catalog.Enemies.FirstOrDefault(item => item != null && !string.IsNullOrWhiteSpace(item.EnemyId))?.EnemyId
            : string.Empty;
        entry.FindPropertyRelative("EnemyId").stringValue = defaultEnemy ?? string.Empty;
        entry.FindPropertyRelative("Quantity").intValue = 1;
        entry.FindPropertyRelative("SpawnDelay").floatValue = 0f;
        entry.FindPropertyRelative("SpawnInterval").floatValue = 1f;
        entry.FindPropertyRelative("SpawnArenaGroupId").stringValue = "Outer";
    }

    private static void DuplicateElement(SerializedProperty array, int index)
    {
        array.InsertArrayElementAtIndex(index);
    }

    private void EnsureFoldoutCount(int count)
    {
        while (m_WaveFoldouts.Count < count) m_WaveFoldouts.Add(true);
        while (m_WaveFoldouts.Count > count) m_WaveFoldouts.RemoveAt(m_WaveFoldouts.Count - 1);
    }

    private void RebuildSerializedStage()
    {
        m_SerializedStage = m_Stage != null ? new SerializedObject(m_Stage) : null;
        m_WaveFoldouts.Clear();
        if (m_Stage?.Waves != null)
            for (int i = 0; i < m_Stage.Waves.Length; i++) m_WaveFoldouts.Add(true);
    }

    private void CreateStageAsset()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(StageFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "WaveSpawn");
        string path = EditorUtility.SaveFilePanelInProject("Create Stage Config", "Stage_New", "asset",
            "Chọn vị trí lưu Stage Config.", StageFolder);
        if (string.IsNullOrWhiteSpace(path)) return;

        StageConfig stage = CreateInstance<StageConfig>();
        stage.StageId = System.IO.Path.GetFileNameWithoutExtension(path);
        stage.Waves = Array.Empty<WaveDefinition>();
        AssetDatabase.CreateAsset(stage, path);
        AssetDatabase.SaveAssets();
        m_Stage = stage;
        RebuildSerializedStage();
        Selection.activeObject = stage;
    }

    private static T FindFirstAsset<T>() where T : UnityEngine.Object
    {
        string guid = AssetDatabase.FindAssets($"t:{typeof(T).Name}").FirstOrDefault();
        return string.IsNullOrEmpty(guid)
            ? null
            : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }
}
