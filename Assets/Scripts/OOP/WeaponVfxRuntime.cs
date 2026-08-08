using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponVfxRuntime : MonoBehaviour
{
    private sealed class ActiveEffect
    {
        public GameObject Prefab;
        public GameObject Instance;
        public float ReturnTime;
    }

    public static GunConfig CurrentConfig { get; set; }
    public static Transform CurrentMuzzle { get; set; }
    private static WeaponVfxRuntime s_Instance;
    private readonly Dictionary<GameObject, Queue<GameObject>> m_Pools = new();
    private readonly List<ActiveEffect> m_Active = new();

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

    public static void Play(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (s_Instance == null || prefab == null) return;
        s_Instance.PlayPooled(prefab, position, rotation, lifetime);
    }

    private void PlayPooled(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (!m_Pools.TryGetValue(prefab, out Queue<GameObject> pool))
            m_Pools[prefab] = pool = new Queue<GameObject>();
        GameObject instance = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
        instance.transform.SetPositionAndRotation(position, rotation);
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
    }
}
