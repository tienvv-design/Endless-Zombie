using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

internal partial struct DamageDigitSystem : ISystem
{
    
    public const float DIGIT_FALL_ACCELERATION = 40f;
    public const float DIGIT_INITIAL_VELOCITY = 3.0f;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<EntityReferences>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        SystemAPI.TryGetSingleton(out CharacterStatsComponent characterStats);
        
        // var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
        // var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var entityReferences = SystemAPI.GetSingletonRW<EntityReferences>();

        EntityCommandBuffer digitEcb = new EntityCommandBuffer(Allocator.Temp);
        
        // Create damage digit when the mob takes damage.
        foreach (var damageTakenEvent in SystemAPI.Query<RefRW<MobDamageTakenEvent>>())
        {
            // Entity might have been destroyed somewhere in between so check if it has LocalTransform
            if(!SystemAPI.HasComponent<LocalTransform>(damageTakenEvent.ValueRO.Entity)) continue;
            
            Entity digitEntity = digitEcb.Instantiate(entityReferences.ValueRO.DamageDigitPrefabEntity);
            
            // Set the digit's transform to mob's transform with an offset on Y axis.
            LocalTransform mobTransform = SystemAPI.GetComponent<LocalTransform>(damageTakenEvent.ValueRO.Entity);
            LocalTransform digitPrefabTransform = SystemAPI.GetComponent<LocalTransform>(entityReferences.ValueRO.DamageDigitPrefabEntity);
            
            LocalTransform digitTransform = new LocalTransform
            {
                Position = new float3(mobTransform.Position.x, mobTransform.Position.y + 1.5f, mobTransform.Position.z),
                Rotation = quaternion.identity,
                Scale = digitPrefabTransform.Scale
            };
            
            digitEcb.SetComponent(digitEntity, digitTransform);
            
            // Adjust the physics velocity component to give the digit an initial upper force for a bounce effect. 
            PhysicsVelocity velocity = SystemAPI.GetComponent<PhysicsVelocity>(entityReferences.ValueRO.DamageDigitPrefabEntity);
            velocity.Linear.y = DIGIT_INITIAL_VELOCITY; 
            digitEcb.SetComponent(digitEntity, velocity);
            
            
            // Adjust the damage digit component to be explosive based on a random value.
            DamageDigit damageDigitComponent = SystemAPI.GetComponent<DamageDigit>(entityReferences.ValueRO.DamageDigitPrefabEntity);
            Random random = entityReferences.ValueRO.Random;
            damageDigitComponent.DamageValue = damageTakenEvent.ValueRO.Amount;
            float explosionChance = random.NextFloat(0f, 1f);
            damageDigitComponent.IsExplosive = explosionChance < characterStats.DamageDigitExplosionChance;
            entityReferences.ValueRW.Random = random;
            digitEcb.SetComponent(digitEntity, damageDigitComponent);
            
            // Set the material property values.
            digitEcb.SetComponent(digitEntity, new DigitValueMatOverride { DigitIndex = damageTakenEvent.ValueRO.Amount });
            digitEcb.SetComponent(digitEntity, new DigitPulseMatOverride { Pulse = damageDigitComponent.IsExplosive ? 1 : 0});
        }
        
        digitEcb.Playback(state.EntityManager);
        
        foreach (var (localTransform, velocity, damageDigit, entity) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<DamageDigit>>().WithEntityAccess())
        {   
            if (localTransform.ValueRO.Position.y > 0.02f)
            {
                velocity.ValueRW.Linear.y -= DIGIT_FALL_ACCELERATION * SystemAPI.Time.DeltaTime;
            }
            else
            {
                localTransform.ValueRW.Position.y = 0f;
                velocity.ValueRW.Linear = float3.zero;

                if (!damageDigit.ValueRO.IsExplosive)
                {
                    ecb.DestroyEntity(entity);
                    return;
                }

                if (damageDigit.ValueRO.ExplosionTimer < damageDigit.ValueRO.ExplosionDelay)
                {
                    damageDigit.ValueRW.ExplosionTimer += SystemAPI.Time.DeltaTime;
                }
                else
                {
                    damageDigit.ValueRW.ExplosionTimer = 0f;

                    Entity digitExplosionEvent = ecb.CreateEntity();
                    
                    ecb.AddComponent(digitExplosionEvent, new DigitExplosionEvent
                    {
                        Position = localTransform.ValueRO.Position,
                        Radius = damageDigit.ValueRO.ExplosionRadius,
                        Damage = damageDigit.ValueRO.DamageValue,
                    });
                    
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
    
    // Entity GetRootParent(Entity entity, ComponentLookup<Parent> parents)
    // {
    //     Entity current = entity;
    //
    //     // Walk upward until no Parent exists
    //     while (parents.HasComponent(current))
    //     {
    //         var parent = parents[current].Value;
    //         current = parent;
    //     }
    //
    //     return current;
    // }
    
}
