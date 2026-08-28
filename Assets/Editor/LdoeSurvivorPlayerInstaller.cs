using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LdoeSurvivorPlayerInstaller
{
    private const string SurvivorPrefabPath =
        "Assets/Models/LDoE Survivor/Survivor_character_fixed.prefab";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string ControllerPath =
        "Assets/Animations/LDoE Survivor/SurvivorGunplay.controller";
    private const string IdleClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_rifle_idle.anim";
    private const string WalkClipPath =
        "Assets/Animations/LDoE Survivor/movement_rifle_walk.anim";
    private const string GunplayClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_rifle.anim";
    private const string PistolIdleClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_pistol_idle.anim";
    private const string PistolFireClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_pistol.anim";
    private const string ShotgunIdleClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_shotgun_idle.anim";
    private const string ShotgunFireClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_shotgun_shooting.anim";
    private const string HarpoonIdleClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_Harpoon_idle.anim";
    private const string HarpoonFireClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_Harpoon_shoot.anim";
    private const string MinigunIdleClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_minigun_idle.anim";
    private const string MinigunFireClipPath =
        "Assets/Animations/LDoE Survivor/action_attack_minigun.anim";
    private const string PistolControllerPath =
        "Assets/Animations/LDoE Survivor/SurvivorPistol.overrideController";
    private const string ShotgunControllerPath =
        "Assets/Animations/LDoE Survivor/SurvivorShotgun.overrideController";
    private const string HarpoonControllerPath =
        "Assets/Animations/LDoE Survivor/SurvivorHarpoon.overrideController";
    private const string MinigunControllerPath =
        "Assets/Animations/LDoE Survivor/SurvivorMinigun.overrideController";
    private const string InstalledName = "Survivor Character (LDoE)";
    private const string PreviousVisualName = "Cyborg Visual RX-1500";
    private const float TargetHeight = 1.35f;
    private const string AutoInstallSessionKey =
        "EndlessZombie.LdoeSurvivorPlayerInstaller.WeaponAnimationsV3";

    [InitializeOnLoadMethod]
    private static void ScheduleAutoInstall()
    {
        if (SessionState.GetBool(AutoInstallSessionKey, false))
            return;

        SessionState.SetBool(AutoInstallSessionKey, true);
        EditorApplication.delayCall += TryAutoInstall;
    }

    private static void TryAutoInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Transform visual = playerPrefab != null ? playerPrefab.transform.Find(InstalledName) : null;
        Animator animator = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        RuntimeAnimatorController pistolController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PistolControllerPath);
        if (playerPrefab != null &&
            (visual == null || controller == null || animator == null ||
             pistolController == null))
            Install();
    }

    [MenuItem("Tools/Endless Zombie/Player/Install LDoE Survivor Model")]
    public static void Install()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        GameObject survivorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurvivorPrefabPath);
        if (survivorPrefab == null)
            throw new MissingReferenceException($"Could not load {SurvivorPrefabPath}.");

        RuntimeAnimatorController controller = BuildOrUpdateController();
        RuntimeAnimatorController[] archetypeControllers =
            BuildOrUpdateArchetypeControllers(controller);

        InstallInPlayerPrefab(survivorPrefab, controller, archetypeControllers);
        InstallInGameScene(survivorPrefab, controller, archetypeControllers);

        AssetDatabase.SaveAssets();
        Debug.Log("Installed Survivor_character_fixed as the Endless Zombie main character model.");
    }

    private static RuntimeAnimatorController BuildOrUpdateController()
    {
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkClipPath);
        AnimationClip gunplayClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(GunplayClipPath);
        if (idleClip == null || walkClip == null || gunplayClip == null)
            throw new MissingReferenceException("One or more LDoE Survivor animation clips are missing.");

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (!controller.parameters.Any(parameter => parameter.name == "IsWalking"))
            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = GetOrCreateState(stateMachine, "Idle");
        AnimatorState walk = GetOrCreateState(stateMachine, "Walk");
        AnimatorState gunplay = GetOrCreateState(stateMachine, "Gunplay");
        idle.motion = idleClip;
        walk.motion = walkClip;
        gunplay.motion = gunplayClip;
        stateMachine.defaultState = idle;

        ClearTransitions(idle);
        ClearTransitions(walk);
        ClearTransitions(gunplay);

        AnimatorStateTransition startWalking = idle.AddTransition(walk);
        startWalking.hasExitTime = false;
        startWalking.duration = 0.12f;
        startWalking.AddCondition(AnimatorConditionMode.If, 0f, "IsWalking");

        AnimatorStateTransition stopWalking = walk.AddTransition(idle);
        stopWalking.hasExitTime = false;
        stopWalking.duration = 0.12f;
        stopWalking.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsWalking");

        AnimatorStateTransition shotFinished = gunplay.AddTransition(idle);
        shotFinished.hasExitTime = true;
        shotFinished.exitTime = 1f;
        shotFinished.hasFixedDuration = true;
        shotFinished.duration = 0.04f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static RuntimeAnimatorController[] BuildOrUpdateArchetypeControllers(
        RuntimeAnimatorController baseController)
    {
        AnimationClip pistolIdle = LoadClip(PistolIdleClipPath);
        AnimationClip pistolFire = LoadClip(PistolFireClipPath);
        AnimationClip shotgunIdle = LoadClip(ShotgunIdleClipPath);
        AnimationClip shotgunFire = LoadClip(ShotgunFireClipPath);
        AnimationClip harpoonIdle = LoadClip(HarpoonIdleClipPath);
        AnimationClip harpoonFire = LoadClip(HarpoonFireClipPath);
        AnimationClip minigunIdle = LoadClip(MinigunIdleClipPath);
        AnimationClip minigunFire = LoadClip(MinigunFireClipPath);

        RuntimeAnimatorController pistol = BuildOrUpdateOverrideController(
            PistolControllerPath, baseController, pistolIdle, pistolFire);
        RuntimeAnimatorController shotgun = BuildOrUpdateOverrideController(
            ShotgunControllerPath, baseController, shotgunIdle, shotgunFire);
        RuntimeAnimatorController harpoon = BuildOrUpdateOverrideController(
            HarpoonControllerPath, baseController, harpoonIdle, harpoonFire);
        BuildOrUpdateOverrideController(
            MinigunControllerPath, baseController, minigunIdle, minigunFire);

        RuntimeAnimatorController[] controllers =
            new RuntimeAnimatorController[Enum.GetValues(typeof(GunArchetype)).Length];
        controllers[(int)GunArchetype.Pistol] = pistol;
        controllers[(int)GunArchetype.Shotgun] = shotgun;
        controllers[(int)GunArchetype.AssaultRifle] = baseController;
        controllers[(int)GunArchetype.SniperRifle] = baseController;
        controllers[(int)GunArchetype.RocketLauncher] = harpoon;
        controllers[(int)GunArchetype.SMG] = baseController;
        controllers[(int)GunArchetype.TeslaGun] = baseController;
        controllers[(int)GunArchetype.FlameRifle] = baseController;
        controllers[(int)GunArchetype.CryoGun] = baseController;
        return controllers;
    }

    private static AnimationClip LoadClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
            throw new MissingReferenceException($"Could not load {path}.");
        return clip;
    }

    private static RuntimeAnimatorController BuildOrUpdateOverrideController(
        string path,
        RuntimeAnimatorController baseController,
        AnimationClip idleClip,
        AnimationClip fireClip)
    {
        AnimatorOverrideController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
        if (controller == null)
        {
            controller = new AnimatorOverrideController();
            AssetDatabase.CreateAsset(controller, path);
        }

        controller.runtimeAnimatorController = baseController;
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new();
        controller.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip source = overrides[i].Key;
            if (source == null) continue;
            if (source.name == "action_attack_rifle_idle")
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(source, idleClip);
            else if (source.name == "action_attack_rifle")
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(source, fireClip);
        }
        controller.ApplyOverrides(overrides);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState GetOrCreateState(AnimatorStateMachine stateMachine, string stateName)
    {
        return stateMachine.states
                   .Select(child => child.state)
                   .FirstOrDefault(state => state.name == stateName)
               ?? stateMachine.AddState(stateName);
    }

    private static void ClearTransitions(AnimatorState state)
    {
        foreach (AnimatorStateTransition transition in state.transitions.ToArray())
            state.RemoveTransition(transition);
    }

    private static void InstallInPlayerPrefab(
        GameObject survivorPrefab,
        RuntimeAnimatorController controller,
        RuntimeAnimatorController[] archetypeControllers)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            RemovePreviousVisuals(player.transform, removeInstalledSurvivor: false);
            Transform existingVisual = player.transform.Find(InstalledName);
            GameObject visual = existingVisual != null
                ? existingVisual.gameObject
                : CreateVisual(survivorPrefab, player.transform, controller);
            ConfigureAnimator(visual, controller);
            BindCharacter(player, visual, controller, archetypeControllers);
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }

    private static void InstallInGameScene(
        GameObject survivorPrefab,
        RuntimeAnimatorController controller,
        RuntimeAnimatorController[] archetypeControllers)
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        GameObject player = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.CompareTag("Player"));
        if (player == null)
            throw new MissingReferenceException($"Could not find the Player in {GameScenePath}.");

        RemovePreviousVisuals(player.transform, removeInstalledSurvivor: false);

        Transform visualTransform = player.transform.Find(InstalledName);
        GameObject visual = visualTransform != null
            ? visualTransform.gameObject
            : CreateVisual(survivorPrefab, player.transform, controller);

        ConfigureAnimator(visual, controller);
        BindCharacter(player, visual, controller, archetypeControllers);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject CreateVisual(
        GameObject survivorPrefab,
        Transform parent,
        RuntimeAnimatorController controller)
    {
        GameObject visual = PrefabUtility.InstantiatePrefab(survivorPrefab, parent) as GameObject;
        if (visual == null)
            throw new UnityException("Failed to instantiate the LDoE Survivor prefab.");

        visual.name = InstalledName;
        visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        visual.transform.localScale = Vector3.one;
        FitToCharacterHeight(visual.transform, TargetHeight);
        ConfigureAnimator(visual, controller);
        return visual;
    }

    private static void ConfigureAnimator(
        GameObject visual,
        RuntimeAnimatorController controller)
    {
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = visual.AddComponent<Animator>();

        animator.applyRootMotion = false;
        if (controller != null)
            animator.runtimeAnimatorController = controller;
    }

    private static void BindCharacter(
        GameObject player,
        GameObject visual,
        RuntimeAnimatorController controller,
        RuntimeAnimatorController[] archetypeControllers)
    {
        CharacterLogic logic = player.GetComponent<CharacterLogic>();
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (logic != null)
        {
            SerializedObject serializedLogic = new(logic);
            serializedLogic.FindProperty("_model").objectReferenceValue = visual.transform;
            serializedLogic.FindProperty("_animator").objectReferenceValue = animator;
            serializedLogic.ApplyModifiedPropertiesWithoutUndo();
        }

        PlayerGunplayAnimator gunplay = player.GetComponent<PlayerGunplayAnimator>();
        if (gunplay != null && controller != null)
        {
            SerializedObject serializedGunplay = new(gunplay);
            serializedGunplay.FindProperty("m_Controller").objectReferenceValue = controller;
            SerializedProperty controllersProperty =
                serializedGunplay.FindProperty("m_ArchetypeControllers");
            controllersProperty.arraySize = archetypeControllers.Length;
            for (int i = 0; i < archetypeControllers.Length; i++)
                controllersProperty.GetArrayElementAtIndex(i).objectReferenceValue =
                    archetypeControllers[i];
            serializedGunplay.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void RemovePreviousVisuals(Transform player, bool removeInstalledSurvivor)
    {
        for (int i = player.childCount - 1; i >= 0; i--)
        {
            Transform child = player.GetChild(i);
            bool isPrevious = child.name == PreviousVisualName || child.name == "Visual";
            bool isInstalled = removeInstalledSurvivor && child.name == InstalledName;
            if (isPrevious || isInstalled)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void FitToCharacterHeight(Transform visual, float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer or TrailRenderer or LineRenderer) continue;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y <= 0.001f)
            return;

        visual.localScale = Vector3.one * (targetHeight / bounds.size.y);

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        visual.position += Vector3.up * (visual.parent.position.y - bounds.min.y);
    }
}
