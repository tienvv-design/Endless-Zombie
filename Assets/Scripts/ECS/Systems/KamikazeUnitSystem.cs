using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct KamikazeUnitSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);
        
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (localTransform, physicsVelocity, kamikaze, entity)
                 in SystemAPI.Query<RefRO<LocalTransform>, RefRW<PhysicsVelocity>, RefRW<KamikazeUnit>>()
                     .WithEntityAccess())
        {
            bool isExploder = SystemAPI.HasComponent<MobVisualVariant>(entity) &&
                              SystemAPI.GetComponent<MobVisualVariant>(entity).Kind == MobVisualKind.ZombieFat;
            if (isExploder && kamikaze.ValueRO.HasExploded != 0)
                continue;
            // A spawning enemy is still inside its portal. Do not let the attack
            // state machine re-enable movement or start an attack until the
            // emergence component is removed.
            if (SystemAPI.HasComponent<SpawnEmergence>(entity))
            {
                StopMovement(ref physicsVelocity.ValueRW);
                if (SystemAPI.HasComponent<UnitMover>(entity) && SystemAPI.IsComponentEnabled<UnitMover>(entity))
                    SystemAPI.SetComponentEnabled<UnitMover>(entity, false);
                kamikaze.ValueRW.AttackTimer = 0f;
                continue;
            }

            if (!SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> goInfoBuffer))
                return;

            GameObjectInfo target = default;
            bool targetMatch = false;

            foreach (var goInfo in goInfoBuffer)
            {
                if (kamikaze.ValueRO.TargetObjectType == goInfo.ObjectType)
                {
                    target = goInfo;
                    targetMatch = true;
                    break;
                }
            }
            
            if(!targetMatch) continue;
            
            float3 direction = target.Position - localTransform.ValueRO.Position;
            float distanceSq = math.lengthsq(direction);

            if (distanceSq <= kamikaze.ValueRO.HitDistanceSq)
            {
                // Disabling an enableable movement component does not clear the
                // Rigidbody velocity written during the previous frame. Stop it
                // explicitly or physics will carry the enemy through the Player.
                StopMovement(ref physicsVelocity.ValueRW);
                bool startedAttack = false;
                if (SystemAPI.HasComponent<UnitMover>(entity) && SystemAPI.IsComponentEnabled<UnitMover>(entity))
                {
                    SystemAPI.SetComponentEnabled<UnitMover>(entity, false);
                    kamikaze.ValueRW.AttackTimer = math.max(0.01f,
                        kamikaze.ValueRO.AttackInterval *
                        math.clamp(kamikaze.ValueRO.AttackImpactNormalizedTime, 0.05f, 0.95f));
                    startedAttack = true;
                }

                kamikaze.ValueRW.AttackTimer -= deltaTime;
                if (!startedAttack && kamikaze.ValueRO.AttackTimer <= 0f)
                {
                    Entity attackEvent = ecb.CreateEntity();
                    ecb.AddComponent(attackEvent, new MobDamageGivenEvent
                    {
                        Id = target.ID,
                        Amount = kamikaze.ValueRO.Damage,
                    });
                    if (isExploder)
                    {
                        Entity explosionEvent = ecb.CreateEntity();
                        ecb.AddComponent(explosionEvent, new MobExplosionEvent
                        {
                            Position = localTransform.ValueRO.Position,
                            Radius = math.sqrt(kamikaze.ValueRO.HitDistanceSq),
                        });
                        Entity selfDamageEvent = ecb.CreateEntity();
                        ecb.AddComponent(selfDamageEvent, new MobDamageTakenEvent
                        {
                            Id = entity.Index,
                            Entity = entity,
                            Amount = int.MaxValue,
                        });
                        kamikaze.ValueRW.HasExploded = 1;
                        if (SystemAPI.HasComponent<UnitMover>(entity))
                            SystemAPI.SetComponentEnabled<UnitMover>(entity, false);
                    }
                    else
                        kamikaze.ValueRW.AttackTimer = math.max(0.05f, kamikaze.ValueRO.AttackInterval);
                }
            }
            else
            {
                if (SystemAPI.HasComponent<UnitMover>(entity) && !SystemAPI.IsComponentEnabled<UnitMover>(entity))
                    SystemAPI.SetComponentEnabled<UnitMover>(entity, true);
                kamikaze.ValueRW.AttackTimer = 0f;
            }
            
        }
    }

    private static void StopMovement(ref PhysicsVelocity velocity)
    {
        velocity.Linear = float3.zero;
        velocity.Angular = float3.zero;
    }
}
