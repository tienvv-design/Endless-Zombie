using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string VibrationKey = "Settings.Vibration";
    public Slider volumeSlider;
    private TMP_Text vibrationText;
    private bool built;

    public static void EnsureExists(Transform parent)
    {
        if (FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include) != null) return;
        GameObject item = new("SettingsMenu", typeof(RectTransform));
        item.SetActive(false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        item.AddComponent<SettingsMenu>();
    }

    private void OnEnable()
    {
        if (!built) Build();
        RefreshVibration();
    }

    public void Show()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }
    public void OnVolumeChanged(float value) => AudioManager.Instance?.SetGlobalVolume(value);

    private void Build()
    {
        built = true;
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        RectTransform panel = ImageRect("KickerMainSetting", transform, Sprite("setting_panel"),
            Vector2.zero, new Vector2(720f, 520f));
        RectTransform header = ImageRect("Header", panel, Sprite("setting_header"),
            new Vector2(0f, 205f), new Vector2(610f, 88f));
        Text("SETTINGS", header, new Vector2(0f, -2f), new Vector2(460f, 62f), 36f, Color.white);
        IconButton("Close", panel, Sprite("setting_close"), new Vector2(302f, 210f),
            new Vector2(60f, 60f), Close);

        Row(panel, "MUSIC", "setting_music", 92f,
            AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.7f,
            value => AudioManager.Instance?.SetMusicVolume(value));
        Row(panel, "SOUND", "setting_sound", -8f,
            AudioManager.Instance != null ? AudioManager.Instance.SoundVolume : 0.7f,
            value => AudioManager.Instance?.SetSoundVolume(value));

        ImageRect("VibrationIcon", panel, Sprite("setting_vibration"), new Vector2(-265f, -108f),
            new Vector2(60f, 60f));
        Text("VIBRATION", panel, new Vector2(-155f, -108f), new Vector2(180f, 44f), 24f, Color.white);
        Button vibration = TextButton("Vibration", panel, "ON", new Vector2(165f, -108f),
            new Vector2(210f, 62f), ToggleVibration);
        vibrationText = vibration.GetComponentInChildren<TextMeshProUGUI>();
        TextButton("CLOSE", panel, "CLOSE", new Vector2(0f, -205f), new Vector2(250f, 70f), Close);
    }

    private static void Row(Transform parent, string label, string icon, float y, float value,
        UnityEngine.Events.UnityAction<float> changed)
    {
        ImageRect(label + "Icon", parent, Sprite(icon), new Vector2(-265f, y), new Vector2(60f, 60f));
        Text(label, parent, new Vector2(-165f, y), new Vector2(150f, 44f), 24f, Color.white);
        Slider slider = SliderControl(parent, new Vector2(125f, y), new Vector2(310f, 38f));
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(changed);
    }

    private static Slider SliderControl(Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform root = ImageRect("Slider", parent, null, position, size);
        root.GetComponent<Image>().color = new Color32(8, 23, 38, 235);
        RectTransform fillArea = Rect("Fill Area", root, Vector2.zero, size - new Vector2(10f, 10f));
        RectTransform fill = ImageRect("Fill", fillArea, null, Vector2.zero, fillArea.sizeDelta);
        fill.GetComponent<Image>().color = new Color32(255, 210, 49, 255);
        RectTransform handleArea = Rect("Handle Slide Area", root, Vector2.zero, size);
        RectTransform handle = ImageRect("Handle", handleArea, Sprite("setting_button_bg"), Vector2.zero,
            new Vector2(42f, 54f));
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private void ToggleVibration()
    {
        PlayerPrefs.SetInt(VibrationKey, PlayerPrefs.GetInt(VibrationKey, 1) == 0 ? 1 : 0);
        RefreshVibration();
    }

    private void RefreshVibration()
    {
        if (vibrationText == null) return;
        bool enabled = PlayerPrefs.GetInt(VibrationKey, 1) != 0;
        vibrationText.text = enabled ? "ON" : "OFF";
        vibrationText.color = enabled ? new Color32(38, 103, 64, 255) : new Color32(120, 75, 75, 255);
    }

    private void Close() => gameObject.SetActive(false);

    private static Button TextButton(string name, Transform parent, string label, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = ImageRect(name, parent, Sprite("setting_button_bg"), position, size);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        Text(label, rect, new Vector2(0f, -2f), size - new Vector2(16f, 10f), 24f, new Color32(42, 63, 75, 255));
        return button;
    }

    private static Button IconButton(string name, Transform parent, Sprite sprite, Vector2 position,
        Vector2 size, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = ImageRect(name, parent, sprite, position, size);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        return button;
    }

    private static RectTransform ImageRect(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
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

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject item = new(name, typeof(RectTransform));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text Text(string value, Transform parent, Vector2 position, Vector2 size, float fontSize, Color color)
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

    private static Sprite Sprite(string name) => Resources.Load<Sprite>("KickerHUD/" + name);
}
