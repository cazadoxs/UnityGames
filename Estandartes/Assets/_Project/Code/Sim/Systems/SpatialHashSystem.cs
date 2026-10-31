// PENDIENTE DE COMPILAR EN UNITY. Reconstruye el hash espacial cada tick en paralelo (paso 1.5).
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Estandartes.Sim
{
    [UpdateInGroup(typeof(SensingGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct SpatialHashSystem : ISystem
    {
        public const float CellSize = 4f;
        private EntityQuery _soldiers;

        public void OnCreate(ref SystemState state)
        {
            _soldiers = SystemAPI.QueryBuilder().WithAll<SoldierTag, SimPosition, Team>().WithDisabled<Dead>().Build();
            var e = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(e, new SpatialHash
            {
                Map = new NativeParallelMultiHashMap<int, SpatialEntry>(16384, Allocator.Persistent),
                CellSize = CellSize,
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonRW<SpatialHash>(out var hash) && hash.ValueRO.Map.IsCreated)
                hash.ValueRW.Map.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hash = SystemAPI.GetSingletonRW<SpatialHash>();
            int count = _soldiers.CalculateEntityCount();
            state.Dependency.Complete();
            hash.ValueRW.Map.Clear();
            if (hash.ValueRO.Map.Capacity < count) hash.ValueRW.Map.Capacity = count * 2;
            state.Dependency = new FillJob { Writer = hash.ValueRW.Map.AsParallelWriter(), CellSize = hash.ValueRO.CellSize }
                .ScheduleParallel(_soldiers, state.Dependency);
        }

        [BurstCompile]
        private partial struct FillJob : IJobEntity
        {
            public NativeParallelMultiHashMap<int, SpatialEntry>.ParallelWriter Writer;
            public float CellSize;

            private void Execute(Entity e, in SimPosition p, in Team t)
            {
                Writer.Add(SpatialHash.Key(p.Current, CellSize), new SpatialEntry { Entity = e, Position = p.Current, Team = t.Value });
            }
        }
    }
}
