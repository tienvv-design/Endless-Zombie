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

        if (useFlowField)
        {
            MobUnitMoverJob mobMoverJob = new MobUnitMoverJob {
                deltaTime = SystemAPI.Time.DeltaTime,
                directions = surface.Directions,
                gridOrigin = surface.Origin,
                cellSize = surface.CellSize,
                gridWidth = surface.Width,
                gridHeight = surface.Height,
            };
            state.Dependency = mobMoverJob.ScheduleParallel(state.Dependency);
        }
        else
        {
            DirectMobUnitMoverJob directMobMoverJob = new DirectMobUnitMoverJob
            {
                deltaTime = SystemAPI.Time.DeltaTime,
            };
            state.Dependency = directMobMoverJob.ScheduleParallel(state.Dependency);
        }

        NonMobUnitMoverJob nonMobMoverJob = new NonMobUnitMoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        };
        state.Dependency = nonMobMoverJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Mob))]
public partial struct MobUnitMoverJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public NativeArray<sbyte> directions;
    public float3 gridOrigin;
    public float cellSize;
    public int gridWidth;
    public int gridHeight;

    public void Execute(ref LocalTransform localTransform, in UnitMover unitMover, ref PhysicsVelocity physicsVelocity)
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
            int direction = directions[z * gridWidth + x];
            if (direction > 0)
            {
                int2 offset = DirectionOffset(direction);
                float3 flowDirection = math.normalizesafe(new float3(offset.x, 0f, offset.y));
                // Blend toward the Player so movement remains natural instead of looking grid-locked.
                moveDirection = math.normalizesafe(flowDirection * 0.85f + moveDirection * 0.15f, flowDirection);
            }
        }

        if (unitMover.LookAtTarget)
        {
            localTransform.Rotation =
                math.slerp(localTransform.Rotation,
                    quaternion.LookRotation(moveDirection, math.up()),
                    deltaTime * unitMover.rotationSpeed);   
        }

        moveDirection.y = 0f;
        physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;
        
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
