using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

partial struct UnitMoverSystem : ISystem
{
    
    public const float REACHED_TARGET_POSITION_DISTANCE_SQ = 2f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameRunningTag>();
    }

    public void OnUpdate(ref SystemState state)
    {
        FlowFieldNavigationSurface surface = FlowFieldNavigationSurface.Active;
        bool useFlowField = surface != null && surface.IsReady;
        int mobCount = SystemAPI.QueryBuilder().WithAll<Mob, LocalTransform>().Build().CalculateEntityCount();
        NativeParallelMultiHashMap<int, CrowdNeighbor> crowdGrid =
            new(math.max(1, mobCount), Allocator.TempJob);
        BuildCrowdGridJob buildCrowdGrid = new()
        {
            writer = crowdGrid.AsParallelWriter(),
            cellSize = CrowdAvoidance.CellSize,
        };
        state.Dependency = buildCrowdGrid.ScheduleParallel(state.Dependency);

        if (useFlowField)
        {
            MobUnitMoverJob mobMoverJob = new MobUnitMoverJob {
                deltaTime = SystemAPI.Time.DeltaTime,
                directions = surface.Directions,
                surfaceHeights = surface.SurfaceHeights,
                gridOrigin = surface.Origin,
                cellSize = surface.CellSize,
                gridWidth = surface.Width,
                gridHeight = surface.Height,
                crowdGrid = crowdGrid.AsReadOnly(),
            };
            state.Dependency = mobMoverJob.ScheduleParallel(state.Dependency);
        }
        else
        {
            DirectMobUnitMoverJob directMobMoverJob = new DirectMobUnitMoverJob
            {
                deltaTime = SystemAPI.Time.DeltaTime,
                crowdGrid = crowdGrid.AsReadOnly(),
            };
            state.Dependency = directMobMoverJob.ScheduleParallel(state.Dependency);
        }

        NonMobUnitMoverJob nonMobMoverJob = new NonMobUnitMoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        };
        state.Dependency = nonMobMoverJob.ScheduleParallel(state.Dependency);
        state.Dependency = crowdGrid.Dispose(state.Dependency);
    }
}

public struct CrowdNeighbor
{
    public Entity Entity;
    public float3 Position;
    public int2 Cell;
    public float MoveSpeed;
}

public static class CrowdAvoidance
{
    public const float CellSize = 1f;
    public const float Radius = 0.8f;
    public const float Strength = 1.35f;

    public static int2 PositionToCell(float3 position, float cellSize)
    {
        return (int2)math.floor(position.xz / cellSize);
    }

    public static int Hash(int2 cell)
    {
        return (cell.x * 73856093) ^ (cell.y * 19349663);
    }

    public static float3 Calculate(Entity entity, float3 position,
        in NativeParallelMultiHashMap<int, CrowdNeighbor>.ReadOnly grid)
    {
        int2 origin = PositionToCell(position, CellSize);
        float2 separation = float2.zero;
        float radiusSq = Radius * Radius;

        for (int z = -1; z <= 1; z++)
        for (int x = -1; x <= 1; x++)
        {
            int2 cell = origin + new int2(x, z);
            if (!grid.TryGetFirstValue(Hash(cell), out CrowdNeighbor neighbor, out var iterator))
                continue;
            do
            {
                if (neighbor.Entity == entity || math.any(neighbor.Cell != cell)) continue;
                float2 delta = position.xz - neighbor.Position.xz;
                float distanceSq = math.lengthsq(delta);
                if (distanceSq >= radiusSq) continue;
                if (distanceSq < 0.000001f)
                    delta = ((entity.Index & 1) == 0 ? new float2(1f, 0f) : new float2(-1f, 0f)) * 0.001f;
                float distance = math.sqrt(math.max(0.000001f, math.lengthsq(delta)));
                separation += delta / distance * (1f - distance / Radius);
            }
            while (grid.TryGetNextValue(out neighbor, ref iterator));
        }

        return new float3(separation.x, 0f, separation.y);
    }
}

