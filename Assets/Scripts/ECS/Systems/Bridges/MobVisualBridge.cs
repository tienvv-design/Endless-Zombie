using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class MobVisualBridge : SystemBase
{
    private const string SettingsResourcePath = "MobVisualSettings";

    private sealed class VisualInstance
    {
        public GameObject GameObject;
        public Animator Animator;
    }

    private readonly Dictionary<Entity, VisualInstance> m_Visuals = new();
    private readonly List<Entity> m_StaleEntities = new();
    private EntityQuery m_MobQuery;
    private MobVisualSettings m_Settings;
    private Transform m_VisualRoot;

    protected override void OnCreate()
    {
        m_MobQuery = GetEntityQuery(
            ComponentType.ReadOnly<Mob>(),
            ComponentType.ReadOnly<UnitMover>(),
            ComponentType.ReadOnly<LocalToWorld>());
    }

    protected override void OnStartRunning()
    {
        m_Settings = Resources.Load<MobVisualSettings>(SettingsResourcePath);
        if (m_Settings == null || m_Settings.VisualPrefab == null)
        {
            Debug.LogError($"Missing Resources/{SettingsResourcePath}.asset or its zombie visual prefab.");
            Enabled = false;
            return;
        }

        GameObject root = new("Mob Visuals");
        m_VisualRoot = root.transform;
    }

    protected override void OnUpdate()
    {
        using NativeArray<Entity> entities = m_MobQuery.ToEntityArray(Allocator.Temp);
        using NativeArray<LocalToWorld> transforms = m_MobQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
        using NativeArray<UnitMover> movers = m_MobQuery.ToComponentDataArray<UnitMover>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!m_Visuals.TryGetValue(entity, out VisualInstance visual))
            {
                GameObject visualObject = Object.Instantiate(m_Settings.VisualPrefab, m_VisualRoot);
                visualObject.name = $"Zombie Visual ({entity.Index}:{entity.Version})";

                Animator animator = visualObject.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    if (m_Settings.AnimatorController != null)
                        animator.runtimeAnimatorController = m_Settings.AnimatorController;
                    animator.applyRootMotion = false;
                }

                visual = new VisualInstance { GameObject = visualObject, Animator = animator };
                m_Visuals.Add(entity, visual);
            }

            ApplyTransform(visual.GameObject.transform, transforms[i]);
            if (visual.Animator != null)
            {
                float loopDuration = math.max(0.01f, m_Settings.WalkLoopDuration);
                float loopDistance = math.max(0.01f, m_Settings.DistancePerWalkLoop);
                visual.Animator.speed = math.max(0f, movers[i].moveSpeed * loopDuration / loopDistance);
            }
        }

        m_StaleEntities.Clear();
        foreach (KeyValuePair<Entity, VisualInstance> pair in m_Visuals)
        {
            if (!EntityManager.Exists(pair.Key) || !EntityManager.HasComponent<Mob>(pair.Key))
                m_StaleEntities.Add(pair.Key);
        }

        foreach (Entity entity in m_StaleEntities)
        {
            if (m_Visuals.Remove(entity, out VisualInstance visual) && visual?.GameObject != null)
                Object.Destroy(visual.GameObject);
        }
    }

    protected override void OnDestroy()
    {
        foreach (VisualInstance visual in m_Visuals.Values)
            if (visual?.GameObject != null) Object.Destroy(visual.GameObject);
        m_Visuals.Clear();

        if (m_VisualRoot != null)
            Object.Destroy(m_VisualRoot.gameObject);
    }

    private static void ApplyTransform(Transform target, LocalToWorld source)
    {
        float4x4 matrix = source.Value;
        target.SetPositionAndRotation(matrix.c3.xyz, new quaternion(matrix));
        target.localScale = new Vector3(
            math.length(matrix.c0.xyz),
            math.length(matrix.c1.xyz),
            math.length(matrix.c2.xyz));
    }
}
