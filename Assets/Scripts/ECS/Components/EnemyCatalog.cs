using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyCatalog", menuName = "Wave Spawn/Enemy Catalog")]
public sealed class EnemyCatalog : ScriptableObject
{
    public EnemyCatalogEntry[] Enemies = Array.Empty<EnemyCatalogEntry>();

    public bool TryGet(string enemyId, out EnemyCatalogEntry result)
    {
        foreach (EnemyCatalogEntry entry in Enemies)
            if (entry != null && string.Equals(entry.EnemyId, enemyId, StringComparison.Ordinal))
            {
                result = entry;
                return true;
            }
        result = null;
        return false;
    }
}

[Serializable]
public sealed class EnemyCatalogEntry
{
    public string EnemyId;
    public GameObject Prefab;
    public EnemyType EnemyType = EnemyType.Normal;
    public MobVisualKind VisualKind = MobVisualKind.Zombie;
    [Min(0.01f)] public float HealthMultiplier = 1f;
    [Min(0.01f)] public float DamageMultiplier = 1f;
    [Min(0.01f)] public float Scale = 1f;
    [Min(1)] public int XPReward = 5;
    [Min(0)] public int GoldReward = 1;
}
