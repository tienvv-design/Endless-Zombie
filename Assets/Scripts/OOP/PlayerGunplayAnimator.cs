using UnityEngine;

public sealed class PlayerGunplayAnimator : MonoBehaviour
{
    private static readonly int GunplayState = Animator.StringToHash("Base Layer.Gunplay");
    [SerializeField] private RuntimeAnimatorController m_Controller;
    private Animator m_Animator;

    private void Awake()
    {
        ResolveAnimator();
    }

    public void PlayShot()
    {
        ResolveAnimator();
        if (m_Animator == null || m_Animator.runtimeAnimatorController == null ||
            !m_Animator.HasState(0, GunplayState))
            return;

        // Restart from the beginning for every trigger pull. This also keeps
        // automatic weapons visually synchronized with their actual fire rate.
        m_Animator.Play(GunplayState, 0, 0f);
        m_Animator.Update(0f);
    }

    private void ResolveAnimator()
    {
        if (m_Animator != null) return;
        CharacterLogic character = GetComponent<CharacterLogic>();
        Transform visual = character != null ? character.AimTransform : transform;
        m_Animator = visual.GetComponentInChildren<Animator>(true);
        if (m_Animator == null && m_Controller != null)
            m_Animator = visual.gameObject.AddComponent<Animator>();
        if (m_Animator != null)
        {
            if (m_Controller != null && m_Animator.runtimeAnimatorController != m_Controller)
                m_Animator.runtimeAnimatorController = m_Controller;
            m_Animator.applyRootMotion = false;
        }
    }
}
