using UnityEngine;

[CreateAssetMenu(fileName = "ArenaEnvironment", menuName = "Environment/Arena Environment")]
public sealed class ArenaEnvironmentConfig : ScriptableObject
{
    [Header("Ground")]
    [Min(10f)] public float GroundSize = 60f;
    public Color GroundColor = new(0.22f, 0.48f, 0.18f, 1f);
    [Min(0f)] public float GroundHeight = 0.03f;

    [Header("Layout")]
    [Min(1f)] public float ClearCombatRadius = 14f;
    [Min(2f)] public float DecorationOuterRadius = 25f;
    public int RandomSeed = 2408;

    [Header("Decoration Prefabs")]
    public GameObject[] TreePrefabs;
    public GameObject[] RockPrefabs;
    public GameObject[] GroundDetailPrefabs;

    [Header("Density")]
    [Min(0)] public int TreeCount = 16;
    [Min(0)] public int RockCount = 10;
    [Min(0)] public int GroundDetailCount = 24;

    [Header("Scale Variation")]
    public Vector2 TreeScaleRange = new(0.85f, 1.2f);
    public Vector2 RockScaleRange = new(0.7f, 1.35f);
    public Vector2 GroundDetailScaleRange = new(0.7f, 1.15f);
}
