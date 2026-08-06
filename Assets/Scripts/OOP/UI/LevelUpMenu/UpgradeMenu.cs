using System;
using System.Collections.Generic;
using UnityEngine;


public class UpgradeMenu : MonoBehaviour, IGameLevelUp
{
    [SerializeField] private GameObject m_UpgradeVerticalLayoutGroup;

    private void Awake()
    {
        LevelUpManager.Instance.OnUpgradesAssigned += UpgradesAssignedCallback;
    }

    private void OnDestroy()
    {
        LevelUpManager.Instance.OnUpgradesAssigned -= UpgradesAssignedCallback;
    }

    private void UpgradesAssignedCallback(List<CharUpgrade> upgrades)
    {
        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgradePanelTransform = m_UpgradeVerticalLayoutGroup.transform.GetChild(i);
            if (upgradePanelTransform.TryGetComponent(out UpgradePanel panel))
            {
                panel.SetUpgrade(upgrades[i]);
            }
        }
    }

    public void OnStateEnable()
    {
        gameObject.SetActive(true);
        RectTransform rect = transform as RectTransform;
        if (rect != null && Screen.height > Screen.width)
        {
            rect.sizeDelta = new Vector2(900f, 980f);
            rect.anchoredPosition = Vector2.zero;
        }
        
    }

    public void OnStateDisable()
    {
        gameObject.SetActive(false);
        
    }
}
