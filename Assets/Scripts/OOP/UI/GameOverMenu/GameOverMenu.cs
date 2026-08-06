using UnityEngine;
using TMPro;

public class GameOverMenu : MonoBehaviour, IGameOver
{
    private TMP_Text m_ResultText;

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        if (m_ResultText == null)
            m_ResultText = CreateResultText();
        m_ResultText.text = $"Gold earned: {(GoldWallet.Instance ? GoldWallet.Instance.LastBankedReward : 0)}";
    }

    public void OnStateDisable()
    {
        gameObject.SetActive(false);
    }

    private TMP_Text CreateResultText()
    {
        GameObject label = new GameObject("RunGoldResult", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(transform, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // Keep the result between the GAME OVER title (y=125) and the
        // Main Menu button (roughly y=-73 on the reference canvas).
        rect.anchoredPosition = new Vector2(0f, 35f);
        rect.sizeDelta = new Vector2(420f, 44f);
        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.fontSize = 26f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.82f, 0.2f, 1f);
        text.raycastTarget = false;
        return text;
    }
}
