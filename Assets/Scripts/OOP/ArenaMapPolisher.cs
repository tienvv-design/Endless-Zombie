using UnityEngine;
using UnityEngine.Rendering;

public sealed class ArenaMapPolisher : MonoBehaviour
{
    private const string RootName = "Arena Map Polish";

    public static void Ensure(ArenaEnvironmentConfig config)
    {
        if (config == null || !config.EnableAtmosphere || GameObject.Find(RootName) != null) return;
        GameObject root = new(RootName);
        root.AddComponent<ArenaMapPolisher>().Apply(config);
    }

    private void Apply(ArenaEnvironmentConfig config)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = config.AmbientColor;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = config.FogColor;
        RenderSettings.fogStartDistance = Mathf.Max(0f, config.FogStart);
        RenderSettings.fogEndDistance = Mathf.Max(config.FogStart + 1f, config.FogEnd);

        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            GameObject sunObject = new("Apocalypse Sun", typeof(Light));
            sunObject.transform.SetParent(transform, false);
            sunObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            RenderSettings.sun = sun;
        }
        sun.color = config.SunColor;
        sun.intensity = config.SunIntensity;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;

        GameObject fillObject = new("Cold Sky Fill", typeof(Light));
        fillObject.transform.SetParent(transform, false);
        fillObject.transform.rotation = Quaternion.Euler(55f, 145f, 0f);
        Light fill = fillObject.GetComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.26f, 0.4f, 0.52f);
        fill.intensity = 0.22f;
        fill.shadows = LightShadows.None;
    }
}
