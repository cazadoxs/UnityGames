// PENDIENTE DE COMPILAR EN UNITY. Formaciones, slots, movimiento del ancla y del soldado (pasos 1.6–1.7).
// La implementación de referencia y sus pruebas están en Core/Formations/FormationSlots.cs.
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Estandartes.Sim
{
    /// <summary>Generadores de slots compatibles con Burst (mismo algoritmo que Core.FormationSlots).</summary>
    [BurstCompile]
    public static class BurstFormation
    {
        public static void Generate(in Squad squad, int count, ref DynamicBuffer<FormationSlot> slots)
        {
            slots.Clear();
            if (count <= 0) return;
            switch (squad.Formation)
            {
                case FormationKind.Column: Line(squad, count, math.max(1, (int)squad.ColumnWidth), ref slots); break;
                case FormationKind.Wedge: Wedge(squad, count, ref slots); break;
                case FormationKind.Schiltron:
                case FormationKind.Circle: Rings(squad, count, squad.Formation == FormationKind.Circle ? 2 : int.MaxValue, ref slots); break;
                default:
                    int width = math.clamp((int)math.round(count / (float)math.max(1, (int)squad.DesiredRanks)), math.min(4, count), 60);
                    Line(squad, count, width, ref slots);
                    break;
            }
        }

        private static void Line(in Squad s, int count, int width, ref DynamicBuffer<FormationSlot> slots)
        {
            width = math.min(width, count);
            for (int i = 0; i < count; i++)
            {
                int rank = i / width, col = i % width;
                int inRank = math.min(width, count - rank * width);
                slots.Add(new FormationSlot { LocalOffset = new float2((col - (inRank - 1) * 0.5f) * s.SpacingX, -rank * s.SpacingY), Rank = (byte)math.min(rank, 255) });
            }
        }

        private static void Wedge(in Squad s, int count, ref DynamicBuffer<FormationSlot> slots)
        {
            int placed = 0;
            for (int j = 0; placed < count; j++)
            {
                int inRank = math.min(2 * j + 1, count - placed);
                for (int k = 0; k < inRank; k++, placed++)
                    slots.Add(new FormationSlot { LocalOffset = new float2((k - (inRank - 1) * 0.5f) * s.SpacingX, -j * s.SpacingY), Rank = (byte)math.min(j, 255) });
            }
        }

        private static void Rings(in Squad s, int count, int maxRings, ref DynamicBuffer<FormationSlot> slots)
        {
            // Mismo cálculo que Core: anillos de dentro hacia fuera; el exterior es la primera fila.
            var ringCounts = new FixedList512Bytes<int>();
            var radii = new FixedList512Bytes<float>();
            float r0 = math.max(s.SpacingY, 1f);
            int remaining = count;
            for (int k = 0; remaining > 0 && ringCounts.Length < ringCounts.Capacity; k++)
            {
                float r = r0 + k * s.SpacingY;
                int n = math.max(1, (int)math.floor(2f * math.PI * r / s.SpacingX));
                if (k + 1 >= maxRings && remaining > n) { n = remaining; r = math.max(r, n * s.SpacingX / (2f * math.PI)); }
                n = math.min(n, remaining);
                ringCounts.Add(n); radii.Add(r);
                remaining -= n;
            }
            int outer = ringCounts.Length - 1;
            for (int k = outer; k >= 0; k--)
                for (int m = 0; m < ringCounts[k]; m++)
                {
                    float a = 2f * math.PI * m / ringCounts[k];
                    slots.Add(new FormationSlot { LocalOffset = new float2(math.sin(a), math.cos(a)) * radii[k], Rank = (byte)(outer - k) });
                }
        }

        public static float3 SlotWorld(float3 anchor, float2 facing, float2 local)
        {
            float2 right = new float2(facing.y, -facing.x);
            float2 w = right * local.x + facing * local.y;
            return anchor + new float3(w.x, 0f, w.y);
        }
    }

    /// <summary>Regenera los slots cuando cambian la formación o los efectivos y asigna ocupantes por orden de slot.</summary>
    [UpdateInGroup(typeof(SquadGroup))]
    public partial struct FormationSlotSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            foreach (var (squad, st, entity) in SystemAPI.Query<RefRO<Squad>, RefRW<SquadState>>().WithEntityAccess())
            {
                if (!st.ValueRO.SlotsDirty) continue;
                var slots = em.GetBuffer<FormationSlot>(entity);
                BurstFormation.Generate(squad.ValueRO, st.ValueRO.Alive, ref slots);
                st.ValueRW.SlotsDirty = false;
            }
            // Asignación simple: cada soldado ocupa el slot de su SlotIndex (la asignación ordenada de
            // Core.SlotAssignment se porta en el paso 1.7, al tener la escena de pruebas).
            foreach (var (member, entity) in SystemAPI.Query<RefRO<SquadMember>>().WithAll<SoldierTag>().WithDisabled<Dead>().WithEntityAccess())
            {
                if (!em.Exists(member.ValueRO.Squad)) continue;
                var slots = em.GetBuffer<FormationSlot>(member.ValueRO.Squad);
                int i = member.ValueRO.SlotIndex;
                if (i < slots.Length && slots[i].Occupant != entity)
                {
                    var s = slots[i]; s.Occupant = entity; slots[i] = s;
                }
            }
        }
    }

    /// <summary>Mueve el ancla de cada escuadra hacia el objetivo de su tarea (flow fields en el paso 1.6).</summary>
    [UpdateInGroup(typeof(SquadGroup))] [UpdateAfter(typeof(FormationSlotSystem))]
    [BurstCompile]
    public partial struct SquadMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            foreach (var (anchor, task, st) in SystemAPI.Query<RefRW<SquadAnchor>, RefRO<SquadTask>, RefRO<SquadState>>())
            {
                float3 to = task.ValueRO.Target - anchor.ValueRO.Position;
                to.y = 0f;
                float d = math.length(to);
                if (d < 0.5f) continue;
                float2 dir = to.xz / d;
                // Frena si la formación está desordenada (cohesión baja)
                float speed = anchor.ValueRO.Speed * math.lerp(0.4f, 1f, math.saturate(st.ValueRO.Cohesion));
                anchor.ValueRW.Position += new float3(dir.x, 0f, dir.y) * math.min(d, speed * dt);
                anchor.ValueRW.Facing = math.normalizesafe(math.lerp(anchor.ValueRO.Facing, dir, math.saturate(dt * 2f)), dir);
            }
        }
    }

    /// <summary>Cada soldado busca su slot con llegada suave y separación de vecinos (hash espacial).</summary>
    [UpdateInGroup(typeof(MovementGroup))]
    [BurstCompile]
    public partial struct SoldierSteeringSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SpatialHash>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hash = SystemAPI.GetSingleton<SpatialHash>();
            state.Dependency = new SteerJob
            {
                Dt = SystemAPI.Time.DeltaTime,
                Hash = hash.Map,
                CellSize = hash.CellSize,
                Anchors = SystemAPI.GetComponentLookup<SquadAnchor>(true),
                Slots = SystemAPI.GetBufferLookup<FormationSlot>(true),
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(SoldierTag))]
        [WithDisabled(typeof(Dead))]
        private partial struct SteerJob : IJobEntity
        {
            public float Dt;
            [ReadOnly] public NativeParallelMultiHashMap<int, SpatialEntry> Hash;
            public float CellSize;
            [ReadOnly] public ComponentLookup<SquadAnchor> Anchors;
            [ReadOnly] public BufferLookup<FormationSlot> Slots;

            private void Execute(Entity self, ref SimPosition pos, ref SimFacing facing, ref Locomotion loco, in SquadMember member)
            {
                if (!Anchors.HasComponent(member.Squad) || !Slots.HasBuffer(member.Squad)) return;
                var anchor = Anchors[member.Squad];
                var slots = Slots[member.Squad];
                if (member.SlotIndex >= slots.Length) return;
                float3 target = BurstFormation.SlotWorld(anchor.Position, anchor.Facing, slots[member.SlotIndex].LocalOffset);

                // Llegada: corre si está lejos, anda cerca, frena al llegar
                float3 to = target - pos.Current; to.y = 0f;
                float d = math.length(to);
                float speed = d > 6f ? loco.RunSpeed : loco.WalkSpeed * math.saturate(d / 1.5f);
                float3 desired = d > 1e-3f ? to / d * speed : float3.zero;

                // Separación con vecinos a menos de 0,8 m
                float3 sep = float3.zero;
                int2 cell = (int2)math.floor(pos.Current.xz / CellSize);
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (!Hash.TryGetFirstValue(SpatialHash.Key(cell + new int2(dx, dy)), out var e, out var it)) continue;
                        do
                        {
                            if (e.Entity == self) continue;
                            float3 off = pos.Current - e.Position; off.y = 0f;
                            float dist2 = math.lengthsq(off);
                            if (dist2 > 0.64f || dist2 < 1e-6f) continue;
                            sep += off / dist2;
                        } while (Hash.TryGetNextValue(out e, ref it));
                    }
                float3 vel = desired + sep * 0.3f;
                float max = loco.RunSpeed;
                if (math.lengthsq(vel) > max * max) vel = math.normalize(vel) * max;
                loco.Velocity = vel;
                pos.Current += vel * Dt;
                facing.Current = math.lengthsq(vel.xz) > 0.04f ? math.normalize(vel.xz) : anchor.Facing;
            }
        }
    }
}
