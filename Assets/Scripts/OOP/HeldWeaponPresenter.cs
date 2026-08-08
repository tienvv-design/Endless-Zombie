using UnityEngine;

public sealed class HeldWeaponPresenter : MonoBehaviour
{
    private GunConfig[] m_GunConfigs;
    private Transform m_Socket;
    private GameObject m_CurrentWeapon;

    private void OnEnable()
    {
        MetaProgression.SelectedWeaponChanged += ShowWeapon;
    }

    private void OnDisable()
    {
        MetaProgression.SelectedWeaponChanged -= ShowWeapon;
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
        WeaponVfxRuntime.CurrentMuzzle = null;

        if (m_GunConfigs == null || index < 0 || index >= m_GunConfigs.Length)
            return;
        GunConfig config = m_GunConfigs[index];
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
        Transform hand = FindHandBone(parent);
        if (hand != null)
            parent = hand;
        Transform existing = parent.Find("HeldWeaponSocket");
        if (existing != null)
        {
            m_Socket = existing;
            return;
        }

        GameObject socket = new("HeldWeaponSocket");
        m_Socket = socket.transform;
        m_Socket.SetParent(parent, false);
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
}
