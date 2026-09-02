using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponVfxRuntime : MonoBehaviour
{
    private sealed class ContinuousEffect
    {
        public GameObject Instance;
        public Transform FollowTarget;
        public Vector3 LocalOffset;
        public Quaternion LocalRotation;
        public long TargetKey = long.MinValue;
        public float Scale;
        public float StopTime;
    }

    private sealed class ActiveEffect
    {
        public GameObject Prefab;
        public GameObject Instance;
        public float ReturnTime;
    }

    public static GunConfig CurrentConfig { get; set; }
    public static Transform CurrentMuzzle { get; set; }
    public static Transform CurrentMuzzleVfxSocket { get; set; }
    public static Transform CurrentBulletSpawn { get; set; }
    private static WeaponVfxRuntime s_Instance;
    private readonly Dictionary<GameObject, Queue<GameObject>> m_Pools = new();
    private readonly List<ActiveEffect> m_Active = new();
    private readonly Dictionary<GameObject, ContinuousEffect> m_Continuous = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (s_Instance == null)
            new GameObject(nameof(WeaponVfxRuntime)).AddComponent<WeaponVfxRuntime>();
    }

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Play(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        float lifetime,
        float scale = 1f)
    {
        if (s_Instance == null || prefab == null) return;
        s_Instance.PlayPooled(prefab, position, rotation, lifetime, Vector3.one * scale);
    }

    public static void Play(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        float lifetime,
        Vector3 scale)
    {
        if (s_Instance == null || prefab == null) return;
        s_Instance.PlayPooled(prefab, position, rotation, lifetime, scale);
    }

    public static void PlayContinuous(
        GameObject prefab,
        Transform followTarget,
        Vector3 localOffset,
        Quaternion worldRotation,
        long targetKey,
        float sustainTime,
        float scale = 1f)
    {
        if (s_Instance == null || prefab == null || followTarget == null) return;
        s_Instance.RefreshContinuous(
            prefab, followTarget, localOffset, worldRotation, targetKey, sustainTime, scale);
    }

    private void RefreshContinuous(GameObject prefab, Transform followTarget, Vector3 localOffset,
        Quaternion worldRotation, long targetKey, float sustainTime, float scale)
    {
        if (!m_Continuous.TryGetValue(prefab, out ContinuousEffect effect) || effect.Instance == null)
        {
            GameObject instance = Instantiate(prefab, followTarget, false);
            instance.name = prefab.name + " (Continuous)";
            ConfigureContinuousParticles(instance);
            effect = new ContinuousEffect { Instance = instance };
            m_Continuous[prefab] = effect;
        }

        bool targetChanged = effect.TargetKey != targetKey;
        if (targetChanged)
        {
            foreach (ParticleSystem particles in
                     effect.Instance.GetComponentsInChildren<ParticleSystem>(true))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.TargetKey = targetKey;
        }

        effect.FollowTarget = followTarget;
        effect.LocalOffset = localOffset;
        effect.LocalRotation = Quaternion.Inverse(followTarget.rotation) * worldRotation;
        effect.Scale = Mathf.Max(0.01f, Mathf.Abs(scale));
        effect.StopTime = Time.unscaledTime + Mathf.Max(0.08f, sustainTime);
        Follow(effect);
        if (!effect.Instance.activeSelf) effect.Instance.SetActive(true);
        foreach (ParticleSystem particles in effect.Instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particles.isPlaying) continue;
            particles.Clear(true);
            particles.Play(true);
            particles.Emit(8);
        }
    }

    private static void ConfigureContinuousParticles(GameObject instance)
    {
        // The LDoE flamethrower prefab is authored as a one-shot effect. Endless
        // Zombie keeps one instance attached to the muzzle, so every child system
        // must loop until the weapon stops firing.
        foreach (ParticleSystem particles in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = Mathf.Max(main.maxParticles, 256);
            main.startSizeMultiplier *= 1.35f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTimeMultiplier *= 1.6f;

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer == null) continue;
            particleRenderer.enabled = true;
            particleRenderer.forceRenderingOff = false;
            particleRenderer.sortingOrder = Mathf.Max(particleRenderer.sortingOrder, 20);

            Material runtimeMaterial = particleRenderer.material;
            if (runtimeMaterial != null && runtimeMaterial.HasProperty("_ColorStrength"))
                runtimeMaterial.SetFloat("_ColorStrength", 2.2f);

            // Fast particles can leave their automatically calculated bounds for a
            // frame and be culled by the top-down camera. A generous local bound is
            // cheap here because only one continuous flame instance exists.
            particleRenderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 40f);
        }
    }

    private void PlayPooled(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        float lifetime,
        Vector3 scale)
    {
        if (!m_Pools.TryGetValue(prefab, out Queue<GameObject> pool))
            m_Pools[prefab] = pool = new Queue<GameObject>();
        GameObject instance = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
        instance.transform.SetPositionAndRotation(position, rotation);
        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        scale.z = Mathf.Abs(scale.z);
        instance.transform.localScale = Vector3.Scale(prefab.transform.localScale, scale);
        instance.SetActive(true);
        foreach (ParticleSystem particles in instance.GetComponentsInChildren<ParticleSystem>())
            particles.Play(true);
        m_Active.Add(new ActiveEffect
        {
            Prefab = prefab,
            Instance = instance,
            ReturnTime = Time.unscaledTime + Mathf.Max(0.05f, lifetime),
        });
    }

    private void Update()
    {
        for (int i = m_Active.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = m_Active[i];
            if (Time.unscaledTime < effect.ReturnTime) continue;
            m_Active.RemoveAt(i);
            if (effect.Instance == null) continue;
            effect.Instance.SetActive(false);
            m_Pools[effect.Prefab].Enqueue(effect.Instance);
        }

        foreach (ContinuousEffect effect in m_Continuous.Values)
        {
            if (effect.Instance == null || !effect.Instance.activeSelf) continue;
            if (effect.FollowTarget == null || Time.unscaledTime >= effect.StopTime)
            {
                foreach (ParticleSystem particles in effect.Instance.GetComponentsInChildren<ParticleSystem>(true))
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.Instance.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        foreach (ContinuousEffect effect in m_Continuous.Values)
            if (effect.Instance != null && effect.Instance.activeSelf && effect.FollowTarget != null)
                Follow(effect);
    }

    private static void Follow(ContinuousEffect effect)
    {
        Transform target = effect.FollowTarget;
        Transform effectTransform = effect.Instance.transform;

        // Make the stream a real child of Muzzle. Inheriting the hierarchy is more
        // reliable than copying world transforms because the player animation and
        // held-weapon presenter can both update the hand later in the frame.
        if (effectTransform.parent != target)
            effectTransform.SetParent(target, false);
        effectTransform.localPosition = effect.LocalOffset;
        effectTransform.localRotation = effect.LocalRotation;
        effectTransform.localScale = Vector3.one * effect.Scale;
    }
}
