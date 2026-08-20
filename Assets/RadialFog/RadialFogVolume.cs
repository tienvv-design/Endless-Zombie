using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Fog/Radial Fog")]
public sealed class RadialFogVolume : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Master switch. Fog is disabled unless a Volume explicitly enables this value.")]
    public BoolParameter enabled = new(false);

    [Tooltip("World-space point where radial distance is zero.")]
    public Vector3Parameter center = new(Vector3.zero);

    [Tooltip("Fog remains completely clear inside this radius.")]
    public MinFloatParameter clearRadius = new(0f, 0f);

    [Tooltip("How quickly fog increases outside the clear radius.")]
    public MinFloatParameter density = new(0.02f, 0f);

    [Tooltip("Maximum amount of fog blended over the scene.")]
    public ClampedFloatParameter maxOpacity = new(1f, 0f, 1f);

    public ColorParameter color = new(new Color(0.5f, 0.55f, 0.6f, 1f), false, false, true);

    public bool IsActive() => active && enabled.value && density.value > 0f && maxOpacity.value > 0f;
    public bool IsTileCompatible() => false;
}
