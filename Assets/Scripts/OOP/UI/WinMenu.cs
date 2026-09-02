using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WinMenu : MonoBehaviour, IGameWin
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text stageProgressText;
    [SerializeField] private Button continueButton;
    private CanvasGroup group;
    private bool built;

    public static void EnsureExists()
    {
        GameObject prefab = Resources.Load<GameObject>("WinMenu");
        if (prefab != null)
        {
            // Always recreate from the asset. This also handles Unity's fast Enter Play Mode,
            // where a runtime-created copy from the previous play session may still exist.
            foreach (WinMenu existing in FindObjectsByType<WinMenu>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Destroy(existing.gameObject);
            GameObject instance = Instantiate(prefab);
            instance.name = "Win Menu";
            instance.SetActive(false);
            return;
        }
        if (FindFirstObjectByType<WinMenu>(FindObjectsInactive.Include) != null) return;
        CreateCanvasRoot().SetActive(false);
    }

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        ResolvePrefabReferences();
        built = goldText != null && stageProgressText != null && continueButton != null;
        // Never replace an edited prefab hierarchy at runtime. Build is only a fallback
        // for the code-created canvas when the prefab asset is genuinely unavailable.
        if (!built && transform.childCount == 0) Build();
        BindContinueButton();
    }

#if UNITY_EDITOR
    public void BuildEditorPreview()
    {
        built = false;
        Build();
    }
