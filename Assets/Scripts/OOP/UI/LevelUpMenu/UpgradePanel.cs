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
    }

    private void OnDisable()
    {
        m_UpgradeChosenButton.onClick.RemoveAllListeners();
    }

    public void SetUpgrade(CharUpgrade upgrade)
    {
        m_Upgrade = upgrade;
        m_UpgradeTitle.text = m_Upgrade.GetUpgradeType().ToString();
        m_UpgradeImage.texture = m_Upgrade.Texture;
        m_UpgradeText.text = m_Upgrade.Description;
    }
}
