using OOP.GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour, IGameOver
{
    public const string RetryRunKey = "EndGame.RetryRun";
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Image progressFill;
    [SerializeField] private RectTransform progressMarker;
    private bool built;
    private GameOverUISettings settings;

    public static void EnsureExists()
    {
        GameObject prefab = Resources.Load<GameObject>("GameOverMenu");
        if (prefab != null)
        {
            // The prefab is the source of truth. Discard stale runtime copies so edits made
            // in Prefab Mode are reflected on the very next Play session.
            foreach (GameOverMenu existing in FindObjectsByType<GameOverMenu>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Destroy(existing.gameObject);
            GameObject instance = Instantiate(prefab);
            instance.name = "Game Over Menu";
            instance.SetActive(false);
            return;
        }

        if (FindFirstObjectByType<GameOverMenu>(FindObjectsInactive.Include) != null) return;

        GameObject root = new("Game Over Menu", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameOverMenu));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.localScale = Vector3.one;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        GameOverUISettings layout = Resources.Load<GameOverUISettings>("GameOverUISettings");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = layout != null ? layout.SortingOrder : 160;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = layout != null ? layout.ReferenceResolution : new Vector2(1080f, 1920f);
        rootRect.sizeDelta = scaler.referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = layout != null ? layout.MatchWidthOrHeight : 0.5f;

        root.SetActive(false);
    }

    private void Awake()
    {
        EnsureSettings();
        ResolvePrefabReferences();
        built = progressText != null && goldText != null && progressFill != null && progressMarker != null;
        // Preserve the prefab hierarchy even when the user removes or reorganizes an
        // optional display element. Only build when there is no prefab UI at all.
        if (!built && transform.childCount == 0) Build();
        BindHomeButton();
    }

#if UNITY_EDITOR
    public void BuildEditorPreview()
    {
        built = false;
        Build();
        BindHomeButton();
    }
#endif

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        EnsureSettings();
        if (!built && transform.childCount == 0) Build();
        int percent = StageProgressView.GetPercent();
        if (progressText != null) progressText.text = $"{percent}%";
        if (progressFill != null) progressFill.fillAmount = percent / 100f;
        if (progressMarker != null)
            progressMarker.anchoredPosition = new Vector2(
                Mathf.Lerp(settings.MarkerStartEnd.x, settings.MarkerStartEnd.y, percent / 100f),
                progressMarker.anchoredPosition.y);
        GoldWallet wallet = GoldWallet.Instance;
        int earnedGold = wallet != null ? wallet.LastBankedReward : 0;
        if (goldText != null) goldText.text = $"+{earnedGold:N0}";
    }

    public void OnStateDisable() => gameObject.SetActive(false);

    private void Build()
    {
        built = true;
        EnsureSettings();
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        RectTransform dim = StretchPanel("Kicker LosegameLayer", transform, settings.BackdropColor);
        RectTransform losePanel = Stretch("LosePanel", dim);
        Sprite zombiePanelSprite = KickerEndGameTheme.LosegameZombiePanel;
        if (zombiePanelSprite != null)
        {
            Image panelImage = ImageRect("Zombie Lose Panel", losePanel, settings.PanelPosition, settings.PanelSize,
                zombiePanelSprite).GetComponent<Image>();
            panelImage.preserveAspect = true;
        }
        else
        {
            ImageRect("Header", losePanel, new Vector2(0f, 512f), new Vector2(1220f, 896f),
                KickerEndGameTheme.LosegameHeader);
        }

        TMP_Text failTitle = Text("LEVEL FAIL!", losePanel, settings.TitlePosition,
            settings.TitleSize, settings.TitleFontSize);
        failTitle.color = new Color32(225, 223, 190, 255);
        failTitle.outlineColor = new Color32(69, 9, 7, 255);
        failTitle.outlineWidth = 0.24f;

        RectTransform progress = ImageRect("Progess", losePanel, settings.ProgressPosition, settings.ProgressSize,
            KickerEndGameTheme.LosegameBar);
        progress.GetComponent<Image>().color = new Color32(70, 82, 67, 255);
        RectTransform fill = ImageRect("ProgessSlider", progress, Vector2.zero, new Vector2(-8f, -8f),
            KickerEndGameTheme.LosegameBarFill);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        progressFill = fill.GetComponent<Image>();
        progressFill.color = new Color32(164, 36, 28, 255);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressMarker = ImageRect("IconBoss", progress, settings.MarkerPosition, settings.MarkerSize,
            KickerEndGameTheme.LosegameBoss);
        progressText = Text("0%", progress, settings.ProgressTextPosition, new Vector2(110f, 96f), 36f);

        Image collectImage = ImageRect("Collect", losePanel, settings.CollectedPosition, settings.CollectedSize,
            KickerEndGameTheme.LosegameCollected).GetComponent<Image>();
        collectImage.color = new Color32(173, 186, 142, 255);
        ImageRect("Gold Icon", losePanel, settings.GoldIconPosition, settings.GoldIconSize,
            KickerEndGameTheme.Gold).GetComponent<Image>().preserveAspect = true;
        goldText = Text("+0", losePanel, settings.GoldTextPosition,
            settings.GoldTextSize, 42f);

        RectTransform buttonGroup = Rect("buttonGroup", dim, settings.ButtonGroupPosition, settings.ButtonGroupSize);
        RectTransform homeRect = ImageRect("Claim", buttonGroup, Vector2.zero, settings.HomeButtonSize,
            KickerEndGameTheme.LosegameHome);
        homeRect.GetComponent<Image>().color = new Color32(104, 124, 72, 255);
        homeRect.GetComponent<Image>().raycastTarget = true;
        Button home = homeRect.gameObject.AddComponent<Button>();
        home.targetGraphic = homeRect.GetComponent<Image>();
        home.onClick.AddListener(Home);
        Text("HOME", homeRect, new Vector2(0f, 4f), new Vector2(412f, 148f), 56f);
    }

    private void EnsureSettings()
    {
        if (settings != null) return;
        settings = Resources.Load<GameOverUISettings>("GameOverUISettings");
        if (settings == null)
            settings = ScriptableObject.CreateInstance<GameOverUISettings>();
    }

    private void BindHomeButton()
    {
        Transform claim = transform.Find("Kicker LosegameLayer/buttonGroup/Claim");
        if (claim == null || !claim.TryGetComponent(out Button home)) return;
        home.onClick.RemoveListener(Home);
        home.onClick.AddListener(Home);
    }

    private void ResolvePrefabReferences()
    {
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (progressFill == null && image.name == "ProgessSlider") progressFill = image;
            if (progressMarker == null && image.name == "IconBoss")
                progressMarker = image.rectTransform;
        }
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (progressText == null && (text.name.Contains("%") || text.text.Contains("%")))
                progressText = text;
            if (goldText == null &&
                (text.name.Contains("Gold Summary") || text.name.Contains("EARNED") ||
                 text.text.Contains("EARNED") || text.text.Contains("TOTAL")))
                goldText = text;
        }
    }

    private static void Home()
    {
        PlayerPrefs.DeleteKey(RetryRunKey);
        Time.timeScale = 1f;
        WaveSpawnLifecycle.ResetStage();
        SceneManager.LoadScene("GameScene");
    }

    private static RectTransform StretchPanel(string name, Transform parent, Color color)
    {
        RectTransform rect = Stretch(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static RectTransform Stretch(string name, Transform parent)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        return rect;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject item = new(name, typeof(RectTransform));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform ImageRect(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text Text(string value, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject item = new(value, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        KickerUITheme.Apply(text);
        text.text = value;
        text.fontSize = fontSize;
        text.fontSizeMin = 18f;
        text.fontSizeMax = fontSize;
        text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
