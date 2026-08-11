using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WinMenu : MonoBehaviour, IGameWin
{
    private CanvasGroup m_Group;
    private TMP_Text m_GoldText;

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<WinMenu>(FindObjectsInactive.Include) != null)
            return;

        GameObject root = new("Win Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(WinMenu));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(786f, 1402f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.SetActive(false);
    }

    private void Awake()
    {
        m_Group = GetComponent<CanvasGroup>();
        Build();
    }

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        if (m_GoldText != null)
            m_GoldText.text = $"GOLD EARNED  +{(GoldWallet.Instance ? GoldWallet.Instance.LastBankedReward : 0):N0}";
        m_Group.alpha = 1f;
        m_Group.interactable = true;
        m_Group.blocksRaycasts = true;
    }

    public void OnStateDisable()
    {
        gameObject.SetActive(false);
    }

    private void Build()
    {
        RectTransform root = transform as RectTransform;
        AddPanel(root, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0.015f, 0.025f, 0.07f, 0.78f));
        RectTransform panel = AddPanel(root, "Victory Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 30f), new Vector2(650f, 610f), Hex("17213F"));
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = Hex("16E96C");
        outline.effectDistance = new Vector2(6f, -6f);

        AddText(panel, "VICTORY!", 70f, new Vector2(0.5f, 1f), new Vector2(0f, -105f),
            new Vector2(560f, 100f), Hex("FFD628"));
        AddText(panel, "STAGE CLEARED", 32f, new Vector2(0.5f, 1f), new Vector2(0f, -185f),
            new Vector2(500f, 55f), Color.white);
        m_GoldText = AddText(panel, "GOLD EARNED  +0", 30f, new Vector2(0.5f, 0.5f), new Vector2(0f, 25f),
            new Vector2(520f, 60f), Hex("FFD628"));

        Button mainMenu = AddButton(panel, "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(0f, 100f),
            new Vector2(390f, 92f), Hex("168EF5"));
        mainMenu.onClick.AddListener(ReturnToMainMenu);
    }

    private static void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
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

    private static RectTransform AddPanel(RectTransform parent, string name, Vector2 min, Vector2 max,
        Vector2 position, Vector2 size, Color color)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        item.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text AddText(RectTransform parent, string value, float fontSize, Vector2 anchor,
        Vector2 position, Vector2 size, Color color)
    {
        GameObject item = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
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

    private static Button AddButton(RectTransform parent, string label, Vector2 anchor, Vector2 position,
        Vector2 size, Color color)
    {
        RectTransform rect = AddPanel(parent, label, anchor, anchor, position, size, color);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        AddText(rect, label, 31f, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(20f, 12f), Color.white);
        return button;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }
}
