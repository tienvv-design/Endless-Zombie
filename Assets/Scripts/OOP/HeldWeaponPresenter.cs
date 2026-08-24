using UnityEngine;

[DefaultExecutionOrder(1000)]
public sealed class HeldWeaponPresenter : MonoBehaviour
{
    private GunConfig[] m_GunConfigs;
    private Transform m_Socket;
    private GameObject m_CurrentWeapon;
    private GunConfig m_CurrentConfig;
    private Transform m_Visual;
    private Transform m_Chest;
    private Transform m_LeftUpperArm;
    private Transform m_LeftForearm;
    private Transform m_LeftHand;
    private Transform m_RightUpperArm;
    private Transform m_RightForearm;
    private Transform m_RightHand;

    private void OnEnable()
    {
        MetaProgression.SelectedWeaponChanged += ShowWeapon;
    }

    private void OnDisable()
    {
        MetaProgression.SelectedWeaponChanged -= ShowWeapon;
    }

    private void LateUpdate()
    {
        // The imported character is scaled to gameplay height. Cancel that inherited
        // scale at the hand socket so weapon-pack models retain their intended size
        // while still following the animated hand position and rotation.
        CompensateInheritedScale();
        ApplyPistolPose();
    }

    public void SetGunConfigs(GunConfig[] gunConfigs)
    {
        m_GunConfigs = gunConfigs;
        ShowWeapon(MetaProgression.SelectedWeapon);
    }

    public void ShowWeapon(int index)
    {
        if (m_CurrentWeapon != null)
            Destroy(m_CurrentWeapon);
        m_CurrentWeapon = null;
        m_CurrentConfig = null;
        WeaponVfxRuntime.CurrentMuzzle = null;

        if (m_GunConfigs == null || index < 0 || index >= m_GunConfigs.Length)
            return;
        GunConfig config = m_GunConfigs[index];
        m_CurrentConfig = config;
        WeaponVfxRuntime.CurrentConfig = config;
        if (config == null || config.HeldWeaponPrefab == null)
            return;

        EnsureSocket();
        m_CurrentWeapon = Instantiate(config.HeldWeaponPrefab, m_Socket, false);
        Transform weaponTransform = m_CurrentWeapon.transform;
        weaponTransform.localPosition = config.HeldLocalPosition;
        weaponTransform.localRotation = Quaternion.Euler(config.HeldLocalEulerAngles);
        weaponTransform.localScale = config.HeldLocalScale == Vector3.zero
            ? Vector3.one
            : config.HeldLocalScale;
        WeaponVfxRuntime.CurrentMuzzle = FindMuzzle(weaponTransform);
    }