#endif

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        if (!built && transform.childCount == 0) Build();
        GoldWallet wallet = GoldWallet.Instance;
        int earnedGold = wallet != null ? wallet.LastBankedReward : 0;
        if (goldText != null) goldText.text = $"GOLD EARNED  +{earnedGold:N0}";
        if (stageProgressText != null) StageProgressView.Refresh(stageProgressText, true);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    public void OnStateDisable() => gameObject.SetActive(false);

    private void Build()
    {
        built = true;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(transform.GetChild(i).gameObject);
            else Destroy(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }

        RectTransform canvasRoot = transform as RectTransform;
        RectTransform backdrop = StretchPanel("Zombie Backdrop", canvasRoot, new Color32(3, 8, 7, 238));
        RectTransform panel = Panel("Survivor Panel", backdrop, new Vector2(0f, 5f),
            new Vector2(960f, 1580f), new Color32(21, 30, 23, 252), null);
        AddOutline(panel.gameObject, new Color32(111, 124, 73, 220), new Vector2(5f, -5f));

        Image zombieArt = Panel("Zombie Artwork", panel, new Vector2(0f, 340f),
            new Vector2(930f, 930f), new Color32(115, 129, 91, 205),
            KickerEndGameTheme.LosegameZombiePanel).GetComponent<Image>();
        zombieArt.preserveAspect = true;
        zombieArt.raycastTarget = false;
        Image flare = Panel("Toxic Flare", panel, new Vector2(0f, 390f),
            new Vector2(900f, 900f), new Color32(118, 147, 60, 105),
            KickerEndGameTheme.Flare).GetComponent<Image>();
        flare.raycastTarget = false;

        RectTransform titlePlate = Panel("Title Plate", panel, new Vector2(0f, 585f),
            new Vector2(760f, 138f), new Color32(48, 57, 35, 245),
            KickerEndGameTheme.UI("UI_Maincenter_Button4"));
        AddOutline(titlePlate.gameObject, new Color32(117, 25, 18, 255), new Vector2(4f, -4f));
        TMP_Text title = Text("SURVIVED", titlePlate, new Vector2(0f, 5f),
            new Vector2(700f, 105f), 72f, new Color32(230, 226, 187, 255));
        title.outlineColor = new Color32(63, 13, 9, 255);
        title.outlineWidth = 0.25f;
        Text("AREA SECURED", panel, new Vector2(0f, 430f), new Vector2(620f, 62f), 34f,
            new Color32(213, 220, 172, 255));

        RectTransform report = Panel("Run Report", panel, new Vector2(0f, -235f),
            new Vector2(820f, 340f), new Color32(10, 17, 13, 238), null);
        AddOutline(report.gameObject, new Color32(91, 103, 65, 210), new Vector2(3f, -3f));
        Panel("Warning Stripe", report, new Vector2(0f, 154f), new Vector2(820f, 12f),
            new Color32(134, 42, 26, 255), null);
        stageProgressText = Text("STAGE PROGRESS  100%", report, new Vector2(0f, 82f),
            new Vector2(700f, 58f), 32f, new Color32(203, 216, 165, 255));
        ImageRect("Gold Icon", report, new Vector2(-245f, -50f), new Vector2(96f, 96f),
            KickerEndGameTheme.Gold).GetComponent<Image>().preserveAspect = true;
        goldText = Text("GOLD EARNED  +0", report, new Vector2(70f, -54f),
            new Vector2(520f, 118f), 34f, new Color32(239, 196, 76, 255));

        continueButton = Button("Continue Button", panel, "CONTINUE TO NEXT ZONE",
            new Vector2(0f, -650f), new Vector2(620f, 124f),
            KickerEndGameTheme.UI("UI_Maincenter_Button4"));
        continueButton.targetGraphic.color = new Color32(101, 116, 62, 255);
        BindContinueButton();
    }

    private void BindContinueButton()
    {
        if (continueButton == null) return;
        // The editable prefab may use child artwork and therefore have no Graphic on
        // the Button object itself. GraphicRaycaster cannot hit such a Button, so add
        // an invisible hit target without changing the prefab's appearance.
        if (continueButton.targetGraphic == null)
        {
            Image hitTarget = continueButton.GetComponent<Image>();
            if (hitTarget == null) hitTarget = continueButton.gameObject.AddComponent<Image>();
            hitTarget.color = new Color(1f, 1f, 1f, 0.001f);
            hitTarget.raycastTarget = true;
            continueButton.targetGraphic = hitTarget;
        }
        else
        {
            continueButton.targetGraphic.raycastTarget = true;
        }
        continueButton.onClick.RemoveListener(ContinueToNextStage);
        continueButton.onClick.AddListener(ContinueToNextStage);
    }

    private void ResolvePrefabReferences()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (stageProgressText == null &&
                (text.name.Contains("STAGE PROGRESS") || text.text.Contains("STAGE PROGRESS")))
                stageProgressText = text;
            if (goldText == null &&
                (text.name.Contains("SALVAGED") || text.name.Contains("GOLD EARNED") ||
                 text.text.Contains("SALVAGED") || text.text.Contains("GOLD EARNED")))
                goldText = text;
        }
        if (continueButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0) continueButton = buttons[0];
        }
    }

    public static GameObject CreateCanvasRoot()
    {
        GameObject root = new("Win Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(WinMenu));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1080f, 1920f);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 155;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static void ContinueToNextStage()
    {
        AudioManager.Instance?.Play(SoundLabel.PickupGoldSound);
        Time.timeScale = 1f;
        int nextStage = StageMapProgression.AdvanceAfterWin();
        // Continue returns to the main menu for the newly unlocked stage. RetryRun
        // would skip that menu and immediately begin combat, so explicitly clear it.
        PlayerPrefs.DeleteKey(GameOverMenu.RetryRunKey);
        PlayerPrefs.Save();
        Debug.Log($"Win continue: returning to menu for Stage {nextStage}.");
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            world.QuitUpdate = false;
            SimulationSystemGroup simulation = world.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulation != null) simulation.Enabled = true;
        }
        WaveSpawnLifecycle.ResetStage();
        SceneManager.LoadScene("LoadingScreen");
    }

    private static Button Button(string name, Transform parent, string label, Vector2 position,
        Vector2 size, Sprite sprite)
    {
        RectTransform rect = Panel(name, parent, position, size, Color.white, sprite);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.targetGraphic.raycastTarget = true;
        Text(label, rect, Vector2.zero, size - new Vector2(36f, 20f), 31f, Color.white);
        return button;
    }

    private static RectTransform StretchPanel(string name, Transform parent, Color color)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        item.GetComponent<Image>().color = color;
        return rect;
    }

    private static RectTransform ImageRect(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite)
        => Panel(name, parent, position, size, Color.white, sprite);

    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size,
        Color color, Sprite sprite)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = sprite != null;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text Text(string value, Transform parent, Vector2 position, Vector2 size,
        float fontSize, Color color)
    {
        GameObject item = new(value, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        KickerUITheme.Apply(text);
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }
}
