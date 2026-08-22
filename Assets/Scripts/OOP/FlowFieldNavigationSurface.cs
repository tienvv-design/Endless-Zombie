using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FlowFieldNavigationSurface : MonoBehaviour
{
    private static readonly Vector2Int[] Neighbours =
    {
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
        new(-1, -1), new(-1, 1), new(1, -1), new(1, 1)
    };

    private static readonly string[] WalkableNames =
    {
        "ground", "road", "floor", "terrain", "street", "pavement", "дорог"
    };

    [SerializeField, Min(0.25f)] private float m_CellSize = 1f;
    [SerializeField, Min(0f)] private float m_AgentRadius = 0.65f;
    [SerializeField, Min(0.1f)] private float m_MinObstacleHeight = 0.8f;
    [SerializeField, Min(0.25f)] private float m_ObstacleProbeHeight = 1.5f;
    [SerializeField] private bool m_SampleColliderGeometry = true;
    [SerializeField] private string m_WalkableSurfaceName = "Plane";
    [SerializeField] private Transform m_Target;
    [SerializeField] private bool m_DrawDebugGrid;

    private NativeArray<sbyte> m_Directions;
    private NativeArray<float> m_SurfaceHeights;
    private float3 m_Origin;
    private int m_Width;
    private int m_Height;
    private int m_TargetCell = -1;

    public static FlowFieldNavigationSurface Active { get; private set; }
    public bool IsReady => m_Directions.IsCreated && m_SurfaceHeights.IsCreated &&
                           m_Directions.Length > 0 && m_SurfaceHeights.Length == m_Directions.Length;
    public NativeArray<sbyte> Directions => m_Directions;
    public NativeArray<float> SurfaceHeights => m_SurfaceHeights;
    public float3 Origin => m_Origin;
    public float CellSize => m_CellSize;
    public int Width => m_Width;
    public int Height => m_Height;

    private void Awake()
    {
        Active = this;
        ResolveTarget();
        Rebuild();
    }

    private void LateUpdate()
    {
        ResolveTarget();
        if (!IsReady || m_Target == null) return;

        int targetCell = WorldToCell(m_Target.position);
        if (targetCell >= 0 && targetCell != m_TargetCell)
            Rebuild();
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
        CompleteEntityJobs();
        if (m_Directions.IsCreated) m_Directions.Dispose();
        if (m_SurfaceHeights.IsCreated) m_SurfaceHeights.Dispose();
    }

    [ContextMenu("Rebuild Flow Field")]
    public void Rebuild()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0 || m_Target == null) return;

        if (!TryGetWalkableSurfaceBounds(renderers, out Bounds mapBounds))
        {
            Debug.LogError($"Flow field could not find the walkable mesh '{m_WalkableSurfaceName}' " +
                           "inside Stage Map.", this);
            return;
        }

        m_Origin = new float3(mapBounds.min.x, 0f, mapBounds.min.z);
        m_Width = Mathf.Max(1, Mathf.CeilToInt(mapBounds.size.x / m_CellSize));
        m_Height = Mathf.Max(1, Mathf.CeilToInt(mapBounds.size.z / m_CellSize));
        int cellCount = m_Width * m_Height;
        bool[] blocked = new bool[cellCount];
        float[] surfaceHeights = new float[cellCount];
        Array.Fill(surfaceHeights, mapBounds.center.y);

        EnsureRuntimeMeshColliders();
        Physics.SyncTransforms();
        if (m_SampleColliderGeometry && GetComponentsInChildren<MeshCollider>(true).Length > 0)
        {
            SampleColliderObstacles(mapBounds, blocked, surfaceHeights);
        }
        else
        {
            foreach (Renderer renderer in renderers)
            {
                if (!IsObstacle(renderer)) continue;
                Bounds bounds = renderer.bounds;
                bounds.Expand(new Vector3(m_AgentRadius * 2f, 0f, m_AgentRadius * 2f));
                MarkBlocked(bounds, blocked);
            }
        }

        int target = FindNearestOpenCell(WorldToCell(m_Target.position), blocked);
        if (target < 0)
        {
            Debug.LogWarning("Flow field could not find an open cell near the Player.", this);
            return;
        }

        int[] distances = BuildDistances(target, blocked);
        CompleteEntityJobs();
        if (m_Directions.IsCreated) m_Directions.Dispose();
        if (m_SurfaceHeights.IsCreated) m_SurfaceHeights.Dispose();
        m_Directions = new NativeArray<sbyte>(cellCount, Allocator.Persistent);
        m_SurfaceHeights = new NativeArray<float>(surfaceHeights, Allocator.Persistent);

        for (int index = 0; index < cellCount; index++)
        {
            if (blocked[index] || distances[index] == int.MaxValue)
            {
                m_Directions[index] = -1;
                continue;
            }
            int best = 0;
            int bestDistance = distances[index];
            Vector2Int cell = ToCoordinates(index);
            for (int direction = 0; direction < Neighbours.Length; direction++)
            {
                Vector2Int next = cell + Neighbours[direction];
                if (!IsInside(next.x, next.y)) continue;
                if (!CanTraverseDiagonal(cell, Neighbours[direction], blocked)) continue;
                int nextIndex = ToIndex(next.x, next.y);
                if (distances[nextIndex] < bestDistance)
                {
                    best = direction + 1;
                    bestDistance = distances[nextIndex];
                }
            }
            m_Directions[index] = (sbyte)best;
        }

        m_TargetCell = WorldToCell(m_Target.position);
        int blockedCount = 0;
        foreach (bool isBlocked in blocked)
            if (isBlocked) blockedCount++;
        Debug.Log($"Flow field built: {m_Width}x{m_Height}, {blockedCount} obstacle cells, " +
                  $"cell {m_CellSize:0.##}m.", this);
    }

    private void EnsureRuntimeMeshColliders()
    {
        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<MeshCollider>() != null) continue;
            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
        }
    }

    private bool TryGetWalkableSurfaceBounds(Renderer[] renderers, out Bounds bounds)
    {
        bool found = false;
        bounds = default;
        foreach (Renderer renderer in renderers)
        {
            if (!string.Equals(renderer.name, m_WalkableSurfaceName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }

    public bool TryProjectToWalkable(float3 position, out float3 projected)
    {
        projected = position;
        if (!IsReady || !m_SurfaceHeights.IsCreated) return false;
        int rawX = Mathf.FloorToInt((position.x - m_Origin.x) / m_CellSize);
        int rawZ = Mathf.FloorToInt((position.z - m_Origin.z) / m_CellSize);
        int x = Mathf.Clamp(rawX, 0, m_Width - 1);
        int z = Mathf.Clamp(rawZ, 0, m_Height - 1);
        int requestedCell = ToIndex(x, z);
        if (rawX == x && rawZ == z && m_Directions[requestedCell] >= 0)
        {
            projected.y = m_SurfaceHeights[requestedCell];
            return true;
        }

        int cell = FindNearestNavigableCell(x, z);
        if (cell < 0) return false;

        Vector2Int coordinates = ToCoordinates(cell);
        projected.x = m_Origin.x + (coordinates.x + 0.5f) * m_CellSize;
        projected.y = m_SurfaceHeights[cell];
        projected.z = m_Origin.z + (coordinates.y + 0.5f) * m_CellSize;
        return true;
    }

    private int FindNearestNavigableCell(int originX, int originZ)
    {
        int origin = ToIndex(originX, originZ);
        if (m_Directions[origin] >= 0) return origin;

        int maxRadius = Mathf.Max(m_Width, m_Height);
        for (int radius = 1; radius < maxRadius; radius++)
        {
            int minX = Mathf.Max(0, originX - radius);
            int maxX = Mathf.Min(m_Width - 1, originX + radius);
            int minZ = Mathf.Max(0, originZ - radius);
            int maxZ = Mathf.Min(m_Height - 1, originZ + radius);

            for (int x = minX; x <= maxX; x++)
            {
                int bottom = ToIndex(x, minZ);
                if (m_Directions[bottom] >= 0) return bottom;
                int top = ToIndex(x, maxZ);
                if (m_Directions[top] >= 0) return top;
            }

            for (int z = minZ + 1; z < maxZ; z++)
            {
                int left = ToIndex(minX, z);
                if (m_Directions[left] >= 0) return left;
                int right = ToIndex(maxX, z);
                if (m_Directions[right] >= 0) return right;
            }
        }

        return -1;
    }

    private void SampleColliderObstacles(Bounds mapBounds, bool[] blocked, float[] surfaceHeights)
    {
        float castTop = mapBounds.max.y + 2f;
        float castDistance = mapBounds.size.y + 4f;
        RaycastHit[] hits = new RaycastHit[32];
        Collider[] overlaps = new Collider[32];

        for (int z = 0; z < m_Height; z++)
        for (int x = 0; x < m_Width; x++)
        {
            float worldX = m_Origin.x + (x + 0.5f) * m_CellSize;
            float worldZ = m_Origin.z + (z + 0.5f) * m_CellSize;
            int hitCount = Physics.RaycastNonAlloc(
                new Vector3(worldX, castTop, worldZ), Vector3.down, hits, castDistance,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);

            float planeHeight = float.NegativeInfinity;
            float highestObstacle = float.NegativeInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || !collider.transform.IsChildOf(transform)) continue;
                if (string.Equals(collider.name, m_WalkableSurfaceName, StringComparison.OrdinalIgnoreCase))
                    planeHeight = Mathf.Max(planeHeight, hits[i].point.y);
                else if (!string.Equals(collider.name, "Gameplay Ground Collider", StringComparison.OrdinalIgnoreCase))
                    highestObstacle = Mathf.Max(highestObstacle, hits[i].point.y);
            }

            int cell = ToIndex(x, z);
            if (float.IsNegativeInfinity(planeHeight))
            {
                blocked[cell] = true;
                continue;
            }

            surfaceHeights[cell] = planeHeight;
            if (!float.IsNegativeInfinity(highestObstacle) &&
                highestObstacle > planeHeight + m_MinObstacleHeight)
                blocked[cell] = true;

            if (!blocked[cell] && HasObstacleVolume(worldX, worldZ, planeHeight, overlaps))
                blocked[cell] = true;
        }

        InflateBlockedCells(blocked, Mathf.CeilToInt(m_AgentRadius / m_CellSize));
    }

    private bool HasObstacleVolume(float worldX, float worldZ, float planeHeight, Collider[] overlaps)
    {
        Vector3 halfExtents = new(m_CellSize * 0.51f, m_ObstacleProbeHeight * 0.5f,
            m_CellSize * 0.51f);
        Vector3 center = new(worldX, planeHeight + halfExtents.y + 0.05f, worldZ);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, overlaps, Quaternion.identity,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            overlaps[i] = null;
            if (collider == null || !collider.transform.IsChildOf(transform)) continue;
            if (string.Equals(collider.name, m_WalkableSurfaceName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(collider.name, "Gameplay Ground Collider", StringComparison.OrdinalIgnoreCase))
                continue;
            return true;
        }

        return false;
    }

    private void InflateBlockedCells(bool[] blocked, int radius)
    {
        if (radius <= 0) return;
        bool[] source = (bool[])blocked.Clone();
        for (int z = 0; z < m_Height; z++)
        for (int x = 0; x < m_Width; x++)
        {
            if (!source[ToIndex(x, z)]) continue;
            for (int offsetZ = -radius; offsetZ <= radius; offsetZ++)
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                if (offsetX * offsetX + offsetZ * offsetZ > radius * radius) continue;
                int nextX = x + offsetX;
                int nextZ = z + offsetZ;
                if (IsInside(nextX, nextZ)) blocked[ToIndex(nextX, nextZ)] = true;
            }
        }
    }

    private bool IsObstacle(Renderer renderer)
    {
        string objectName = renderer.name.ToLowerInvariant();
        foreach (string walkableName in WalkableNames)
            if (objectName.Contains(walkableName)) return false;
        return renderer.bounds.size.y >= m_MinObstacleHeight;
    }

    private void MarkBlocked(Bounds bounds, bool[] blocked)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt((bounds.min.x - m_Origin.x) / m_CellSize), 0, m_Width - 1);
        int maxX = Mathf.Clamp(Mathf.FloorToInt((bounds.max.x - m_Origin.x) / m_CellSize), 0, m_Width - 1);
        int minZ = Mathf.Clamp(Mathf.FloorToInt((bounds.min.z - m_Origin.z) / m_CellSize), 0, m_Height - 1);
        int maxZ = Mathf.Clamp(Mathf.FloorToInt((bounds.max.z - m_Origin.z) / m_CellSize), 0, m_Height - 1);
        for (int z = minZ; z <= maxZ; z++)
        for (int x = minX; x <= maxX; x++)
            blocked[ToIndex(x, z)] = true;
    }

    private int[] BuildDistances(int target, bool[] blocked)
    {
        int[] distances = new int[blocked.Length];
        Array.Fill(distances, int.MaxValue);
        Queue<int> open = new();
        distances[target] = 0;
        open.Enqueue(target);
        while (open.Count > 0)
        {
            int current = open.Dequeue();
            Vector2Int cell = ToCoordinates(current);
            foreach (Vector2Int offset in Neighbours)
            {
                int x = cell.x + offset.x;
                int z = cell.y + offset.y;
                if (!IsInside(x, z)) continue;
                if (!CanTraverseDiagonal(cell, offset, blocked)) continue;
                int next = ToIndex(x, z);
                if (blocked[next] || distances[next] != int.MaxValue) continue;
                distances[next] = distances[current] + (offset.x == 0 || offset.y == 0 ? 10 : 14);
                open.Enqueue(next);
            }
        }
        return distances;
    }

    private bool CanTraverseDiagonal(Vector2Int cell, Vector2Int offset, bool[] blocked)
    {
        if (offset.x == 0 || offset.y == 0) return true;
        int horizontal = ToIndex(cell.x + offset.x, cell.y);
        int vertical = ToIndex(cell.x, cell.y + offset.y);
        return !blocked[horizontal] && !blocked[vertical];
    }

    private int FindNearestOpenCell(int requested, bool[] blocked)
    {
        if (requested >= 0 && !blocked[requested]) return requested;
        if (requested < 0) return -1;
        Vector2Int origin = ToCoordinates(requested);
        int maxRadius = Mathf.Max(m_Width, m_Height);
        for (int radius = 1; radius < maxRadius; radius++)
        for (int z = origin.y - radius; z <= origin.y + radius; z++)
        for (int x = origin.x - radius; x <= origin.x + radius; x++)
            if (IsInside(x, z) && !blocked[ToIndex(x, z)]) return ToIndex(x, z);
        return -1;
    }

    private int WorldToCell(Vector3 position)
    {
        int x = Mathf.FloorToInt((position.x - m_Origin.x) / m_CellSize);
        int z = Mathf.FloorToInt((position.z - m_Origin.z) / m_CellSize);
        return IsInside(x, z) ? ToIndex(x, z) : -1;
    }

    private Vector2Int ToCoordinates(int index) => new(index % m_Width, index / m_Width);
    private int ToIndex(int x, int z) => z * m_Width + x;
    private bool IsInside(int x, int z) => x >= 0 && z >= 0 && x < m_Width && z < m_Height;

    private void ResolveTarget()
    {
        if (m_Target != null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) m_Target = player.transform;
    }

    private static void CompleteEntityJobs()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world != null && world.IsCreated) world.EntityManager.CompleteAllTrackedJobs();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!m_DrawDebugGrid || !IsReady) return;
        Gizmos.color = new Color(0.15f, 0.8f, 1f, 0.4f);
        for (int i = 0; i < m_Directions.Length; i++)
        {
            int direction = m_Directions[i];
            if (direction <= 0) continue;
            Vector2Int cell = ToCoordinates(i);
            Vector2Int offset = Neighbours[direction - 1];
            Vector3 center = new(m_Origin.x + (cell.x + 0.5f) * m_CellSize, 0.15f,
                m_Origin.z + (cell.y + 0.5f) * m_CellSize);
            Gizmos.DrawLine(center, center + new Vector3(offset.x, 0f, offset.y) * m_CellSize * 0.4f);
        }
    }
#endif
}
