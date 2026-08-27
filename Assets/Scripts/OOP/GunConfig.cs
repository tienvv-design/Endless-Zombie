using UnityEngine;

public enum GunArchetype
{
    Pistol,
    Shotgun,
    AssaultRifle,
    SniperRifle,
    RocketLauncher,
    SMG,
    TeslaGun,
    FlameRifle,
    CryoGun,
}

public enum ElementType : byte
{
    None,
    Fire,
    Frost,
}

[CreateAssetMenu(fileName = "GunConfig", menuName = "Settings-Configs/Gun Config")]
public class GunConfig : ScriptableObject
{
    [Header("Identity & Main Menu Visual")]
    public string WeaponId;
    public string DisplayName;
    public Sprite Icon;
    public GameObject HeldWeaponPrefab;
    public GameObject ProjectilePrefab;
    [Tooltip("Adjust this while the game is running; the held weapon updates immediately.")]
    public Vector3 HeldLocalPosition;
    [Tooltip("Local rotation of the held weapon. Changes update immediately in Play Mode.")]
    public Vector3 HeldLocalEulerAngles;
    [Tooltip("Local scale of the held weapon. Changes update immediately in Play Mode.")]
    public Vector3 HeldLocalScale = Vector3.one;

    [Header("Per-Weapon Arm Pose")]
    [Tooltip("Enable procedural arm posing for this weapon. All values update immediately in Play Mode.")]
    public bool UseCustomHoldPose;
    [Range(0f, 1f)] public float HoldPoseWeight = 1f;
    [Tooltip("Right-hand target relative to the chest: X = right, Y = up, Z = forward (world units).")]
    public Vector3 RightHandTargetOffset = new(0.08f, 0f, 0.35f);
    [Tooltip("Make the left hand follow a grip point on the weapon.")]
    public bool UseLeftHandIk = true;
    [Tooltip("Fallback left-hand grip in the weapon's local space. A child named LeftHandGrip overrides this value.")]
    public Vector3 LeftHandGripLocalPosition = new(0f, 0f, 0.25f);

    [Header("Legacy Pistol Pose (Custom Hold Pose Off)")]
    [Range(0f, 1f)] public float PistolPoseWeight = 1f;
    [Range(0.4f, 0.95f)] public float PistolArmReach = 0.78f;
    [Min(0.02f)] public float PistolHandSpacing = 0.14f;
    public float PistolPoseHeightOffset;

    [Header("VFX")]
    public GameObject MuzzleVfxPrefab;
    public GameObject ImpactVfxPrefab;
    public GameObject ExplosionVfxPrefab;
    public GameObject ReloadVfxPrefab;
    [Min(0.05f)] public float VfxLifetime = 1f;

    [Header("Combat")]
    public GunArchetype Archetype;
    [Min(1)] public int BaseDamage = 1;
    [Min(0.01f)] public float BaseShotsPerSecond = 1f;
    [Min(1)] public int BaseProjectileCount = 1;
    [Range(0f, 1f)] public float BaseCriticalChance = 0.1f;
    [Min(1f)] public float BaseCriticalDamage = 1.5f;
    [Min(0.1f)] public float BaseRange = 8f;
    [Min(0.1f)] public float BaseProjectileSpeed = 12f;
    [Min(0)] public int BasePierce;
    [Min(0f)] public float BaseKnockback = 0.5f;
    [Min(0f)] public float BaseSpreadAngle;
    [Min(0.01f)] public float MinimumFireInterval = 0.05f;
    [Header("Automatic Magazine")]
    [Min(1)] public int BaseMagazineSize = 12;
    [Min(0.05f)] public float BaseReloadDuration = 1.2f;
    [Header("Explosive Projectile")]
    public bool IsExplosive;
    [Min(0f)] public float ExplosionRadius;
    [Min(0f)] public float ExplosionDamageMultiplier = 1f;
    [Min(0f)] public float ExplosionKnockbackMultiplier = 1f;
    [Header("Ricochet")]
    [Min(0)] public int RicochetCount;
    [Min(0.1f)] public float RicochetSearchRange = 5f;
    [Header("Chain Lightning")]
    [Min(0)] public int ChainCount;
    [Min(0.1f)] public float ChainRange = 4f;
    [Range(0.1f, 1f)] public float ChainDamageMultiplier = 0.75f;
    [Header("Elemental Effect")]
    public ElementType Element;
    [Range(0f, 1f)] public float ElementChance;
    [Min(0f)] public float ElementDuration;
    [Min(0f)] public float ElementMagnitude;
}
