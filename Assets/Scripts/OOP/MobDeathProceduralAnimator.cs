using System.Collections.Generic;
using UnityEngine;

public sealed class MobDeathProceduralAnimator : MonoBehaviour
{
    private static readonly LinkedList<MobDeathProceduralAnimator> s_ActiveCorpses = new();

    private bool m_Dog;
    private float m_Time;
    private float m_FallDuration;
    private float m_StayDuration;
    private float m_CleanupDuration;
    private float m_GroundHeight;
    private float m_GroundLift;
    private bool m_ForcedCleanup;
    private LinkedListNode<MobDeathProceduralAnimator> m_CorpseNode;
    private Vector3 m_StartPosition;
    private Quaternion m_StartRotation;
    private Vector3 m_StartScale;
    private float m_FallSide;
    private Transform m_Hips, m_Spine, m_Head, m_FrontLegL, m_FrontLegR;
    private Quaternion m_HipsBase, m_SpineBase, m_HeadBase, m_FrontLegLBase, m_FrontLegRBase;

    public void Begin(bool dog, int variationSeed)
    {
        Begin(dog, variationSeed, 6f, 1.25f, 32, 0.04f);
    }

    public void Begin(bool dog, int variationSeed, float stayDuration, float cleanupDuration,
        int maxVisibleCorpses, float groundLift = 0.04f)
    {
        m_Dog = dog;
        m_FallDuration = dog ? 0.9f : 1.15f;
        m_StayDuration = Mathf.Max(0f, stayDuration);
        m_CleanupDuration = Mathf.Max(0.05f, cleanupDuration);
        m_FallSide = (variationSeed & 1) == 0 ? -1f : 1f;
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;
        m_StartScale = transform.localScale;
        m_GroundHeight = CalculateLowestRendererPoint();
        m_GroundLift = groundLift;
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null) animator.enabled = false;
        // The dog attack layer applies additive root movement in LateUpdate.
        // It must stop immediately or it keeps lunging the corpse every frame.
        DogAttackProceduralAnimator dogAttack = GetComponent<DogAttackProceduralAnimator>();
        if (dogAttack != null) dogAttack.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        m_Hips = dog ? FindBone("hips") : FindBone("Z_Hip", "Base HumanPelvis");
        m_Spine = dog ? FindBone("spine mid") : FindBone("Base HumanSpine1", "Z_Body");
        m_Head = dog ? FindBone("head") : FindBone("Z_Head", "Base HumanHead");
        if (dog)
        {
            m_FrontLegL = FindBone("leg front 1_L");
            m_FrontLegR = FindBone("leg front 1_R");
        }
        CapturePose();

