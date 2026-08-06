using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal partial struct WeaponManagerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WeaponManager>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        RefRW<WeaponManager> weaponManager = SystemAPI.GetSingletonRW<WeaponManager>();
        
        EntityCommandBuffer ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        if (weaponManager.ValueRO.isActive)
        {
            if (weaponManager.ValueRO.Timer < weaponManager.ValueRO.ActiveDuration)
            {
                weaponManager.ValueRW.Timer += SystemAPI.Time.DeltaTime;
            }
            else
            {
                foreach (var (weaponEntity, entity) in SystemAPI.Query<RefRW<Weapon>>().WithEntityAccess())
                {
                    ecb.DestroyEntity(entity);
                }

                weaponManager.ValueRW.Timer = 0.0f;
                weaponManager.ValueRW.isActive = false;
            }
        }
        else
        {
            if (weaponManager.ValueRO.Timer < weaponManager.ValueRO.Cooldown)
            {
                weaponManager.ValueRW.Timer += SystemAPI.Time.DeltaTime;
            }
            else
            {
                NativeArray<Entity> weapons =
                    new NativeArray<Entity>(weaponManager.ValueRO.NumberOfWeapons, Allocator.Temp);
                
                // Usage of entity manager instead of ecb to spawn the weapons is safe because
                // This line is not iterating a query, and it is on the main thread.   
                ecb.Instantiate(weaponManager.ValueRO.WeaponEntityPrefab, weapons);

                for (int i = 0; i < weapons.Length; i++)
                {
                    ecb.AddComponent(weapons[i], new Weapon
                    {
                        Pivot = float3.zero,
                        Number = i,
                        Radius = weaponManager.ValueRO.Radius,
                        RotateSpeed = weaponManager.ValueRO.RotateSpeed,
                        ClockWise = weaponManager.ValueRO.ClockWise,
                    });
                }

                weapons.Dispose();

                weaponManager.ValueRW.isActive = true;
                weaponManager.ValueRW.Timer = 0.0f;
            }
        }
    }
}
    