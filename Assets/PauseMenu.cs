using OOP.GameStates;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour, IGamePlayerPause
{
    private const string VibrationKey = "Settings.Vibration";
    private const string RuntimeMenuName = "InGameSettingsMenu";
    public Slider volumeSlider;
    private TMP_Text vibrationStateText;

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include) != null)
            return;

        Canvas pauseCanvas = null;
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (canvas.name != "PauseCanvas") continue;
            pauseCanvas = canvas;
            break;
        }

        if (pauseCanvas == null)
        {
            Debug.LogError("PauseCanvas was not found. In-game Settings cannot be displayed.");
            return;
        }

        pauseCanvas.overrideSorting = true;
        pauseCanvas.sortingOrder = 100;

        GameObject menuObject = new(RuntimeMenuName, typeof(RectTransform), typeof(PauseMenu));
        RectTransform rect = menuObject.GetComponent<RectTransform>();
        rect.SetParent(pauseCanvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        menuObject.SetActive(false);
    }

    private void Start() => BuildKickerSettingControls();
    public void OnVolumeChanged(float value) => AudioManager.Instance?.SetGlobalVolume(value);

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        RefreshVibrationState();
    }

    public void OnStateDisable() => gameObject.SetActive(false);

    private void BuildKickerSettingControls()
    {
        if (transform.Find("KickerSettingControls") != null) return;
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        RectTransform panel = CreateImage("KickerSettingControls", transform, LoadSprite("setting_panel"),
            Vector2.zero, new Vector2(720f, 520f));
        RectTransform header = CreateImage("Header", panel, LoadSprite("setting_header"),
            new Vector2(0f, 205f), new Vector2(610f, 88f));
        CreateText("SETTINGS", header, new Vector2(0f, -2f), new Vector2(460f, 62f), 36f, Color.white);
        CreateIconButton("Close", panel, LoadSprite("setting_close"), new Vector2(302f, 210f),
            new Vector2(60f, 60f), Resume);

        CreateSettingRow(panel, "MUSIC", "setting_music", 92f,
            AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.7f,
            value => AudioManager.Instance?.SetMusicVolume(value));
        CreateSettingRow(panel, "SOUND", "setting_sound", -8f,
            AudioManager.Instance != null ? AudioManager.Instance.SoundVolume : 0.7f,
            value => AudioManager.Instance?.SetSoundVolume(value));

        CreateImage("VibrationIcon", panel, LoadSprite("setting_vibration"),
            new Vector2(-265f, -108f), new Vector2(60f, 60f));
        CreateText("VIBRATION", panel, new Vector2(-155f, -108f), new Vector2(180f, 44f), 24f, Color.white);
        Button vibration = CreateSpriteButton("VibrationToggle", panel, "ON", new Vector2(165f, -108f),
            new Vector2(210f, 62f), ToggleVibration);
        vibrationStateText = vibration.GetComponentInChildren<TextMeshProUGUI>();
        RefreshVibrationState();

        CreateSpriteButton("Resume", panel, "RESUME", new Vector2(-145f, -205f),
            new Vector2(230f, 70f), Resume);
        CreateSpriteButton("MainMenu", panel, "MAIN MENU", new Vector2(145f, -205f),
            new Vector2(230f, 70f), MainMenu);
    }

    private static void CreateSettingRow(Transform parent, string label, string iconName, float y,
        float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        CreateImage(label + "Icon", parent, LoadSprite(iconName), new Vector2(-265f, y), new Vector2(60f, 60f));
        CreateText(label, parent, new Vector2(-165f, y), new Vector2(150f, 44f), 24f, Color.white);
        Slider slider = CreateSlider(parent, new Vector2(125f, y), new Vector2(365.7f, 38f));
        slider.SetValueWithoutNotify(initialValue);
        slider.onValueChanged.AddListener(onChanged);
    }

    private static Slider CreateSlider(Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform root = CreateImage("Slider", parent, null, position, size);
        root.GetComponent<Image>().color = new Color32(8, 23, 38, 235);

        // Match Resources/SettingsMenu.prefab. Slider changes the Fill anchors at
        // runtime, so both containers must be stretch-based with a zero-sized Fill;
        // a fixed Fill sizeDelta gets added to the anchor width and appears oversized.
        RectTransform fillArea = CreateRect("Fill Area", root, Vector2.zero, Vector2.zero);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.anchoredPosition = Vector2.zero;
        fillArea.sizeDelta = new Vector2(-42f, -30f);

        RectTransform fill = CreateImage("Fill", fillArea, null, new Vector2(-12f, 0f), Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.zero;
        fill.sizeDelta = Vector2.zero;
        fill.localScale = new Vector3(1f, 3.5f, 1f);
        fill.GetComponent<Image>().color = new Color32(255, 210, 49, 255);

        RectTransform handleArea = CreateRect("Handle Slide Area", root, new Vector2(-12f, 0f), Vector2.zero);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.sizeDelta = new Vector2(-42f, 0f);
        RectTransform handle = CreateImage("Handle", handleArea, LoadSprite("setting_button_bg"),
            Vector2.zero, new Vector2(42f, 54f));
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private void ToggleVibration()
    {
        PlayerPrefs.SetInt(VibrationKey, IsVibrationEnabled() ? 0 : 1);
        RefreshVibrationState();
    }

    private static bool IsVibrationEnabled() => PlayerPrefs.GetInt(VibrationKey, 1) != 0;

    private void RefreshVibrationState()
    {
        if (vibrationStateText == null) return;
        bool enabled = IsVibrationEnabled();
        vibrationStateText.text = enabled ? "ON" : "OFF";
        vibrationStateText.color = enabled ? new Color32(38, 103, 64, 255) : new Color32(120, 75, 75, 255);
    }

    private static Button CreateSpriteButton(string name, Transform parent, string label, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateImage(name, parent, LoadSprite("setting_button_bg"), position, size);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        CreateText(label, rect, new Vector2(0f, -2f), size - new Vector2(20f, 12f), 24f, new Color32(42, 63, 75, 255));
        return button;
    }

    private static Button CreateIconButton(string name, Transform parent, Sprite sprite, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateImage(name, parent, sprite, position, size);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        return button;
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Vector2 position,
        Vector2 size)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
        image.preserveAspect = false;
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject item = new(name, typeof(RectTransform));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text CreateText(string value, Transform parent, Vector2 position, Vector2 size,
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
        text.margin = Vector4.zero;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite LoadSprite(string name) => Resources.Load<Sprite>("KickerHUD/" + name);
    private static void Resume() => FindFirstObjectByType<GameStateMachineRunner>()?.ResumeGameplay();

    private static void MainMenu()
    {
        Time.timeScale = 1f;
        WaveSpawnLifecycle.ResetStage();
        SceneManager.LoadScene("GameScene");
    }
}