        m_CorpseNode = s_ActiveCorpses.AddLast(this);
        int corpseLimit = Mathf.Max(1, maxVisibleCorpses);
        while (s_ActiveCorpses.Count > corpseLimit)
        {
            MobDeathProceduralAnimator oldest = s_ActiveCorpses.First.Value;
            s_ActiveCorpses.RemoveFirst();
            oldest.m_CorpseNode = null;
            oldest.m_ForcedCleanup = true;
        }
    }

    private void Update()
    {
        m_Time += Time.deltaTime;
        float t = Mathf.Clamp01(m_Time / Mathf.Max(0.05f, m_FallDuration));
        float fall = Smooth01(Mathf.Clamp01(t / 0.68f));
        float settle = Smooth01(Mathf.Clamp01((t - 0.68f) / 0.32f));

        if (m_Dog) AnimateDog(fall, settle);
        else AnimateZombie(fall, settle);
        KeepCorpseOnSurface();

        float cleanupStart = m_ForcedCleanup ? m_FallDuration : m_FallDuration + m_StayDuration;
        if (m_Time < cleanupStart) return;

        float cleanup = Smooth01(Mathf.Clamp01((m_Time - cleanupStart) / m_CleanupDuration));
        // Opaque imported zombie materials cannot reliably alpha-fade. Sinking and
        // shrinking is consistent across all LDoE shaders and keeps cleanup cheap.
        // Keep this offset absolute. Adding it every frame made corpses accelerate
        // downward and disappear under the map almost immediately.
        transform.position += Vector3.down * (0.28f * cleanup);
        transform.localScale *= Mathf.Lerp(1f, 0.15f, cleanup);
        if (cleanup >= 1f) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (m_CorpseNode != null)
        {
            s_ActiveCorpses.Remove(m_CorpseNode);
            m_CorpseNode = null;
        }
    }

    private void AnimateZombie(float fall, float settle)
    {
        // Knees/body lose strength first, then the whole corpse rolls sideways.
        transform.rotation = m_StartRotation * Quaternion.Euler(18f * fall, 0f, m_FallSide * 82f * fall);
        transform.position = m_StartPosition + Vector3.down * (0.34f * fall + 0.1f * settle);
        transform.localScale = m_StartScale * Mathf.Lerp(1f, 0.86f, settle);
        SetAdditive(m_Hips, m_HipsBase, new Vector3(24f * fall, 0f, 8f * m_FallSide * fall));
        SetAdditive(m_Spine, m_SpineBase, new Vector3(-18f * fall, 0f, 12f * m_FallSide * fall));
        SetAdditive(m_Head, m_HeadBase, new Vector3(12f * fall, 0f, -14f * m_FallSide * fall));
    }

    private void AnimateDog(float fall, float settle)
    {
        // Front legs buckle and the head hits the ground before the torso rolls.
        transform.rotation = m_StartRotation * Quaternion.Euler(18f * fall, 0f, m_FallSide * 24f * fall);
        transform.position = m_StartPosition + Vector3.down * (0.14f * fall + 0.07f * settle);
        transform.localScale = m_StartScale * Mathf.Lerp(1f, 0.9f, settle);
        SetAdditive(m_FrontLegL, m_FrontLegLBase, new Vector3(48f * fall, 0f, -8f * fall));
        SetAdditive(m_FrontLegR, m_FrontLegRBase, new Vector3(48f * fall, 0f, 8f * fall));
        SetAdditive(m_Hips, m_HipsBase, new Vector3(-20f * fall, 0f, 10f * m_FallSide * fall));
        SetAdditive(m_Spine, m_SpineBase, new Vector3(26f * fall, 0f, 14f * m_FallSide * fall));
        SetAdditive(m_Head, m_HeadBase, new Vector3(35f * fall, 0f, -10f * m_FallSide * fall));
    }

    private void CapturePose()
    {
        if (m_Hips != null) m_HipsBase = m_Hips.localRotation;
        if (m_Spine != null) m_SpineBase = m_Spine.localRotation;
        if (m_Head != null) m_HeadBase = m_Head.localRotation;
        if (m_FrontLegL != null) m_FrontLegLBase = m_FrontLegL.localRotation;
        if (m_FrontLegR != null) m_FrontLegRBase = m_FrontLegR.localRotation;
    }

    private void KeepCorpseOnSurface()
    {
        float lowestPoint = CalculateLowestRendererPoint();
        if (float.IsNaN(lowestPoint) || float.IsNaN(m_GroundHeight)) return;
        float desiredLowestPoint = m_GroundHeight + m_GroundLift;
        transform.position += Vector3.up * (desiredLowestPoint - lowestPoint);
    }

    private float CalculateLowestRendererPoint()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return float.NaN;
        float lowestPoint = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            if (renderer.enabled)
                lowestPoint = Mathf.Min(lowestPoint, renderer.bounds.min.y);
        return float.IsPositiveInfinity(lowestPoint) ? float.NaN : lowestPoint;
    }

    private Transform FindBone(params string[] names)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            foreach (string candidate in names)
                if (string.Equals(child.name.Trim(), candidate, System.StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }

    private static void SetAdditive(Transform bone, Quaternion baseline, Vector3 euler)
    {
        if (bone != null) bone.localRotation = baseline * Quaternion.Euler(euler);
    }

    private static float Smooth01(float value) => value * value * (3f - 2f * value);
}
