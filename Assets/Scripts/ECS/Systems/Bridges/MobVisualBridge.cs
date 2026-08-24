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
        public AnimatorOverrideController OverrideController;
        public AnimationClip LocomotionClip;
        public bool IsAttacking;
        public bool SupportsAttack;
        public LineRenderer BossWarning;
        public LineRenderer SpawnPortal;
        public DogAttackProceduralAnimator DogAttackAnimator;
        public bool IsDogMutant;
    }

    private readonly Dictionary<Entity, VisualInstance> m_Visuals = new();
    private readonly List<Entity> m_StaleEntities = new();
    private EntityQuery m_MobQuery;
    private MobVisualSettings m_Settings;
    private Transform m_VisualRoot;

    protected override void OnCreate()
    {
        m_MobQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<Mob>(),
                ComponentType.ReadOnly<UnitMover>(),
                ComponentType.ReadOnly<KamikazeUnit>(),
                ComponentType.ReadOnly<LocalTransform>(),
            },
            Options = EntityQueryOptions.IgnoreComponentEnabledState,
        });
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
        using NativeArray<KamikazeUnit> attacks = m_MobQuery.ToComponentDataArray<KamikazeUnit>(Allocator.Temp);
        using NativeArray<Mob> mobs = m_MobQuery.ToComponentDataArray<Mob>(Allocator.Temp);

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            bool activateAfterTransform = false;
            if (!m_Visuals.TryGetValue(entity, out VisualInstance visual))
            {
                bool isDogMutant = EntityManager.HasComponent<MobVisualVariant>(entity) &&
                                   EntityManager.GetComponentData<MobVisualVariant>(entity).Kind == MobVisualKind.DogMutant;
                bool isBoss = mobs[i].EnemyType == EnemyType.Boss;
                GameObject prefab = isBoss && m_Settings.BossVisualPrefab != null ? m_Settings.BossVisualPrefab :
                    isDogMutant && m_Settings.DogMutantVisualPrefab != null ? m_Settings.DogMutantVisualPrefab : m_Settings.VisualPrefab;
                RuntimeAnimatorController controller = isBoss && m_Settings.BossAnimatorController != null
                    ? m_Settings.BossAnimatorController : isDogMutant ? m_Settings.DogMutantAnimatorController : m_Settings.AnimatorController;
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
                if (EntityManager.HasComponent<EliteModifier>(entity))
                    ApplyEliteAppearance(visualObject, EntityManager.GetComponentData<EliteModifier>(entity).Kind);

                Animator animator = visualObject.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    if (controller != null)
                    {
                        AnimatorOverrideController overrideController = new(controller);
                        animator.runtimeAnimatorController = overrideController;
                    }
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
                                   (isBoss ? m_Settings.BossGroundOffset : isDogMutant ? m_Settings.DogMutantGroundOffset : m_Settings.ZombieGroundOffset),
                    OverrideController = animator != null
                        ? animator.runtimeAnimatorController as AnimatorOverrideController
                        : null,
                    LocomotionClip = controller != null && controller.animationClips.Length > 0
                        ? controller.animationClips[0]
                        : null,
                    SupportsAttack = !isDogMutant,
                    IsDogMutant = isDogMutant,
                };
                if (isDogMutant)
                {
                    visual.DogAttackAnimator = visualObject.AddComponent<DogAttackProceduralAnimator>();
                    visual.DogAttackAnimator.Initialize();
                }
                m_Visuals.Add(entity, visual);
                if (isBoss && m_Settings.BossVisualPrefab == null)
                    ApplyBossPlaceholderAppearance(visualObject);
                activateAfterTransform = true;
            }

            ApplyTransform(visual.GameObject.transform, transforms[i], visual.GroundOffset);
            if (EntityManager.HasComponent<SpawnEmergence>(entity))
                UpdateSpawnPortal(visual, transforms[i], EntityManager.GetComponentData<SpawnEmergence>(entity));
            else
                FinishSpawnPortal(visual);
            if (activateAfterTransform)
                visual.GameObject.SetActive(true);
            if (visual.Animator != null)
            {
                bool emerging = EntityManager.HasComponent<SpawnEmergence>(entity);
                bool movementLocked = !EntityManager.IsComponentEnabled<UnitMover>(entity);
                bool attacking = visual.SupportsAttack && !emerging && movementLocked;
                bool dogAttacking = visual.DogAttackAnimator != null && !emerging && movementLocked;
                if (visual.DogAttackAnimator != null)
                {
                    float interval = math.max(0.05f, attacks[i].AttackInterval);
                    float attackProgress = 1f - math.saturate(attacks[i].AttackTimer / interval);
                    visual.DogAttackAnimator.SetAttack(dogAttacking, attackProgress);
                }
                UpdateAttackAnimation(visual, attacking);
                if (attacking && m_Settings.ZombieAttackClip != null)
                    visual.Animator.speed = m_Settings.ZombieAttackClip.length /
                                            math.max(0.05f, attacks[i].AttackInterval);
                else if (!emerging && movementLocked)
                    // Dog Mutant currently has no dedicated attack clip. Freeze
                    // locomotion during its bite instead of letting it run in place.
                    visual.Animator.speed = 0f;
                else
                {
                    float loopDuration = math.max(0.01f, visual.LoopDuration);
                    float loopDistance = math.max(0.01f, visual.DistancePerLoop);
                    visual.Animator.speed = math.max(0f, movers[i].moveSpeed * loopDuration / loopDistance);
                }
                EnsureLocomotionLoops(visual.Animator);
            }
            if (mobs[i].EnemyType == EnemyType.Boss && EntityManager.HasComponent<BossShockwave>(entity))
                UpdateBossWarning(visual, transforms[i], EntityManager.GetComponentData<BossShockwave>(entity));
        }

        m_StaleEntities.Clear();
        foreach (KeyValuePair<Entity, VisualInstance> pair in m_Visuals)
        {
            if (!EntityManager.Exists(pair.Key) || !EntityManager.HasComponent<Mob>(pair.Key))
                m_StaleEntities.Add(pair.Key);
        }

        foreach (Entity entity in m_StaleEntities)
        {
            if (!m_Visuals.Remove(entity, out VisualInstance visual)) continue;
            if (visual?.BossWarning != null) Object.Destroy(visual.BossWarning.gameObject);
            if (visual?.SpawnPortal != null) Object.Destroy(visual.SpawnPortal.gameObject);
            if (visual?.GameObject != null)
            {
                MobDeathProceduralAnimator death = visual.GameObject.AddComponent<MobDeathProceduralAnimator>();
                death.Begin(visual.IsDogMutant, entity.Index);
            }
        }
    }

    protected override void OnDestroy()
    {
        foreach (VisualInstance visual in m_Visuals.Values)
        {
            if (visual?.BossWarning != null) Object.Destroy(visual.BossWarning.gameObject);
            if (visual?.SpawnPortal != null) Object.Destroy(visual.SpawnPortal.gameObject);
            if (visual?.GameObject != null) Object.Destroy(visual.GameObject);
        }
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

    private static void ApplyEliteAppearance(GameObject visualObject, EliteModifierKind kind)
    {
        Color tint = kind switch
        {
            EliteModifierKind.Bulwark => new Color(0.32f, 0.58f, 0.8f),
            EliteModifierKind.Frenzied => new Color(1f, 0.2f, 0.12f),
            EliteModifierKind.Colossus => new Color(0.95f, 0.55f, 0.12f),
            EliteModifierKind.Revenant => new Color(0.25f, 1f, 0.35f),
            _ => Color.white,
        };
        MaterialPropertyBlock block = new();
        block.SetColor("_BaseColor", tint);
        block.SetColor("_Color", tint);
        foreach (Renderer renderer in visualObject.GetComponentsInChildren<Renderer>(true))
            renderer.SetPropertyBlock(block);
        visualObject.name = $"{kind} Elite - {visualObject.name}";
    }

    private static void ApplyBossPlaceholderAppearance(GameObject visualObject)
    {
        MaterialPropertyBlock block = new();
        Color tint = new(0.58f, 0.16f, 0.72f);
        block.SetColor("_BaseColor", tint);
        block.SetColor("_Color", tint);
        foreach (Renderer renderer in visualObject.GetComponentsInChildren<Renderer>(true))
            renderer.SetPropertyBlock(block);
        visualObject.name = "BOSS Placeholder - " + visualObject.name;
    }

    private static void UpdateBossWarning(VisualInstance visual, LocalTransform transform, BossShockwave shockwave)
    {
        if (visual.BossWarning == null)
        {
            GameObject warning = new("Boss Shockwave Warning", typeof(LineRenderer));
            warning.transform.SetParent(visual.GameObject.transform.parent, false);
            visual.BossWarning = warning.GetComponent<LineRenderer>();
            visual.BossWarning.loop = true;
            visual.BossWarning.useWorldSpace = true;
            visual.BossWarning.positionCount = 48;
            visual.BossWarning.startWidth = 0.1f;
            visual.BossWarning.endWidth = 0.1f;
            visual.BossWarning.material = new Material(Shader.Find("Sprites/Default"));
            visual.BossWarning.startColor = new Color(1f, 0.08f, 0.03f, 0.9f);
            visual.BossWarning.endColor = visual.BossWarning.startColor;
        }
        bool active = shockwave.IsWarning != 0;
        visual.BossWarning.gameObject.SetActive(active);
        if (!active) return;
        for (int point = 0; point < visual.BossWarning.positionCount; point++)
        {
            float angle = point * math.PI * 2f / visual.BossWarning.positionCount;
            visual.BossWarning.SetPosition(point, new Vector3(
                transform.Position.x + math.cos(angle) * shockwave.Radius,
                transform.Position.y + 0.08f,
                transform.Position.z + math.sin(angle) * shockwave.Radius));
        }
    }

    private static void UpdateSpawnPortal(VisualInstance visual, LocalTransform transform, SpawnEmergence emergence)
    {
        if (visual.SpawnPortal == null)
        {
            GameObject portal = new("Enemy Spawn Portal", typeof(LineRenderer));
            portal.transform.SetParent(visual.GameObject.transform.parent, false);
            visual.SpawnPortal = portal.GetComponent<LineRenderer>();
            visual.SpawnPortal.loop = true;
            visual.SpawnPortal.useWorldSpace = true;
            visual.SpawnPortal.positionCount = 40;
            visual.SpawnPortal.startWidth = 0.14f;
            visual.SpawnPortal.endWidth = 0.14f;
            visual.SpawnPortal.material = new Material(Shader.Find("Sprites/Default"));
            visual.SpawnPortal.startColor = new Color(0.55f, 0.08f, 1f, 0.95f);
            visual.SpawnPortal.endColor = new Color(0.05f, 0.8f, 1f, 0.9f);
        }
        float progress = Mathf.Clamp01(emergence.Elapsed / Mathf.Max(0.01f, emergence.Duration));
        float eased = progress * progress * (3f - 2f * progress);
        visual.GameObject.transform.localScale *= Mathf.Lerp(0.05f, 1f, eased);
        float radius = transform.Scale * Mathf.Lerp(1.4f, 0.45f, eased);
        visual.SpawnPortal.startWidth = visual.SpawnPortal.endWidth = Mathf.Lerp(0.2f, 0.04f, progress);
        Color color = Color.Lerp(new Color(0.55f, 0.08f, 1f, 0.95f), new Color(0.05f, 0.8f, 1f, 0f), progress);
        visual.SpawnPortal.startColor = visual.SpawnPortal.endColor = color;
        for (int point = 0; point < visual.SpawnPortal.positionCount; point++)
        {
            float angle = point * math.PI * 2f / visual.SpawnPortal.positionCount + progress * 4f;
            visual.SpawnPortal.SetPosition(point, new Vector3(
                transform.Position.x + math.cos(angle) * radius,
                transform.Position.y + 0.06f,
                transform.Position.z + math.sin(angle) * radius));
        }
        visual.SpawnPortal.gameObject.SetActive(true);
    }

    private static void FinishSpawnPortal(VisualInstance visual)
    {
        if (visual.SpawnPortal == null) return;
        Material material = visual.SpawnPortal.material;
        Object.Destroy(visual.SpawnPortal.gameObject);
        if (material != null) Object.Destroy(material);
        visual.SpawnPortal = null;
    }

    private void UpdateAttackAnimation(VisualInstance visual, bool attacking)
    {
        if (visual.IsAttacking == attacking || visual.OverrideController == null ||
            visual.LocomotionClip == null || m_Settings.ZombieAttackClip == null)
            return;

        visual.IsAttacking = attacking;
        visual.OverrideController[visual.LocomotionClip.name] = attacking
            ? m_Settings.ZombieAttackClip
            : visual.LocomotionClip;
        visual.Animator.Play("Walk", 0, 0f);
        visual.Animator.Update(0f);
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
