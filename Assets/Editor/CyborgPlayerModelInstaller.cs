using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CyborgPlayerModelInstaller
{
    private const string ModelPath = "Assets/Models/CYBORG SOLDIER RX-1500/Gunplay.fbx";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string InstalledName = "Cyborg Visual RX-1500";
    private const string ReplacementVisualName = "Survivor Character (LDoE)";

    static CyborgPlayerModelInstaller()
    {
        EditorApplication.delayCall += TryInstall;
    }

    [MenuItem("Tools/Endless Zombie/Install Cyborg Player Model")]
    public static void TryInstall()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelAsset == null)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;
        if (openedTemporarily)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject player = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.CompareTag("Player"))
            {
                player = root;
                break;
            }
        }

        if (player == null)
        {
            if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        if (player.transform.Find(ReplacementVisualName) != null)
        {
            if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        Transform installedVisual = player.transform.Find(InstalledName);
        if (installedVisual != null)
        {
            GameObject installedSource = PrefabUtility.GetCorrespondingObjectFromSource(installedVisual.gameObject);
            if (installedSource == modelAsset)
            {
                if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
                return;
            }
        }

        CharacterLogic logic = player.GetComponent<CharacterLogic>();
        Transform oldVisual = installedVisual != null
            ? installedVisual
            : logic != null ? logic.AimTransform : player.transform.Find("Visual");

        GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
        if (visual == null)
        {
            if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        visual.name = InstalledName;
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        FitToCharacterHeight(visual.transform, 1.35f);

        if (logic != null)
        {
            SerializedObject serializedLogic = new SerializedObject(logic);
            serializedLogic.FindProperty("_model").objectReferenceValue = visual.transform;
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            serializedLogic.FindProperty("_animator").objectReferenceValue = animator;
            serializedLogic.ApplyModifiedPropertiesWithoutUndo();
        }

        if (oldVisual != null && oldVisual != player.transform && oldVisual != visual.transform)
            Object.DestroyImmediate(oldVisual.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedTemporarily) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Installed CYBORG SOLDIER RX-1500 as the Player visual.");
    }

    private static void FitToCharacterHeight(Transform visual, float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer or TrailRenderer or LineRenderer) continue;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        if (bounds.size.y <= 0.001f) return;

        float scale = targetHeight / bounds.size.y;
        visual.localScale = Vector3.one * scale;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.position += Vector3.up * (visual.parent.position.y - bounds.min.y);
    }
}

public class CyborgModelImportHook : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        foreach (string path in imported)
        {
            if (path == "Assets/Models/CYBORG SOLDIER RX-1500/Gunplay.fbx")
            {
                EditorApplication.delayCall += CyborgPlayerModelInstaller.TryInstall;
                break;
            }
        }
    }
}
