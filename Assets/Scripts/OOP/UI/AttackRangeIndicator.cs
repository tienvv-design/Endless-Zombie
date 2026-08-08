using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class AttackRangeIndicator : MonoBehaviour, IGameRunning
{
    private const int Segments = 96;
    private LineRenderer m_Line;
    private EntityQuery m_WeaponQuery;
    private EntityQuery m_StageQuery;
    private EntityQuery m_GameplayGateQuery;
    private EntityManager m_EntityManager;
    private bool m_QueryCreated;
    private Material m_Material;
    private float m_LastRange = -1f;

    private void Awake()
    {
        GameObject ring = new GameObject("AttackRangeRing");
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        m_Line = ring.AddComponent<LineRenderer>();
        m_Line.useWorldSpace = false;
        m_Line.loop = true;
        m_Line.positionCount = Segments;
        m_Line.startWidth = 0.045f;
        m_Line.endWidth = 0.045f;
        m_Line.numCornerVertices = 2;
        m_Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_Line.receiveShadows = false;
        m_Material = new Material(Shader.Find("Sprites/Default"));
        m_Material.color = new Color(0.15f, 0.85f, 1f, 0.62f);
        m_Line.material = m_Material;
        m_Line.enabled = false;
    }

    private void Update()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        if (!m_QueryCreated)
        {
            m_EntityManager = world.EntityManager;
            m_WeaponQuery = m_EntityManager.CreateEntityQuery(typeof(WeaponManager));
            m_StageQuery = m_EntityManager.CreateEntityQuery(typeof(StageRuntime));
            m_GameplayGateQuery = m_EntityManager.CreateEntityQuery(typeof(GameplayStartedTag));
            m_QueryCreated = true;
        }
        bool stageActive = m_GameplayGateQuery.CalculateEntityCount() == 1 &&
                           m_StageQuery.CalculateEntityCount() == 1 &&
                           m_StageQuery.GetSingleton<StageRuntime>().State == StageRuntimeState.Running;
        if (m_Line.enabled != stageActive)
            m_Line.enabled = stageActive;
        if (!stageActive || m_WeaponQuery.CalculateEntityCount() != 1) return;

        float range = m_WeaponQuery.GetSingleton<WeaponManager>().AttackRange;
        if (Mathf.Abs(range - m_LastRange) < 0.01f) return;
        m_LastRange = range;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            m_Line.SetPosition(i, new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
        }
    }

    public void OnStateEnable()
    {
        enabled = true;
        if (m_Line != null) m_Line.enabled = false;
    }

    public void OnStateDisable()
    {
        if (m_Line != null) m_Line.enabled = false;
        enabled = false;
    }

    private void OnDestroy()
    {
        if (m_QueryCreated)
        {
            m_WeaponQuery.Dispose();
            m_StageQuery.Dispose();
            m_GameplayGateQuery.Dispose();
            m_QueryCreated = false;
        }
        if (m_Material != null) Destroy(m_Material);
    }
}
