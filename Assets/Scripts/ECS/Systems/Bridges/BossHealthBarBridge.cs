using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class BossHealthBarBridge : SystemBase
{
    private EntityQuery m_Query;
    private GameObject m_Root;
    private Image m_Fill;
    private TMP_Text m_Label;

    protected override void OnCreate() => m_Query = GetEntityQuery(
        ComponentType.ReadOnly<Mob>(), ComponentType.ReadOnly<BossPhase>());

    protected override void OnUpdate()
    {
        EnsureUI();
        using NativeArray<Mob> mobs = m_Query.ToComponentDataArray<Mob>(Allocator.Temp);
        using NativeArray<BossPhase> phases = m_Query.ToComponentDataArray<BossPhase>(Allocator.Temp);
        bool visible = mobs.Length > 0;
        if (m_Root.activeSelf != visible) m_Root.SetActive(visible);
        if (!visible) return;
        Mob boss = mobs[0];
        m_Fill.fillAmount = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.Health / boss.MaxHealth) : 0f;
        m_Label.text = $"BOSS   •   PHASE {phases[0].CurrentPhase}     {boss.Health:N0} / {boss.MaxHealth:N0}";
    }

    private void EnsureUI()
    {
        if (m_Root != null) return;
        m_Root = new GameObject("Boss Health UI", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = m_Root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = m_Root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = MakeImage("Frame", m_Root.transform, new Color32(25, 14, 19, 245));
        RectTransform frame = background.rectTransform;
        frame.anchorMin = new Vector2(0.15f, 0.91f); frame.anchorMax = new Vector2(0.85f, 0.955f);
        frame.offsetMin = frame.offsetMax = Vector2.zero;
        m_Fill = MakeImage("Health", frame, new Color32(172, 32, 38, 255));
        m_Fill.type = Image.Type.Filled; m_Fill.fillMethod = Image.FillMethod.Horizontal;
        RectTransform fill = m_Fill.rectTransform;
        fill.anchorMin = new Vector2(0.01f, 0.1f); fill.anchorMax = new Vector2(0.99f, 0.9f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        GameObject label = new("Boss Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(frame, false);
        m_Label = label.GetComponent<TextMeshProUGUI>();
        m_Label.alignment = TextAlignmentOptions.Center; m_Label.fontStyle = FontStyles.Bold;
        m_Label.fontSize = 27f; m_Label.color = Color.white; m_Label.raycastTarget = false;
        RectTransform labelRect = m_Label.rectTransform;
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        m_Root.SetActive(false);
    }

    private static Sprite s_WhiteSprite;
    private static Sprite WhiteSprite => s_WhiteSprite != null
        ? s_WhiteSprite
        : (s_WhiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f)));

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        value.transform.SetParent(parent, false);
        Image image = value.GetComponent<Image>();
        image.sprite = WhiteSprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    protected override void OnDestroy()
    {
        if (m_Root != null) Object.Destroy(m_Root);
    }
}