[BurstCompile]
[WithAll(typeof(Mob))]
[WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
public partial struct BuildCrowdGridJob : IJobEntity
{
    public NativeParallelMultiHashMap<int, CrowdNeighbor>.ParallelWriter writer;
    public float cellSize;

    public void Execute(Entity entity, in LocalTransform transform, in UnitMover mover)
    {
        int2 cell = CrowdAvoidance.PositionToCell(transform.Position, cellSize);
        writer.Add(CrowdAvoidance.Hash(cell), new CrowdNeighbor
        {
            Entity = entity,
            Position = transform.Position,
            Cell = cell,
            MoveSpeed = mover.moveSpeed,
        });
    }
}

[BurstCompile]
[WithAll(typeof(Mob))]
public partial struct MobUnitMoverJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public NativeArray<sbyte> directions;
    [ReadOnly] public NativeArray<float> surfaceHeights;
    public float3 gridOrigin;
    public float cellSize;
    public int gridWidth;
    public int gridHeight;
    [ReadOnly] public NativeParallelMultiHashMap<int, CrowdNeighbor>.ReadOnly crowdGrid;

    public void Execute(Entity entity, ref LocalTransform localTransform, in UnitMover unitMover,
        ref PhysicsVelocity physicsVelocity)
    {
        float3 moveDirection = unitMover.targetPosition - localTransform.Position;

        float reachedTargetDistanceSq = UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ;
        if (math.lengthsq(moveDirection) <= reachedTargetDistanceSq)
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }
        
        moveDirection = math.normalize(moveDirection);
        int x = (int)math.floor((localTransform.Position.x - gridOrigin.x) / cellSize);
        int z = (int)math.floor((localTransform.Position.z - gridOrigin.z) / cellSize);
        if (x >= 0 && z >= 0 && x < gridWidth && z < gridHeight)
        {
            int cellIndex = z * gridWidth + x;
            localTransform.Position.y = surfaceHeights[cellIndex];
            int direction = directions[cellIndex];
            if (direction < 0)
            {
                int2 escapeOffset = FindEscapeOffset(x, z, unitMover.targetPosition);
                if (math.all(escapeOffset == int2.zero))
                {
                    physicsVelocity.Linear = float3.zero;
                    physicsVelocity.Angular = float3.zero;
                    return;
                }
                float3 escapeCenter = new(
                    gridOrigin.x + (x + escapeOffset.x + 0.5f) * cellSize,
                    localTransform.Position.y,
                    gridOrigin.z + (z + escapeOffset.y + 0.5f) * cellSize);
                moveDirection = math.normalizesafe(escapeCenter - localTransform.Position);
            }
            else if (direction > 0)
            {
                int2 offset = DirectionOffset(direction);
                // Steer through the centre of the next cell instead of using a
                // coarse cardinal/diagonal vector. This gives fast units a real
                // turning point and stops them pressing into concave corners.
                float3 nextCellCenter = new float3(
                    gridOrigin.x + (x + offset.x + 0.5f) * cellSize,
                    localTransform.Position.y,
                    gridOrigin.z + (z + offset.y + 0.5f) * cellSize);
                float3 flowDirection = math.normalizesafe(nextCellCenter - localTransform.Position,
                    new float3(offset.x, 0f, offset.y));
                moveDirection = flowDirection;
            }
        }

        moveDirection.y = 0f;
        float3 separation = CrowdAvoidance.Calculate(entity, localTransform.Position, crowdGrid);
        float3 overtake = CalculateOvertake(entity, localTransform.Position, moveDirection,
            unitMover.moveSpeed);
        moveDirection = math.normalizesafe(moveDirection +
            separation * CrowdAvoidance.Strength + overtake,
            moveDirection);
        moveDirection = math.normalizesafe(moveDirection);

        if (unitMover.LookAtTarget)
        {
            localTransform.Rotation =
                math.slerp(localTransform.Rotation,
                    quaternion.LookRotation(moveDirection, math.up()),
                    deltaTime * unitMover.rotationSpeed);   
        }

        physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;
        
    }
    private float3 CalculateOvertake(Entity entity, float3 position, float3 forward, float selfSpeed)
    {
        float2 forward2 = math.normalizesafe(forward.xz);
        if (math.lengthsq(forward2) < 0.1f) return float3.zero;
        bool slowerUnitAhead = false;
        int2 originCell = CrowdAvoidance.PositionToCell(position, CrowdAvoidance.CellSize);
        for (int z = -1; z <= 1 && !slowerUnitAhead; z++)
        for (int x = -1; x <= 1 && !slowerUnitAhead; x++)
        {
            int2 cell = originCell + new int2(x, z);
            if (!crowdGrid.TryGetFirstValue(CrowdAvoidance.Hash(cell), out CrowdNeighbor neighbor, out var iterator))
                continue;
            do
            {
                if (neighbor.Entity == entity || math.any(neighbor.Cell != cell) ||
                    neighbor.MoveSpeed >= selfSpeed * 0.82f) continue;
                float2 delta = neighbor.Position.xz - position.xz;
                float distanceSq = math.lengthsq(delta);
                if (distanceSq > 2.25f || math.dot(forward2, delta) <= 0.15f) continue;
                slowerUnitAhead = true;
                break;
            }
            while (crowdGrid.TryGetNextValue(out neighbor, ref iterator));
        }
        if (!slowerUnitAhead) return float3.zero;

        float2 left = new(-forward2.y, forward2.x);
        bool leftOpen = IsWalkable(position.xz + left * 0.75f);
        bool rightOpen = IsWalkable(position.xz - left * 0.75f);
        if (!leftOpen && !rightOpen) return float3.zero;
        float side = leftOpen && rightOpen ? ((entity.Index & 1) == 0 ? 1f : -1f) : leftOpen ? 1f : -1f;
        return new float3(left.x * side, 0f, left.y * side) * 1.65f;
    }

    private bool IsWalkable(float2 position)
    {
        int x = (int)math.floor((position.x - gridOrigin.x) / cellSize);
        int z = (int)math.floor((position.y - gridOrigin.z) / cellSize);
        return x >= 0 && z >= 0 && x < gridWidth && z < gridHeight &&
               directions[z * gridWidth + x] >= 0;
    }
    private int2 FindEscapeOffset(int x, int z, float3 targetPosition)
    {
        int2 best = int2.zero;
        float bestDistanceSq = float.MaxValue;
        for (int direction = 1; direction <= 8; direction++)
        {
            int2 offset = DirectionOffset(direction);
            int nextX = x + offset.x;
            int nextZ = z + offset.y;
            if (nextX < 0 || nextZ < 0 || nextX >= gridWidth || nextZ >= gridHeight) continue;
            if (directions[nextZ * gridWidth + nextX] < 0) continue;
            float2 center = new(
                gridOrigin.x + (nextX + 0.5f) * cellSize,
                gridOrigin.z + (nextZ + 0.5f) * cellSize);
            float distanceSq = math.distancesq(center, targetPosition.xz);
            if (distanceSq >= bestDistanceSq) continue;
            bestDistanceSq = distanceSq;
            best = offset;
        }
        return best;
    }
    private static int2 DirectionOffset(int direction)
    {
        return direction switch
        {
            1 => new int2(-1, 0),
            2 => new int2(1, 0),
            3 => new int2(0, -1),
            4 => new int2(0, 1),
            5 => new int2(-1, -1),
            6 => new int2(-1, 1),
            7 => new int2(1, -1),
            8 => new int2(1, 1),
            _ => int2.zero,
        };
    }
}

