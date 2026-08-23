using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsMenuView : MonoBehaviour
{
    public Slider MusicSlider;
    public Slider SoundSlider;
    public Button VibrationButton;
    public TMP_Text VibrationText;
    public Button HeaderCloseButton;
    public Button FooterCloseButton;

    public void CaptureReferences()
    {
        Transform panel = transform.Find("KickerMainSetting");
        if (panel == null) return;
        MusicSlider = panel.Find("MUSICSlider")?.GetComponent<Slider>();
        SoundSlider = panel.Find("SOUNDSlider")?.GetComponent<Slider>();
        VibrationButton = panel.Find("Vibration")?.GetComponent<Button>();
        VibrationText = VibrationButton != null
            ? VibrationButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        HeaderCloseButton = panel.Find("Close")?.GetComponent<Button>();
        FooterCloseButton = panel.Find("CLOSE")?.GetComponent<Button>();
    }
}
