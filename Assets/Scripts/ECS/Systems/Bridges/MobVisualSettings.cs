using UnityEngine;

[CreateAssetMenu(menuName = "Endless Zombie/Mob Visual Settings")]
public sealed class MobVisualSettings : ScriptableObject
{
    public GameObject VisualPrefab;
    public RuntimeAnimatorController AnimatorController;
    [Tooltip("Duration in seconds of one complete walk animation loop at Animator speed 1.")]
    [Min(0.01f)] public float WalkLoopDuration = 2f;
    [Tooltip("World-space distance the zombie should travel during one complete walk loop.")]
    [Min(0.01f)] public float DistancePerWalkLoop = 2f;
    [Tooltip("Additional vertical correction after automatically aligning the zombie's lowest renderer point to the map surface.")]
    public float ZombieGroundOffset;

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
