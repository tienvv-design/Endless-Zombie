using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class PlayerFacingBridge : SystemBase
{
    private CharacterLogic m_Player;

    protected override void OnCreate()
    {
        RequireForUpdate<WeaponManager>();
        RequireForUpdate<GameRunningTag>();
    }

    protected override void OnUpdate()
    {
        if (m_Player == null)
            m_Player = Object.FindFirstObjectByType<CharacterLogic>();
        if (m_Player == null || !m_Player.isActiveAndEnabled)
            return;

        WeaponManager gun = SystemAPI.GetSingleton<WeaponManager>();
        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
            return;

        float3 playerPosition = m_Player.transform.position;
        float closestDistanceSq = gun.AttackRange * gun.AttackRange;
        float3 targetPosition = float3.zero;
        bool foundTarget = false;

        foreach (RefRO<LocalTransform> mobTransform in
                 SystemAPI.Query<RefRO<LocalTransform>>().WithAll<Mob>())
        {
            if (!GameplayCameraVisibility.Contains(gameplayCamera, mobTransform.ValueRO.Position))
                continue;

            float distanceSq = math.distancesq(playerPosition, mobTransform.ValueRO.Position);
            if (distanceSq > closestDistanceSq)
                continue;

            closestDistanceSq = distanceSq;
            targetPosition = mobTransform.ValueRO.Position;
            foundTarget = true;
        }

        if (!foundTarget)
            return;

        Vector3 direction = (Vector3)(targetPosition - playerPosition);
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Transform aimTransform = m_Player.AimTransform;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        // The weapon prefab can have a yaw offset relative to the character rig.
        // Compensate that offset so BulletSpawn.forward, rather than the player's
        // model forward, is the axis that actually points at the selected zombie.
        Transform bulletSpawn = WeaponVfxRuntime.CurrentBulletSpawn;
        if (bulletSpawn != null)
        {
            Vector3 characterForward = aimTransform.forward;
            Vector3 gunForward = bulletSpawn.forward;
            characterForward.y = 0f;
            gunForward.y = 0f;
            if (characterForward.sqrMagnitude > 0.0001f && gunForward.sqrMagnitude > 0.0001f)
            {
                float gunYawOffset = Vector3.SignedAngle(
                    characterForward.normalized, gunForward.normalized, Vector3.up);
                targetRotation = Quaternion.AngleAxis(-gunYawOffset, Vector3.up) * targetRotation;
            }
        }

        aimTransform.rotation = Quaternion.RotateTowards(
            aimTransform.rotation,
            targetRotation,
            m_Player.TurnSpeed * 90f * SystemAPI.Time.DeltaTime);
    }
}
