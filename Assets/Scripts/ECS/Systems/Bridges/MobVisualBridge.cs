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
    private const float AttackTransitionDuration = 0.12f;

    private sealed class VisualInstance
    {
        public GameObject GameObject;
        public Animator Animator;
        public float LoopDuration;
        public float DistancePerLoop;
        public float GroundOffset;
        public AnimationClip LocomotionClip;
        public AnimationClip AttackClip;
        public bool IsAttacking;
        public bool SupportsAttack;
        public float ScaleMultiplier;
        public LineRenderer BossWarning;
        public LineRenderer SpawnPortal;
        public DogAttackProceduralAnimator DogAttackAnimator;
        public bool IsDogMutant;
        public bool Exploded;
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
                MobVisualKind visualKind = EntityManager.HasComponent<MobVisualVariant>(entity)
                    ? EntityManager.GetComponentData<MobVisualVariant>(entity).Kind
                    : MobVisualKind.Zombie;
                bool isDogMutant = visualKind == MobVisualKind.DogMutant;
                bool isBoss = mobs[i].EnemyType == EnemyType.Boss;
                int zombieVariantIndex = SelectIndex(entity, m_Settings.ZombieVisualPrefabs, 0x9e3779b9u);
                GameObject zombiePrefab = zombieVariantIndex >= 0
                    ? m_Settings.ZombieVisualPrefabs[zombieVariantIndex]
                    : m_Settings.VisualPrefab;
                GameObject specialPrefab = GetSpecialPrefab(visualKind);
                GameObject prefab = specialPrefab != null ? specialPrefab :
                    isBoss && m_Settings.BossVisualPrefab != null ? m_Settings.BossVisualPrefab :
                    isDogMutant && m_Settings.DogMutantVisualPrefab != null ? m_Settings.DogMutantVisualPrefab : zombiePrefab;
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
                visualObject.name = $"{visualKind} Visual ({entity.Index}:{entity.Version})";
                RemoveImportedHelperObjects(visualObject);
                if (EntityManager.HasComponent<EliteModifier>(entity))
                    ApplyEliteAppearance(visualObject, EntityManager.GetComponentData<EliteModifier>(entity).Kind);

                Animator animator = visualObject.GetComponentInChildren<Animator>(true);
                Avatar avatar = GetSpecialAvatar(visualKind);
                if (avatar == null && zombieVariantIndex >= 0 && m_Settings.ZombieVisualAvatars != null &&
                    zombieVariantIndex < m_Settings.ZombieVisualAvatars.Length)
                    avatar = m_Settings.ZombieVisualAvatars[zombieVariantIndex];
                if (animator == null && !isDogMutant)
                    animator = visualObject.AddComponent<Animator>();
                if (animator != null && avatar != null)
                    animator.avatar = avatar;
                AnimationClip controllerBaseClip = controller != null && controller.animationClips.Length > 0
                    ? controller.animationClips[0]
                    : null;
                AnimationClip locomotionClip = isDogMutant
                    ? controllerBaseClip
                    : GetSpecialLocomotionClip(visualKind) ??
                      SelectClip(entity, m_Settings.ZombieLocomotionClips, 0x85ebca6bu);
                AnimationClip attackClip = isDogMutant
                    ? null
                    : GetSpecialAttackClip(visualKind) ??
                      SelectClip(entity, m_Settings.ZombieAttackClips, 0xc2b2ae35u);
                AnimationClip locomotionSlotClip = !isDogMutant && m_Settings.AnimatorLocomotionSlotClip != null
                    ? m_Settings.AnimatorLocomotionSlotClip
                    : controllerBaseClip;
                AnimationClip attackSlotClip = !isDogMutant ? m_Settings.AnimatorAttackSlotClip : null;
                if (!isDogMutant && attackClip == null)
                    attackClip = m_Settings.ZombieAttackClip;
                if (animator != null)
                {
                    if (controller != null)
                    {
                        if (isDogMutant)
                            animator.runtimeAnimatorController = controller;
                        else
                        {
                            AnimatorOverrideController overrideController = new(controller);
                            animator.runtimeAnimatorController = overrideController;
                            if (locomotionSlotClip != null && locomotionClip != null)
                                overrideController[locomotionSlotClip.name] = locomotionClip;
                            if (attackSlotClip != null && attackClip != null)
                                overrideController[attackSlotClip.name] = attackClip;
                        }
                    }
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                visual = new VisualInstance
                {
                    GameObject = visualObject,
                    Animator = animator,
                    LoopDuration = !isDogMutant && locomotionClip != null
                        ? locomotionClip.length
                        : loopDuration,
                    DistancePerLoop = distancePerLoop,
                    GroundOffset = CalculateGroundOffset(visualObject) +
                                   (isBoss ? m_Settings.BossGroundOffset : isDogMutant ? m_Settings.DogMutantGroundOffset : m_Settings.ZombieGroundOffset),
                    LocomotionClip = locomotionClip != null
                        ? locomotionClip
                        : controller != null && controller.animationClips.Length > 0 ? controller.animationClips[0] : null,
                    AttackClip = attackClip,
                    SupportsAttack = !isDogMutant,
                    ScaleMultiplier = isDogMutant ? 1f : specialPrefab != null
                        ? Mathf.Max(0.1f, m_Settings.SpecialZombieScale)
                        : Mathf.Max(0.1f, m_Settings.HumanoidScale),
                    IsDogMutant = isDogMutant,
                };
                if (isDogMutant)
                {
                    visual.DogAttackAnimator = visualObject.AddComponent<DogAttackProceduralAnimator>();
                    visual.DogAttackAnimator.Initialize();
                }
                m_Visuals.Add(entity, visual);
                if (isBoss && specialPrefab == null && m_Settings.BossVisualPrefab == null)
                    ApplyBossPlaceholderAppearance(visualObject);
                activateAfterTransform = true;
            }

            ApplyTransform(
                visual.GameObject.transform,
                transforms[i],
                visual.GroundOffset,
                visual.ScaleMultiplier);
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
                if (attacking && visual.AttackClip != null)
                    visual.Animator.speed = visual.AttackClip.length /
                                            math.max(0.05f, attacks[i].AttackInterval);
                else if (dogAttacking)
                    // The mutant dog has a locomotion-only controller. Freeze that
                    // controller while the procedural bite owns the attack pose so
                    // the legs do not keep playing Run while the dog is stationary.
                    visual.Animator.speed = 0f;
                else if (!emerging && movementLocked && !visual.IsDogMutant)
                    visual.Animator.speed = 0f;
                else
                {
                    float loopDuration = math.max(0.01f, visual.LoopDuration);
                    float loopDistance = math.max(0.01f, visual.DistancePerLoop);
                    visual.Animator.speed = math.max(0f, movers[i].moveSpeed * loopDuration / loopDistance);
                }
                EnsureLocomotionLoops(visual.Animator);
            }
            if (attacks[i].HasExploded != 0)
            {
                visual.Exploded = true;
                visual.GameObject.SetActive(false);
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
                if (visual.Exploded)
                    Object.Destroy(visual.GameObject);
                else
                {
                    MobDeathProceduralAnimator death = visual.GameObject.AddComponent<MobDeathProceduralAnimator>();
                    death.Begin(
                        visual.IsDogMutant,
                        entity.Index,
                        m_Settings != null ? m_Settings.CorpseStayDuration : 6f,
                        m_Settings != null ? m_Settings.CorpseCleanupDuration : 1.25f,
                        m_Settings != null ? m_Settings.MaxVisibleCorpses : 32,
                        m_Settings != null ? m_Settings.CorpseGroundLift : 0.04f);
                }
            }
        }
    }

    private GameObject GetSpecialPrefab(MobVisualKind kind) => kind switch
    {
        MobVisualKind.ZombieFat => m_Settings.ZombieFatVisualPrefab,
        MobVisualKind.ZombieSquat => m_Settings.ZombieSquatVisualPrefab,
        MobVisualKind.ZombieTank => m_Settings.ZombieTankVisualPrefab,
        MobVisualKind.ZombieWitch => m_Settings.ZombieWitchVisualPrefab,
        _ => null,
    };

    private AnimationClip GetSpecialLocomotionClip(MobVisualKind kind) => kind switch
    {
        MobVisualKind.ZombieFat => m_Settings.ZombieFatLocomotionClip,
        MobVisualKind.ZombieSquat => m_Settings.ZombieSquatLocomotionClip,
        MobVisualKind.ZombieTank => m_Settings.ZombieTankLocomotionClip,
        MobVisualKind.ZombieWitch => m_Settings.ZombieWitchLocomotionClip,
        _ => null,
    };

    private AnimationClip GetSpecialAttackClip(MobVisualKind kind) => kind switch
    {
        MobVisualKind.ZombieFat => m_Settings.ZombieFatAttackClip,
        MobVisualKind.ZombieSquat => m_Settings.ZombieSquatAttackClip,
        MobVisualKind.ZombieTank => m_Settings.ZombieTankAttackClip,
        MobVisualKind.ZombieWitch => m_Settings.ZombieWitchAttackClip,
        _ => null,
    };

    private Avatar GetSpecialAvatar(MobVisualKind kind) => kind switch
    {
        MobVisualKind.ZombieWitch => m_Settings.ZombieWitchAvatar,
        _ => null,
    };

    private static void RemoveImportedHelperObjects(GameObject root)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = descendants.Length - 1; i >= 0; i--)
        {
            Transform item = descendants[i];
            if (item == root.transform) continue;
            if (item.name == "CharacterTrigger" || item.name == "Prefab_Minimap_Enemy" ||
                item.name == "Prefab_Minimap_Enemy_Corpse")
                Object.Destroy(item.gameObject);
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
        if (visual.IsAttacking == attacking || visual.Animator == null ||
            visual.LocomotionClip == null || visual.AttackClip == null)
            return;

        visual.IsAttacking = attacking;
        // Both overrides are assigned once when the visual is spawned. Switching
        // fixed states here avoids rebinding the humanoid avatar (and therefore
        // cannot replace or reset the selected visual mesh during an attack).
        visual.Animator.CrossFadeInFixedTime(attacking ? "Attack" : "Walk",
            AttackTransitionDuration, 0, 0f);
    }

    private static int SelectIndex(Entity entity, GameObject[] values, uint salt)
    {
        if (values == null || values.Length == 0)
            return -1;

        uint seed = math.hash(new uint3((uint)entity.Index, (uint)entity.Version, salt));
        int start = (int)(seed % (uint)values.Length);
        for (int offset = 0; offset < values.Length; offset++)
        {
            int index = (start + offset) % values.Length;
            if (values[index] != null)
                return index;
        }
        return -1;
    }

    private static AnimationClip SelectClip(Entity entity, AnimationClip[] clips, uint salt)
    {
        if (clips == null || clips.Length == 0)
            return null;

        uint seed = math.hash(new uint3((uint)entity.Index, (uint)entity.Version, salt));
        int start = (int)(seed % (uint)clips.Length);
        for (int offset = 0; offset < clips.Length; offset++)
        {
            AnimationClip clip = clips[(start + offset) % clips.Length];
            if (clip != null)
                return clip;
        }
        return null;
    }

    private static void ApplyTransform(
        Transform target,
        LocalTransform source,
        float groundOffset,
        float scaleMultiplier)
    {
        float3 groundedPosition = source.Position;
        groundedPosition.y += groundOffset * source.Scale;
        target.SetPositionAndRotation(groundedPosition, source.Rotation);
        target.localScale = Vector3.one * (source.Scale * scaleMultiplier);
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
