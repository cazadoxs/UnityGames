// PENDIENTE DE COMPILAR EN UNITY. LOD de IA por relevancia y cupos (Vol. III §14; paso 1.10).
// Versión inicial por distancia al foco (héroe o cámara); los cupos por presupuesto se añaden en el paso 1.10.
using Estandartes.Sim;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Estandartes.Sim.AI
{
    /// <summary>Punto de interés del jugador (héroe o cámara), escrito por la capa de juego.</summary>
    public struct PlayerFocus : IComponentData
    {
        public float3 Position;
        public float3 Forward;
    }

    [UpdateInGroup(typeof(SensingGroup))]
    [UpdateAfter(typeof(SpatialHashSystem))]
    [BurstCompile]
    public partial struct AILodAssignSystem : ISystem
    {
        public const float L0Distance = 40f, L1Distance = 120f, L2Distance = 350f, Hysteresis = 0.15f, DowngradeDelay = 2f;

        public void OnCreate(ref SystemState state)
        {
            var e = state.EntityManager.CreateEntity(typeof(PlayerFocus));
            state.EntityManager.SetComponentData(e, new PlayerFocus { Forward = new float3(0, 0, 1) });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var focus = SystemAPI.GetSingleton<PlayerFocus>();
            new AssignJob { Focus = focus, Dt = SystemAPI.Time.DeltaTime }.ScheduleParallel();
        }

        public static byte LevelFor(float distance)
        {
            if (distance <= L0Distance) return 0;
            if (distance <= L1Distance) return 1;
            if (distance <= L2Distance) return 2;
            return 3;
        }

        [BurstCompile]
        [WithAll(typeof(SoldierTag))]
        private partial struct AssignJob : IJobEntity
        {
            public PlayerFocus Focus;
            public float Dt;

            private void Execute(ref AILod lod, in SimPosition p)
            {
                float d = math.distance(p.Current.xz, Focus.Position.xz);
                // Delante de la cámara cuenta como más cerca (relevancia por estar en pantalla)
                float3 to = math.normalizesafe(p.Current - Focus.Position);
                if (math.dot(to, Focus.Forward) > 0.5f) d *= 0.8f;
                lod.Relevance = 1f - math.saturate(d / 500f);
                byte target = LevelFor(d);
                if (target < lod.Level) { lod.Level = target; lod.DowngradeTimer = 0f; return; }
                if (target > lod.Level)
                {
                    // Histéresis: bajar de nivel solo tras 2 s y con un 15 % de margen
                    float limit = lod.Level == 0 ? L0Distance : (lod.Level == 1 ? L1Distance : L2Distance);
                    if (d > limit * (1f + Hysteresis)) lod.DowngradeTimer += Dt; else lod.DowngradeTimer = 0f;
                    if (lod.DowngradeTimer >= DowngradeDelay) { lod.Level++; lod.DowngradeTimer = 0f; }
                }
            }
        }
    }
}
