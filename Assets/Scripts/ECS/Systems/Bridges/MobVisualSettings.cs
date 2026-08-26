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

    [Header("Boss (optional - zombie is used as placeholder)")]
    public GameObject BossVisualPrefab;
    public RuntimeAnimatorController BossAnimatorController;
    public float BossGroundOffset;

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
