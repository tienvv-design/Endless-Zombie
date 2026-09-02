using System;
using UnityEngine;

public class CharacterHealthManager : Targetable
{
    [SerializeField] private CharacterStats _characterStats;

    public Action<int> OnDamageTaken;
    public event Action OnDeath;
    
    private int m_Health;
    private int m_MaxHealth;
    private bool m_IsDead;

    public bool DebugInvincible { get; set; }

    protected override void OnAwake()
    {
        base.OnAwake();

        ApplyMetaProgression();
        Transform legacyHealthBar = transform.Find("HealtBar");
        if (legacyHealthBar != null)
            legacyHealthBar.gameObject.SetActive(false);
        if (!TryGetComponent<RuntimeGunTuner>(out _))
            gameObject.AddComponent<RuntimeGunTuner>();
    }

    public override void TakeDamage(int damageAmount)
    {
        if (m_IsDead || DebugInvincible) return;
        m_Health = Mathf.Max(0, m_Health - Mathf.Max(1, damageAmount));
        OnDamageTaken?.Invoke(damageAmount);
        if (m_Health == 0)
        {
            m_IsDead = true;
            OnDeath?.Invoke();
        }
    }

    public float GetHealthPercentage()
    {
        return m_MaxHealth > 0 ? (float)m_Health / m_MaxHealth : 0f;
    }

    public int BaseHealth => _characterStats != null ? _characterStats.Health : 0;
    public int CurrentHealth => m_Health;
    public int MaxHealth => m_MaxHealth;
    public bool IsDead => m_IsDead;

    public void RestoreFullHealth()
    {
        m_Health = m_MaxHealth;
        m_IsDead = false;
    }

    public void ApplyMetaProgression()
    {
        m_MaxHealth = Mathf.Max(1, Mathf.RoundToInt(BaseHealth + MetaProgression.HealthBonus));
        m_Health = m_MaxHealth;
        m_IsDead = false;
    }
}
