using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuCanvasView : MonoBehaviour
{
    public Button SettingsButton;
    public TextMeshProUGUI GoldText;
    public Button StartButton;
    public Button PreviousStageButton;
    public Button NextStageButton;
    public Button PetButton;
    public Button WeaponButton;
    public Button InventoryButton;
    public Button ShopButton;
    public RectTransform HealthCard;
    public RectTransform IncomeCard;

    public void CaptureReferences()
    {
        SettingsButton = FindComponent<Button>("Settings Button");
        StartButton = FindComponent<Button>("TAP TO START");
        PreviousStageButton = FindComponent<Button>("Previous Stage Button");
        NextStageButton = FindComponent<Button>("Next Stage Button");
        PetButton = FindComponent<Button>("Navigation/PET Tab");
        WeaponButton = FindComponent<Button>("Navigation/WEAPON Tab");
        InventoryButton = FindComponent<Button>("Navigation/INVENTORY Tab");
        ShopButton = FindComponent<Button>("Navigation/SHOP Tab");
        HealthCard = FindRect("MAX HP");
        IncomeCard = FindRect("INCOME");
        RectTransform goldPill = FindRect("Gold Pill");
        GoldText = goldPill != null ? goldPill.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform item = transform.Find(path);
        return item != null ? item.GetComponent<T>() : null;
    }

    private RectTransform FindRect(string path)
    {
        return transform.Find(path) as RectTransform;
    }
}
