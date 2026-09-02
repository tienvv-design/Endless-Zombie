using UnityEngine;

[CreateAssetMenu(fileName = "GameOverUISettings", menuName = "Endless Zombie/UI/Game Over Layout")]
public sealed class GameOverUISettings : ScriptableObject
{
    [Header("Canvas")]
    public Vector2 ReferenceResolution = new(1080f, 1920f);
    [Range(0f, 1f)] public float MatchWidthOrHeight = 0.5f;
    public int SortingOrder = 160;
    public Color BackdropColor = new(0f, 0f, 0f, 0.902f);

    [Header("Main Zombie Panel")]
    public Vector2 PanelPosition = Vector2.zero;
    public Vector2 PanelSize = new(860f, 1290f);

    [Header("Title")]
    public Vector2 TitlePosition = new(0f, 466f);
    public Vector2 TitleSize = new(610f, 104f);
    [Min(12f)] public float TitleFontSize = 68f;

    [Header("Stage Progress")]
    public Vector2 ProgressPosition = new(14f, 209f);
    public Vector2 ProgressSize = new(624f, 28f);
    public Vector2 ProgressTextPosition = new(0f, 48f);
    public Vector2 MarkerStartEnd = new(-302f, 302f);
    public Vector2 MarkerPosition = new(302.889f, 31.589f);
    public Vector2 MarkerSize = new(85f, 85f);

    [Header("Reward")]
    public Vector2 CollectedPosition = new(0f, 54f);
    public Vector2 CollectedSize = new(600f, 55f);
    public Vector2 GoldIconPosition = new(-115f, -110f);
    public Vector2 GoldIconSize = new(72f, 72f);
    public Vector2 GoldTextPosition = new(45f, -110f);
    public Vector2 GoldTextSize = new(260f, 110f);

    [Header("Home Button")]
    public Vector2 ButtonGroupPosition = new(0f, -542f);
    public Vector2 ButtonGroupSize = new(723.2f, 137.2f);
    public Vector2 HomeButtonSize = new(412f, 156f);
}
