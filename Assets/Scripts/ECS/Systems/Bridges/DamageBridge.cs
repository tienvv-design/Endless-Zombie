using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;


[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class DamageBridgeSystem : SystemBase
{
    private Dictionary<int, Targetable> _targetables = new();
    
    protected override void OnCreate()
    {
        Targetable.OnCreated += DamageableCreatedCallback;
    }

    protected override void OnDestroy()
    {
        Targetable.OnCreated -= DamageableCreatedCallback;
    }

    protected override void OnUpdate()
    {
        foreach (var (damageEvent, entity) in SystemAPI.Query<RefRO<MobDamageGivenEvent>>().WithEntityAccess())
        {
            if (_targetables.TryGetValue(damageEvent.ValueRO.Id, out Targetable targetable))
            {
                targetable.TakeDamage(damageEvent.ValueRO.Amount);
                AudioManager.Instance.Play(SoundLabel.MobGiveDamageSound);
                AudioManager.Instance.Play(SoundLabel.PlayerDamageSound);
                
            }
        }
    }
    
    private void DamageableCreatedCallback(Targetable targetable)
    {
        if (!_targetables.ContainsKey(targetable.ID))
        {
            _targetables.Add(targetable.ID, targetable);
        }
    }
}
