using System;
using System.Collections.Generic;

namespace Estandartes.Core.Content
{
    /// <summary>Arquetipos de rol independientes de la época (Vol. III §20.1).</summary>
    public enum Archetype
    {
        ShockInfantry, PolearmInfantry, LineInfantry, Ranged, Skirmisher,
        ShockCavalry, LightCavalry, Artillery, Engineer, Command
    }

    public enum DamageType { Cut, Pierce, Blunt }

    public enum WeaponFamily { Blade, Polearm, Blunt, Bow, Crossbow, Thrown, MuzzleLoader, BreechLoader, TorsionArtillery, CounterweightArtillery, GunpowderArtillery }

    /// <summary>Clase base de todo contenido registrable.</summary>
    [Serializable]
    public abstract class Definition
    {
        public string Id;
        public string Name;
        /// <summary>Tipo de ID esperado (segunda parte del ID: unit, weapon…).</summary>
        [Newtonsoft.Json.JsonIgnore] public abstract string Kind { get; }
    }

    [Serializable]
    public class ArmorValues
    {
        public float Cut;
        public float Pierce;
        public float Blunt;

        public float For(DamageType t) => t == DamageType.Cut ? Cut : (t == DamageType.Pierce ? Pierce : Blunt);
    }

    [Serializable]
    public class UnitStats
    {
        public float Health = 100f;
        public float MountHealth;          // 0 si va a pie
        public float Morale = 50f;
        public float Discipline = 50f;
        public float Walk = 1.4f;
        public float Run = 5f;
    }

    [Serializable]
    public class PersonalityRanges
    {
        public float[] Courage = { 0.3f, 0.7f };
        public float[] Aggression = { 0.3f, 0.7f };
        public float[] Skill = { 0.3f, 0.7f };
        public float[] Discipline = { 0.3f, 0.7f };
        public float[] Experience = { 0.2f, 0.6f };
    }

    [Serializable]
    public class UnitDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "unit";
        public Archetype Archetype;
        public List<string> Capabilities = new List<string>();
        public List<string> Weapons = new List<string>();
        public ArmorValues Armor = new ArmorValues();
        public UnitStats Stats = new UnitStats();
        public PersonalityRanges Personality = new PersonalityRanges();
        public int SquadSize = 60;
        public int Cost = 5;
        public string Era = "medieval";
        public string Culture;
        public string Voices;

        public bool Has(string capability) => Capabilities != null && Capabilities.Contains(capability);
    }

    [Serializable]
    public class WeaponDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "weapon";
        public WeaponFamily Family;
        public DamageType DamageType;
        public float BaseDamage;
        public float Penetration;
        /// <summary>Alcance en metros (melé: longitud efectiva; distancia: alcance máximo de juego).</summary>
        public float Range;
        public float EffectiveRange;
        public float Windup = 0.5f;
        public float Strike = 0.15f;
        public float Recover = 0.4f;
        public float StaminaCost = 10f;
        public int Ammo;
        public float Reload;
        public bool Ranged;
    }

    [Serializable]
    public class SiegeEngineDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "siege";
        public int CrewMin;
        public int CrewMax;
        public int Carried;
        public float Health;
        public float RangeMin;
        public float RangeMax;
        public float Reload;
        public string Targets;
    }

    [Serializable]
    public class FactionDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "faction";
        public string Culture;
        public string Era = "medieval";
        public string Doctrine;
        public List<string> Units = new List<string>();
        public List<string> Colors = new List<string>();
    }

    [Serializable]
    public class DoctrineDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "doctrine";
        /// <summary>Peso de composición por ID de unidad (Anexo B).</summary>
        public Dictionary<string, float> Weights = new Dictionary<string, float>();
        /// <summary>Sesgo por plan HTN preferido (ID de método).</summary>
        public Dictionary<string, float> PreferredPlans = new Dictionary<string, float>();
        /// <summary>Zona de despliegue por arquetipo (Center, Wings, Front, Reserve, Artillery, Escort).</summary>
        public Dictionary<string, string> Deployment = new Dictionary<string, string>();
    }

    [Serializable]
    public class PersonalityDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "personality";
        public float CasualtyAversion = 1f;
        public float RiskAversion = 0.5f;
        /// <summary>Margen δ para sustituir el plan actual (Vol. III §6.6).</summary>
        public float Patience = 0.1f;
        public float DeceptionBias;
        public float AttackBias;
        public float DoctrineBias;
        public float MinReserve = 0.15f;
        /// <summary>Temperatura base del softmax de planes.</summary>
        public float Temperature = 0.15f;
    }

    public enum FormationGenerator { Line, ShieldWall, Column, Wedge, Schiltron, Open, Circle }

    [Serializable]
    public class FormationDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "formation";
        public FormationGenerator Generator;
        public float SpacingX = 1f;
        public float SpacingY = 1.2f;
        public int DesiredRanks = 4;
        public int Width;              // anchura fija (columna); 0 = calculada
        public bool ShieldsOverlap;
        public bool FacesOutward;
        public float Jitter;
        public List<string> RequiresCapability = new List<string>();
    }

    [Serializable]
    public class MoraleEventEntry
    {
        public string Event;
        public float Magnitude;
        /// <summary>Individual, Squad, Wing o Army.</summary>
        public string Scope = "Squad";
    }

    [Serializable]
    public class MoraleTableDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "morale";
        public List<MoraleEventEntry> Events = new List<MoraleEventEntry>();
        public Dictionary<string, float> Continuous = new Dictionary<string, float>();

        public float Magnitude(string ev)
        {
            foreach (var e in Events) if (e.Event == ev) return e.Magnitude;
            return 0f;
        }
    }

    [Serializable]
    public class CurveDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "curve";
        public Utility.CurveType Type;
        public float Slope = 1f;
        public float Exponent = 1f;
        public float X0 = 0.5f;
        public float InputMin;
        public float InputMax = 1f;
        public bool Invert;
    }
}
