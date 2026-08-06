using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [SerializeField] private Button m_UpgradeChosenButton;

    [SerializeField] private RawImage m_UpgradeImage;
    [SerializeField] private TMP_Text m_UpgradeTitle;
    [SerializeField] private TMP_Text m_UpgradeText;
    
    private CharUpgrade m_Upgrade;

    private void OnEnable()
    {
        m_UpgradeChosenButton.onClick.AddListener(() => LevelUpManager.Instance.UpgradeChosenCallback(m_Upgrade));
        ConfigureResponsiveLayout();
    }

    private void OnDisable()
    {
        m_UpgradeChosenButton.onClick.RemoveAllListeners();
    }

    public void SetUpgrade(CharUpgrade upgrade)
    {
        m_Upgrade = upgrade;
        m_UpgradeTitle.text = $"{m_Upgrade.GetUpgradeType()}  [{m_Upgrade.Rarity}]";
        m_UpgradeTitle.color = m_Upgrade.GetRarityColor();
        m_UpgradeImage.texture = m_Upgrade.Texture;
        Refresh();
        ConfigureResponsiveLayout();
    }

    private void ConfigureResponsiveLayout()
    {
        bool portrait = Screen.height > Screen.width;
        RectTransform buttonRect = m_UpgradeChosenButton.transform as RectTransform;
        if (buttonRect != null)
        {
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = Vector2.zero;
        }

        RectTransform titleRect = m_UpgradeTitle.rectTransform;
        titleRect.anchorMin = new Vector2(portrait ? 0.07f : 0.23f, 0.62f);
        titleRect.anchorMax = new Vector2(0.93f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        m_UpgradeTitle.alignment = TextAlignmentOptions.Center;
        m_UpgradeTitle.enableAutoSizing = true;
        m_UpgradeTitle.fontSizeMin = 12f;
        m_UpgradeTitle.fontSizeMax = portrait ? 25f : 32f;
        m_UpgradeTitle.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform descriptionRect = m_UpgradeText.rectTransform;
        descriptionRect.anchorMin = new Vector2(portrait ? 0.07f : 0.23f, 0.08f);
        descriptionRect.anchorMax = new Vector2(0.93f, 0.62f);
        descriptionRect.offsetMin = Vector2.zero;
        descriptionRect.offsetMax = Vector2.zero;
        m_UpgradeText.alignment = TextAlignmentOptions.Center;
        m_UpgradeText.enableAutoSizing = true;
        m_UpgradeText.fontSizeMin = 10f;
        m_UpgradeText.fontSizeMax = portrait ? 19f : 27f;
        m_UpgradeText.enableWordWrapping = true;
        m_UpgradeText.overflowMode = TextOverflowModes.Ellipsis;

        if (m_UpgradeImage != null)
        {
            m_UpgradeImage.gameObject.SetActive(!portrait);
            if (!portrait)
            {
                RectTransform imageRect = m_UpgradeImage.rectTransform;
                imageRect.anchorMin = new Vector2(0.04f, 0.2f);
                imageRect.anchorMax = new Vector2(0.2f, 0.8f);
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;
            }
        }
    }

    private void Refresh()
    {
        if (m_Upgrade == null || LevelUpManager.Instance == null)
            return;

        int level = LevelUpManager.Instance.GetUpgradeLevel(m_Upgrade);
        m_UpgradeText.text = $"{m_Upgrade.Description}\nLevel {level} -> {level + 1}\nFree level-up choice";
        m_UpgradeChosenButton.interactable = true;
    }
}
