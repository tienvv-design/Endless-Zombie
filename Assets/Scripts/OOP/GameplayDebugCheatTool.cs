#if UNITY_EDITOR
using OOP.GameStates;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameplayDebugCheatTool : MonoBehaviour
{
    private const string BossSpawnPointName = "[DEBUG] Boss Spawn Point";

    [Header("Hotkeys")]
    [SerializeField] private Key m_WinKey = Key.F1;
    [SerializeField] private Key m_LoseKey = Key.F2;
    [SerializeField] private Key m_SpawnBossKey = Key.F3;
    [SerializeField] private Key m_PlayerInvincibleKey = Key.F4;
    [SerializeField] private Key m_EnemyInvincibleKey = Key.F5;
    [SerializeField] private Key m_ToggleOverlayKey = Key.F10;

    [Header("Boss Spawn")]
    [SerializeField] private Transform m_BossSpawnPoint;
    [SerializeField] private Vector3 m_DefaultBossOffset = new(0f, 0f, 8f);
    [SerializeField] private bool m_SpawnBossOutsideCamera = true;
    [SerializeField, Range(0.05f, 0.5f)] private float m_OffscreenViewportPadding = 0.25f;
    [SerializeField, Min(1f)] private float m_OffscreenFallbackDistance = 12f;

    [Header("Runtime")]
    [SerializeField] private bool m_ShowOverlay = true;
    [SerializeField] private bool m_PlayerInvincible;
    [SerializeField] private bool m_EnemyInvincible;

    private GameStateMachineRunner m_Runner;
    private string m_Status = "Ready";
    private static bool s_Registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistration() => s_Registered = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        if (s_Registered) return;
        s_Registered = true;
        SceneManager.sceneLoaded += InstallForScene;
    }

    private static void InstallForScene(Scene scene, LoadSceneMode mode)
    {
        GameStateMachineRunner runner = FindFirstObjectByType<GameStateMachineRunner>();
        if (runner == null || runner.transform.Find("[DEBUG] Gameplay Cheat Tool") != null) return;
        GameObject toolObject = new("[DEBUG] Gameplay Cheat Tool");
        toolObject.transform.SetParent(runner.transform, false);
        GameplayDebugCheatTool tool = toolObject.AddComponent<GameplayDebugCheatTool>();
        tool.m_Runner = runner;
    }

    private void Awake()
    {
        m_Runner = GetComponent<GameStateMachineRunner>();
        EnsureBossSpawnPoint();
        ApplyPlayerInvincibility();
        ApplyEnemyInvincibility();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[m_WinKey].wasPressedThisFrame) ForceWin();
        if (keyboard[m_LoseKey].wasPressedThisFrame) ForceLose();
        if (keyboard[m_SpawnBossKey].wasPressedThisFrame) SpawnBoss();
        if (keyboard[m_PlayerInvincibleKey].wasPressedThisFrame) TogglePlayerInvincible();
        if (keyboard[m_EnemyInvincibleKey].wasPressedThisFrame) ToggleEnemyInvincible();
        if (keyboard[m_ToggleOverlayKey].wasPressedThisFrame) m_ShowOverlay = !m_ShowOverlay;

        if (m_PlayerInvincible)
            ApplyPlayerInvincibility();
    }

    private void EnsureBossSpawnPoint()
    {
        if (m_BossSpawnPoint != null) return;
        Transform existing = transform.Find(BossSpawnPointName);
        if (existing != null)
        {
            m_BossSpawnPoint = existing;
            return;
        }

        GameObject point = new(BossSpawnPointName);
        m_BossSpawnPoint = point.transform;
        m_BossSpawnPoint.SetParent(transform, false);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        m_BossSpawnPoint.position = (player != null ? player.transform.position : Vector3.zero) + m_DefaultBossOffset;
    }

    private void ForceWin()
    {
        m_Runner ??= FindFirstObjectByType<GameStateMachineRunner>();
        m_Runner?.DebugForceWin();
        m_Status = "Forced WIN";
    }

    private void ForceLose()
    {
        m_Runner ??= FindFirstObjectByType<GameStateMachineRunner>();
        m_Runner?.DebugForceLose();
        m_Status = "Forced LOSE";
    }

    private void TogglePlayerInvincible()
    {
        m_PlayerInvincible = !m_PlayerInvincible;
        ApplyPlayerInvincibility();
        m_Status = $"Player immortal: {m_PlayerInvincible}";
    }

    private void ApplyPlayerInvincibility()
    {
        foreach (CharacterHealthManager health in FindObjectsByType<CharacterHealthManager>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            health.DebugInvincible = m_PlayerInvincible;
            if (m_PlayerInvincible && health.IsDead)
                health.RestoreFullHealth();
        }
    }

    private void ToggleEnemyInvincible()
    {
        m_EnemyInvincible = !m_EnemyInvincible;
        ApplyEnemyInvincibility();
        m_Status = $"Enemy immortal: {m_EnemyInvincible}";
    }

    private void ApplyEnemyInvincibility()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        EntityManager manager = world.EntityManager;
        EntityQuery query = manager.CreateEntityQuery(typeof(GameplayDebugFlags));
        Entity flagsEntity = query.CalculateEntityCount() == 0
            ? manager.CreateEntity(typeof(GameplayDebugFlags))
            : query.GetSingletonEntity();
        manager.SetComponentData(flagsEntity, new GameplayDebugFlags
        {
            EnemyInvincible = m_EnemyInvincible ? (byte)1 : (byte)0,
        });
        query.Dispose();
    }

    private void SpawnBoss()
    {
        EnsureBossSpawnPoint();
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            m_Status = "Boss failed: ECS World missing";
            return;
        }

        EntityManager manager = world.EntityManager;
        EntityQuery stageQuery = manager.CreateEntityQuery(
            ComponentType.ReadOnly<StageRuntime>(), ComponentType.ReadOnly<EnemyCatalogRuntime>());
        if (stageQuery.CalculateEntityCount() != 1)
        {
            stageQuery.Dispose();
            m_Status = "Boss failed: Stage/Catalog missing";
            return;
        }

        Entity stageEntity = stageQuery.GetSingletonEntity();
        StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
        DynamicBuffer<EnemyCatalogRuntime> catalog = manager.GetBuffer<EnemyCatalogRuntime>(stageEntity);
        int selected = FindBoss(catalog);
        if (selected < 0)
        {
            stageQuery.Dispose();
            m_Status = "Boss failed: no enemy prefab";
            return;
        }

        EnemyCatalogRuntime source = catalog[selected];
        Vector3 spawnPosition = ResolveBossSpawnPosition(stage);
        Entity boss = manager.Instantiate(source.EnemyPrefab);
        SetOrAdd(manager, boss, new MobVisualVariant { Kind = source.VisualKind });

        Mob mob = manager.GetComponentData<Mob>(boss);
        float healthMultiplier = WaveSpawnRuntimeRules.CalculateEnemyHealthMultiplier(
            source.HealthMultiplier,
            stage.StageNumber,
            stage.CurrentWaveIndex,
            stage.HealthGrowthPerStage,
            stage.HealthGrowthPerWave);
        mob.Health = math.max(1, (int)math.ceil(mob.Health * healthMultiplier * stage.BossHealthMultiplier));
        mob.MaxHealth = mob.Health;
        mob.EnemyType = EnemyType.Boss;
        mob.KnockbackResistance = 1f;
        mob.CrowdControlResistance = 0.8f;
        mob.XPReward = math.max(1, source.XPReward * 5);
        mob.GoldReward = math.max(1, source.GoldReward * 5);
        mob.GoldMultiplier = math.max(5, mob.GoldMultiplier);
        mob.SpawnTime = (float)world.Time.ElapsedTime;
        manager.SetComponentData(boss, mob);

        float baseSpeed = 1f;
        if (manager.HasComponent<UnitMover>(boss))
        {
            UnitMover mover = manager.GetComponentData<UnitMover>(boss);
            baseSpeed = mover.moveSpeed;
            manager.SetComponentEnabled<UnitMover>(boss, false);
        }

        int baseDamage = 1;
        if (manager.HasComponent<KamikazeUnit>(boss))
        {
            KamikazeUnit attack = manager.GetComponentData<KamikazeUnit>(boss);
            attack.HitDistanceSq = math.max(0.01f, stage.AttackDistance * stage.AttackDistance);
            attack.Damage = math.max(1, (int)math.ceil(attack.Damage * source.DamageMultiplier * stage.BossDamageMultiplier));
            baseDamage = attack.Damage;
            manager.SetComponentData(boss, attack);
        }

        SetOrAdd(manager, boss, new BossPhase
        {
            CurrentPhase = 1,
            PhaseTwoHealthRatio = stage.BossPhaseTwoHealth,
            PhaseThreeHealthRatio = stage.BossPhaseThreeHealth,
            SpeedMultiplierPerPhase = stage.BossSpeedPerPhase,
            DamageMultiplierPerPhase = stage.BossDamagePerPhase,
            BaseMoveSpeed = baseSpeed,
            BaseDamage = baseDamage,
        });
        SetOrAdd(manager, boss, new BossShockwave
        {
            Cooldown = stage.BossShockwaveCooldown,
            WarningDuration = stage.BossShockwaveWarning,
            Radius = stage.BossShockwaveRadius,
            Damage = stage.BossShockwaveDamage,
            Timer = stage.BossShockwaveCooldown,
        });
        SetOrAdd(manager, boss, LocalTransform.FromPositionRotationScale(
            spawnPosition, quaternion.identity,
            math.max(0.01f, source.Scale * stage.BossScaleMultiplier)));
        SetOrAdd(manager, boss, new SpawnEmergence { Duration = math.max(0.1f, stage.SpawnPortalDuration) });

        stageQuery.Dispose();
        m_Status = $"Boss spawned offscreen at {spawnPosition:0.0}";
    }

    private Vector3 ResolveBossSpawnPosition(StageRuntime stage)
    {
        if (!m_SpawnBossOutsideCamera)
            return m_BossSpawnPoint.position;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPosition = player != null ? player.transform.position : m_BossSpawnPoint.position;
        Camera camera = Camera.main;
        float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float padding = Mathf.Max(m_OffscreenViewportPadding, stage.OffscreenSpawnPadding);

        if (camera != null && camera.isActiveAndEnabled)
        {
            float viewportX = side < 0f ? -padding : 1f + padding;
            Ray ray = camera.ViewportPointToRay(new Vector3(viewportX, 0.5f, 0f));
            Plane gameplayPlane = new(Vector3.up, playerPosition);
            if (gameplayPlane.Raycast(ray, out float enter))
            {
                Vector3 result = ray.GetPoint(enter);
                result.y = playerPosition.y;
                m_BossSpawnPoint.position = result;
                return result;
            }
        }

        Vector3 fallbackDirection = camera != null
            ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized * side
            : Vector3.right * side;
        if (fallbackDirection.sqrMagnitude < 0.01f)
            fallbackDirection = Vector3.right * side;
        Vector3 fallback = playerPosition + fallbackDirection * m_OffscreenFallbackDistance;
        fallback.y = playerPosition.y;
        m_BossSpawnPoint.position = fallback;
        return fallback;
    }

    private static int FindBoss(DynamicBuffer<EnemyCatalogRuntime> catalog)
    {
        int fallback = -1;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i].EnemyPrefab == Entity.Null) continue;
            if (fallback < 0) fallback = i;
            if (catalog[i].EnemyType == EnemyType.Boss) return i;
            if (catalog[i].VisualKind == MobVisualKind.ZombieWitch) fallback = i;
        }
        return fallback;
    }

    private static void SetOrAdd<T>(EntityManager manager, Entity entity, T value)
        where T : unmanaged, IComponentData
    {
        if (manager.HasComponent<T>(entity)) manager.SetComponentData(entity, value);
        else manager.AddComponentData(entity, value);
    }

    private void OnGUI()
    {
        if (!m_ShowOverlay) return;
        GUILayout.BeginArea(new Rect(14f, 14f, 285f, 245f), GUI.skin.box);
        GUILayout.Label("ENDLESS ZOMBIE - DEBUG");
        if (GUILayout.Button("F1  FORCE WIN")) ForceWin();
        if (GUILayout.Button("F2  FORCE LOSE")) ForceLose();
        if (GUILayout.Button("F3  SPAWN BOSS")) SpawnBoss();
        if (GUILayout.Button($"F4  PLAYER IMMORTAL: {(m_PlayerInvincible ? "ON" : "OFF")}"))
            TogglePlayerInvincible();
        if (GUILayout.Button($"F5  ENEMY IMMORTAL: {(m_EnemyInvincible ? "ON" : "OFF")}"))
            ToggleEnemyInvincible();
        GUILayout.Label($"Spawn: {(m_BossSpawnPoint != null ? m_BossSpawnPoint.position.ToString("F1") : "missing")}");
        GUILayout.Label(m_Status);
        GUILayout.Label("F10: hide/show this panel");
        GUILayout.EndArea();
    }

    private void OnDrawGizmos()
    {
        if (m_BossSpawnPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(m_BossSpawnPoint.position, 0.8f);
        Gizmos.DrawLine(m_BossSpawnPoint.position, m_BossSpawnPoint.position + Vector3.up * 2f);
    }

    private void OnDestroy()
    {
        m_PlayerInvincible = false;
        m_EnemyInvincible = false;
        ApplyPlayerInvincibility();
        ApplyEnemyInvincibility();
    }
}
#endif
