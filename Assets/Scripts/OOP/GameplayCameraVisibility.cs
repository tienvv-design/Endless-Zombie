using UnityEngine;

public static class GameplayCameraVisibility
{
    private const float MobAimHeight = 0.75f;

    public static bool Contains(Camera camera, Vector3 worldPosition)
    {
        if (camera == null || !camera.isActiveAndEnabled)
            return false;

        Vector3 viewport = camera.WorldToViewportPoint(
            worldPosition + Vector3.up * MobAimHeight);
        return viewport.z > camera.nearClipPlane &&
               viewport.x >= 0f && viewport.x <= 1f &&
               viewport.y >= 0f && viewport.y <= 1f;
    }
}
