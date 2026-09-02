using UnityEngine;

[CreateAssetMenu(menuName = "Endless Zombie/Mob Visual Settings")]
public sealed class MobVisualSettings : ScriptableObject
{
    public GameObject VisualPrefab;
    [Tooltip("Random visual pool used by regular zombies. When empty, Visual Prefab is used as a fallback.")]
    public GameObject[] ZombieVisualPrefabs;
    [Tooltip("Humanoid avatar matching each entry in Zombie Visual Prefabs. The arrays use the same index.")]
    public Avatar[] ZombieVisualAvatars;
    public RuntimeAnimatorController AnimatorController;
    [Tooltip("Placeholder clip used by the Walk state in the base zombie controller.")]
    public AnimationClip AnimatorLocomotionSlotClip;
    [Tooltip("Placeholder clip used by the Attack state in the base zombie controller.")]
    public AnimationClip AnimatorAttackSlotClip;
    public AnimationClip ZombieAttackClip;
    [Tooltip("Movement clips selected per spawned zombie. Idle clips are intentionally not supported.")]
    public AnimationClip[] ZombieLocomotionClips;
    [Tooltip("Attack clips selected per spawned zombie. Zombie Attack Clip is the fallback.")]
    public AnimationClip[] ZombieAttackClips;
    [Tooltip("Fallback duration for a zombie locomotion loop. Imported clips use their actual clip length.")]
    [Min(0.01f)] public float WalkLoopDuration = 2f;
    [Tooltip("World-space distance the zombie should travel during one complete walk loop.")]
    [Min(0.01f)] public float DistancePerWalkLoop = 2f;
    [Tooltip("Additional vertical correction after automatically aligning the zombie's lowest renderer point to the map surface.")]
    public float ZombieGroundOffset;
    [Tooltip("Uniform runtime scale for humanoid zombies and humanoid bosses. Dog mutants are unaffected.")]
    [Range(0.1f, 2f)] public float HumanoidScale = 0.7f;

    [Header("Corpse Persistence")]
    [Tooltip("Seconds a defeated zombie remains on the ground after its fall animation.")]
    [Min(0f)] public float CorpseStayDuration = 6f;
    [Tooltip("Small world-space lift keeping fallen bodies visibly above the map surface.")]
    [Range(0f, 0.25f)] public float CorpseGroundLift = 0.04f;
    [Tooltip("Seconds used to sink and shrink a corpse before it is removed.")]
    [Min(0.05f)] public float CorpseCleanupDuration = 1.25f;
    [Tooltip("Oldest corpses are removed first when this limit is exceeded.")]
    [Range(1, 100)] public int MaxVisibleCorpses = 32;

    [Header("Boss (optional - zombie is used as placeholder)")]
    public GameObject BossVisualPrefab;
    public RuntimeAnimatorController BossAnimatorController;
    public float BossGroundOffset;

    [Header("LDoE Special Zombies")]
    public GameObject ZombieFatVisualPrefab;
    public AnimationClip ZombieFatLocomotionClip;
    public AnimationClip ZombieFatAttackClip;
    public GameObject ZombieFatExplosionVfx;
    public AudioClip ZombieFatExplosionSound;
    public GameObject ZombieSquatVisualPrefab;
    public AnimationClip ZombieSquatLocomotionClip;
    public AnimationClip ZombieSquatAttackClip;
    public GameObject ZombieTankVisualPrefab;
    public AnimationClip ZombieTankLocomotionClip;
    public AnimationClip ZombieTankAttackClip;
    public GameObject ZombieWitchVisualPrefab;
    [Tooltip("Humanoid avatar matching the Zombie Witch skeleton.")]
    public Avatar ZombieWitchAvatar;
    public AnimationClip ZombieWitchLocomotionClip;
    public AnimationClip ZombieWitchAttackClip;
    [Range(0.1f, 2f)] public float SpecialZombieScale = 0.7f;

    [Header("Dog Mutant")]
    public GameObject DogMutantVisualPrefab;
    public RuntimeAnimatorController DogMutantAnimatorController;
    [Tooltip("Duration in seconds of one complete Dog Mutant run loop at Animator speed 1.")]
    [Min(0.01f)] public float DogMutantRunLoopDuration = 1f;
    [Tooltip("World-space distance the Dog Mutant should travel during one complete run loop.")]
    [Min(0.01f)] public float DogMutantDistancePerRunLoop = 3f;
    [Tooltip("Additional vertical correction after automatically aligning the mutant dog's lowest renderer point to the map surface.")]
    public float DogMutantGroundOffset;
}
