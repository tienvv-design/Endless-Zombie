using UnityEngine;

public enum SoundLabel
{
    InGameMusic,
    MainMenuMusic,
    MobDeathSound,
    DigitExplosionSound,
    LevelUpSound,
    PlayerDeathSound,
    MobGiveDamageSound,
    MobDamageSound,
    PlayerDamageSound,
    UiClickSound,
    StageWinSound,
    StageStartSound,
    CityAmbience,
    WastelandAmbience,
    DogAttackSound,
    PickupGoldSound,
    PickupXpSound,
    WeaponPistol,
    WeaponShotgun,
    WeaponAssaultRifle,
    WeaponSniper,
    WeaponRocket,
    WeaponSmg,
    WeaponTesla,
    WeaponFlame,
    WeaponCryo,
    WeaponMinigun,
    WeaponGrenade,
    WeaponReload,
}

[System.Serializable]
public class Sound
{
    public SoundLabel label;
    public AudioClip clip;

    [Range(0f, 1f)] 
    public float volume = 0.7f;
    
    [Range(0.1f, 3f)]
    public float pitch = 1f;

    public bool loop;

    [Range(0f, 0.5f)]
    public float randomPitch;

    [Min(0f)]
    public float minimumInterval;

    [HideInInspector]
    public AudioSource source;

    [System.NonSerialized] public AudioClip[] variants;
    [System.NonSerialized] public float nextAllowedPlayTime;
}
