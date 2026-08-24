using UnityEngine;

[CreateAssetMenu(fileName = "ArenaEnvironment", menuName = "Environment/Arena Environment")]
public sealed class ArenaEnvironmentConfig : ScriptableObject
{
    [Header("Map Polish")]
    public bool EnableAtmosphere = true;
    public Color AmbientColor = new(0.19f, 0.23f, 0.2f, 1f);
    public Color FogColor = new(0.12f, 0.15f, 0.13f, 1f);
    [Min(0f)] public float FogStart = 24f;
    [Min(1f)] public float FogEnd = 58f;
    public Color SunColor = new(1f, 0.78f, 0.6f, 1f);
    [Min(0f)] public float SunIntensity = 1.15f;
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
