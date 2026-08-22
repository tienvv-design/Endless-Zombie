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
        public float LoopDuration;
        public float DistancePerLoop;
        public float GroundOffset;
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
            ComponentType.ReadOnly<LocalTransform>());
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
        using NativeArray<LocalTransform> transforms = m_MobQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using NativeArray<UnitMover> movers = m_MobQuery.ToComponentDataArray<UnitMover>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            bool activateAfterTransform = false;
            if (!m_Visuals.TryGetValue(entity, out VisualInstance visual))
            {
                bool isDogMutant = EntityManager.HasComponent<MobVisualVariant>(entity) &&
                                   EntityManager.GetComponentData<MobVisualVariant>(entity).Kind == MobVisualKind.DogMutant;
                GameObject prefab = isDogMutant && m_Settings.DogMutantVisualPrefab != null
                    ? m_Settings.DogMutantVisualPrefab
                    : m_Settings.VisualPrefab;
                RuntimeAnimatorController controller = isDogMutant
                    ? m_Settings.DogMutantAnimatorController
                    : m_Settings.AnimatorController;
                float loopDuration = isDogMutant
                    ? m_Settings.DogMutantRunLoopDuration
                    : m_Settings.WalkLoopDuration;
                float distancePerLoop = isDogMutant
                    ? m_Settings.DogMutantDistancePerRunLoop
                    : m_Settings.DistancePerWalkLoop;

                GameObject visualObject = Object.Instantiate(prefab, m_VisualRoot);
                // A newly instantiated GameObject starts at the visual root (world origin).
                // Keep it hidden until the entity's actual spawn transform is applied so it
                // can never render for one frame on top of the player.
                visualObject.SetActive(false);
                visualObject.name = $"{(isDogMutant ? "Dog Mutant" : "Zombie")} Visual ({entity.Index}:{entity.Version})";

                Animator animator = visualObject.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    if (controller != null)
                        animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                visual = new VisualInstance
                {
                    GameObject = visualObject,
                    Animator = animator,
                    LoopDuration = loopDuration,
                    DistancePerLoop = distancePerLoop,
                    GroundOffset = CalculateGroundOffset(visualObject) +
                                   (isDogMutant ? m_Settings.DogMutantGroundOffset : m_Settings.ZombieGroundOffset),
                };
                m_Visuals.Add(entity, visual);
                activateAfterTransform = true;
            }

            ApplyTransform(visual.GameObject.transform, transforms[i], visual.GroundOffset);
            if (activateAfterTransform)
                visual.GameObject.SetActive(true);
            if (visual.Animator != null)
            {
                float loopDuration = math.max(0.01f, visual.LoopDuration);
                float loopDistance = math.max(0.01f, visual.DistancePerLoop);
                visual.Animator.speed = math.max(0f, movers[i].moveSpeed * loopDuration / loopDistance);
                EnsureLocomotionLoops(visual.Animator);
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

    private static float CalculateGroundOffset(GameObject visualObject)
    {
        Renderer[] renderers = visualObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return 0f;

        float lowestPoint = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            lowestPoint = Mathf.Min(lowestPoint, renderer.bounds.min.y);
        return float.IsPositiveInfinity(lowestPoint) ? 0f : visualObject.transform.position.y - lowestPoint;
    }

    private static void ApplyTransform(Transform target, LocalTransform source, float groundOffset)
    {
        float3 groundedPosition = source.Position;
        groundedPosition.y += groundOffset * source.Scale;
        target.SetPositionAndRotation(groundedPosition, source.Rotation);
        target.localScale = Vector3.one * source.Scale;
    }

    private static void EnsureLocomotionLoops(Animator animator)
    {
        if (animator.IsInTransition(0)) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.loop || state.normalizedTime < 1f) return;

        // Some visual variants use an animation clip embedded in an FBX whose
        // importer does not expose Loop Time. Keep the controller's locomotion
        // state cycling without modifying the source model asset.
        animator.Play(state.fullPathHash, 0, state.normalizedTime % 1f);
    }
}
