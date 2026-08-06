using System;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform m_XPFillBar;
    [SerializeField] private RectTransform m_HealthFillBar;
    
    private CharacterHealthManager m_HealthManager;
    
    private float m_XPMaxWidth;
    private float m_HealthMaxWidth;

    private void Awake()
    {
        if (m_XPFillBar != null)
        {
            m_XPMaxWidth = m_XPFillBar.sizeDelta.x;
            m_XPFillBar.sizeDelta = new Vector2(0f, m_XPFillBar.sizeDelta.y); 
        }

        if (m_HealthFillBar != null)
        {
            m_HealthMaxWidth = m_HealthFillBar.sizeDelta.x;
        }
    }

    private void Start()
    {
        if(CharacterXPManager.Instance) 
            CharacterXPManager.Instance.OnXPGain += GainXPCallback;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            m_HealthManager = player.GetComponent<CharacterHealthManager>();
        }
    }

    private void OnEnable()
    {
        if(CharacterXPManager.Instance) 
            CharacterXPManager.Instance.OnXPGain += GainXPCallback;
    }

    private void OnDisable()
    {
        if(CharacterXPManager.Instance) 
            CharacterXPManager.Instance.OnXPGain -= GainXPCallback;
    }

    private void Update()
    {
        if (m_HealthManager != null)
        {
            SetFillBar(m_HealthFillBar, m_HealthManager.GetHealthPercentage(), m_HealthMaxWidth);
        }
    }

    public void GainXPCallback(XPGainEventInfo info)
    {
        if (info.NextLevelRequiredXP == 0) return; 
        
        float percentage = (float)info.CurrentXP / info.NextLevelRequiredXP;
        
        SetFillBar(m_XPFillBar, percentage, m_XPMaxWidth);
    }
    
    public void SetFillBar(RectTransform rectTransform, float percentage, float maxWidth)
    {
        if (rectTransform == null) return;

        float fillAmount = percentage * maxWidth;
        
        float clampedWidth = Mathf.Clamp(fillAmount, 0, maxWidth);

        Vector2 sizeDelta = rectTransform.sizeDelta;
        rectTransform.sizeDelta = new Vector2(clampedWidth, sizeDelta.y);
    }
}