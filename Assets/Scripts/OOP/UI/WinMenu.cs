using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WinMenu : MonoBehaviour, IGameWin
{
    private CanvasGroup group;
    private TMP_Text goldText;
    private TMP_Text stageProgressText;

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<WinMenu>(FindObjectsInactive.Include) != null) return;
        GameObject root = new("Win Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(WinMenu));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.SetActive(false);
    }

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        Build();
    }

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        goldText.text = $"GOLD EARNED   +{(GoldWallet.Instance ? GoldWallet.Instance.LastBankedReward : 0):N0}";
        StageProgressView.Refresh(stageProgressText, true);
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    public void OnStateDisable() => gameObject.SetActive(false);

    private void Build()
    {
        RectTransform canvasRoot = transform as RectTransform;
        Panel("Backdrop", canvasRoot, Vector2.zero, new Vector2(1920f, 1080f),
            new Color(0.005f, 0.015f, 0.025f, 0.84f), null);
        RectTransform root = Panel("KickerVictoryPopup", canvasRoot, Vector2.zero, new Vector2(760f, 850f),
            new Color32(8, 28, 43, 250), null);

        Image flare = Panel("Flare", root, new Vector2(0f, 210f), new Vector2(760f, 760f), Color.white,
            KickerEndGameTheme.Flare).GetComponent<Image>();
        flare.raycastTarget = false;
        Panel("LeftHorn", root, new Vector2(-238f, 277f), new Vector2(210f, 158f), Color.white,
            KickerEndGameTheme.Win("Win_Atlas_2"));
        Panel("RightHorn", root, new Vector2(238f, 277f), new Vector2(210f, 158f), Color.white,
            KickerEndGameTheme.Win("Win_Atlas_3"));
        Panel("VictoryBadge", root, new Vector2(0f, 280f), new Vector2(280f, 272f), Color.white,
            KickerEndGameTheme.Win("Win_Atlas_5"));
        Text("VICTORY", root, new Vector2(0f, 288f), new Vector2(440f, 90f), 50f, Color.white);

        Panel("Flag", root, new Vector2(-120f, 85f), new Vector2(78f, 78f), Color.white,
            KickerEndGameTheme.Flag);
        Text("STAGE CLEARED", root, new Vector2(40f, 85f), new Vector2(360f, 58f), 32f,
            new Color32(205, 239, 255, 255));
        stageProgressText = Text("STAGE PROGRESS  100%", root, new Vector2(0f, 5f),
            new Vector2(520f, 52f), 29f, new Color32(180, 225, 248, 255));
        goldText = Text("GOLD EARNED   +0", root, new Vector2(0f, -70f),
            new Vector2(540f, 58f), 31f, new Color32(255, 211, 50, 255));

        Button claim = Button("ContinueBtn", root, "CLAIM & CONTINUE", new Vector2(0f, -220f),
            new Vector2(350f, 95f), KickerEndGameTheme.UI("UI_Maincenter_Button4"));
        claim.onClick.AddListener(ReturnToMainMenu);
    }

    private static void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerPrefs.DeleteKey(GameOverMenu.RetryRunKey);
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            world.QuitUpdate = false;
            SimulationSystemGroup simulation = world.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simulation != null) simulation.Enabled = true;
        }
        WaveSpawnLifecycle.ResetStage();
        SceneManager.LoadScene("GameScene");
    }

    private static Button Button(string name, Transform parent, string label, Vector2 position,
        Vector2 size, Sprite sprite)
    {
        RectTransform rect = Panel(name, parent, position, size, Color.white, sprite);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        Text(label, rect, new Vector2(0f, -3f), size - new Vector2(28f, 16f), 29f, Color.white);
        return button;
    }

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
        image.raycastTarget = name is not "Flare";
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
}
