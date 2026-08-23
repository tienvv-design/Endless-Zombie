using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsMenu : MonoBehaviour
{
    private const string VibrationKey = "Settings.Vibration";

    [SerializeField] private SettingsMenuView m_View;
    private bool m_Bound;

    public static void EnsureExists(Transform parent)
    {
        if (FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include) != null)
            return;

        SettingsMenu prefab = Resources.Load<SettingsMenu>("SettingsMenu");
        if (prefab == null)
        {
            Debug.LogError("Missing Resources/SettingsMenu.prefab.");
            return;
        }

        SettingsMenu instance = Instantiate(prefab, parent, false);
        instance.name = "SettingsMenu";
        instance.gameObject.SetActive(false);
    }

    private void Awake()
    {
        BindView();
    }

    private void OnEnable()
    {
        BindView();
        RefreshControls();
    }

    public void Show()
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    private void BindView()
    {
        if (m_Bound) return;
        if (m_View == null)
            m_View = GetComponent<SettingsMenuView>();
        if (m_View == null)
        {
            Debug.LogError("SettingsMenu prefab is missing SettingsMenuView.", this);
            return;
        }

        m_View.CaptureReferences();
        BindSlider(m_View.MusicSlider, value => AudioManager.Instance?.SetMusicVolume(value));
        BindSlider(m_View.SoundSlider, value => AudioManager.Instance?.SetSoundVolume(value));
        BindButton(m_View.VibrationButton, ToggleVibration);
        BindButton(m_View.HeaderCloseButton, Close);
        BindButton(m_View.FooterCloseButton, Close);
        m_Bound = true;
    }

    private static void BindSlider(Slider slider, UnityEngine.Events.UnityAction<float> action)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void RefreshControls()
    {
        if (m_View == null) return;
        if (m_View.MusicSlider != null)
            m_View.MusicSlider.SetValueWithoutNotify(AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.7f);
        if (m_View.SoundSlider != null)
            m_View.SoundSlider.SetValueWithoutNotify(AudioManager.Instance != null ? AudioManager.Instance.SoundVolume : 0.7f);
        RefreshVibration();
    }

    private void ToggleVibration()
    {
        PlayerPrefs.SetInt(VibrationKey, PlayerPrefs.GetInt(VibrationKey, 1) == 0 ? 1 : 0);
        RefreshVibration();
    }

    private void RefreshVibration()
    {
        TMP_Text text = m_View != null ? m_View.VibrationText : null;
        if (text == null) return;
        bool vibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) != 0;
        text.text = vibrationEnabled ? "ON" : "OFF";
        text.color = vibrationEnabled ? new Color32(38, 103, 64, 255) : new Color32(120, 75, 75, 255);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
