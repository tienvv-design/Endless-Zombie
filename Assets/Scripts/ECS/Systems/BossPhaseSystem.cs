using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
internal partial struct BossPhaseSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (boss, mob, mover, attack) in
                 SystemAPI.Query<RefRW<BossPhase>, RefRO<Mob>, RefRW<UnitMover>, RefRW<KamikazeUnit>>())
        {
            float healthRatio = mob.ValueRO.MaxHealth > 0
                ? (float)mob.ValueRO.Health / mob.ValueRO.MaxHealth
                : 0f;
            byte desiredPhase = healthRatio <= boss.ValueRO.PhaseThreeHealthRatio
                ? (byte)3
                : healthRatio <= boss.ValueRO.PhaseTwoHealthRatio ? (byte)2 : (byte)1;

            if (desiredPhase == boss.ValueRO.CurrentPhase)
                continue;

            boss.ValueRW.CurrentPhase = desiredPhase;
            int phaseSteps = desiredPhase - 1;
            mover.ValueRW.moveSpeed = boss.ValueRO.BaseMoveSpeed *
                                      math.pow(boss.ValueRO.SpeedMultiplierPerPhase, phaseSteps);
            attack.ValueRW.Damage = math.max(1, (int)math.round(
                boss.ValueRO.BaseDamage * math.pow(boss.ValueRO.DamageMultiplierPerPhase, phaseSteps)));
        }
    }
}
