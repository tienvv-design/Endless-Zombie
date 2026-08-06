using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SocialPlatforms;

internal partial struct FaceTheCameraSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 cameraForward = float3.zero;
        if (Camera.main != null)
        {
            cameraForward = Camera.main.transform.forward;
        }
        
        foreach (var (localTransform, faceTheCamera) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<FaceTheCamera>>())
        {
            localTransform.ValueRW.Rotation = quaternion.LookRotationSafe(cameraForward, math.up());
            // TO DO: Make it also work if the entity has a parent.
        }
    }
}

