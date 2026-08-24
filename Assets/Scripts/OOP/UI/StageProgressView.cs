using TMPro;
using Unity.Entities;
using UnityEngine;

public static class StageProgressView
{
    public static TMP_Text AddLabel(Transform parent, Vector2 position, float fontSize = 26f)
    {
        GameObject label = new("StageProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(520f, 50f);

        TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.75f, 0.95f, 1f, 1f);
        text.raycastTarget = false;
        return text;
    }

    public static void Refresh(TMP_Text label, bool stageCleared = false)
    {
        if (label == null) return;

        label.text = $"STAGE PROGRESS  {GetPercent(stageCleared)}%";
    }

    public static int GetPercent(bool stageCleared = false)
    {

        int kills = 0;
        int totalEnemies = 0;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated)
        {
            EntityManager manager = world.EntityManager;
            EntityQuery stageQuery = manager.CreateEntityQuery(typeof(StageRuntime));
            if (stageQuery.CalculateEntityCount() == 1)
            {
                Entity stageEntity = stageQuery.GetSingletonEntity();
                DynamicBuffer<SpawnEntryRuntime> entries = manager.GetBuffer<SpawnEntryRuntime>(stageEntity);
                for (int i = 0; i < entries.Length; i++)
                    totalEnemies += entries[i].Quantity;
            }
            stageQuery.Dispose();

            EntityQuery metricsQuery = manager.CreateEntityQuery(typeof(CombatMetrics));
            if (metricsQuery.CalculateEntityCount() == 1)
                kills = metricsQuery.GetSingleton<CombatMetrics>().KillCount;
            metricsQuery.Dispose();
        }

        int percent = stageCleared ? 100 : totalEnemies > 0
            ? Mathf.Clamp(Mathf.RoundToInt(kills * 100f / totalEnemies), 0, 100)
            : 0;
        return percent;
    }
}
