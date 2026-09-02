using UnityEngine;

public sealed class DogAttackProceduralAnimator : MonoBehaviour
{
    private Transform m_Jaw;
    private Transform m_Neck;
    private Transform m_UpperSpine;
    private Transform m_MidSpine;
    private Transform m_LowerSpine;
    private Transform m_Hips;
    private Transform m_FrontLegL, m_FrontLegR, m_BackLegL, m_BackLegR;
    private Quaternion m_JawBase, m_NeckBase, m_UpperSpineBase, m_MidSpineBase, m_LowerSpineBase, m_HipsBase;
    private Quaternion m_FrontLegLBase, m_FrontLegRBase, m_BackLegLBase, m_BackLegRBase;
    private bool m_Attacking;
    private bool m_PoseCaptured;
    private float m_Progress;

    public void Initialize()
    {
        m_Jaw = FindBone(transform, "head jaw");
        m_Neck = FindBone(transform, "head neck.001");
        m_UpperSpine = FindBone(transform, "spine up 2");
        m_MidSpine = FindBone(transform, "spine mid");
        m_LowerSpine = FindBone(transform, "spine lower");
        m_Hips = FindBone(transform, "hips");
        m_FrontLegL = FindBone(transform, "leg front 1_L");
        m_FrontLegR = FindBone(transform, "leg front 1_R");
        m_BackLegL = FindBone(transform, "Leg back 1_L");
        m_BackLegR = FindBone(transform, "Leg back 1_R");
    }

    public void SetAttack(bool attacking, float progress)
    {
        if (attacking && !m_Attacking)
        {
            m_PoseCaptured = false;
            AudioManager.Instance?.Play(SoundLabel.DogAttackSound);
        }
        m_Attacking = attacking;
        m_Progress = Mathf.Repeat(progress, 1f);
    }

    private void LateUpdate()
    {
        if (!m_Attacking) return;
        if (!m_PoseCaptured)
            CaptureBasePose();
        ApplyPose();
    }

#if UNITY_EDITOR
    public void EvaluatePreview(float progress)
    {
        if (m_Jaw == null && m_Neck == null)
            Initialize();
        m_Progress = Mathf.Repeat(progress, 1f);
        CaptureBasePose();
        ApplyPose();
    }
#endif

    private void CaptureBasePose()
    {
        if (m_Jaw != null) m_JawBase = m_Jaw.localRotation;
        if (m_Neck != null) m_NeckBase = m_Neck.localRotation;
        if (m_UpperSpine != null) m_UpperSpineBase = m_UpperSpine.localRotation;
        if (m_MidSpine != null) m_MidSpineBase = m_MidSpine.localRotation;
        if (m_LowerSpine != null) m_LowerSpineBase = m_LowerSpine.localRotation;
        if (m_Hips != null) m_HipsBase = m_Hips.localRotation;
        if (m_FrontLegL != null) m_FrontLegLBase = m_FrontLegL.localRotation;
        if (m_FrontLegR != null) m_FrontLegRBase = m_FrontLegR.localRotation;
        if (m_BackLegL != null) m_BackLegLBase = m_BackLegL.localRotation;
        if (m_BackLegR != null) m_BackLegRBase = m_BackLegR.localRotation;
        m_PoseCaptured = true;
    }

    private void ApplyPose()
    {
        // MobVisualBridge freezes the locomotion controller for the whole attack
        // window. Apply only the procedural bite on top of the pose captured when
        // the attack began; Run must never continue underneath the attack.

        // 0-45%: crouch and open jaw. 45-62%: snap forward and bite.
        // 62-100%: recover smoothly into the next attack cycle.
        float windup = Smooth01(m_Progress / 0.45f);
        float strike = Smooth01((m_Progress - 0.45f) / 0.17f);
        float recovery = Smooth01((m_Progress - 0.62f) / 0.38f);
        float lunge = Mathf.Clamp01(strike - recovery);
        float tension = Mathf.Clamp01(windup - recovery);
        float jawOpen = Mathf.Clamp01(windup * (1f - strike) + lunge * 0.2f);

        transform.position += transform.forward * (0.52f * lunge - 0.13f * tension);
        transform.position += Vector3.down * (0.2f * tension - 0.05f * lunge);
        if (m_Hips != null)
            m_Hips.localRotation = m_HipsBase * Quaternion.Euler(-13f * tension + 20f * lunge, 0f, 0f);
        if (m_LowerSpine != null)
            m_LowerSpine.localRotation = m_LowerSpineBase * Quaternion.Euler(-15f * tension + 19f * lunge, 0f, 0f);
        if (m_MidSpine != null)
            m_MidSpine.localRotation = m_MidSpineBase * Quaternion.Euler(-18f * tension + 24f * lunge, 0f, 0f);
        if (m_UpperSpine != null)
            m_UpperSpine.localRotation = m_UpperSpineBase * Quaternion.Euler(-22f * tension + 30f * lunge, 0f, 0f);
        if (m_Neck != null)
            m_Neck.localRotation = m_NeckBase * Quaternion.Euler(-20f * tension + 34f * lunge, 0f, 0f);
        if (m_Jaw != null)
            m_Jaw.localRotation = m_JawBase * Quaternion.Euler(38f * jawOpen - 8f * lunge, 0f, 0f);
        // Front legs brace during the windup while the rear legs push the
        // whole body into the bite. A small opposite roll keeps it organic.
        if (m_FrontLegL != null)
            m_FrontLegL.localRotation = m_FrontLegLBase * Quaternion.Euler(18f * tension - 13f * lunge, 0f, -5f * tension);
        if (m_FrontLegR != null)
            m_FrontLegR.localRotation = m_FrontLegRBase * Quaternion.Euler(18f * tension - 13f * lunge, 0f, 5f * tension);
        if (m_BackLegL != null)
            m_BackLegL.localRotation = m_BackLegLBase * Quaternion.Euler(-16f * tension + 24f * lunge, 0f, 3f * lunge);
        if (m_BackLegR != null)
            m_BackLegR.localRotation = m_BackLegRBase * Quaternion.Euler(-16f * tension + 24f * lunge, 0f, -3f * lunge);
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static Transform FindBone(Transform root, string boneName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(child.name.Trim(), boneName, System.StringComparison.OrdinalIgnoreCase)) return child;
        return null;
    }
}
