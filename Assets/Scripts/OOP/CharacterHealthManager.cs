using System;
using UnityEngine;

public class CharacterHealthManager : Targetable
{
    [SerializeField] private CharacterStats _characterStats;

    public Action<int> OnDamageTaken;
    public Action OnDeath;
    
    private int m_Health;

    protected override void OnAwake()
    {
        base.OnAwake();

        m_Health = _characterStats.Health;
    }

    public override void TakeDamage(int damageAmount)
    {
        m_Health -= damageAmount;
        
        if (m_Health <= 0)
        {
            // TO DO: die.
            OnDeath?.Invoke();
        }
        
        OnDamageTaken?.Invoke(damageAmount);
    }

    public float GetHealthPercentage()
    {
        return (float)m_Health / _characterStats.Health;
    }
}
    