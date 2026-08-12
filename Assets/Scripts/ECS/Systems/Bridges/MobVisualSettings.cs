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
}
