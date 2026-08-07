using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance;
    
    private PlayerInputActions _inputActions; 
    public PlayerInputActions InputActions => _inputActions;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        // Debug.Log("Player input is initialized!");
        
        _inputActions = new PlayerInputActions();
    }

    public void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    public void OnDisable()
    {
        _inputActions.Player.Disable();
    }
}

