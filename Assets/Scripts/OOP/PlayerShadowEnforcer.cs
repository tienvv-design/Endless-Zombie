using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerShadowEnforcer : MonoBehaviour
{
    private const string GroundShadowName = "Player Ground Shadow";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<PlayerShadowEnforcer>() == null)
            player.AddComponent<PlayerShadowEnforcer>();
    }

    private void OnEnable()
    {
        RemoveLegacyGroundShadow();
        ApplyShadows();
        StartCoroutine(ApplyAfterVisualSetup());
    }

    private void OnTransformChildrenChanged()
    {
        ApplyShadows();
    }

    private IEnumerator ApplyAfterVisualSetup()
    {
        yield return null;
        ApplyShadows();
    }

    private void ApplyShadows()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer or TrailRenderer or LineRenderer)
                continue;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private void RemoveLegacyGroundShadow()
    {
        Transform shadow = transform.Find(GroundShadowName);
        if (shadow != null)
            Destroy(shadow.gameObject);
    }
}