[BurstCompile]
[WithAll(typeof(Mob))]
public partial struct DirectMobUnitMoverJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public NativeParallelMultiHashMap<int, CrowdNeighbor>.ReadOnly crowdGrid;

    public void Execute(Entity entity, ref LocalTransform localTransform, in UnitMover unitMover,
        ref PhysicsVelocity physicsVelocity)
    {
        float3 moveDirection = unitMover.targetPosition - localTransform.Position;
        if (math.lengthsq(moveDirection) <= UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ)
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }

        moveDirection = math.normalize(moveDirection);
        moveDirection = math.normalizesafe(moveDirection +
            CrowdAvoidance.Calculate(entity, localTransform.Position, crowdGrid) * CrowdAvoidance.Strength,
            moveDirection);
        if (unitMover.LookAtTarget)
            localTransform.Rotation = math.slerp(localTransform.Rotation,
                quaternion.LookRotation(moveDirection, math.up()), deltaTime * unitMover.rotationSpeed);
        physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;
    }
}

[BurstCompile]
[WithNone(typeof(Mob))]
public partial struct NonMobUnitMoverJob : IJobEntity
{
    public float deltaTime;

    public void Execute(ref LocalTransform localTransform, in UnitMover unitMover, ref PhysicsVelocity physicsVelocity)
    {
        float3 moveDirection = unitMover.targetPosition - localTransform.Position;
        if (math.lengthsq(moveDirection) <= UnitMoverSystem.REACHED_TARGET_POSITION_DISTANCE_SQ)
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }

        moveDirection = math.normalize(moveDirection);
        if (unitMover.LookAtTarget)
            localTransform.Rotation = math.slerp(localTransform.Rotation,
                quaternion.LookRotation(moveDirection, math.up()), deltaTime * unitMover.rotationSpeed);
        physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;
    }
}
