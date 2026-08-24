using OOP.GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour, IGameOver
{
    public const string RetryRunKey = "EndGame.RetryRun";
    private TMP_Text progressText;
    private TMP_Text goldText;
    private Image progressFill;
    private RectTransform progressMarker;
    private bool built;

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        if (!built) Build();
        int percent = StageProgressView.GetPercent();
        progressText.text = $"{percent}%";
        progressFill.fillAmount = percent / 100f;
        progressMarker.anchoredPosition = new Vector2(Mathf.Lerp(-302f, 302f, percent / 100f), 31.6f);
        goldText.text = $"{(GoldWallet.Instance ? GoldWallet.Instance.LastBankedReward : 0):N0}";
    }

    public void OnStateDisable() => gameObject.SetActive(false);

    private void Build()
    {
        built = true;
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        RectTransform dim = StretchPanel("Kicker LosegameLayer", transform, new Color(0f, 0f, 0f, .902f));
        RectTransform losePanel = Stretch("LosePanel", dim);
        RectTransform header = ImageRect("Header", losePanel, new Vector2(0f, 512f), new Vector2(1220f, 896f),
            KickerEndGameTheme.LosegameHeader);
        Text("LEVEL FAIL!", header, new Vector2(0f, 7f), new Vector2(319.2578f, 76.7302f), 66.2f);

        RectTransform progress = ImageRect("Progess", losePanel, new Vector2(14f, 209f), new Vector2(624f, 28f),
            KickerEndGameTheme.LosegameBar);
        RectTransform fill = ImageRect("ProgessSlider", progress, Vector2.zero, new Vector2(-8f, -8f),
            KickerEndGameTheme.LosegameBarFill);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        progressFill = fill.GetComponent<Image>();
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressMarker = ImageRect("IconBoss", progress, new Vector2(302.889f, 31.589f), new Vector2(85f, 85f),
            KickerEndGameTheme.LosegameBoss);
        progressText = Text("0%", progress, new Vector2(0f, 48f), new Vector2(110f, 96f), 36f);

        ImageRect("Collect", losePanel, new Vector2(0f, 54f), new Vector2(600f, 55f),
            KickerEndGameTheme.LosegameCollected);
        ImageRect("Gold Icon", losePanel, new Vector2(-115f, -110f), new Vector2(72f, 72f),
            KickerEndGameTheme.Gold).GetComponent<Image>().preserveAspect = true;
        goldText = Text("0", losePanel, new Vector2(45f, -110f), new Vector2(260f, 110f), 42f);

        RectTransform buttonGroup = Rect("buttonGroup", dim, new Vector2(0f, -542f), new Vector2(723.2f, 137.2f));
        RectTransform homeRect = ImageRect("Claim", buttonGroup, Vector2.zero, new Vector2(412f, 156f),
            KickerEndGameTheme.LosegameHome);
        homeRect.GetComponent<Image>().raycastTarget = true;
        Button home = homeRect.gameObject.AddComponent<Button>();
        home.targetGraphic = homeRect.GetComponent<Image>();
        home.onClick.AddListener(Home);
        Text("HOME", homeRect, new Vector2(0f, 4f), new Vector2(412f, 148f), 56f);
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
