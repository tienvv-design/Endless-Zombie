using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

internal partial struct MobHealthManager : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();   
        state.RequireForUpdate<GameRunningTag>();
        state.RequireForUpdate<CombatMetrics>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        var mobs = SystemAPI.GetComponentLookup<Mob>(false);
        var transforms = SystemAPI.GetComponentLookup<LocalTransform>(false);
        var statusEffects = SystemAPI.GetComponentLookup<MobStatusEffects>(false);
        float combatRadius = SystemAPI.TryGetSingleton(out MobSpawnSettings spawnSettings)
            ? spawnSettings.SpawnRadius
            : 25f;
        RefRW<CombatMetrics> metrics = SystemAPI.GetSingletonRW<CombatMetrics>();
        
        foreach (var damageTakenEvent in SystemAPI.Query<RefRW<MobDamageTakenEvent>>())
        {
            Entity mobEntity = damageTakenEvent.ValueRO.Entity;

            // Entity might have been destroyed somewhere in between so check if it has LocalTransform
            if (!SystemAPI.HasComponent<LocalTransform>(mobEntity)) continue;
            
            Mob mobData = mobs[mobEntity];
            if (mobData.Health <= 0) continue;
            int appliedDamage = math.min(mobData.Health, math.max(0, damageTakenEvent.ValueRO.Amount));
            metrics.ValueRW.WindowDamage += appliedDamage;
            metrics.ValueRW.TotalDamage += appliedDamage;
            mobData.Health -= damageTakenEvent.ValueRO.Amount;

            if (statusEffects.HasComponent(mobEntity) && damageTakenEvent.ValueRO.Element != ElementType.None)
            {
                MobStatusEffects status = statusEffects[mobEntity];
                if (damageTakenEvent.ValueRO.Element == ElementType.Fire)
                {
                    status.BurnRemaining = math.max(status.BurnRemaining,
                        damageTakenEvent.ValueRO.ElementDuration);
                    status.BurnDamagePerTick = math.max(status.BurnDamagePerTick,
                        math.max(1, (int)math.round(damageTakenEvent.ValueRO.ElementMagnitude)));
                }
                else if (damageTakenEvent.ValueRO.Element == ElementType.Frost)
                {
                    float resistance = math.saturate(mobData.CrowdControlResistance);
                    float effectiveDuration = damageTakenEvent.ValueRO.ElementDuration * (1f - resistance);
                    float requestedMultiplier = math.clamp(
                        damageTakenEvent.ValueRO.ElementMagnitude, 0.1f, 1f);
                    float effectiveMultiplier = math.lerp(requestedMultiplier, 1f, resistance);
                    status.FrostRemaining = math.max(status.FrostRemaining, effectiveDuration);
                    status.FrostSpeedMultiplier = effectiveMultiplier;
                }
                statusEffects[mobEntity] = status;
            }
        
            // Debug.Log($"Frame: {Time.frameCount}: {mobEntity.Index} Mob has taken {damageTakenEvent.ValueRO.Amount} damage. Health is: {mobData.Health}");
        
            if (mobData.Health <= 0)
            {
                mobData.Health = 0;
                metrics.ValueRW.KillCount++;
                metrics.ValueRW.TotalTimeToKill += math.max(0f,
                    spawnSettings.ElapsedTime - mobData.SpawnTime);
                metrics.ValueRW.AverageTimeToKill = metrics.ValueRO.KillCount > 0
                    ? metrics.ValueRO.TotalTimeToKill / metrics.ValueRO.KillCount
                    : 0f;
                // Debug.Log("Mob is being destroyed.");
                ecb.DestroyEntity(mobEntity);
                
                // Need to check if the entity has transform (to check if it has been destroyed)

                if (SystemAPI.HasComponent<LocalTransform>(mobEntity))
                {
                    var localTransform = SystemAPI.GetComponent<LocalTransform>(mobEntity);
                    Entity mobDeathEvent = ecb.CreateEntity();
                    ecb.AddComponent(mobDeathEvent, new MobDeathEvent
                    {
                        LocalTransform = localTransform,
                        GoldMultiplier = math.max(1, mobData.GoldMultiplier),
                        XPReward = math.max(1, mobData.XPReward),
                        GoldReward = math.max(0, mobData.GoldReward),
                    });   
                }
            }
            else if (damageTakenEvent.ValueRO.KnockbackDistance > 0f)
            {
                LocalTransform mobTransform = transforms[mobEntity];
                float resistance = math.saturate(mobData.KnockbackResistance);
                float3 direction = math.normalizesafe(damageTakenEvent.ValueRO.KnockbackDirection);
                mobTransform.Position += direction * damageTakenEvent.ValueRO.KnockbackDistance * (1f - resistance);

                float2 horizontal = mobTransform.Position.xz;
                float horizontalLength = math.length(horizontal);
                if (horizontalLength > combatRadius)
                    mobTransform.Position.xz = horizontal / horizontalLength * combatRadius;
                transforms[mobEntity] = mobTransform;
            }

            mobs[mobEntity] = mobData; 
            
        }
    }
}
