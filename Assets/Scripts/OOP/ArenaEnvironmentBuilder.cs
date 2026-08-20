using UnityEngine;

public sealed class ArenaEnvironmentBuilder : MonoBehaviour
{
    private const string RootName = "Generated Arena Environment";

    public static void EnsureBuilt(ArenaEnvironmentConfig config)
    {
        // A hand-authored/imported stage map takes precedence over the old
        // procedurally decorated arena so two grounds are never stacked.
        if (config == null || GameObject.Find(RootName) != null || GameObject.Find("Stage Map - VERSION1") != null)
            return;
        GameObject host = new(RootName);
        ArenaEnvironmentBuilder builder = host.AddComponent<ArenaEnvironmentBuilder>();
        builder.Build(config);
    }

    private void Build(ArenaEnvironmentConfig config)
    {
        Random.State previousState = Random.state;
        Random.InitState(config.RandomSeed);
        CreateGround(config);
        CreateGroup("Trees", config.TreePrefabs, config.TreeCount,
            config.ClearCombatRadius, config.DecorationOuterRadius, config.TreeScaleRange);
        CreateGroup("Rocks", config.RockPrefabs, config.RockCount,
            config.ClearCombatRadius + 1f, config.DecorationOuterRadius, config.RockScaleRange);
        CreateGroup("Ground Details", config.GroundDetailPrefabs, config.GroundDetailCount,
            config.ClearCombatRadius, config.DecorationOuterRadius, config.GroundDetailScaleRange);
        Random.state = previousState;
    }

    private void CreateGround(ArenaEnvironmentConfig config)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Green Grass Ground";
        ground.transform.SetParent(transform, false);
        ground.transform.localPosition = new Vector3(0f, config.GroundHeight, 0f);
        ground.transform.localScale = Vector3.one * (config.GroundSize / 10f);
        Collider collider = ground.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;
        Material material = new(shader)
        {
            name = "Runtime Grass Ground",
            color = config.GroundColor,
        };
        ground.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void CreateGroup(string groupName, GameObject[] prefabs, int count,
        float innerRadius, float outerRadius, Vector2 scaleRange)
    {
        if (prefabs == null || prefabs.Length == 0 || count <= 0) return;
        Transform group = new GameObject(groupName).transform;
        group.SetParent(transform, false);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            if (prefab == null) continue;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
            Vector3 position = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            GameObject item = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), group);
            float scale = Random.Range(Mathf.Min(scaleRange.x, scaleRange.y), Mathf.Max(scaleRange.x, scaleRange.y));
            item.transform.localScale *= scale;
            item.name = $"{prefab.name} {i + 1:00}";

            foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }
    }
}
