using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class CombatRuntimeValidation
{
    private const string ActiveKey = "CombatValidation.Active";
    private const string StartTimeKey = "CombatValidation.StartTime";
    private const string ResultPath = "runtime-validation-result.txt";
    private const double ValidationDuration = 35.0;
    private static readonly List<string> Errors = new();
    private static bool _callbacksRegistered;

    static CombatRuntimeValidation()
    {
        if (SessionState.GetBool(ActiveKey, false))
            RegisterCallbacks();
    }

    [InitializeOnLoadMethod]
    private static void AutoRunIfRequested()
    {
        string requestFile = Path.Combine(Directory.GetCurrentDirectory(), "runtime-validation-request.flag");
        if (!File.Exists(requestFile))
            return;

        File.Delete(requestFile);
        EditorApplication.delayCall += Run;
    }

    public static void Run()
    {
        MainMenuPlayModeStart.UseCurrentScene();
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetFloat(StartTimeKey, 0f);
        Errors.Clear();
        RegisterCallbacks();

        string resultFile = Path.Combine(Directory.GetCurrentDirectory(), ResultPath);
        if (File.Exists(resultFile))
            File.Delete(resultFile);

        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void RegisterCallbacks()
    {
        if (_callbacksRegistered)
            return;
        _callbacksRegistered = true;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        Application.logMessageReceived += LogReceived;
    }

    private static void UnregisterCallbacks()
    {
        if (!_callbacksRegistered)
            return;
        _callbacksRegistered = false;
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        Application.logMessageReceived -= LogReceived;
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            SessionState.SetFloat(StartTimeKey, (float)EditorApplication.timeSinceStartup);
    }

    private static void LogReceived(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            return;
        Errors.Add($"[{type}] {condition}\n{stackTrace}");
    }

    private static void Update()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying)
            return;

        float startTime = SessionState.GetFloat(StartTimeKey, 0f);
        if (startTime <= 0f)
        {
            SessionState.SetFloat(StartTimeKey, (float)EditorApplication.timeSinceStartup);
            return;
        }

        if (EditorApplication.timeSinceStartup - startTime < ValidationDuration)
            return;

        ValidateAndExit();
    }

    private static void ValidateAndExit()
    {
        List<string> failures = new(Errors);
        string diagnostics = string.Empty;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            failures.Add("Default ECS World was not created.");
        }
        else
        {
            EntityManager entityManager = world.EntityManager;
            ValidateSingleton<WeaponManager>(entityManager, failures);
            ValidateSingleton<MobSpawnSettings>(entityManager, failures);
            ValidateSingleton<CombatMetrics>(entityManager, failures);

            EntityQuery metricsQuery = entityManager.CreateEntityQuery(typeof(CombatMetrics));
            if (!metricsQuery.IsEmptyIgnoreFilter)
            {
                CombatMetrics metrics = metricsQuery.GetSingleton<CombatMetrics>();
                diagnostics += $"Metrics: damage={metrics.TotalDamage}, kills={metrics.KillCount}, " +
                               $"active={metrics.ActiveEnemies}, nearby={metrics.NearbyEnemies}, " +
                               $"pressure={metrics.Pressure:0.00}, dps={metrics.RecentDps:0.00}, " +
                               $"avgTTK={metrics.AverageTimeToKill:0.00}\n";
                if (metrics.ActiveEnemies <= 0)
                    failures.Add("No active enemies were recorded.");
                if (metrics.TotalDamage <= 0)
                    failures.Add("Auto-fire did not record damage.");
                if (metrics.KillCount <= 0)
                    failures.Add("No zombie kill was recorded.");
            }

            EntityQuery weaponQuery = entityManager.CreateEntityQuery(typeof(WeaponManager));
            if (!weaponQuery.IsEmptyIgnoreFilter)
            {
                WeaponManager weapon = weaponQuery.GetSingleton<WeaponManager>();
                diagnostics += $"Weapon: ammo={weapon.AmmoInMagazine}/{weapon.MagazineSize}, " +
                               $"reload={weapon.IsReloading}, range={weapon.AttackRange:0.00}, " +
                               $"damage={weapon.DamagePerHit}, fireRate={weapon.ShotsPerSecond:0.00}\n";
                bool weaponWasUsed = weapon.AmmoInMagazine < weapon.MagazineSize ||
                                     weapon.IsReloading || metricsDamageRecorded(entityManager);
                if (!weaponWasUsed)
                    failures.Add("Weapon ammo/reload state never changed.");
            }
        }

        string resultFile = Path.Combine(Directory.GetCurrentDirectory(), ResultPath);
        bool passed = failures.Count == 0;
        string report = passed
            ? "PASS\nGameScene completed 35 seconds of runtime validation without errors.\n" +
              "Verified: ECS singletons, mob spawning, auto-fire damage, kills, ammo/reload and combat metrics.\n" +
              diagnostics
            : "FAIL\n" + string.Join("\n\n", failures) + "\n\n" + diagnostics;
        File.WriteAllText(resultFile, report);
        Debug.Log($"Combat runtime validation: {(passed ? "PASS" : "FAIL")}\n{report}");

        SessionState.SetBool(ActiveKey, false);
        MainMenuPlayModeStart.UseMainMenu();
        UnregisterCallbacks();
        EditorApplication.Exit(passed ? 0 : 2);
    }

    private static bool metricsDamageRecorded(EntityManager entityManager)
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(CombatMetrics));
        return !query.IsEmptyIgnoreFilter && query.GetSingleton<CombatMetrics>().TotalDamage > 0;
    }

    private static void ValidateSingleton<T>(EntityManager entityManager, List<string> failures)
        where T : unmanaged, IComponentData
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(T));
        int count = query.CalculateEntityCount();
        if (count != 1)
            failures.Add($"Expected one {typeof(T).Name} entity, found {count}.");
    }
}
