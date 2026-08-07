using System;
using UnityEngine;

public class CharacterLogic : MonoBehaviour, IGameRunning
{
    [SerializeField] private CharacterStats _characterStatsAsset;
    private CharacterStats _characterCharacterStats;
    public CharacterStats CharacterStats => _characterCharacterStats;
    
    [SerializeField] private Transform _model;
    [SerializeField] private float _turnSpeed = 10f; 
    public Transform AimTransform => _model != null ? _model : transform;
    public float TurnSpeed => Mathf.Max(1f, _turnSpeed);

    [SerializeField] private Animator _animator; 
    
    private int _isWalkingHash;

    public Action<int> OnDamageTaken;

    void Awake()
    {
        _characterCharacterStats = Instantiate(_characterStatsAsset);
        // Cache the animator parameter ID for performance
        _isWalkingHash = Animator.StringToHash("IsWalking");
    }


    void Update()
    {
        // The player is a stationary defender. Combat is handled automatically by ECS.
        _animator.SetBool(_isWalkingHash, false);
    }

    public DamageableType GetDamageableType()
    {
        return DamageableType.Character;
    }

    public void OnStateEnable()
    {
        // Ensure we have an instance before accessing
        enabled = true;
    }

    public void OnStateDisable()
    {
        enabled = false;
    }
}
