using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


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
        ConfigureResponsiveLayout();
    }

    public void OnStateDisable()
    {
        gameObject.SetActive(false);
        
    }

    private void ConfigureResponsiveLayout()
    {
        RectTransform menuRect = transform as RectTransform;
        RectTransform layoutRect = m_UpgradeVerticalLayoutGroup != null
            ? m_UpgradeVerticalLayoutGroup.transform as RectTransform
            : null;
        if (menuRect == null || layoutRect == null)
            return;

        // Keep the menu centred. Previously portrait mode forced a 900x980 box,
        // moving the bottom-anchored card group down and overriding Inspector edits.
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.anchoredPosition = Vector2.zero;

        if (menuRect.parent is RectTransform parentRect)
        {
            float width = Mathf.Min(1040f, Mathf.Max(620f, parentRect.rect.width - 64f));
            float height = Mathf.Min(820f, Mathf.Max(680f, parentRect.rect.height - 80f));
            menuRect.sizeDelta = new Vector2(width, height);
        }

        // Reserve the upper area for the title and distribute all three cards
        // evenly in the remaining space on every aspect ratio.
        layoutRect.anchorMin = new Vector2(0.08f, 0.07f);
        layoutRect.anchorMax = new Vector2(0.92f, 0.79f);
        layoutRect.pivot = new Vector2(0.5f, 0.5f);
        layoutRect.anchoredPosition = Vector2.zero;
        layoutRect.sizeDelta = Vector2.zero;

        if (m_UpgradeVerticalLayoutGroup.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRect);
    }
}
