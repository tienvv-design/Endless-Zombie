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
    private RectTransform m_BottomCombatPanel;
    private Image m_BottomHealthFill;
    private Image m_BottomXPFill;
    private TMP_Text m_BottomHealthText;
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
        CreateBottomCombatHUD();
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
            m_XPFillBar.parent.gameObject.SetActive(false);
        }

        if (m_HealthFillBar != null)
        {
            m_HealthMaxWidth = m_HealthFillBar.sizeDelta.x;
            m_HealthFillBar.parent.gameObject.SetActive(false);
        }
    }

    private void CreateBottomCombatHUD()
    {
        GameObject panel = new("BottomCombatHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);
        m_BottomCombatPanel = panel.GetComponent<RectTransform>();
        m_BottomCombatPanel.anchorMin = m_BottomCombatPanel.anchorMax = new Vector2(0.5f, 0f);
        m_BottomCombatPanel.pivot = new Vector2(0.5f, 0f);
        m_BottomCombatPanel.anchoredPosition = new Vector2(0f, 24f);
        m_BottomCombatPanel.sizeDelta = new Vector2(980f, 286f);
        Image background = panel.GetComponent<Image>();
        background.sprite = LoadHUDSprite("stat_bg");
        background.type = Image.Type.Simple;
        background.color = Color.white;
        background.raycastTarget = false;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color32(36, 117, 143, 220);
        outline.effectDistance = new Vector2(3f, -3f);

        AddHUDIcon(m_BottomCombatPanel, "HealthIcon", LoadHUDSprite("icon_heart"),
            new Vector2(-443f, 26f), new Vector2(76f, 76f));
        AddHUDText(m_BottomCombatPanel, "HEALTH", 22f, new Vector2(-350f, 58f),
            new Vector2(150f, 34f), TextAlignmentOptions.Left, new Color32(239, 244, 247, 255));
        m_BottomHealthText = AddHUDText(m_BottomCombatPanel, "0 / 0", 22f, new Vector2(-82f, 58f),
            new Vector2(220f, 34f), TextAlignmentOptions.Right, Color.white);
        m_BottomHealthFill = AddHUDBar(m_BottomCombatPanel, new Vector2(-255f, 28f),
            new Vector2(330f, 24f), LoadHUDSprite("health_bg"), LoadHUDSprite("health_progress"));
        m_BottomXPFill = AddHUDBar(m_BottomCombatPanel, new Vector2(-255f, -48f),
            new Vector2(390f, 18f), LoadHUDSprite("level_bg"), LoadHUDSprite("level_progress"));

        RectTransform divider = AddHUDPanel(m_BottomCombatPanel, "Divider", new Vector2(-20f, 0f),
            new Vector2(3f, 132f), new Color32(42, 74, 91, 230));
        divider.GetComponent<Image>().raycastTarget = false;

        Sprite tile = LoadHUDSprite("attack_stats_bg");
        AddStatIcon("Damage", "icon_dmg", tile, 65f);
        AddStatIcon("Range", "icon_range", tile, 155f);
        AddStatIcon("FireRate", "icon_firerate", tile, 245f);
        AddStatIcon("CritChance", "icon_crc", tile, 335f);
        AddStatIcon("CritDamage", "icon_crd", tile, 425f);
    }

    private void AddStatIcon(string name, string spriteName, Sprite backgroundSprite, float x)
    {
        RectTransform tile = AddHUDPanel(m_BottomCombatPanel, name, new Vector2(x, 24f),
            new Vector2(76f, 68f), Color.white);
        Image tileImage = tile.GetComponent<Image>();
        tileImage.sprite = backgroundSprite;
        tileImage.type = Image.Type.Simple;
        tileImage.raycastTarget = false;
        AddHUDIcon(tile, name + "Icon", LoadHUDSprite(spriteName), Vector2.zero, new Vector2(58f, 58f));
    }

    private static Sprite LoadHUDSprite(string name) => Resources.Load<Sprite>("KickerHUD/" + name);

    private static RectTransform AddHUDPanel(RectTransform parent, string name, Vector2 position,
        Vector2 size, Color color)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        item.GetComponent<Image>().color = color;
        return rect;
    }

    private static Image AddHUDBar(RectTransform parent, Vector2 position, Vector2 size,
        Sprite backgroundSprite, Sprite fillSprite)
    {
        RectTransform background = AddHUDPanel(parent, "BarBackground", position, size,
            Color.white);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = backgroundSprite;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.raycastTarget = false;
        GameObject fillObject = new("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(background, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        Image fill = fillObject.GetComponent<Image>();
        fill.sprite = fillSprite;
        fill.color = Color.white;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;
        return fill;
    }

    private static Image AddHUDIcon(RectTransform parent, string name, Sprite sprite, Vector2 position,
        Vector2 size)
    {
        GameObject iconObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = iconObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text AddHUDText(RectTransform parent, string value, float fontSize, Vector2 position,
        Vector2 size, TextAlignmentOptions alignment, Color color)
    {
        GameObject label = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
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
        label.transform.SetParent(m_BottomCombatPanel, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-255f, -20f);
        rect.sizeDelta = new Vector2(390f, 30f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "LEVEL 0   0/0 XP";
        text.fontSize = 19f;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color32(186, 228, 255, 255);
        text.raycastTarget = false;
        return text;
    }

    private TMP_Text CreateWeaponStatsText()
    {
        GameObject label = new GameObject("WeaponRuntimeStats", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(m_BottomCombatPanel, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(245f, -45f);
        rect.sizeDelta = new Vector2(470f, 42f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.text = "--       --       --       --       --";
        text.fontSize = 19f;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
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
        if (transform.Find("SettingButton") != null) return;
        GameObject item = new GameObject("SettingButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        item.transform.SetParent(transform, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(90f, -90f);
        rect.sizeDelta = new Vector2(78f, 78f);
        Image image = item.GetComponent<Image>();
        image.sprite = LoadHUDSprite("setting_button");
        image.color = Color.white;
        image.preserveAspect = true;
        Button button = item.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(PauseGameplay);

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
            float healthPercent = m_HealthManager.GetHealthPercentage();
            SetFillBar(m_HealthFillBar, healthPercent, m_HealthMaxWidth);
            if (m_BottomHealthFill != null)
                m_BottomHealthFill.fillAmount = healthPercent;
            if (m_BottomHealthText != null)
                m_BottomHealthText.text = $"{m_HealthManager.CurrentHealth:N0} / {m_HealthManager.MaxHealth:N0}";
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
        m_WeaponStatsText.text =
            $"{gun.DamagePerHit:N0}       {gun.AttackRange:0.0}m       {gun.ShotsPerSecond:0.0}/s       " +
            $"{gun.CriticalChance * 100f:0.#}%       x{gun.CriticalDamage:0.##}";
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
            m_LevelText.text = $"LEVEL {info.CurrentLevel}   {info.CurrentXP:N0}/{info.NextLevelRequiredXP:N0} XP";
        if (m_BottomXPFill != null)
            m_BottomXPFill.fillAmount = Mathf.Clamp01(percentage);
        
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
