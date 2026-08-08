using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthManager : Targetable
{
    [SerializeField] private CharacterStats _characterStats;

    public Action<int> OnDamageTaken;
    public Action OnDeath;
    
    private int m_Health;
    private int m_MaxHealth;
    private Image m_WorldHealthFill;

    protected override void OnAwake()
    {
        base.OnAwake();

        ApplyMetaProgression();
        CreateWorldHealthBar();
        if (!TryGetComponent<AttackRangeIndicator>(out _))
            gameObject.AddComponent<AttackRangeIndicator>();
        if (!TryGetComponent<RuntimeGunTuner>(out _))
            gameObject.AddComponent<RuntimeGunTuner>();
    }

    public override void TakeDamage(int damageAmount)
    {
        m_Health -= Mathf.Max(1, damageAmount);
        
        if (m_Health <= 0)
        {
            // TO DO: die.
            OnDeath?.Invoke();
        }
        
        OnDamageTaken?.Invoke(damageAmount);
        if (m_WorldHealthFill != null)
            m_WorldHealthFill.fillAmount = GetHealthPercentage();
    }

    public float GetHealthPercentage()
    {
        return m_MaxHealth > 0 ? (float)m_Health / m_MaxHealth : 0f;
    }

    public int BaseHealth => _characterStats != null ? _characterStats.Health : 0;

    public void ApplyMetaProgression()
    {
        m_MaxHealth = Mathf.Max(1, Mathf.RoundToInt(BaseHealth + MetaProgression.HealthBonus));
        m_Health = m_MaxHealth;
        if (m_WorldHealthFill != null)
            m_WorldHealthFill.fillAmount = 1f;
    }

    private void CreateWorldHealthBar()
    {
        GameObject canvasObject = new GameObject("PlayerWorldHealth", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        canvasObject.transform.localScale = Vector3.one * 0.008f;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(110f, 12f);

        GameObject background = new GameObject("Background", typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);
        Image bg = background.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        bg.rectTransform.anchorMin = Vector2.zero;
        bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(background.transform, false);
        m_WorldHealthFill = fill.GetComponent<Image>();
        m_WorldHealthFill.color = new Color(0.2f, 0.9f, 0.32f, 1f);
        m_WorldHealthFill.type = Image.Type.Filled;
        m_WorldHealthFill.fillMethod = Image.FillMethod.Horizontal;
        m_WorldHealthFill.rectTransform.anchorMin = new Vector2(0.04f, 0.18f);
        m_WorldHealthFill.rectTransform.anchorMax = new Vector2(0.96f, 0.82f);
        m_WorldHealthFill.rectTransform.sizeDelta = Vector2.zero;
        canvasObject.AddComponent<WorldSpaceUIBillboard>();
    }
}

public class WorldSpaceUIBillboard : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}
