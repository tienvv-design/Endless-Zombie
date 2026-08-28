using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioLowPassFilter))]
public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SoundVolumeKey = "Settings.SoundVolume";
    public static AudioManager Instance;

    public Sound[] sounds;

    private readonly HashSet<Button> boundButtons = new();
    private float nextButtonScanTime;

    private sealed class DefaultSoundSpec
    {
        public readonly SoundLabel Label;
        public readonly float Volume;
        public readonly float Pitch;
        public readonly bool Loop;
        public readonly float RandomPitch;
        public readonly float MinimumInterval;
        public readonly string[] Clips;

        public DefaultSoundSpec(SoundLabel label, float volume, float pitch, bool loop,
            float randomPitch, float minimumInterval, params string[] clips)
        {
            Label = label;
            Volume = volume;
            Pitch = pitch;
            Loop = loop;
            RandomPitch = randomPitch;
            MinimumInterval = minimumInterval;
            Clips = clips;
        }
    }

    private static readonly DefaultSoundSpec[] LdoeDefaults =
    {
        new(SoundLabel.MainMenuMusic, 0.42f, 1f, true, 0f, 0f, "Music_MainMenu"),
        new(SoundLabel.InGameMusic, 0.5f, 1f, true, 0f, 0f, "Music_Gameplay"),
        new(SoundLabel.CityAmbience, 0.16f, 1f, true, 0f, 0f, "Ambience_City"),
        new(SoundLabel.WastelandAmbience, 0.22f, 1f, true, 0f, 0f, "Ambience_Wasteland"),
        new(SoundLabel.MobDeathSound, 0.48f, 1f, false, 0.09f, 0.06f,
            "Zombie_Death_01", "Zombie_Death_02"),
        new(SoundLabel.MobDamageSound, 0.3f, 1f, false, 0.12f, 0.055f,
            "Zombie_Damage_01", "Zombie_Damage_02"),
        new(SoundLabel.MobGiveDamageSound, 0.38f, 1f, false, 0.1f, 0.12f,
            "Zombie_Attack_01", "Zombie_Attack_02"),
        new(SoundLabel.PlayerDamageSound, 0.55f, 1f, false, 0.05f, 0.14f, "Player_Damage"),
        new(SoundLabel.PlayerDeathSound, 0.7f, 1f, false, 0f, 0.2f, "Player_Death"),
        new(SoundLabel.LevelUpSound, 0.6f, 1f, false, 0f, 0.2f, "Level_Up"),
        new(SoundLabel.DigitExplosionSound, 0.65f, 1f, false, 0.05f, 0.08f, "Explosion"),
        new(SoundLabel.UiClickSound, 0.34f, 1f, false, 0.03f, 0.035f, "UI_Click"),
        new(SoundLabel.StageWinSound, 0.7f, 1f, false, 0f, 0.5f, "Stage_Win"),
        new(SoundLabel.StageStartSound, 0.58f, 1f, false, 0f, 0.5f, "Battle_Start"),
        new(SoundLabel.DogAttackSound, 0.35f, 1f, false, 0.08f, 0.16f, "Dog_Attack"),
        new(SoundLabel.PickupGoldSound, 0.32f, 1f, false, 0.05f, 0.06f, "Pickup_Gold"),
        new(SoundLabel.PickupXpSound, 0.26f, 1f, false, 0.04f, 0.06f, "Pickup_XP"),
        new(SoundLabel.WeaponPistol, 0.48f, 1f, false, 0.025f, 0.055f, "Weapon_Pistol"),
        new(SoundLabel.WeaponShotgun, 0.62f, 1f, false, 0.025f, 0.12f, "Weapon_Shotgun"),
        new(SoundLabel.WeaponAssaultRifle, 0.34f, 1f, false, 0.025f, 0.045f, "Weapon_AssaultRifle"),
        new(SoundLabel.WeaponSniper, 0.64f, 1f, false, 0.02f, 0.15f, "Weapon_Sniper"),
        new(SoundLabel.WeaponRocket, 0.56f, 1f, false, 0.02f, 0.12f, "Weapon_Rocket"),
        new(SoundLabel.WeaponSmg, 0.3f, 1f, false, 0.03f, 0.04f, "Weapon_SMG"),
        new(SoundLabel.WeaponTesla, 0.42f, 1f, false, 0.04f, 0.055f, "Weapon_Tesla"),
        new(SoundLabel.WeaponFlame, 0.25f, 1f, false, 0.02f, 0.075f, "Weapon_Flame"),
        new(SoundLabel.WeaponCryo, 0.4f, 1f, false, 0.04f, 0.07f, "Weapon_Cryo"),
        new(SoundLabel.WeaponMinigun, 0.23f, 1f, false, 0.025f, 0.035f, "Weapon_Minigun"),
        new(SoundLabel.WeaponGrenade, 0.55f, 1f, false, 0.02f, 0.1f, "Weapon_Grenade"),
        new(SoundLabel.WeaponReload, 0.36f, 1f, false, 0.02f, 0.2f, "Weapon_Reload"),
    };

    // private AudioLowPassFilter lowPassFilter;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        sounds ??= Array.Empty<Sound>();
        InstallLdoeDefaults();

        // lowPassFilter = GetComponent<AudioLowPassFilter>();
        // // 22000Hz is standard high quality audio (invisible/clear)
        // lowPassFilter.cutoffFrequency = 22000f; 

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }

        SetMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
        SetSoundVolume(PlayerPrefs.GetFloat(SoundVolumeKey, 0.7f));
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextButtonScanTime) return;
        nextButtonScanTime = Time.unscaledTime + 0.75f;
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || !boundButtons.Add(button)) continue;
            button.onClick.AddListener(PlayUiClick);
        }
    }

    public void Play(SoundLabel label)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null || !EnsureSource(s)) return;
        float now = Time.unscaledTime;
        if (now < s.nextAllowedPlayTime) return;
        s.nextAllowedPlayTime = now + s.minimumInterval;

        AudioClip clip = ChooseClip(s);
        if (clip == null) return;
        s.source.pitch = s.pitch * UnityEngine.Random.Range(1f - s.randomPitch, 1f + s.randomPitch);
        if (s.loop)
        {
            if (s.source.clip != clip) s.source.clip = clip;
            if (!s.source.isPlaying) s.source.Play();
        }
        else
        {
            s.source.PlayOneShot(clip);
        }
    }

    public void Stop(SoundLabel label)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null || s.source == null) return;
        s.source.Stop();
    }

    public bool IsPlaying(SoundLabel label)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null || s.source == null) return false;
        return s.source.isPlaying;
    }

    public void SetGlobalVolume(float value)
    {
        AudioListener.volume = value;
    }

    public float MusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
    public float SoundVolume => PlayerPrefs.GetFloat(SoundVolumeKey, 0.7f);

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        foreach (Sound sound in sounds)
            if (IsMusic(sound.label) && EnsureSource(sound))
                sound.source.volume = sound.volume * value;
    }

    public void SetSoundVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SoundVolumeKey, value);
        foreach (Sound sound in sounds)
        {
            if (IsMusic(sound.label)) continue;
            if (!EnsureSource(sound)) continue;
            sound.source.volume = sound.volume * value;
        }
    }

    public void SetLocalVolume(SoundLabel label, float value)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null || !EnsureSource(s)) return;
        s.volume = value;
        s.source.volume = value * (IsMusic(label) ? MusicVolume : SoundVolume);
    }

    public void SetStageAmbience(int stage)
    {
        if (stage < 2)
        {
            Stop(SoundLabel.WastelandAmbience);
            if (!IsPlaying(SoundLabel.CityAmbience))
                Play(SoundLabel.CityAmbience);
            return;
        }

        Stop(SoundLabel.CityAmbience);
        if (!IsPlaying(SoundLabel.WastelandAmbience))
            Play(SoundLabel.WastelandAmbience);
    }

    public void PlayWeapon(GunArchetype archetype)
    {
        Play(archetype switch
        {
            GunArchetype.Shotgun => SoundLabel.WeaponShotgun,
            GunArchetype.AssaultRifle => SoundLabel.WeaponAssaultRifle,
            GunArchetype.SniperRifle => SoundLabel.WeaponSniper,
            GunArchetype.RocketLauncher => SoundLabel.WeaponRocket,
            GunArchetype.SMG => SoundLabel.WeaponSmg,
            GunArchetype.TeslaGun => SoundLabel.WeaponTesla,
            GunArchetype.FlameRifle => SoundLabel.WeaponFlame,
            GunArchetype.CryoGun => SoundLabel.WeaponCryo,
            GunArchetype.Minigun => SoundLabel.WeaponMinigun,
            GunArchetype.GrenadeLauncher => SoundLabel.WeaponGrenade,
            _ => SoundLabel.WeaponPistol,
        });
    }

    public void PlayWeaponReload(GunArchetype archetype)
    {
        if (archetype is GunArchetype.TeslaGun or GunArchetype.CryoGun)
            Play(SoundLabel.WeaponReload);
    }

    public void SetMuffled(bool isMuffled)
    {
        // 500Hz makes it sound like it's behind a wall or underwater
        // lowPassFilter.cutoffFrequency = isMuffled ? 500f : 22000f;
    }

    private bool EnsureSource(Sound sound)
    {
        if (sound == null) return false;
        if (sound.source != null) return true;
        sound.source = gameObject.AddComponent<AudioSource>();
        sound.source.clip = sound.clip;
        sound.source.volume = sound.volume * (IsMusic(sound.label) ? MusicVolume : SoundVolume);
        sound.source.pitch = sound.pitch;
        sound.source.loop = sound.loop;
        return true;
    }

    private void InstallLdoeDefaults()
    {
        foreach (DefaultSoundSpec spec in LdoeDefaults)
        {
            List<AudioClip> loaded = new();
            foreach (string clipName in spec.Clips)
            {
                AudioClip clip = Resources.Load<AudioClip>($"LDoEAudio/{clipName}");
                if (clip != null) loaded.Add(clip);
            }
            if (loaded.Count == 0) continue;

            Sound sound = Array.Find(sounds, item => item.label == spec.Label);
            if (sound == null)
            {
                sound = new Sound { label = spec.Label };
                Array.Resize(ref sounds, sounds.Length + 1);
                sounds[^1] = sound;
            }

            sound.clip = loaded[0];
            sound.variants = loaded.ToArray();
            sound.volume = spec.Volume;
            sound.pitch = spec.Pitch;
            sound.loop = spec.Loop;
            sound.randomPitch = spec.RandomPitch;
            sound.minimumInterval = spec.MinimumInterval;
        }
    }

    private static AudioClip ChooseClip(Sound sound)
    {
        if (sound.variants is { Length: > 0 })
            return sound.variants[UnityEngine.Random.Range(0, sound.variants.Length)];
        return sound.clip;
    }

    private static bool IsMusic(SoundLabel label) =>
        label is SoundLabel.InGameMusic or SoundLabel.MainMenuMusic or
            SoundLabel.CityAmbience or SoundLabel.WastelandAmbience;

    private void PlayUiClick() => Play(SoundLabel.UiClickSound);

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        boundButtons.Clear();
        nextButtonScanTime = 0f;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }
}
