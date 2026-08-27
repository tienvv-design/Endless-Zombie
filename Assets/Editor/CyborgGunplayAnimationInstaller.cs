using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CyborgGunplayAnimationInstaller
{
    private const string GunplayModelPath = "Assets/Models/CYBORG SOLDIER RX-1500/Gunplay.fbx";
    private const string ControllerFolder = "Assets/Animations/Cyborg";
    private const string ControllerPath = ControllerFolder + "/CyborgGunplay.controller";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";

    static CyborgGunplayAnimationInstaller()
    {
        EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += Install;
        };
    }

    [MenuItem("Tools/Endless Zombie/Install Cyborg Gunplay Animation")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(GunplayModelPath)
            .OfType<AnimationClip>()
            .Where(item => !item.name.StartsWith("__preview__"))
            .OrderByDescending(item => item.name.ToLowerInvariant().Contains("gunplay"))
            .ThenByDescending(item => item.length)
            .FirstOrDefault();
        if (clip == null)
        {
            Debug.LogWarning("Gunplay.fbx was found, but it contains no importable animation clip.");
            return;
        }

        EnsureFolder("Assets/Animations");
        EnsureFolder(ControllerFolder);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (!controller.parameters.Any(parameter => parameter.name == "IsWalking"))
            controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.states.Select(item => item.state).FirstOrDefault(item => item.name == "Idle");
        if (idle == null)
            idle = stateMachine.AddState("Idle");
        idle.motion = null;
        stateMachine.defaultState = idle;

        AnimatorState gunplay = stateMachine.states.Select(item => item.state).FirstOrDefault(item => item.name == "Gunplay");
        if (gunplay == null)
            gunplay = stateMachine.AddState("Gunplay");
        gunplay.motion = clip;
        gunplay.speed = 1f;

        foreach (AnimatorStateTransition transition in gunplay.transitions)
            gunplay.RemoveTransition(transition);
        AnimatorStateTransition exit = gunplay.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = 1f;
        exit.duration = 0.03f;
        exit.hasFixedDuration = true;
        EditorUtility.SetDirty(controller);

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
        if (openedTemporarily)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject player = scene.GetRootGameObjects().FirstOrDefault(root => root.CompareTag("Player"));
        if (player != null && player.transform.Find("Survivor Character (LDoE)") != null)
        {
            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
            return;
        }

        if (player != null)
        {
            CharacterLogic character = player.GetComponent<CharacterLogic>();
            Transform visual = character != null ? character.AimTransform : player.transform;
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = visual.gameObject.AddComponent<Animator>();

            if (animator != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                if (character != null)
                {
                    SerializedObject serializedCharacter = new(character);
                    serializedCharacter.FindProperty("_animator").objectReferenceValue = animator;
                    serializedCharacter.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            if (player.GetComponent<PlayerGunplayAnimator>() == null)
                player.AddComponent<PlayerGunplayAnimator>();
            SerializedObject serializedGunplay = new(player.GetComponent<PlayerGunplayAnimator>());
            serializedGunplay.FindProperty("m_Controller").objectReferenceValue = controller;
            serializedGunplay.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (openedTemporarily)
            EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"Installed Cyborg gunplay animation clip '{clip.name}' ({clip.length:0.00}s). ");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
    }
}

public sealed class CyborgGunplayImportHook : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Contains("Assets/Models/CYBORG SOLDIER RX-1500/Gunplay.fbx"))
            EditorApplication.delayCall += CyborgGunplayAnimationInstaller.Install;
    }
}
