using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterLogic : MonoBehaviour, IGameRunning
{
    [SerializeField] private CharacterStats _characterStatsAsset;
    private CharacterStats _characterCharacterStats;
    public CharacterStats CharacterStats => _characterCharacterStats;
    
    private Vector2 _moveVector;

    [SerializeField] private Transform _model;
    [SerializeField] private float _turnSpeed = 10f; 

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
        Vector3 moveDirection = new Vector3(_moveVector.x, 0.0f, _moveVector.y);
        
        transform.position += moveDirection * _characterCharacterStats.MoveSpeed * Time.deltaTime;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _model.rotation = Quaternion.Slerp(_model.rotation, targetRotation, _turnSpeed * Time.deltaTime);
            
            _animator.SetBool(_isWalkingHash, true);
        }
        else
        {
            _animator.SetBool(_isWalkingHash, false);
        }
    }

    public void MoveInputCallback(InputAction.CallbackContext ctx)
    {
        if (ctx.phase == InputActionPhase.Performed)
        {
            _moveVector = ctx.ReadValue<Vector2>();
        }
        else if (ctx.phase == InputActionPhase.Canceled)
        {
            _moveVector = Vector2.zero;
        }
    }

    public DamageableType GetDamageableType()
    {
        return DamageableType.Character;
    }

    public void OnStateEnable()
    {
        // Ensure we have an instance before accessing
        if(PlayerInput.Instance != null)
            PlayerInput.Instance.InputActions.Player.Move.SubscribeAll(MoveInputCallback);

        enabled = true;
    }

    public void OnStateDisable()
    {
        if(PlayerInput.Instance != null)
            PlayerInput.Instance.InputActions.Player.Move.UnsubscribeAll(MoveInputCallback);

        enabled = false;
    }
}