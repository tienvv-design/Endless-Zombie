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

            int displayDamage = math.max(0, damageTakenEvent.ValueRO.Amount);
            int digitCount = CountDigits(displayDamage);
            int divisor = Pow10(digitCount - 1);
            LocalTransform mobTransform = SystemAPI.GetComponent<LocalTransform>(damageTakenEvent.ValueRO.Entity);
            LocalTransform digitPrefabTransform = SystemAPI.GetComponent<LocalTransform>(entityReferences.ValueRO.DamageDigitPrefabEntity);

            float criticalScale = damageTakenEvent.ValueRO.IsCritical ? 1.35f : 1f;
            // Derive glyph spacing from the prefab scale so artists can resize
            // Digit3D without multi-digit values becoming too loose or cramped.
            float spacing = 0.57f * digitPrefabTransform.Scale * criticalScale;
            float center = (digitCount - 1) * 0.5f;
            for (int digitIndex = 0; digitIndex < digitCount; digitIndex++)
            {
                int digit = divisor > 0 ? displayDamage / divisor % 10 : 0;
                divisor = math.max(1, divisor / 10);
                Entity digitEntity = digitEcb.Instantiate(entityReferences.ValueRO.DamageDigitPrefabEntity);
                LocalTransform digitTransform = new LocalTransform
                {
                    Position = new float3(
                        mobTransform.Position.x + (digitIndex - center) * spacing,
                        mobTransform.Position.y + 1.5f,
                        mobTransform.Position.z),
                    Rotation = quaternion.identity,
                    Scale = digitPrefabTransform.Scale * criticalScale
                };
                digitEcb.SetComponent(digitEntity, digitTransform);

                PhysicsVelocity velocity = SystemAPI.GetComponent<PhysicsVelocity>(entityReferences.ValueRO.DamageDigitPrefabEntity);
                velocity.Linear.y = DIGIT_INITIAL_VELOCITY;
                digitEcb.SetComponent(digitEntity, velocity);

                DamageDigit damageDigitComponent = SystemAPI.GetComponent<DamageDigit>(entityReferences.ValueRO.DamageDigitPrefabEntity);
                damageDigitComponent.DamageValue = displayDamage;
                damageDigitComponent.IsExplosive = false;
                damageDigitComponent.IsCritical = damageTakenEvent.ValueRO.IsCritical;
                damageDigitComponent.BaseScale = digitPrefabTransform.Scale * criticalScale;
                damageDigitComponent.FeedbackTime = 0f;
                digitEcb.SetComponent(digitEntity, damageDigitComponent);
                // The shader atlas contains exactly one glyph per index (0-9), so
                // multi-digit damage is rendered as several centered entities.
                digitEcb.SetComponent(digitEntity, new DigitValueMatOverride { DigitIndex = digit });
                digitEcb.SetComponent(digitEntity, new DigitPulseMatOverride
                {
                    Pulse = damageTakenEvent.ValueRO.IsCritical ? 1f : 0f,
                });
            }
        }
        
        digitEcb.Playback(state.EntityManager);
        
        foreach (var (localTransform, velocity, damageDigit, entity) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<DamageDigit>>().WithEntityAccess())
        {   
            damageDigit.ValueRW.FeedbackTime += SystemAPI.Time.DeltaTime;
            float time = damageDigit.ValueRO.FeedbackTime;
            float punch = 1f + 0.35f * math.exp(-time * 10f) * math.sin(time * 30f);
            localTransform.ValueRW.Scale = damageDigit.ValueRO.BaseScale * punch;

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

    private static int CountDigits(int value)
    {
        int count = 1;
        while (value >= 10)
        {
            value /= 10;
            count++;
        }
        return count;
    }

    private static int Pow10(int exponent)
    {
        int value = 1;
        for (int i = 0; i < exponent; i++) value *= 10;
        return value;
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
