using Unity.Entities;
using UnityEngine;

public class RuntimeGunTuner : MonoBehaviour
{
    [Header("Live Gun Base Stats (Play Mode)")]
    [Min(1)] public int Damage = 2;
    [Min(0.01f)] public float ShotsPerSecond = 1.5f;
    [Min(0.1f)] public float AttackRange = 8f;
    [Min(0.1f)] public float ProjectileSpeed = 14f;
    [Min(1)] public int MagazineSize = 12;
    [Min(0.05f)] public float ReloadDuration = 1.2f;
    [Min(1)] public int ProjectileCount = 1;
    [Range(0f, 1f)] public float CriticalChance = 0.1f;
    [Min(1f)] public float CriticalMultiplier = 1.5f;

    private EntityManager m_EntityManager;
    private EntityQuery m_Query;
    private bool m_QueryCreated;
    private bool m_Initialized;
    private int m_LastHash;

    private void Update()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated) return;
        if (!m_QueryCreated)
        {
            m_EntityManager = world.EntityManager;
            m_Query = m_EntityManager.CreateEntityQuery(typeof(WeaponManager));
            m_QueryCreated = true;
        }
        if (m_Query.CalculateEntityCount() != 1) return;

        Entity entity = m_Query.GetSingletonEntity();
        if (!m_Initialized)
        {
            PullFromWeapon(m_EntityManager.GetComponentData<WeaponManager>(entity));
            m_LastHash = CalculateHash();
            m_Initialized = true;
            return;
        }

        int hash = CalculateHash();
        if (hash == m_LastHash) return;
        ApplyToWeapon(entity);
        m_LastHash = hash;
    }

    [ContextMenu("Apply Runtime Gun Stats")]
    public void ApplyNow()
    {
        if (!Application.isPlaying || !m_QueryCreated || m_Query.CalculateEntityCount() != 1) return;
        ApplyToWeapon(m_Query.GetSingletonEntity());
        m_LastHash = CalculateHash();
    }

    private void PullFromWeapon(WeaponManager gun)
    {
        Damage = gun.BaseDamage;
        ShotsPerSecond = gun.BaseShotsPerSecond;
        AttackRange = gun.BaseAttackRange;
        ProjectileSpeed = gun.BaseProjectileSpeed;
        MagazineSize = gun.BaseMagazineSize;
        ReloadDuration = gun.BaseReloadDuration;
        ProjectileCount = gun.BaseProjectileCount;
        CriticalChance = gun.BaseCriticalChance;
        CriticalMultiplier = gun.BaseCriticalDamage;
    }

    private void ApplyToWeapon(Entity entity)
    {
        WeaponManager gun = m_EntityManager.GetComponentData<WeaponManager>(entity);
        gun.BaseDamage = Mathf.Max(1, Damage);
        gun.BaseShotsPerSecond = Mathf.Max(0.01f, ShotsPerSecond);
        gun.BaseAttackRange = Mathf.Max(0.1f, AttackRange);
        gun.BaseProjectileSpeed = Mathf.Max(0.1f, ProjectileSpeed);
        gun.BaseMagazineSize = Mathf.Max(1, MagazineSize);
        gun.BaseReloadDuration = Mathf.Max(0.05f, ReloadDuration);
        gun.BaseProjectileCount = Mathf.Max(1, ProjectileCount);
        gun.BaseCriticalChance = Mathf.Clamp01(CriticalChance);
        gun.BaseCriticalDamage = Mathf.Max(1f, CriticalMultiplier);
        m_EntityManager.SetComponentData(entity, gun);
    }

    private int CalculateHash()
    {
        unchecked
        {
            int hash = Damage;
            hash = hash * 31 + ShotsPerSecond.GetHashCode();
            hash = hash * 31 + AttackRange.GetHashCode();
            hash = hash * 31 + ProjectileSpeed.GetHashCode();
            hash = hash * 31 + MagazineSize;
            hash = hash * 31 + ReloadDuration.GetHashCode();
            hash = hash * 31 + ProjectileCount;
            hash = hash * 31 + CriticalChance.GetHashCode();
            hash = hash * 31 + CriticalMultiplier.GetHashCode();
            return hash;
        }
    }

    private void OnDestroy()
    {
        if (m_QueryCreated)
        {
            m_Query.Dispose();
            m_QueryCreated = false;
        }
    }
}
