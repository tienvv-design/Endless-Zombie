using UnityEngine;

public sealed class MobDeathProceduralAnimator : MonoBehaviour
{
    private bool m_Dog;
    private float m_Time;
    private float m_Duration;
    private Vector3 m_StartPosition;
    private Quaternion m_StartRotation;
    private Vector3 m_StartScale;
    private float m_FallSide;
    private Transform m_Hips, m_Spine, m_Head, m_FrontLegL, m_FrontLegR;
    private Quaternion m_HipsBase, m_SpineBase, m_HeadBase, m_FrontLegLBase, m_FrontLegRBase;

    public void Begin(bool dog, int variationSeed)
    {
        m_Dog = dog;
        m_Duration = dog ? 0.9f : 1.15f;
        m_FallSide = (variationSeed & 1) == 0 ? -1f : 1f;
        m_StartPosition = transform.position;
        m_StartRotation = transform.rotation;
        m_StartScale = transform.localScale;
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null) animator.enabled = false;

        m_Hips = dog ? FindBone("hips") : FindBone("Z_Hip", "Base HumanPelvis");
        m_Spine = dog ? FindBone("spine mid") : FindBone("Base HumanSpine1", "Z_Body");
        m_Head = dog ? FindBone("head") : FindBone("Z_Head", "Base HumanHead");
        if (dog)
        {
            m_FrontLegL = FindBone("leg front 1_L");
            m_FrontLegR = FindBone("leg front 1_R");
        }
        CapturePose();
    }

    private void Update()
    {
        m_Time += Time.deltaTime;
        float t = Mathf.Clamp01(m_Time / Mathf.Max(0.05f, m_Duration));
        float fall = Smooth01(Mathf.Clamp01(t / 0.68f));
        float settle = Smooth01(Mathf.Clamp01((t - 0.68f) / 0.32f));

        if (m_Dog) AnimateDog(fall, settle);
        else AnimateZombie(fall, settle);

        if (t >= 1f) Destroy(gameObject);
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
        transform.rotation = m_StartRotation * Quaternion.Euler(34f * fall, 0f, m_FallSide * 38f * fall);
        transform.position = m_StartPosition + transform.forward * (0.16f * fall) +
                             Vector3.down * (0.25f * fall + 0.12f * settle);
        transform.localScale = m_StartScale * Mathf.Lerp(1f, 0.82f, settle);
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
