// PENDIENTE DE COMPILAR EN UNITY (ver Docs/PendienteDeVerificar.md). Componentes del soldado: Vol. III §3.2.
using Unity.Entities;
using Unity.Mathematics;

namespace Estandartes.Sim
{
    public struct SoldierTag : IComponentData {}

    public struct Team : IComponentData { public byte Value; }

    public enum SquadRole : byte { Soldier, Captain, StandardBearer, Musician, Crew }

    public struct SquadMember : IComponentData
    {
        public Entity Squad;
        public ushort SlotIndex;
        public byte Rank;
        public SquadRole Role;
    }

    /// <summary>Posición de simulación (tick fijo). La presentación interpola entre Previous y Current.</summary>
    public struct SimPosition : IComponentData
    {
        public float3 Current;
        public float3 Previous;
    }

    public struct SimFacing : IComponentData
    {
        public float2 Current;   // dirección en el plano XZ, normalizada
        public float2 Previous;
    }

    public struct Health : IComponentData { public float Value, Max; }
    public struct Stamina : IComponentData { public float Value, Max; }
    public struct Fatigue : IComponentData { public float Value; }

    public enum MoraleStateId : byte { Steady, Wavering, Retreating, Routing, Surrendered }

    public struct Morale : IComponentData
    {
        public float Value;
        public MoraleStateId State;
    }

    public struct Personality : IComponentData
    {
        public half Courage, Aggression, Skill, Discipline, Experience;
    }

    public struct Locomotion : IComponentData
    {
        public float3 Velocity;
        public float WalkSpeed;
        public float RunSpeed;
        public byte NavLayer;
    }

    public enum MeleePhase : byte { Idle, Windup, Strike, Recover, Block, Parry, Stagger }

    public struct MeleeState : IComponentData
    {
        public Entity Target;
        public MeleePhase Phase;
        public byte Direction;
        public float PhaseTime;
        public float PendingReaction;
    }

    public struct RangedState : IComponentData
    {
        public byte Ammo;
        public float ReloadTimer;
        public float2 AimBias;
        public Entity Target;
    }

    public enum SoldierState : byte { InFormation, Seeking, Dueling, Shooting, Retreating, Fleeing, Surrendered, Wounded, Dead }

    public struct SoldierBrain : IComponentData
    {
        public SoldierState State;
        public uint NextThinkTick;
        public byte Bucket;
    }

    /// <summary>Nivel de detalle de IA (Vol. III §14).</summary>
    public struct AILod : IComponentData
    {
        public byte Level;
        public float Relevance;
        public float DowngradeTimer;
    }

    /// <summary>Despertar para pensar ya (golpe, orden, amenaza).</summary>
    public struct Wake : IComponentData, IEnableableComponent {}

    /// <summary>Muerto: se desactiva en lugar de destruir la entidad en mitad del tick.</summary>
    public struct Dead : IComponentData, IEnableableComponent {}

    /// <summary>Índice de definición de unidad en la tabla de blobs del contenido cargado.</summary>
    public struct UnitRef : IComponentData { public int UnitIndex; }
}
