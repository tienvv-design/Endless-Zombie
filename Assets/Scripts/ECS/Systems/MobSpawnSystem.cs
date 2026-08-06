using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MobSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<MobSpawnSettings>();
        state.RequireForUpdate<EntityReferences>();
        state.RequireForUpdate<GameRunningTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<GameObjectInfo> goInfoBuffer))
            return;

        GameObjectInfo target = default;
        bool targetMatch = false;

        foreach (var goInfo in goInfoBuffer)
        {
            if (goInfo.ObjectType == GameObjectType.Character1)
            {
                target = goInfo;
                targetMatch = true;
                break;
            }
        }
        
        if (!targetMatch) return;
        
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Singletons
        var spawner    = SystemAPI.GetSingleton<MobSpawnSettings>();
        var references = SystemAPI.GetSingleton<EntityReferences>();

        // Time-driven difficulty
        spawner.ElapsedTime += deltaTime;

        float spawnRate = spawner.BaseSpawnRate + (spawner.ElapsedTime * spawner.RateIncrease);
        float interval  = 1f / math.max(spawnRate, 0.25f);

        spawner.Timer += deltaTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        while (spawner.Timer >= interval)
        {
            spawner.Timer -= interval;

            // Update random
            references.Random = Unity.Mathematics.Random.CreateFromIndex(
                references.Random.NextUInt()
            );

            // 50/50 mob type
            bool typeA = references.Random.NextBool();

            Entity prefab = typeA
                ? references.MobPrefabEntity
                : references.MobOrbitingPrefabEntity;

            float angle = references.Random.NextFloat(0, math.PI * 2f);

            float3 spawnPos = target.Position + new float3(
                math.cos(angle) * spawner.SpawnRadius,
                0f,
                math.sin(angle) * spawner.SpawnRadius
            );

            Entity mob = ecb.Instantiate(prefab);

            int waveIndex = (int)math.floor(spawner.ElapsedTime / math.max(1f, spawner.WaveDuration));
            int waveNumber = waveIndex + 1;
            bool isBossWave = spawner.BossWaveInterval > 0 &&
                              waveNumber % spawner.BossWaveInterval == 0;
            bool isBoss = isBossWave && spawner.LastBossWave != waveNumber;
            if (isBoss)
                spawner.LastBossWave = waveNumber;
            float eliteChance = waveNumber >= spawner.EliteStartWave
                ? math.min(spawner.EliteMaxChance,
                    spawner.EliteBaseChance + (waveNumber - spawner.EliteStartWave) * spawner.EliteChancePerWave)
                : 0f;
            bool isElite = !isBoss && references.Random.NextFloat() < eliteChance;

            Mob mobStats = SystemAPI.GetComponent<Mob>(prefab);
            float healthMultiplier = math.pow(spawner.WaveHealthMultiplier, waveIndex) *
                                     (isBoss ? spawner.BossHealthMultiplier :
                                         isElite ? spawner.EliteHealthMultiplier : 1f);
            mobStats.Health = math.max(1, (int)math.round(mobStats.Health * healthMultiplier));
            mobStats.MaxHealth = mobStats.Health;
            mobStats.SpawnTime = spawner.ElapsedTime;
            mobStats.EnemyType = isBoss ? EnemyType.Boss : isElite ? EnemyType.Elite : EnemyType.Normal;
            mobStats.GoldMultiplier = isBoss
                ? math.max(1, spawner.BossGoldMultiplier)
                : isElite ? math.max(1, spawner.EliteGoldMultiplier) : 1;
            mobStats.XPReward = isBoss ? 100 : isElite ? 25 : 5;
            mobStats.GoldReward = isBoss ? 20 : isElite ? 5 : 1;
            mobStats.CrowdControlResistance = isBoss
                ? math.saturate(spawner.BossCrowdControlResistance)
                : 0f;
            if (isBoss)
                mobStats.KnockbackResistance = math.max(mobStats.KnockbackResistance,
                    spawner.BossKnockbackResistance);
            else if (isElite)
                mobStats.KnockbackResistance = math.max(mobStats.KnockbackResistance,
                    spawner.EliteKnockbackResistance);
            ecb.SetComponent(mob, mobStats);

            if (SystemAPI.HasComponent<KamikazeUnit>(prefab))
            {
                KamikazeUnit enemyAttack = SystemAPI.GetComponent<KamikazeUnit>(prefab);
                float damageMultiplier = math.pow(spawner.WaveDamageMultiplier, waveIndex) *
                                         (isBoss ? spawner.BossDamageMultiplier :
                                             isElite ? spawner.EliteDamageMultiplier : 1f);
                enemyAttack.Damage = math.max(1, (int)math.round(enemyAttack.Damage * damageMultiplier));
                ecb.SetComponent(mob, enemyAttack);

                if (isBoss)
                {
                    UnitMover mover = SystemAPI.GetComponent<UnitMover>(prefab);
                    ecb.AddComponent(mob, new BossPhase
                    {
                        CurrentPhase = 1,
                        PhaseTwoHealthRatio = 0.66f,
                        PhaseThreeHealthRatio = 0.33f,
                        SpeedMultiplierPerPhase = math.max(1f, spawner.BossPhaseSpeedMultiplier),
                        DamageMultiplierPerPhase = math.max(1f, spawner.BossPhaseDamageMultiplier),
                        BaseMoveSpeed = mover.moveSpeed,
                        BaseDamage = enemyAttack.Damage,
                    });
                }
            }

            ecb.SetComponent(mob, new LocalTransform
            {
                Position = spawnPos,
                Rotation = quaternion.identity,
                Scale = isBoss ? math.max(1f, spawner.BossScale) :
                    isElite ? math.max(1f, spawner.EliteScale) : 1f
            });
        }

        // Write back the updated random + timers
        SystemAPI.SetSingleton(references);
        SystemAPI.SetSingleton(spawner);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
