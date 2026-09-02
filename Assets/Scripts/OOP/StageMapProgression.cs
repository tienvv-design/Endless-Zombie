using UnityEngine;
using UnityEngine.SceneManagement;

public static class StageMapProgression
{
    public const int FirstStage = 1;
    public const int LastConfiguredStage = 2;
    public const string CurrentStageKey = "StageProgress.CurrentStage";

    public static int CurrentStage => Mathf.Clamp(
        PlayerPrefs.GetInt(CurrentStageKey, FirstStage), FirstStage, LastConfiguredStage);

    public static string CurrentStageId => $"Stage{CurrentStage}";

    public static int AdvanceAfterWin()
    {
        int nextStage = Mathf.Min(CurrentStage + 1, LastConfiguredStage);
        PlayerPrefs.SetInt(CurrentStageKey, nextStage);
        PlayerPrefs.Save();
        return nextStage;
    }

    public static void SelectStage(int stage)
    {
        PlayerPrefs.SetInt(CurrentStageKey, Mathf.Clamp(stage, FirstStage, LastConfiguredStage));
        PlayerPrefs.Save();
    }
}

public static class StageMapRuntimeLoader
{
    private const string GameSceneName = "GameScene";
    private const string CityMapName = "Stage Map - VERSION1";
    private const string CityPrefabName = "Map_VERSION1";
    private const string CityResourcePath = "StageMaps/Map_VERSION1";
    private const string WastelandMapName = "Stage Map - Wasteland";
    private const string WastelandPrefabName = "WastelandArena_Endless";
    private const string WastelandResourcePath = "StageMaps/WastelandArena_Endless";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameSceneName) return;
        ApplyMapForCurrentStage(scene);
    }

    public static void ApplyMapForCurrentStage(Scene scene, bool movePlayerToSpawn = true)
    {
        GameObject cityMap = FindSceneObject(scene, CityMapName, CityPrefabName);
        GameObject fences = FindSceneObject(scene, "Fences");
        GameObject wasteland = FindSceneObject(scene, WastelandMapName, WastelandPrefabName);

        if (StageMapProgression.CurrentStage < 2)
        {
            GameObject cityPrefab = Resources.Load<GameObject>(CityResourcePath);
            if (cityPrefab == null)
            {
                Debug.LogError($"Stage 1 map is missing at Resources/{CityResourcePath}.");
                if (wasteland != null) wasteland.SetActive(false);
                return;
            }

            cityMap = cityMap != null ? cityMap : Object.Instantiate(cityPrefab);
            cityMap.name = CityMapName;
            if (cityMap.scene != scene)
                SceneManager.MoveGameObjectToScene(cityMap, scene);
            cityMap.SetActive(true);
            if (fences != null) fences.SetActive(true);
            if (wasteland != null) wasteland.SetActive(false);
            if (movePlayerToSpawn)
                MovePlayerToStageSpawn(cityMap.transform);
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(WastelandResourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Stage 2 map is missing at Resources/{WastelandResourcePath}.");
            if (cityMap != null) cityMap.SetActive(true);
            return;
        }

        if (cityMap != null) cityMap.SetActive(false);
        if (fences != null) fences.SetActive(false);

        wasteland = wasteland != null ? wasteland : Object.Instantiate(prefab);
        wasteland.name = WastelandMapName;
        if (wasteland.scene != scene)
            SceneManager.MoveGameObjectToScene(wasteland, scene);
        wasteland.SetActive(true);
        if (movePlayerToSpawn)
            MovePlayerToStageSpawn(wasteland.transform);
    }

    private static GameObject FindSceneObject(Scene scene, params string[] names)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (string name in names)
                    if (item.name == name) return item.gameObject;
            }
        }

        return null;
    }

    private static void MovePlayerToStageSpawn(Transform map)
    {
        Transform spawn = FindDescendant(map, "Spawn_Player");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (spawn == null || player == null) return;

        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        if (player.TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item;
        return null;
    }
}
