using UnityEngine;

public sealed class PlayerGunplayAnimator : MonoBehaviour
{
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int GunplayState = Animator.StringToHash("Base Layer.Gunplay");
    [SerializeField] private RuntimeAnimatorController m_Controller;
    [SerializeField] private RuntimeAnimatorController[] m_ArchetypeControllers;
    private Animator m_Animator;
    private GunArchetype m_CurrentArchetype = GunArchetype.Pistol;

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

    public void SetWeaponArchetype(GunArchetype archetype)
    {
        m_CurrentArchetype = archetype;
        ResolveAnimator();
        if (m_Animator == null) return;

        RuntimeAnimatorController controller = GetController(archetype);
        if (controller != null && m_Animator.runtimeAnimatorController != controller)
            m_Animator.runtimeAnimatorController = controller;

        if (m_Animator.runtimeAnimatorController != null && m_Animator.HasState(0, IdleState))
        {
            m_Animator.Play(IdleState, 0, 0f);
            m_Animator.Update(0f);
        }
    }

    private void ResolveAnimator()
    {
        if (m_Animator == null)
        {
            CharacterLogic character = GetComponent<CharacterLogic>();
            Transform visual = character != null ? character.AimTransform : transform;
            m_Animator = visual.GetComponentInChildren<Animator>(true);
            if (m_Animator == null && m_Controller != null)
                m_Animator = visual.gameObject.AddComponent<Animator>();
        }

        if (m_Animator == null) return;

        RuntimeAnimatorController controller = GetController(m_CurrentArchetype);
        if (m_Animator.runtimeAnimatorController == null && controller != null)
            m_Animator.runtimeAnimatorController = controller;
        m_Animator.applyRootMotion = false;
    }

    private RuntimeAnimatorController GetController(GunArchetype archetype)
    {
        int index = (int)archetype;
        if (m_ArchetypeControllers != null && index >= 0 && index < m_ArchetypeControllers.Length &&
            m_ArchetypeControllers[index] != null)
            return m_ArchetypeControllers[index];
        return m_Controller;
    }
}
