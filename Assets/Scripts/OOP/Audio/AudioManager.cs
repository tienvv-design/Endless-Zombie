using UnityEngine;
using System;

[RequireComponent(typeof(AudioLowPassFilter))]
public class AudioManager : MonoBehaviour
{
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
    }

    public void Play(SoundLabel label)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null) return;
        s.source.Play();
    }

    public void Stop(SoundLabel label)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null) return;
        s.source.Stop();
    }

    public bool IsPlaying(SoundLabel label)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null) return false;
        return s.source.isPlaying;
    }

    public void SetGlobalVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetLocalVolume(SoundLabel label, float value)
    {
        Sound s = Array.Find(sounds, sound => sound.label == label);
        if (s == null) return;
        s.volume = value;
        s.source.volume = value;
    }

    public void SetMuffled(bool isMuffled)
    {
        // 500Hz makes it sound like it's behind a wall or underwater
        // lowPassFilter.cutoffFrequency = isMuffled ? 500f : 22000f;
    }
}