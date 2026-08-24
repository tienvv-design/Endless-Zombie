#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GunUpgradePoolGenerator
{
    private const string Folder = "Assets/Resources/WeaponUpgrades";
    static GunUpgradePoolGenerator() { EditorApplication.delayCall += Generate; }

    [MenuItem("Tools/Endless Zombie/Regenerate Gun Upgrade Pools")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Add("Pistol_Deadeye", "Deadeye Rounds", GunArchetype.Pistol, "Precision rounds greatly improve critical hits.", E(crit:.08f, critDamage:.25f));
        Add("Pistol_TrickShot", "Trick Shot", GunArchetype.Pistol, "Bullets bounce to another nearby zombie.", E(ricochets:1));
        Add("Pistol_QuickDraw", "Quick Draw", GunArchetype.Pistol, "Faster trigger rhythm and a snappier reload.", E(fireRate:15, reload:15));
        Add("Pistol_ExtendedMag", "Extended Sidearm Mag", GunArchetype.Pistol, "Adds four rounds to every magazine.", E(magazine:4));

        Add("Shotgun_MorePellets", "Gravedigger Buckshot", GunArchetype.Shotgun, "Adds two pellets to every blast.", E(projectiles:2));
        Add("Shotgun_Choke", "Tight Choke", GunArchetype.Shotgun, "Tighter spread and faster pellets.", E(spread:25, speed:12));
        Add("Shotgun_Heavy", "Heavy Buckshot", GunArchetype.Shotgun, "Harder hits send the horde stumbling back.", E(damage:20, knockback:.4f));
        Add("Shotgun_Loader", "Combat Speedloader", GunArchetype.Shotgun, "Reload shells faster and carry two more.", E(magazine:2, reload:25));

        Add("Rifle_Suppressive", "Suppressive Fire", GunArchetype.AssaultRifle, "Higher cyclic rate keeps lanes under control.", E(fireRate:18));
        Add("Rifle_AP", "Armor-Piercing Rounds", GunArchetype.AssaultRifle, "Rounds punch through one additional target.", E(pierce:1, speed:12));
        Add("Rifle_Drum", "Drum Magazine", GunArchetype.AssaultRifle, "A large magazine for sustained automatic fire.", E(magazine:12));
        Add("Rifle_Zeroing", "Combat Zeroing", GunArchetype.AssaultRifle, "Improved optics increase damage and crit chance.", E(damage:12, crit:.05f));

        Add("Rocket_BiggerBoom", "Bigger Boom", GunArchetype.RocketLauncher, "Expands the lethal radius of every explosion.", E(blastRadius:22));
        Add("Rocket_Warhead", "Overpacked Warhead", GunArchetype.RocketLauncher, "More explosive payload in every rocket.", E(damage:25, blastDamage:.15f));
        Add("Rocket_Autoloader", "Hydraulic Autoloader", GunArchetype.RocketLauncher, "Cycles and reloads heavy rockets faster.", E(fireRate:12, reload:25));
        Add("Rocket_Twin", "Twin Launch", GunArchetype.RocketLauncher, "Launches an additional rocket per volley.", E(projectiles:1));

        AddFuturePools();
        AssetDatabase.SaveAssets();
    }

    private static void AddFuturePools()
    {
        Add("Sniper_OneShot", "One Shot, One Grave", GunArchetype.SniperRifle, "Massive precision damage.", E(damage:30));
        Add("Sniper_Penetrator", "Penetrator", GunArchetype.SniperRifle, "The round passes through another body.", E(pierce:1));
        Add("Sniper_Scope", "Dead City Optics", GunArchetype.SniperRifle, "Faster rounds and higher critical chance.", E(speed:20, crit:.1f));
        Add("SMG_BulletHose", "Bullet Hose", GunArchetype.SMG, "Extreme close-range fire rate.", E(fireRate:22));
        Add("SMG_BoxMag", "Box Magazine", GunArchetype.SMG, "Adds ten rounds and reload speed.", E(magazine:10, reload:12));
        Add("SMG_Runner", "Runaway Rounds", GunArchetype.SMG, "Shots can ricochet through the crowd.", E(ricochets:1));
        Add("Tesla_Fork", "Forked Current", GunArchetype.TeslaGun, "Electric arcs jump to another target.", E(chains:1));
        Add("Tesla_Overcharge", "Overcharge", GunArchetype.TeslaGun, "Stronger, faster electric discharge.", E(damage:15, fireRate:12));
        Add("Tesla_Capacitor", "Field Capacitor", GunArchetype.TeslaGun, "More charge capacity and elemental force.", E(magazine:5, elementMagnitude:20));
        Add("Flame_Inferno", "Inferno Mix", GunArchetype.FlameRifle, "Burns become stronger and more reliable.", E(elementChance:.12f, elementMagnitude:25));
        Add("Flame_Pressure", "High-Pressure Tank", GunArchetype.FlameRifle, "A larger tank with faster pressure recovery.", E(magazine:8, reload:18));
        Add("Flame_Fan", "Wide Flame", GunArchetype.FlameRifle, "Adds another stream of burning fuel.", E(projectiles:1));
        Add("Cryo_DeepFreeze", "Deep Freeze", GunArchetype.CryoGun, "Cold effects become stronger and more reliable.", E(elementChance:.12f, elementMagnitude:25));
        Add("Cryo_Shatter", "Shatter Rounds", GunArchetype.CryoGun, "Frozen ammunition hits harder.", E(damage:20, critDamage:.25f));
        Add("Cryo_Splinter", "Cryo Splinters", GunArchetype.CryoGun, "Adds another freezing projectile.", E(projectiles:1));
    }

    private static void Add(string file, string title, GunArchetype gun, string description, GunUpgradeEffect effect)
    {
        string path = $"{Folder}/{file}.asset";
        GunPoolUpgrade asset = AssetDatabase.LoadAssetAtPath<GunPoolUpgrade>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<GunPoolUpgrade>(); AssetDatabase.CreateAsset(asset, path); }
        asset.Configure(title, gun, 5, effect);
        SerializedObject serialized = new(asset);
        serialized.FindProperty("m_Description").stringValue = description;
        serialized.FindProperty("m_RollWeight").floatValue = 45f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    private static GunUpgradeEffect E(float damage=0, float fireRate=0, float range=0, float speed=0,
        int projectiles=0, int pierce=0, int magazine=0, int ricochets=0, int chains=0,
        float crit=0, float critDamage=0, float knockback=0, float reload=0, float spread=0,
        float blastRadius=0, float blastDamage=0, float elementChance=0, float elementMagnitude=0) => new()
    {
        DamagePercent=damage, FireRatePercent=fireRate, RangePercent=range, ProjectileSpeedPercent=speed,
        Projectiles=projectiles, Pierce=pierce, Magazine=magazine, Ricochets=ricochets, Chains=chains,
        CriticalChance=crit, CriticalDamage=critDamage, Knockback=knockback, ReloadSpeedPercent=reload,
        SpreadReductionPercent=spread, ExplosionRadiusPercent=blastRadius, ExplosionDamage=blastDamage,
        ElementChance=elementChance, ElementMagnitudePercent=elementMagnitude
    };
}
#endif
