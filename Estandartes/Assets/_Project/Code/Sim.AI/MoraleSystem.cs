// PENDIENTE DE COMPILAR EN UNITY. Moral continua y contagio con vecinos del hash espacial (Vol. III §11).
// Las fórmulas son las de Core.Morale.MoraleModel (funciones estáticas puras, llamables desde Burst).
using Estandartes.Core.Morale;
using Estandartes.Sim;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Estandartes.Sim.AI
{
    /// <summary>Copia de la moral del tick anterior: el contagio lee esta copia para no depender del orden de los hilos.</summary>
    public struct MoralePrevious : IComponentData { public float Value; }

    [UpdateInGroup(typeof(MoraleGroup))]
    [BurstCompile]
    public partial struct MoraleContagionSystem : ISystem
    {
        public const float NeighborRadius = 6f;

        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SpatialHash>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hash = SystemAPI.GetSingleton<SpatialHash>();
            state.Dependency = new CopyJob().ScheduleParallel(state.Dependency);
            state.Dependency = new ContagionJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                Hash = hash.Map,
                CellSize = hash.CellSize,
                Previous = SystemAPI.GetComponentLookup<MoralePrevious>(true),
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct CopyJob : IJobEntity
        {
            private void Execute(ref MoralePrevious prev, in Morale m) => prev.Value = m.Value;
        }

        [BurstCompile]
        [WithAll(typeof(SoldierTag))]
        [WithDisabled(typeof(Dead))]
        private partial struct ContagionJob : IJobEntity
        {
            public float Dt;
            [ReadOnly] public NativeParallelMultiHashMap<int, SpatialEntry> Hash;
            public float CellSize;
            [ReadOnly] public ComponentLookup<MoralePrevious> Previous;

            private void Execute(Entity self, ref Morale morale, in SimPosition pos, in Team team, in Personality pers, in Fatigue fatigue)
            {
                float sum = 0f; int n = 0;
                int2 cell = (int2)math.floor(pos.Current.xz / CellSize);
                int range = (int)math.ceil(NeighborRadius / CellSize);
                for (int dy = -range; dy <= range; dy++)
                    for (int dx = -range; dx <= range; dx++)
                    {
                        if (!Hash.TryGetFirstValue(SpatialHash.Key(cell + new int2(dx, dy)), out var e, out var it)) continue;
                        do
                        {
                            if (e.Entity == self || e.Team != team.Value) continue;
                            if (math.distancesq(e.Position.xz, pos.Current.xz) > NeighborRadius * NeighborRadius) continue;
                            if (!Previous.HasComponent(e.Entity)) continue;
                            sum += Previous[e.Entity].Value; n++;
                        } while (Hash.TryGetNextValue(out e, ref it));
                    }
                var sources = new MoraleSources { Fatigue01 = fatigue.Value };
                float m = MoraleModel.Step(morale.Value, sources, n > 0 ? sum / n : 0f, n > 0, pers.Discipline, Dt);
                morale.Value = m;
                morale.State = (MoraleStateId)(byte)MoraleModel.StateFor(m, false);
            }
        }
    }
}
