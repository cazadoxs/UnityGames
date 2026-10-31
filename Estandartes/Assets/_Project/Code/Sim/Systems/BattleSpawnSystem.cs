// PENDIENTE DE COMPILAR EN UNITY. Genera ejércitos de prueba en escuadras (paso 1.3).
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Estandartes.Sim
{
    /// <summary>Petición de generación creada por el authoring BattleSpawnerAuthoring.</summary>
    public struct BattleSpawner : IComponentData
    {
        public Entity SoldierPrefab;
        public int SoldiersPerTeam;
        public int SquadSize;
        public float Separation;     // distancia entre los dos ejércitos
        public FormationKind Formation;
        public uint Seed;
        public bool Done;
    }

    [UpdateInGroup(typeof(SimInputGroup))]
    public partial struct BattleSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<BattleSpawner>();

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (spawnerRW, entity) in SystemAPI.Query<RefRW<BattleSpawner>>().WithEntityAccess())
            {
                if (spawnerRW.ValueRO.Done) continue;
                var s = spawnerRW.ValueRO;
                spawnerRW.ValueRW.Done = true;
                for (byte team = 0; team < 2; team++)
                {
                    int squads = math.max(1, s.SoldiersPerTeam / math.max(1, s.SquadSize));
                    int perRow = (int)math.ceil(math.sqrt(squads));
                    float z = team == 0 ? -s.Separation * 0.5f : s.Separation * 0.5f;
                    float2 facing = team == 0 ? new float2(0, 1) : new float2(0, -1);
                    for (int q = 0; q < squads; q++)
                    {
                        float3 anchor = new float3((q % perRow - perRow * 0.5f) * 45f, 0f, z + (q / perRow) * (team == 0 ? -25f : 25f));
                        var squad = em.CreateEntity();
                        em.AddComponentData(squad, new Squad { Team = team, Formation = s.Formation, SpacingX = 1f, SpacingY = 1.2f, DesiredRanks = 4, ColumnWidth = 5 });
                        em.AddComponentData(squad, new SquadState { State = SquadFsm.Deployed, Alive = s.SquadSize, SlotsDirty = true, Cohesion = 1f, MoraleAvg = 60f });
                        em.AddComponentData(squad, new SquadAnchor { Position = anchor, Facing = facing, Speed = 1.3f });
                        em.AddComponentData(squad, new SquadTask { Type = TaskTypeId.Hold, Target = anchor });
                        em.AddComponentData(squad, new SquadPerception());
                        em.AddBuffer<FormationSlot>(squad);
                        em.AddBuffer<PendingOrder>(squad);

                        var soldiers = em.Instantiate(s.SoldierPrefab, s.SquadSize, Allocator.Temp);
                        for (int i = 0; i < soldiers.Length; i++)
                        {
                            var rng = Unity.Mathematics.Random.CreateFromIndex(s.Seed ^ (uint)(team * 100003 + q * 1009 + i));
                            float3 pos = anchor + new float3(rng.NextFloat(-15f, 15f), 0f, rng.NextFloat(-8f, 8f));
                            em.SetComponentData(soldiers[i], new Team { Value = team });
                            em.SetComponentData(soldiers[i], new SquadMember { Squad = squad, SlotIndex = (ushort)i, Rank = 0, Role = i == 0 ? SquadRole.Captain : SquadRole.Soldier });
                            em.SetComponentData(soldiers[i], new SimPosition { Current = pos, Previous = pos });
                            em.SetComponentData(soldiers[i], new SimFacing { Current = facing, Previous = facing });
                            em.SetComponentData(soldiers[i], new Personality
                            {
                                Courage = (half)rng.NextFloat(0.3f, 0.8f), Aggression = (half)rng.NextFloat(0.3f, 0.8f),
                                Skill = (half)rng.NextFloat(0.3f, 0.8f), Discipline = (half)rng.NextFloat(0.3f, 0.8f), Experience = (half)rng.NextFloat(0.1f, 0.6f),
                            });
                            em.SetComponentData(soldiers[i], new SoldierBrain { State = SoldierState.InFormation, Bucket = (byte)(i % 8) });
                        }
                        soldiers.Dispose();
                    }
                }
            }
        }
    }
}
