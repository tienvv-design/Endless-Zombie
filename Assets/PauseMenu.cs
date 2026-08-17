using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using OOP.GameStates;

public class PauseMenu : MonoBehaviour, IGamePlayerPause
{
    public Slider volumeSlider;

    void Start()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        BuildBasicControls();
    }

    public void OnVolumeChanged(float value)
    {
        AudioManager.Instance.SetGlobalVolume(value);
    }
    
    public void OnStateEnable()
    {
        gameObject.SetActive(true);
    }

    public void OnStateDisable()
    {
        gameObject.SetActive(false);
    }

    private void BuildBasicControls()
    {
        if (transform.Find("KickerStyleControls") != null) return;

        RectTransform panel = CreatePanel("KickerStyleControls", transform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(520f, 430f), new Color32(23, 33, 63, 245));
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(255, 205, 35, 255);
        outline.effectDistance = new Vector2(5f, -5f);

        CreateText("PAUSED", panel, new Vector2(0f, 135f), new Vector2(440f, 80f), 52f,
            new Color32(255, 214, 40, 255));
        CreateButton("RESUME", panel, new Vector2(0f, 30f), new Color32(31, 190, 105, 255), Resume);
        CreateButton("MAIN MENU", panel, new Vector2(0f, -85f), new Color32(35, 137, 225, 255), MainMenu);
    }

    private static void Resume()
    {
        GameStateMachineRunner runner = FindFirstObjectByType<GameStateMachineRunner>();
        runner?.ResumeGameplay();
    }

    private static void MainMenu()
    {
        Time.timeScale = 1f;
        WaveSpawnLifecycle.ResetStage();
        SceneManager.LoadScene("GameScene");
    }

    private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchor,
        Vector2 position, Vector2 size, Color color)
    {
        GameObject item = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        item.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text CreateText(string value, Transform parent, Vector2 position,
        Vector2 size, float fontSize, Color color)
    {
        GameObject item = new(value, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
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

    private static void CreateButton(string label, Transform parent, Vector2 position, Color color,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel(label, parent, new Vector2(0.5f, 0.5f), position,
            new Vector2(350f, 82f), color);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);
        CreateText(label, rect, Vector2.zero, new Vector2(320f, 66f), 29f, Color.white);
    }
}