    private static Transform FindMuzzle(Transform root)
    {
        if (root == null) return null;
        if (root.name.ToLowerInvariant().Contains("muzzle")) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindMuzzle(root.GetChild(i));
            if (found != null) return found;
        }
        return null;
    }

    private void EnsureSocket()
    {
        if (m_Socket != null) return;
        CharacterLogic character = GetComponent<CharacterLogic>();
        Transform parent = character != null ? character.AimTransform : transform;
        m_Visual = parent;
        ResolvePoseBones(parent);
        Transform hand = FindHandBone(parent);
        if (hand != null)
            parent = hand;
        Transform existing = parent.Find("HeldWeaponSocket");
        if (existing != null)
        {
            m_Socket = existing;
            CompensateInheritedScale();
            return;
        }

        GameObject socket = new("HeldWeaponSocket");
        m_Socket = socket.transform;
        m_Socket.SetParent(parent, false);
        CompensateInheritedScale();
    }

    private void CompensateInheritedScale()
    {
        if (m_Socket == null || m_Socket.parent == null) return;
        Vector3 inherited = m_Socket.parent.lossyScale;
        m_Socket.localScale = new Vector3(
            SafeInverse(inherited.x),
            SafeInverse(inherited.y),
            SafeInverse(inherited.z));
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) > 0.0001f ? 1f / Mathf.Abs(value) : 1f;
    }

    private static Transform FindHandBone(Transform root)
    {
        if (root == null) return null;
        string normalized = root.name.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (normalized.Contains("righthand") || normalized.Contains("handr"))
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindHandBone(root.GetChild(i));
            if (found != null) return found;
        }
        return null;
    }

    private void ResolvePoseBones(Transform root)
    {
        m_Chest = FindBone(root, "Spine2") ?? FindBone(root, "Spine1") ?? root;
        m_LeftUpperArm = FindBone(root, "LeftArm");
        m_LeftForearm = FindBone(root, "LeftForeArm");
        m_LeftHand = FindBone(root, "LeftHand");
        m_RightUpperArm = FindBone(root, "RightArm");
        m_RightForearm = FindBone(root, "RightForeArm");
        m_RightHand = FindBone(root, "RightHand");
    }

    private void ApplyPistolPose()
    {
        if (m_CurrentConfig == null || m_CurrentConfig.Archetype != GunArchetype.Pistol ||
            m_Visual == null || m_Chest == null || m_LeftUpperArm == null ||
            m_LeftForearm == null || m_LeftHand == null || m_RightUpperArm == null ||
            m_RightForearm == null || m_RightHand == null)
            return;

        float weight = Mathf.Clamp01(m_CurrentConfig.PistolPoseWeight);
        if (weight <= 0f) return;

        float rightLength = Vector3.Distance(m_RightUpperArm.position, m_RightForearm.position) +
                            Vector3.Distance(m_RightForearm.position, m_RightHand.position);
        float leftLength = Vector3.Distance(m_LeftUpperArm.position, m_LeftForearm.position) +
                           Vector3.Distance(m_LeftForearm.position, m_LeftHand.position);
        float reach = Mathf.Min(rightLength, leftLength) * m_CurrentConfig.PistolArmReach;
        Vector3 forward = m_Visual.forward;
        Vector3 right = m_Visual.right;
        Vector3 up = m_Visual.up;
        Vector3 center = m_Chest.position + forward * reach + up * m_CurrentConfig.PistolPoseHeightOffset;
        float halfSpacing = m_CurrentConfig.PistolHandSpacing * 0.5f;

        Vector3 rightTarget = center + right * halfSpacing;
        ApplyTwoBoneIk(m_RightUpperArm, m_RightForearm, m_RightHand, rightTarget, weight);

        Transform grip = FindNamedChild(m_CurrentWeapon != null ? m_CurrentWeapon.transform : null, "LeftHandGrip");
        Vector3 leftTarget = grip != null
            ? grip.position
            : m_RightHand.position - right * m_CurrentConfig.PistolHandSpacing - forward * 0.025f;
        ApplyTwoBoneIk(m_LeftUpperArm, m_LeftForearm, m_LeftHand, leftTarget, weight);
    }

    private static void ApplyTwoBoneIk(Transform upper, Transform lower, Transform hand,
        Vector3 target, float weight)
    {
        Vector3 shoulder = upper.position;
        Vector3 elbow = lower.position;
        float upperLength = Vector3.Distance(shoulder, elbow);
        float lowerLength = Vector3.Distance(elbow, hand.position);
        Vector3 toTarget = target - shoulder;
        float distance = Mathf.Clamp(toTarget.magnitude, 0.001f,
            Mathf.Max(0.001f, upperLength + lowerLength - 0.001f));
        Vector3 direction = toTarget.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(elbow - shoulder, direction).normalized;
        if (bend.sqrMagnitude < 0.0001f)
            bend = Vector3.ProjectOnPlane(upper.up, direction).normalized;

        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) /
                      (2f * distance);
        float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        Vector3 desiredElbow = shoulder + direction * along + bend * height;

        Quaternion upperTarget = Quaternion.FromToRotation(elbow - shoulder, desiredElbow - shoulder) * upper.rotation;
        upper.rotation = Quaternion.Slerp(upper.rotation, upperTarget, weight);

        Vector3 currentForearm = hand.position - lower.position;
        Vector3 desiredForearm = target - lower.position;
        if (currentForearm.sqrMagnitude > 0.0001f && desiredForearm.sqrMagnitude > 0.0001f)
        {
            Quaternion lowerTarget = Quaternion.FromToRotation(currentForearm, desiredForearm) * lower.rotation;
            lower.rotation = Quaternion.Slerp(lower.rotation, lowerTarget, weight);
        }
    }

    private static Transform FindBone(Transform root, string boneName)
    {
        if (root == null) return null;
        if (string.Equals(root.name, boneName, System.StringComparison.OrdinalIgnoreCase)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindBone(root.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindNamedChild(Transform root, string childName)
    {
        if (root == null) return null;
        if (string.Equals(root.name, childName, System.StringComparison.OrdinalIgnoreCase)) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedChild(root.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }
}
