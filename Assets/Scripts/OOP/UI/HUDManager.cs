using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Entities;
using OOP.GameStates;

public class HUDManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform m_XPFillBar;
    [SerializeField] private RectTransform m_HealthFillBar;
    [SerializeField] private TMP_Text m_GoldText;
    [Header("Weapon Ammo HUD")]
    [SerializeField] private TMP_Text m_AmmoText;
    [SerializeField] private Image m_AmmoIcon;
    private TMP_Text m_LevelText;
    private TMP_Text m_WeaponStatsText;
    private TMP_Text m_WaveText;
    private TMP_Text m_KillProgressText;
    private EntityQuery m_WeaponQuery;
    private EntityQuery m_StageQuery;
    private EntityQuery m_MetricsQuery;
    private EntityManager m_EntityManager;
    private bool m_WeaponQueryCreated;
    private bool m_StageQueryCreated;
    private bool m_MetricsQueryCreated;
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
        m_WaveText = CreateWaveText();
        m_KillProgressText = CreateKillProgressText();
        CreatePauseButton();
        if (m_AmmoText == null)
            CreateAmmoDisplay();

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

    private TMP_Text CreateWaveText()
    {
        GameObject label = new GameObject("WaveStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(520f, 44f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "Wave --";
        text.fontSize = 24f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Top;
        text.color = new Color(1f, 0.9f, 0.55f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text CreateKillProgressText()
    {
        GameObject label = new GameObject("KillProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -66f);
        rect.sizeDelta = new Vector2(360f, 36f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "KILLS  0 / 0";
        text.fontSize = 20f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Top;
        text.color = new Color32(238, 238, 238, 255);
        text.raycastTarget = false;
        return text;
    }

    private void CreatePauseButton()
    {
        if (transform.Find("PauseButton") != null) return;
        GameObject item = new GameObject("PauseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        item.transform.SetParent(transform, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -82f);
        rect.sizeDelta = new Vector2(72f, 72f);
        Image image = item.GetComponent<Image>();
        image.color = new Color32(27, 40, 72, 235);
        Button button = item.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(PauseGameplay);

        GameObject label = new GameObject("PauseIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(item.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "II";
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void PauseGameplay()
    {
        GameStateMachineRunner runner = FindFirstObjectByType<GameStateMachineRunner>();
        runner?.PauseGameplay();
    }

    private void CreateAmmoDisplay()
    {
        GameObject panel = new GameObject("AmmoDisplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-28f, 78f);
        panelRect.sizeDelta = new Vector2(250f, 70f);
        Image background = panel.GetComponent<Image>();
        background.color = new Color32(25, 28, 24, 220);
        background.raycastTarget = false;

        GameObject icon = new GameObject("AmmoIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icon.transform.SetParent(panel.transform, false);
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(12f, 0f);
        iconRect.sizeDelta = new Vector2(46f, 46f);
        m_AmmoIcon = icon.GetComponent<Image>();
        m_AmmoIcon.color = new Color32(214, 179, 82, 255);
        m_AmmoIcon.preserveAspect = true;
        m_AmmoIcon.raycastTarget = false;
        m_AmmoIcon.enabled = m_AmmoIcon.sprite != null;

        GameObject label = new GameObject("AmmoCount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(panel.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(66f, 5f);
        labelRect.offsetMax = new Vector2(-10f, -5f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "AMMO  -- / --";
        text.fontSize = 25f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(236, 224, 190, 255);
        text.raycastTarget = false;
        m_AmmoText = text;
    }

    private void Update()
    {
        if (m_HealthManager != null)
        {
            SetFillBar(m_HealthFillBar, m_HealthManager.GetHealthPercentage(), m_HealthMaxWidth);
        }
        RefreshWeaponStats();
        RefreshWaveStatus();
    }

    private void RefreshWaveStatus()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated || m_WaveText == null) return;
        if (!m_StageQueryCreated)
        {
            m_StageQuery = world.EntityManager.CreateEntityQuery(typeof(StageRuntime));
            m_StageQueryCreated = true;
        }
        if (!m_MetricsQueryCreated)
        {
            m_MetricsQuery = world.EntityManager.CreateEntityQuery(typeof(CombatMetrics));
            m_MetricsQueryCreated = true;
        }
        if (m_StageQuery.CalculateEntityCount() != 1) return;
        Entity stageEntity = m_StageQuery.GetSingletonEntity();
        EntityManager manager = world.EntityManager;
        StageRuntime stage = manager.GetComponentData<StageRuntime>(stageEntity);
        DynamicBuffer<WaveRuntime> waves = manager.GetBuffer<WaveRuntime>(stageEntity);
        DynamicBuffer<SpawnRequest> queue = manager.GetBuffer<SpawnRequest>(stageEntity);
        int alive = 0;
        int kills = 0;
        if (m_MetricsQuery.CalculateEntityCount() == 1)
        {
            CombatMetrics metrics = m_MetricsQuery.GetSingleton<CombatMetrics>();
            alive = metrics.ActiveEnemies;
            kills = metrics.KillCount;
        }
        DynamicBuffer<SpawnEntryRuntime> entries = manager.GetBuffer<SpawnEntryRuntime>(stageEntity);
        int totalEnemies = 0;
        for (int i = 0; i < entries.Length; i++)
            totalEnemies += entries[i].Quantity;
        int displayWave = stage.CurrentWaveIndex >= 0 ? stage.CurrentWaveIndex + 1 : 0;
        string waveState = stage.CurrentWaveIndex >= 0 && stage.CurrentWaveIndex < waves.Length
            ? waves[stage.CurrentWaveIndex].State.ToString()
            : stage.State.ToString();
        m_WaveText.text = $"Wave {displayWave}/{waves.Length}  |  {waveState}  |  Alive {alive}  |  Queue {queue.Length}";
        if (m_KillProgressText != null)
            m_KillProgressText.text = $"KILLS  {kills} / {totalEnemies}";
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
        if (m_AmmoText != null)
        {
            if (gun.IsReloading)
            {
                m_AmmoText.text = $"<color=#D6B352>RELOADING</color>\n{gun.AmmoInMagazine} / {gun.MagazineSize}  {gun.ReloadTimer:0.0}s";
            }
            else
            {
                m_AmmoText.text = $"AMMO  {gun.AmmoInMagazine} / {gun.MagazineSize}";
            }
        }
    }

    private void OnDestroy()
    {
        if (m_WeaponQueryCreated)
        {
            m_WeaponQuery.Dispose();
            m_WeaponQueryCreated = false;
        }
        if (m_StageQueryCreated)
        {
            m_StageQuery.Dispose();
            m_StageQueryCreated = false;
        }
        if (m_MetricsQueryCreated)
        {
            m_MetricsQuery.Dispose();
            m_MetricsQueryCreated = false;
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
