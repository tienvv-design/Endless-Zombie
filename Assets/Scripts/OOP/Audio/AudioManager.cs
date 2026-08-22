using UnityEngine;
using System;

[RequireComponent(typeof(AudioLowPassFilter))]
public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SoundVolumeKey = "Settings.SoundVolume";
    public static AudioManager Instance;

    public Sound[] sounds;

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
    }

    public void Play(SoundLabel label)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null || !EnsureSource(s)) return;
        s.source.Play();
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
        SetLocalVolume(SoundLabel.InGameMusic, value);
        SetLocalVolume(SoundLabel.MainMenuMusic, value);
    }

    public void SetSoundVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SoundVolumeKey, value);
        foreach (Sound sound in sounds)
        {
            if (sound.label is SoundLabel.InGameMusic or SoundLabel.MainMenuMusic) continue;
            if (!EnsureSource(sound)) continue;
            sound.volume = value;
            sound.source.volume = value;
        }
    }

    public void SetLocalVolume(SoundLabel label, float value)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null || !EnsureSource(s)) return;
        s.volume = value;
        s.source.volume = value;
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
        sound.source.volume = sound.volume;
        sound.source.pitch = sound.pitch;
        sound.source.loop = sound.loop;
        return true;
    }
}
