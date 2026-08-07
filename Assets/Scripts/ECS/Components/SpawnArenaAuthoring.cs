using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SpawnPositionSettings : IComponentData
{
    public float GameplayRadius;
    public float MinPlayerDistance;
    public float MinClearance;
    public int MaxPositionAttempts;
    public float3 DeadZoneCenter;
    public float3 DeadZoneHalfExtents;
    public Unity.Mathematics.Random Random;
}

[InternalBufferCapacity(8)]
public struct SpawnArenaRegion : IBufferElementData
{
    public FixedString64Bytes GroupId;
    public float3 Center;
    public float3 HalfExtents;
}

[Serializable]
public struct SpawnArenaRegionDefinition
{
    public string GroupId;
    public Vector3 Center;
    public Vector3 Size;
}

public sealed class SpawnArenaAuthoring : MonoBehaviour
{
    [Min(1f)] public float GameplayRadius = 25f;
    [Min(0f)] public float MinPlayerDistance = 10f;
    [Min(0f)] public float MinClearance = 1f;
    [Min(1)] public int MaxPositionAttempts = 8;
    public Vector3 DeadZoneCenter;
    public Vector3 DeadZoneSize = new(18f, 10f, 12f);
    public SpawnArenaRegionDefinition[] Regions = Array.Empty<SpawnArenaRegionDefinition>();

    private sealed class Baker : Baker<SpawnArenaAuthoring>
    {
        public override void Bake(SpawnArenaAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SpawnPositionSettings
            {
                GameplayRadius = authoring.GameplayRadius,
                MinPlayerDistance = authoring.MinPlayerDistance,
                MinClearance = authoring.MinClearance,
                MaxPositionAttempts = authoring.MaxPositionAttempts,
                DeadZoneCenter = authoring.DeadZoneCenter,
                DeadZoneHalfExtents = (float3)authoring.DeadZoneSize * 0.5f,
                Random = Unity.Mathematics.Random.CreateFromIndex(0x5EEDu),
            });

            DynamicBuffer<SpawnArenaRegion> regions = AddBuffer<SpawnArenaRegion>(entity);
            foreach (SpawnArenaRegionDefinition region in authoring.Regions)
            {
                if (string.IsNullOrWhiteSpace(region.GroupId) || region.Size.x <= 0f || region.Size.z <= 0f)
                    continue;
                regions.Add(new SpawnArenaRegion
                {
                    GroupId = new FixedString64Bytes(region.GroupId),
                    Center = region.Center,
                    HalfExtents = (float3)region.Size * 0.5f,
                });
            }
        }
    }
}
