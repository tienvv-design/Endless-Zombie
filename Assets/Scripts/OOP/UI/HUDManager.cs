using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Entities;

public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform m_XPFillBar;
    [SerializeField] private RectTransform m_HealthFillBar;
    [SerializeField] private TMP_Text m_GoldText;
    private TMP_Text m_LevelText;
    private TMP_Text m_WeaponStatsText;
    private EntityQuery m_WeaponQuery;
    private EntityManager m_EntityManager;
    private bool m_WeaponQueryCreated;
    private float m_StatsRefreshTimer;
    
    private CharacterHealthManager m_HealthManager;
    
    private float m_XPMaxWidth;
    private float m_HealthMaxWidth;

    private void Awake()
    {
        if (m_GoldText == null)
            m_GoldText = CreateGoldCounter();
        m_LevelText = CreateLevelText();
        m_WeaponStatsText = CreateWeaponStatsText();

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
        SubscribeToXP();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            m_HealthManager = player.GetComponent<CharacterHealthManager>();
        }
        if (CharacterXPManager.Instance)
            GainXPCallback(new XPGainEventInfo
            {
                CurrentLevel = CharacterXPManager.Instance.CharacterLevel,
                CurrentXP = CharacterXPManager.Instance.CurrentXP,
                NextLevelRequiredXP = CharacterXPManager.Instance.NextLevelRequiredXP,
            });
    }

    private void OnEnable()
    {
        SubscribeToXP();

        if (GoldWallet.Instance)
        {
            GoldWallet.Instance.OnRunRewardChanged += GoldChangedCallback;
            GoldChangedCallback(GoldWallet.Instance.RunReward);
        }
    }

    private void OnDisable()
    {
        if(CharacterXPManager.Instance) 
            CharacterXPManager.Instance.OnXPGain -= GainXPCallback;

        if (GoldWallet.Instance)
            GoldWallet.Instance.OnRunRewardChanged -= GoldChangedCallback;
    }

    private void SubscribeToXP()
    {
        if (!CharacterXPManager.Instance) return;
        CharacterXPManager.Instance.OnXPGain -= GainXPCallback;
        CharacterXPManager.Instance.OnXPGain += GainXPCallback;
    }

    private void GoldChangedCallback(int balance)
    {
        if (m_GoldText != null)
            m_GoldText.text = $"Run Gold: {balance}";
    }

    private TMP_Text CreateGoldCounter()
    {
        GameObject counter = new GameObject("GoldCounter", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        counter.transform.SetParent(transform, false);

        RectTransform rect = counter.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -24f);
        rect.sizeDelta = new Vector2(260f, 50f);

        TextMeshProUGUI text = counter.GetComponent<TextMeshProUGUI>();
        text.text = "Run Gold: 0";
        text.fontSize = 28f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.TopRight;
        text.color = new Color(1f, 0.82f, 0.2f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text CreateLevelText()
    {
        GameObject label = new GameObject("PlayerLevel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 26f);
        rect.sizeDelta = new Vector2(220f, 36f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "Level 0";
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text CreateWeaponStatsText()
    {
        GameObject label = new GameObject("WeaponRuntimeStats", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(420f, 42f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "DMG --  |  Fire rate --  |  Range --";
        text.fontSize = 19f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = new Color(0.75f, 0.95f, 1f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private void Update()
    {
        if (m_HealthManager != null)
        {
            SetFillBar(m_HealthFillBar, m_HealthManager.GetHealthPercentage(), m_HealthMaxWidth);
        }
        RefreshWeaponStats();
    }

    private void RefreshWeaponStats()
    {
        m_StatsRefreshTimer -= Time.unscaledDeltaTime;
        if (m_StatsRefreshTimer > 0f) return;
        m_StatsRefreshTimer = 0.2f;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || m_WeaponStatsText == null) return;
        if (!m_WeaponQueryCreated)
        {
            m_EntityManager = world.EntityManager;
            m_WeaponQuery = m_EntityManager.CreateEntityQuery(typeof(WeaponManager));
            m_WeaponQueryCreated = true;
        }
        if (m_WeaponQuery.CalculateEntityCount() != 1) return;
        WeaponManager gun = m_WeaponQuery.GetSingleton<WeaponManager>();
        m_WeaponStatsText.text = $"DMG {gun.DamagePerHit}  |  Fire rate {gun.ShotsPerSecond:0.00}/s  |  Range {gun.AttackRange:0.0}";
    }

    private void OnDestroy()
    {
        if (m_WeaponQueryCreated)
        {
            m_WeaponQuery.Dispose();
            m_WeaponQueryCreated = false;
        }
    }

    public void GainXPCallback(XPGainEventInfo info)
    {
        if (info.NextLevelRequiredXP == 0) return; 
        
        float percentage = (float)info.CurrentXP / info.NextLevelRequiredXP;
        if (m_LevelText != null)
            m_LevelText.text = $"Level {info.CurrentLevel}  {info.CurrentXP}/{info.NextLevelRequiredXP} XP";
        
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
