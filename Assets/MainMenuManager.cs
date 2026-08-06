using System;
using Unity.Entities;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance.Play(SoundLabel.MainMenuMusic);
    }

    private void OnDisable()
    {
        AudioManager.Instance.Stop(SoundLabel.MainMenuMusic);
    }
}
