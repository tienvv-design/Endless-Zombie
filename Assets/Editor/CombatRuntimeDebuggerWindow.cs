#if UNITY_EDITOR
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class CombatRuntimeDebuggerWindow : EditorWindow
{
    private Vector2 m_Scroll;
    private double m_NextRepaint;
    private bool m_GunLoaded;
    private int m_Damage, m_Projectiles, m_Magazine, m_Pierce;
    private float m_FireRate, m_Reload, m_CritChance, m_CritDamage, m_ProjectileSpeed, m_Knockback;
    private int m_TestDamage = 10;
    private int m_SpawnCount = 1;

    [MenuItem("Tools/Endless Zombie/Combat Runtime Debugger")]
    public static void Open()
    {
        CombatRuntimeDebuggerWindow window = GetWindow<CombatRuntimeDebuggerWindow>("Combat Debugger");
        window.minSize = new Vector2(520f, 640f);
        window.Show();
    }

    private void OnEnable() => EditorApplication.update += Refresh;
    private void OnDisable() => EditorApplication.update -= Refresh;

    private void Refresh()
    {
        if (EditorApplication.timeSinceStartup < m_NextRepaint) return;
        m_NextRepaint = EditorApplication.timeSinceStartup + 0.2;
        Repaint();
    }

    private void OnGUI()
    {
        DrawHeader();
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Nhấn Play để kết nối với combat runtime. Tool không sửa asset gốc.", MessageType.Info);
            return;
        }
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            EditorGUILayout.HelpBox("ECS World chưa sẵn sàng.", MessageType.Warning);
            return;
        }

        EntityManager manager = world.EntityManager;
        m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
        DrawLiveOverview(manager);
        DrawRuntimeControls();
        DrawPlayerControls();
        DrawGunTuner(manager);
        DrawEnemyTools(manager);
        DrawWaveTools(manager);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawHeader()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("COMBAT RUNTIME DEBUGGER", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
        });
        EditorGUILayout.Space(4f);
    }

    private static void DrawLiveOverview(EntityManager manager)
    {
        Section("LIVE OVERVIEW");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EntityQuery metricsQuery = manager.CreateEntityQuery(typeof(CombatMetrics));
            if (metricsQuery.CalculateEntityCount() == 1)
            {
                CombatMetrics metrics = metricsQuery.GetSingleton<CombatMetrics>();
                EditorGUILayout.LabelField($"DPS gần nhất: {metrics.RecentDps:0.0}", $"Damage tổng: {metrics.TotalDamage:N0}");
                EditorGUILayout.LabelField($"Kills: {metrics.KillCount:N0}", $"TTK trung bình: {metrics.AverageTimeToKill:0.00}s");
                EditorGUILayout.LabelField($"Quái sống: {metrics.ActiveEnemies}", $"Gần player: {metrics.NearbyEnemies}");
                EditorGUILayout.LabelField($"Combat pressure: {metrics.Pressure:0.00}");
            }
            else EditorGUILayout.LabelField("CombatMetrics chưa sẵn sàng.");
            metricsQuery.Dispose();

            if (GoldWallet.Instance != null)
                EditorGUILayout.LabelField($"Run Gold: {GoldWallet.Instance.RunReward:N0}");
        }
    }

    private static void DrawRuntimeControls()
    {
        Section("RUNTIME");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            Time.timeScale = EditorGUILayout.Slider("Game Speed", Time.timeScale, 0f, 3f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pause")) Time.timeScale = 0f;
                if (GUILayout.Button("Normal x1")) Time.timeScale = 1f;
                if (GUILayout.Button("Fast x2")) Time.timeScale = 2f;
            }
        }
    }

    private static void DrawPlayerControls()
    {
        Section("PLAYER");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            CharacterHealthManager health = Object.FindFirstObjectByType<CharacterHealthManager>();
            if (health == null) { EditorGUILayout.LabelField("Không tìm thấy Player Health."); return; }
            EditorGUILayout.LabelField($"HP: {health.CurrentHealth:N0} / {health.MaxHealth:N0}");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Heal Full")) health.RestoreFullHealth();
                if (GUILayout.Button("Take 10 Damage")) health.TakeDamage(10);
            }
        }
    }

    private void DrawGunTuner(EntityManager manager)
    {
        Section("LIVE GUN TUNER");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EntityQuery query = manager.CreateEntityQuery(typeof(WeaponManager), typeof(GunModifiers));
            if (query.CalculateEntityCount() != 1)
            {
                EditorGUILayout.LabelField("Weapon entity chưa sẵn sàng."); query.Dispose(); return;
            }
            Entity entity = query.GetSingletonEntity();
            WeaponManager gun = manager.GetComponentData<WeaponManager>(entity);
            GunModifiers modifiers = manager.GetComponentData<GunModifiers>(entity);
            if (!m_GunLoaded) PullGun(gun);

            EditorGUILayout.LabelField($"Equipped: {gun.Archetype}   •   Ammo {gun.AmmoInMagazine}/{gun.MagazineSize}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Runtime DPS thô: {(gun.DamagePerHit * gun.ShotsPerSecond * gun.ProjectileCount):0.0}");
            m_Damage = EditorGUILayout.IntField("Damage / projectile", m_Damage);
            m_FireRate = EditorGUILayout.FloatField("Shots / second", m_FireRate);
            m_Projectiles = EditorGUILayout.IntField("Projectile count", m_Projectiles);
            m_Magazine = EditorGUILayout.IntField("Magazine", m_Magazine);
            m_Reload = EditorGUILayout.FloatField("Reload duration", m_Reload);
            m_ProjectileSpeed = EditorGUILayout.FloatField("Projectile speed", m_ProjectileSpeed);
            m_CritChance = EditorGUILayout.Slider("Critical chance", m_CritChance, 0f, 1f);
            m_CritDamage = EditorGUILayout.FloatField("Critical multiplier", m_CritDamage);
            m_Pierce = EditorGUILayout.IntField("Pierce", m_Pierce);
            m_Knockback = EditorGUILayout.FloatField("Knockback", m_Knockback);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Live")) ApplyGun(manager, entity, gun);
                if (GUILayout.Button("Pull Runtime")) PullGun(gun);
                if (GUILayout.Button("Reset Upgrades")) manager.SetComponentData(entity, default(GunModifiers));
            }
            EditorGUILayout.LabelField($"Modifiers: DMG +{modifiers.DamageBonusPercent:0.#}%  •  Fire +{modifiers.FireRateBonusPercent:0.#}%  •  Mag +{modifiers.AdditionalMagazineSize}", EditorStyles.miniLabel);
            query.Dispose();
        }
    }

    private void DrawEnemyTools(EntityManager manager)
    {
        Section("ENEMY TEST TOOLS");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EntityQuery query = manager.CreateEntityQuery(typeof(Mob));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<Mob> mobs = query.ToComponentDataArray<Mob>(Allocator.Temp);
            int normal = 0, elite = 0, boss = 0, totalHealth = 0;
            for (int i = 0; i < mobs.Length; i++)
            {
                totalHealth += math.max(0, mobs[i].Health);
                if (mobs[i].EnemyType == EnemyType.Boss) boss++;
                else if (mobs[i].EnemyType == EnemyType.Elite) elite++;
                else normal++;
            }
            EditorGUILayout.LabelField($"Normal/Dog: {normal}   •   Elite: {elite}   •   Boss: {boss}");
            EditorGUILayout.LabelField($"Tổng HP còn lại: {totalHealth:N0}");
            m_TestDamage = Mathf.Max(1, EditorGUILayout.IntField("Test damage", m_TestDamage));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Damage All")) DamageEnemies(manager, entities, m_TestDamage);
                if (GUILayout.Button("Kill All + Rewards")) DamageEnemies(manager, entities, 9999999);
                if (GUILayout.Button("Clear No Reward")) manager.DestroyEntity(query);
            }
            query.Dispose();
        }
    }

    private void DrawWaveTools(EntityManager manager)
    {
        Section("WAVE & SPAWN");
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EntityQuery query = manager.CreateEntityQuery(typeof(StageRuntime));
            if (query.CalculateEntityCount() != 1)
            {
                EditorGUILayout.LabelField("Stage runtime chưa sẵn sàng."); query.Dispose(); return;
            }
            Entity stageEntity = query.GetSingletonEntity();
            StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
            DynamicBuffer<WaveRuntime> waves = manager.GetBuffer<WaveRuntime>(stageEntity);
            DynamicBuffer<SpawnEntryRuntime> entries = manager.GetBuffer<SpawnEntryRuntime>(stageEntity);
            EditorGUILayout.LabelField($"Stage: {stage.State}   •   Wave: {stage.CurrentWaveIndex + 1}/{waves.Length}");
            m_SpawnCount = Mathf.Clamp(EditorGUILayout.IntField("Debug spawn count", m_SpawnCount), 1, 50);
            if (stage.CurrentWaveIndex >= 0 && stage.CurrentWaveIndex < waves.Length)
            {
                WaveRuntime wave = waves[stage.CurrentWaveIndex];
                int end = wave.FirstSpawnEntryIndex + wave.SpawnEntryCount;
                for (int i = wave.FirstSpawnEntryIndex; i < end; i++)
                {
                    SpawnEntryRuntime entry = entries[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{entry.EnemyId}  {entry.SpawnedCount}/{entry.Quantity}");
                        if (GUILayout.Button($"Spawn +{m_SpawnCount}", GUILayout.Width(100f)))
                            QueueDebugSpawn(manager, stageEntity, i, m_SpawnCount);
                    }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Restart Stage")) WaveSpawnLifecycle.BeginStage();
                if (GUILayout.Button("Stop Stage")) WaveSpawnLifecycle.StopStage();
            }
            query.Dispose();
        }
    }

    private void PullGun(WeaponManager gun)
    {
        m_Damage = gun.BaseDamage; m_FireRate = gun.BaseShotsPerSecond;
        m_Projectiles = gun.BaseProjectileCount; m_Magazine = gun.BaseMagazineSize;
        m_Reload = gun.BaseReloadDuration; m_ProjectileSpeed = gun.BaseProjectileSpeed;
        m_CritChance = gun.BaseCriticalChance; m_CritDamage = gun.BaseCriticalDamage;
        m_Pierce = gun.BasePierce; m_Knockback = gun.BaseKnockback; m_GunLoaded = true;
    }

    private void ApplyGun(EntityManager manager, Entity entity, WeaponManager gun)
    {
        gun.BaseDamage = Mathf.Max(1, m_Damage); gun.BaseShotsPerSecond = Mathf.Max(0.01f, m_FireRate);
        gun.BaseProjectileCount = Mathf.Max(1, m_Projectiles); gun.BaseMagazineSize = Mathf.Max(1, m_Magazine);
        gun.BaseReloadDuration = Mathf.Max(0.05f, m_Reload); gun.BaseProjectileSpeed = Mathf.Max(0.1f, m_ProjectileSpeed);
        gun.BaseCriticalChance = Mathf.Clamp01(m_CritChance); gun.BaseCriticalDamage = Mathf.Max(1f, m_CritDamage);
        gun.BasePierce = Mathf.Max(0, m_Pierce); gun.BaseKnockback = Mathf.Max(0f, m_Knockback);
        gun.AmmoInMagazine = Mathf.Min(gun.AmmoInMagazine, gun.BaseMagazineSize);
        manager.SetComponentData(entity, gun);
    }

    private static void DamageEnemies(EntityManager manager, NativeArray<Entity> enemies, int damage)
    {
        foreach (Entity enemy in enemies)
        {
            Entity damageEvent = manager.CreateEntity();
            manager.AddComponentData(damageEvent, new MobDamageTakenEvent { Entity = enemy, Amount = damage });
        }
    }

    private static void QueueDebugSpawn(EntityManager manager, Entity stageEntity, int entryIndex, int amount)
    {
        StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
        DynamicBuffer<SpawnEntryRuntime> entries = manager.GetBuffer<SpawnEntryRuntime>(stageEntity);
        DynamicBuffer<SpawnRequest> requests = manager.GetBuffer<SpawnRequest>(stageEntity);
        SpawnEntryRuntime entry = entries[entryIndex];
        for (int i = 0; i < amount; i++)
            requests.Add(new SpawnRequest
            {
                Sequence = stage.NextRequestSequence++, WaveIndex = entry.WaveIndex, SpawnEntryIndex = entryIndex,
                EnemyPrefab = entry.EnemyPrefab, EnemyType = entry.EnemyType,
                VisualKind = entry.VisualKind,
                HealthMultiplier = entry.HealthMultiplier, DamageMultiplier = entry.DamageMultiplier,
                Scale = entry.Scale, XPReward = entry.XPReward, GoldReward = entry.GoldReward,
                SpawnArenaGroupId = entry.SpawnArenaGroupId,
            });
        entry.Quantity += amount; entry.EnqueuedCount += amount; entry.State = SpawnEntryRuntimeState.Active;
        entries[entryIndex] = entry; manager.SetComponentData(stageEntity, stage);
    }

    private static void Section(string title)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }
}
#endif
