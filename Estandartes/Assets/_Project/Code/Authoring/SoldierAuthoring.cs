// PENDIENTE DE COMPILAR EN UNITY. Prefab de soldado de pruebas (cápsula) y su baker (paso 1.3).
using Estandartes.Sim;
using Estandartes.Sim.AI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Estandartes.Authoring
{
    public class SoldierAuthoring : MonoBehaviour
    {
        public float Health = 100f;
        public float WalkSpeed = 1.4f;
        public float RunSpeed = 5f;
        public float Morale = 60f;

        private class Baker : Baker<SoldierAuthoring>
        {
            public override void Bake(SoldierAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<SoldierTag>(e);
                AddComponent(e, new Team());
                AddComponent(e, new SquadMember());
                AddComponent(e, new SimPosition());
                AddComponent(e, new SimFacing { Current = new float2(0, 1), Previous = new float2(0, 1) });
                AddComponent(e, new Health { Value = a.Health, Max = a.Health });
                AddComponent(e, new Stamina { Value = 100f, Max = 100f });
                AddComponent(e, new Fatigue());
                AddComponent(e, new Morale { Value = a.Morale });
                AddComponent(e, new MoralePrevious { Value = a.Morale });
                AddComponent(e, new Personality());
                AddComponent(e, new Locomotion { WalkSpeed = a.WalkSpeed, RunSpeed = a.RunSpeed });
                AddComponent(e, new MeleeState());
                AddComponent(e, new SoldierBrain());
                AddComponent(e, new AILod());
                AddComponent(e, new UnitRef());
                AddComponent<Wake>(e);
                SetComponentEnabled<Wake>(e, false);
                AddComponent<Dead>(e);
                SetComponentEnabled<Dead>(e, false);
            }
        }
    }

    public class BattleSpawnerAuthoring : MonoBehaviour
    {
        public GameObject SoldierPrefab;
        public int SoldiersPerTeam = 2500;
        public int SquadSize = 60;
        public float Separation = 300f;
        public FormationKind Formation = FormationKind.Line;
        public uint Seed = 1;

        private class Baker : Baker<BattleSpawnerAuthoring>
        {
            public override void Bake(BattleSpawnerAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new BattleSpawner
                {
                    SoldierPrefab = GetEntity(a.SoldierPrefab, TransformUsageFlags.Dynamic),
                    SoldiersPerTeam = a.SoldiersPerTeam,
                    SquadSize = a.SquadSize,
                    Separation = a.Separation,
                    Formation = a.Formation,
                    Seed = a.Seed,
                });
            }
        }
    }
}
