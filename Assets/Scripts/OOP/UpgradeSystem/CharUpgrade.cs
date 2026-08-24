using UnityEngine;
using Unity.Entities;

public enum UpgradeRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
}

public enum UpgradeTypes
{
    None,
    Speed,
    WeaponDamage,
    WeaponSpeed,
    ProjectileSpeed,
    WeaponSynergy,
}

public abstract class CharUpgrade : ScriptableObject
{
    [SerializeField] private Texture m_Texture;
    [SerializeField] private string m_Description;
    [SerializeField, Min(1)] private int m_BaseCost = 5;
    [SerializeField, Min(1f)] private float m_CostGrowth = 1.5f;
    [Header("Rarity")]
    [SerializeField] private UpgradeRarity m_Rarity = UpgradeRarity.Common;
    [SerializeField, Min(0.01f)] private float m_RollWeight = 60f;
    [SerializeField, Min(0.1f)] private float m_RarityCostMultiplier = 1f;
    
    public Texture Texture => m_Texture;
    public string Description => m_Description;
    public UpgradeRarity Rarity => m_Rarity;
    public float RollWeight => Mathf.Max(0.01f, m_RollWeight);
    public virtual string DisplayName => GetUpgradeType().ToString();
    public virtual bool IsEligible(GunArchetype archetype, int currentLevel) => true;

    public int GetCost(int currentLevel)
    {
        float scaledCost = m_BaseCost * Mathf.Pow(m_CostGrowth, currentLevel) * m_RarityCostMultiplier;
        return Mathf.Max(1, Mathf.CeilToInt(scaledCost));
    }

    public Color GetRarityColor()
    {
        return m_Rarity switch
        {
            UpgradeRarity.Uncommon => new Color32(74, 222, 128, 255),
            UpgradeRarity.Rare => new Color32(96, 165, 250, 255),
            UpgradeRarity.Epic => new Color32(192, 132, 252, 255),
            UpgradeRarity.Legendary => new Color32(251, 191, 36, 255),
            _ => Color.white,
        };
    }

    public abstract void Init();
    public abstract void ApplyUpgrade();
    public abstract UpgradeTypes GetUpgradeType();

    public virtual string GetValuePreview(int currentLevel)
    {
        return $"{currentLevel} → {currentLevel + 1}";
    }

    protected static bool TryGetGunStats(out WeaponManager gun, out GunModifiers modifiers)
    {
        gun = default;
        modifiers = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return false;
        EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<WeaponManager>(), ComponentType.ReadOnly<GunModifiers>());
        bool found = query.CalculateEntityCount() == 1;
        if (found)
        {
            Entity entity = query.GetSingletonEntity();
            gun = world.EntityManager.GetComponentData<WeaponManager>(entity);
            modifiers = world.EntityManager.GetComponentData<GunModifiers>(entity);
        }
        query.Dispose();
        return found;
    }

    public static bool TryGetActiveGunArchetype(out GunArchetype archetype)
    {
        archetype = GunArchetype.Pistol;
        if (!TryGetGunStats(out WeaponManager gun, out _)) return false;
        archetype = gun.Archetype;
        return true;
    }

    protected static string FormatStat(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.##");
    }
}
