// PENDIENTE DE COMPILAR EN UNITY. Interpola entre ticks de simulación para un render fluido (paso 1.4).
using Estandartes.Sim;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Estandartes.Presentation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct InterpolationSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SimClock>();

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var clock = SystemAPI.GetSingleton<SimClock>();
            float alpha = math.saturate((float)((SystemAPI.Time.ElapsedTime - clock.LastTickTime) / clock.Timestep));
            new InterpolateJob { Alpha = alpha }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct InterpolateJob : IJobEntity
        {
            public float Alpha;

            private void Execute(ref LocalTransform t, in SimPosition p, in SimFacing f)
            {
                t.Position = math.lerp(p.Previous, p.Current, Alpha);
                float2 dir = math.normalizesafe(math.lerp(f.Previous, f.Current, Alpha), new float2(0, 1));
                t.Rotation = quaternion.LookRotationSafe(new float3(dir.x, 0f, dir.y), math.up());
            }
        }
    }
}
