using TMPro;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class CombatMetricsBridge : SystemBase
{
    private TextMeshProUGUI _text;
    private float _refreshTimer;

    protected override void OnCreate()
    {
        RequireForUpdate<CombatMetrics>();
    }

    protected override void OnDestroy()
    {
        if (_text != null)
            Object.Destroy(_text.gameObject);
    }

    protected override void OnUpdate()
    {
        EnsureTextExists();
        if (_text == null)
            return;

        _refreshTimer -= SystemAPI.Time.DeltaTime;
        if (_refreshTimer > 0f)
            return;
        _refreshTimer = 0.25f;

        CombatMetrics metrics = SystemAPI.GetSingleton<CombatMetrics>();
        _text.text =
            $"COMBAT METRICS\n" +
            $"DPS (1s): {metrics.RecentDps:0.0}\n" +
            $"Avg TTK: {metrics.AverageTimeToKill:0.0}s\n" +
            $"Enemies: {metrics.ActiveEnemies}  Near: {metrics.NearbyEnemies}\n" +
            $"Pressure: {metrics.Pressure:0.0}\n" +
            $"Kills: {metrics.KillCount}";
    }

    private void EnsureTextExists()
    {
        if (_text != null)
            return;

        HUDManager hud = Object.FindFirstObjectByType<HUDManager>();
        if (hud == null)
            return;

        GameObject panel = new GameObject(
            "CombatMetrics",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        panel.transform.SetParent(hud.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(340f, 180f);

        _text = panel.GetComponent<TextMeshProUGUI>();
        _text.fontSize = 20f;
        _text.fontStyle = FontStyles.Bold;
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.color = new Color(0.75f, 0.92f, 1f, 0.95f);
        _text.raycastTarget = false;
    }
}
