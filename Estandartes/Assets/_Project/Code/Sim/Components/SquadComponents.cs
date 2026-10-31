// PENDIENTE DE COMPILAR EN UNITY. Componentes de escuadra, comandante y bando: Vol. III §3.3–§3.4.
using Unity.Entities;
using Unity.Mathematics;

namespace Estandartes.Sim
{
    public enum SquadFsm : byte { Deployed, Marching, Charging, Engaged, Withdrawing, Routing, Rallying, Surrendered }

    public enum FormationKind : byte { Line, ShieldWall, Column, Wedge, Schiltron, Open, Circle }

    public struct Squad : IComponentData
    {
        public byte Team;
        public Entity Commander, Captain, StandardBearer, Musician;
        public FormationKind Formation;
        public float SpacingX, SpacingY;
        public byte DesiredRanks;
        public byte ColumnWidth;
        public int UnitIndex;
    }

    public struct SquadState : IComponentData
    {
        public SquadFsm State;
        public float StateTime;
        public float Cohesion;
        public float MoraleAvg, FatigueAvg;
        public int Alive;
        public bool SlotsDirty;
    }

    public struct SquadAnchor : IComponentData
    {
        public float3 Position;
        public float2 Facing;
        public float Speed;
    }

    public enum TaskTypeId : byte { None, Hold, Attack, Capture, Flank, Harass, Escort, Feint, FeignedRetreat, Bombard, Engineer, Reserve, Retreat, MoveTo }

    public struct SquadTask : IComponentData
    {
        public TaskTypeId Type;
        public float3 Target;
        public Entity TargetSquad;
        public int ObjectiveIndex;
        public uint AssignedTick;
    }

    public struct SquadPerception : IComponentData
    {
        public float4 Threat;          // frente, flanco izq., flanco der., retaguardia
        public float LocalForceRatio;
        public Entity NearestEnemySquad;
        public float TimeToContact;
        public float CavalryThreat;
    }

    /// <summary>Slot de la formación en local (X lateral, Y delante) y su ocupante.</summary>
    public struct FormationSlot : IBufferElementData
    {
        public float2 LocalOffset;
        public Entity Occupant;
        public byte Rank;
    }

    public struct PendingOrder : IBufferElementData
    {
        public TaskTypeId Type;
        public float3 Target;
        public uint DeliverTick;
    }

    public struct Commander : IComponentData
    {
        public byte Team;
        public byte Wing;
        public float Coordination;
        public uint NextThinkTick;
    }

    /// <summary>Reloj de la simulación (singleton). Lo usa la presentación para interpolar.</summary>
    public struct SimClock : IComponentData
    {
        public uint Tick;
        public double LastTickTime;
        public float Timestep;
    }
}
