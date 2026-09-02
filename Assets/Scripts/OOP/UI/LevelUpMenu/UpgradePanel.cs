using TMPro;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [SerializeField] private Button m_UpgradeChosenButton;

    [SerializeField] private RawImage m_UpgradeImage;
    [SerializeField] private TMP_Text m_UpgradeTitle;
    [SerializeField] private TMP_Text m_UpgradeText;

    [Header("Apocalypse Palette")]
    [SerializeField] private Color m_CardColor = new Color32(48, 55, 43, 255);
    [SerializeField] private Color m_TitleColor = new Color32(236, 224, 190, 255);
    [SerializeField] private Color m_DescriptionColor = new Color32(205, 197, 170, 255);
    [SerializeField] private Color m_StatPillColor = new Color32(25, 28, 24, 242);
    [SerializeField] private Color m_IncreaseColor = new Color32(155, 201, 72, 255);
    private TMP_Text m_RarityText;
    private TMP_Text m_ValueText;
    private RectTransform m_ValuePill;
    private Image m_ValuePillBackground;
    private Graphic m_CardBackground;
    private Outline m_CardOutline;
    
    private CharUpgrade m_Upgrade;

    private void OnEnable()
    {
        EnsureRuntimeVisuals();
        m_UpgradeChosenButton.onClick.AddListener(() => LevelUpManager.Instance.UpgradeChosenCallback(m_Upgrade));
        ConfigureResponsiveLayout();
    }

    private void OnDisable()
    {
        m_UpgradeChosenButton.onClick.RemoveAllListeners();
    }

    public void SetUpgrade(CharUpgrade upgrade)
    {
        EnsureRuntimeVisuals();
        m_Upgrade = upgrade;
        m_UpgradeTitle.text = Nicify(m_Upgrade.DisplayName);
        m_UpgradeTitle.color = m_TitleColor;
        m_UpgradeText.color = m_DescriptionColor;
        m_RarityText.text = m_Upgrade.Rarity.ToString().ToUpperInvariant();
        m_RarityText.color = m_Upgrade.GetRarityColor();
        m_CardBackground.color = m_CardColor;
        m_ValuePillBackground.color = m_StatPillColor;
        m_CardOutline.effectColor = m_Upgrade.GetRarityColor();
        m_CardOutline.effectDistance = new Vector2(3f, -3f);
        m_UpgradeImage.texture = m_Upgrade.Texture;
        m_UpgradeImage.color = Color.white;
        Refresh();
        ConfigureResponsiveLayout();
    }

    private void ConfigureResponsiveLayout()
    {
        RectTransform buttonRect = m_UpgradeChosenButton.transform as RectTransform;
        if (buttonRect != null)
        {
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = Vector2.zero;
        }

        RectTransform titleRect = m_UpgradeTitle.rectTransform;
        titleRect.anchorMin = new Vector2(0.34f, 0.64f);
        titleRect.anchorMax = new Vector2(0.96f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        m_UpgradeTitle.alignment = TextAlignmentOptions.Left;
        m_UpgradeTitle.enableAutoSizing = true;
        m_UpgradeTitle.fontSizeMin = 12f;
        m_UpgradeTitle.fontSizeMax = 31f;
        m_UpgradeTitle.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform descriptionRect = m_UpgradeText.rectTransform;
        descriptionRect.anchorMin = new Vector2(0.34f, 0.31f);
        descriptionRect.anchorMax = new Vector2(0.96f, 0.65f);
        descriptionRect.offsetMin = Vector2.zero;
        descriptionRect.offsetMax = Vector2.zero;
        m_UpgradeText.alignment = TextAlignmentOptions.TopLeft;
        m_UpgradeText.enableAutoSizing = true;
        m_UpgradeText.fontSizeMin = 10f;
        m_UpgradeText.fontSizeMax = 22f;
        m_UpgradeText.textWrappingMode = TextWrappingModes.Normal;
        m_UpgradeText.overflowMode = TextOverflowModes.Ellipsis;

        if (m_UpgradeImage != null)
        {
            m_UpgradeImage.gameObject.SetActive(m_UpgradeImage.texture != null);
            RectTransform imageRect = m_UpgradeImage.rectTransform;
            imageRect.anchorMin = new Vector2(0.055f, 0.22f);
            imageRect.anchorMax = new Vector2(0.285f, 0.82f);
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            AspectRatioFitter fitter = m_UpgradeImage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = m_UpgradeImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
        }

        RectTransform rarityRect = m_RarityText.rectTransform;
        rarityRect.anchorMin = new Vector2(0.035f, 0.035f);
        rarityRect.anchorMax = new Vector2(0.305f, 0.18f);
        rarityRect.offsetMin = Vector2.zero;
        rarityRect.offsetMax = Vector2.zero;

        m_ValuePill.anchorMin = new Vector2(0.34f, 0.065f);
        m_ValuePill.anchorMax = new Vector2(0.95f, 0.285f);
        m_ValuePill.offsetMin = Vector2.zero;
        m_ValuePill.offsetMax = Vector2.zero;
    }

    private void Refresh()
    {
        if (m_Upgrade == null || LevelUpManager.Instance == null)
            return;

        int level = LevelUpManager.Instance.GetUpgradeLevel(m_Upgrade);
        m_UpgradeText.text = m_Upgrade.Description;
        m_ValueText.text = ColorizeIncrease(m_Upgrade.GetValuePreview(level));
        m_UpgradeChosenButton.interactable = true;
    }

    private void EnsureRuntimeVisuals()
    {
        // SetUpgrade can be called in the same frame that this panel is enabled.
        // Resolve the persistent card components every time before using them.
        m_CardBackground = GetComponent<Graphic>();
        if (m_CardBackground == null)
            m_CardBackground = gameObject.AddComponent<Image>();
        m_CardOutline = GetComponent<Outline>();
        if (m_CardOutline == null)
            m_CardOutline = gameObject.AddComponent<Outline>();

        if (m_RarityText == null)
            m_RarityText = FindLabel("Rarity") ?? CreateLabel("Rarity", 19f, TextAlignmentOptions.Center, FontStyles.Bold);

        if (m_ValueText != null && m_ValuePillBackground != null)
            return;

        Transform existingPill = transform.Find("Stat Preview Pill");
        if (existingPill != null)
        {
            m_ValuePill = existingPill as RectTransform;
            m_ValuePillBackground = existingPill.GetComponent<Image>();
            m_ValueText = existingPill.GetComponentInChildren<TMP_Text>(true);
            if (m_ValuePillBackground != null && m_ValueText != null)
                return;
        }

        GameObject pillObject = new("Stat Preview Pill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pillObject.transform.SetParent(transform, false);
        m_ValuePill = pillObject.GetComponent<RectTransform>();
        m_ValuePillBackground = pillObject.GetComponent<Image>();
        m_ValuePillBackground.color = m_StatPillColor;
        m_ValuePillBackground.raycastTarget = false;
        m_ValueText = CreateLabel("Stat Preview", 23f, TextAlignmentOptions.Center, FontStyles.Bold, m_ValuePill);
        RectTransform valueRect = m_ValueText.rectTransform;
        valueRect.anchorMin = Vector2.zero;
        valueRect.anchorMax = Vector2.one;
        valueRect.offsetMin = new Vector2(8f, 2f);
        valueRect.offsetMax = new Vector2(-8f, -2f);
    }

    private TMP_Text FindLabel(string objectName)
    {
        Transform child = transform.Find(objectName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private TMP_Text CreateLabel(string objectName, float fontSize, TextAlignmentOptions alignment, FontStyles style, Transform parent = null)
    {
        GameObject label = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent != null ? parent : transform, false);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.fontSizeMin = 11f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static string Nicify(string value)
    {
        StringBuilder result = new();
        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && !char.IsUpper(value[i - 1])) result.Append(' ');
            result.Append(char.ToUpperInvariant(value[i]));
        }
        return result.ToString();
    }

    private string ColorizeIncrease(string preview)
    {
        int arrow = preview.IndexOf('→');
        if (arrow < 0) return preview;
        string increaseHex = ColorUtility.ToHtmlStringRGB(m_IncreaseColor);
        return preview.Substring(0, arrow) + $"<color=#{increaseHex}>→" + preview.Substring(arrow + 1) + "</color>";
    }
}
