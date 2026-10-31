// PENDIENTE DE COMPILAR EN UNITY. Grupos y orden de ejecución por tick: Vol. III §4.
using Unity.Entities;

namespace Estandartes.Sim
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class SimInputGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(SimInputGroup))]
    public partial class SensingGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(SensingGroup))]
    public partial class CommandGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(CommandGroup))]
    public partial class SquadGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(SquadGroup))]
    public partial class SoldierDecisionGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(SoldierDecisionGroup))]
    public partial class MovementGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(MovementGroup))]
    public partial class CombatGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(CombatGroup))]
    public partial class MoraleGroup : ComponentSystemGroup {}

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))] [UpdateAfter(typeof(MoraleGroup))]
    public partial class AggregateGroup : ComponentSystemGroup {}

    /// <summary>Fija el tick de simulación a 30 Hz y crea el reloj (paso 1.4 de la guía).</summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SimulationBootstrapSystem : SystemBase
    {
        public const float TickRate = 30f;

        protected override void OnCreate()
        {
            World.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>().Timestep = 1f / TickRate;
            var e = EntityManager.CreateEntity(typeof(SimClock));
            EntityManager.SetComponentData(e, new SimClock { Tick = 0, Timestep = 1f / TickRate });
            Enabled = false;
        }

        protected override void OnUpdate() {}
    }

    /// <summary>Avanza el reloj al principio de cada tick y guarda las posiciones anteriores para interpolar.</summary>
    [UpdateInGroup(typeof(SimInputGroup), OrderFirst = true)]
    public partial struct SimClockSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SimClock>();

        [Unity.Burst.BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var clock = SystemAPI.GetSingletonRW<SimClock>();
            clock.ValueRW.Tick++;
            clock.ValueRW.LastTickTime = SystemAPI.Time.ElapsedTime;
            new StorePreviousJob().ScheduleParallel();
        }

        [Unity.Burst.BurstCompile]
        private partial struct StorePreviousJob : IJobEntity
        {
            private void Execute(ref SimPosition p, ref SimFacing f)
            {
                p.Previous = p.Current;
                f.Previous = f.Current;
            }
        }
    }
}
