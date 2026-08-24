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
    private readonly TMP_Text[] m_WeaponStatValues = new TMP_Text[4];
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
        GameplayHUDView prefab = Resources.Load<GameplayHUDView>("GameplayHUDLayout");
        if (prefab == null)
        {
            Debug.LogError("Missing Resources/GameplayHUDLayout.prefab. Gameplay HUD cannot be displayed.", this);
            enabled = false;
            return;
        }

        GameplayHUDView view = Instantiate(prefab, transform, false);
        view.name = "GameplayHUDLayout";
        BindHUD(view);

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

    private void BindHUD(GameplayHUDView view)
    {
        view.CaptureReferences();
        m_BottomCombatPanel = view.BottomCombatPanel;
        m_BottomHealthFill = view.HealthFill;
        m_BottomXPFill = view.XPFill;
        m_BottomHealthText = view.HealthText;
        m_LevelText = view.LevelText;
        m_GoldText = view.GoldText;
        m_WaveText = view.WaveText;
        m_KillProgressText = view.KillProgressText;
        m_AmmoText = view.AmmoText;
        m_AmmoIcon = view.AmmoIcon;
        for (int i = 0; i < m_WeaponStatValues.Length; i++)
            m_WeaponStatValues[i] = view.WeaponStatValues[i];

        if (view.SettingsButton != null)
        {
            view.SettingsButton.onClick.RemoveAllListeners();
            view.SettingsButton.onClick.AddListener(PauseGameplay);
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

    private static void PauseGameplay()
    {
        GameStateMachineRunner runner = FindFirstObjectByType<GameStateMachineRunner>();
        runner?.PauseGameplay();
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
        if (world == null || !world.IsCreated || m_WeaponStatValues[0] == null) return;
        if (!m_WeaponQueryCreated)
        {
            m_EntityManager = world.EntityManager;
            m_WeaponQuery = m_EntityManager.CreateEntityQuery(typeof(WeaponManager));
            m_WeaponQueryCreated = true;
        }
        if (m_WeaponQuery.CalculateEntityCount() != 1) return;
        WeaponManager gun = m_WeaponQuery.GetSingleton<WeaponManager>();
        m_WeaponStatValues[0].text = $"{gun.DamagePerHit:N0}";
        m_WeaponStatValues[1].text = $"{gun.ShotsPerSecond:0.0}/s";
        m_WeaponStatValues[2].text = $"{gun.CriticalChance * 100f:0.#}%";
        m_WeaponStatValues[3].text = $"x{gun.CriticalDamage:0.##}";
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
