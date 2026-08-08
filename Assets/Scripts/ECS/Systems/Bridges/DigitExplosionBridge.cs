using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DigitExplosionBridge : SystemBase
{
    protected override void OnCreate()
    {
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnUpdate()
    {
        foreach (var (digitExplosionEvent, entity) in SystemAPI.Query<RefRO<DigitExplosionEvent>>().WithEntityAccess())
        {
            AudioManager.Instance.Play(SoundLabel.DigitExplosionSound);
            if (WeaponVfxRuntime.CurrentConfig == null ||
                WeaponVfxRuntime.CurrentConfig.ExplosionVfxPrefab == null)
            {
                GameObject go = GameObject.Instantiate(VFXReferences.Instance.DigitExplosionEffect);
                go.transform.position = digitExplosionEvent.ValueRO.Position;
            }
        }
    }
}
