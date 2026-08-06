using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

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
            
            
            // Damage digits are visual feedback only. AoE explosions belong to
            // explosive weapon projectiles (Rocket Launcher), never to UI digits.
            DamageDigit damageDigitComponent = SystemAPI.GetComponent<DamageDigit>(entityReferences.ValueRO.DamageDigitPrefabEntity);
            damageDigitComponent.DamageValue = damageTakenEvent.ValueRO.Amount;
            damageDigitComponent.IsExplosive = false;
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

                ecb.DestroyEntity(entity);
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
