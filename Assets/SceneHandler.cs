using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneHandler : MonoBehaviour
{
    private SimulationSystemGroup _simulationSystemGroup;
    
    public void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            
            
            // Debug.Log("Setting systems enabled!");
            // World.DefaultGameObjectInjectionWorld.QuitUpdate = true;
        }

        // if (SceneManager.GetActiveScene().name == "GameScene")
        // {
        //     Debug.Log("Setting systems enabled!");
        //     World.DefaultGameObjectInjectionWorld.QuitUpdate = false;
        // }
    }

    public void LoadSceneCallback(string name)
    {
        if (name == "GameScene")
        {
            // Debug.Log("Setting systems enabled!");
            // World.DefaultGameObjectInjectionWorld.QuitUpdate = false;
        }
        
        if (name == "MainMenu")
        {
            PlayerInput.Instance?.InputActions.UI.Disable();
            PlayerInput.Instance?.InputActions.Player.Enable();
        }
        
        SceneManager.LoadScene(name);
    }
}
