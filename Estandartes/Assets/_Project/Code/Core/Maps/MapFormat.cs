using System;
using System.Collections.Generic;
using Estandartes.Core.Content;
using Estandartes.Core.Objectives;

namespace Estandartes.Core.Maps
{
    /// <summary>Tipos de zona (Vol. I §4.4).</summary>
    public enum ZoneType { Conquest, Warrior, Deployment, Reserve, Artillery, Supply, Ambush, Reinforcement, Retreat, Forbidden }

    [Serializable]
    public class MapManifest
    {
        public string Id;
        public string Name;
        public string Version = "1.0.0";
        public int FormatVersion = MapPackage.CurrentFormatVersion;
        public string Era = "medieval";
        public float SizeX = 2048f;
        public float SizeY = 2048f;
        public string Author;
        public List<string> Dependencies = new List<string>();
    }

    [Serializable]
    public class ZoneData
    {
        public string Id;
        public string Name;
        public ZoneType Type;
        public int Owner = -1;
        public List<Vec2> Polygon = new List<Vec2>();
    }

    [Serializable]
    public class AiHint
    {
        /// <summary>HighGround, Chokepoint, FlankRoute, DefensiveLine, Ambush, Artillery, RetreatRoute.</summary>
        public string Type;
        public List<Vec2> Points = new List<Vec2>();
        public int Team = -1;
        /// <summary>auto (análisis del terreno) o manual.</summary>
        public string Source = "manual";
        public float Value = 1f;
    }

    [Serializable]
    public class NavLinkData
    {
        public string Id;
        /// <summary>Stair, Ladder, SiegeTowerBridge, Breach, Postern, Bridge.</summary>
        public string Type;
        public Vec2 From;
        public Vec2 To;
        public int FromLayer;
        public int ToLayer;
        public int Capacity = 1;
        public float TraverseTime = 2f;
    }

    [Serializable]
    public class EventCondition
    {
        /// <summary>Time, ObjectiveAchieved, ObjectiveLost, ZoneControlled, MoraleBelow, LeaderDead, Casualties, HeroInZone, EventFired.</summary>
        public string Type;
        public string Target;
        public int Team = -1;
        public float Value;
    }

    [Serializable]
    public class EventAction
    {
        /// <summary>Reinforcements, Weather, TimeOfDay, Message, Narration, EnableObjective, DisableObjective, OpenGate, CloseGate, SetPlan, SetPersonality, Signal, MoraleEvent.</summary>
        public string Type;
        public string Target;
        public int Team = -1;
        public float Value;
        public string Text;
    }

    [Serializable]
    public class ScenarioEvent
    {
        public string Id;
        public RequireMode Mode = RequireMode.And;
        public List<EventCondition> Conditions = new List<EventCondition>();
        public List<EventAction> Actions = new List<EventAction>();
        public bool Once = true;
    }

    [Serializable]
    public class CommanderSetup
    {
        public string Name;
        public string Wing;
        public string Personality;
        public float Coordination = 0.8f;
    }

    [Serializable]
    public class SquadSetup
    {
        public string Unit;
        public int Count = 1;
        public string Wing;
        public string Zone;
        public string Role = "Warrior";
        public string InitialOrder = "Hold";
    }

    [Serializable]
    public class ArmySetup
    {
        public int Team;
        public string Faction;
        public string GeneralName;
        public string GeneralPersonality;
        public string HtnDomain;
        public int Total;
        /// <summary>Quick (reparto automático por doctrina), Detailed o Planned.</summary>
        public string CompositionMode = "Quick";
        public List<SquadSetup> Squads = new List<SquadSetup>();
        public List<CommanderSetup> Commanders = new List<CommanderSetup>();
        public int Reinforcements;
        public bool PlayerSide;
    }

    [Serializable]
    public class ScenarioRules
    {
        public List<VictoryCondition> Victory = new List<VictoryCondition>();
        public float TimeLimit;
        public bool FriendlyFire = true;
        public bool LimitedAmmo = true;
        public string Weather = "Clear";
        public string TimeOfDay = "Morning";
        public float Temperature = 15f;
    }

    /// <summary>Escenario jugable de un mapa (Vol. V §3.9). También es contenido registrable.</summary>
    [Serializable]
    public class ScenarioDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "scenario";
        public string Map;
        public string Era = "medieval";
        public string Description;
        public string Date;
        public string Protagonist;
        public string PreferredPreset = "Alto";
        public List<ArmySetup> Armies = new List<ArmySetup>();
        public List<ZoneData> Zones = new List<ZoneData>();
        public ObjectiveGraph Objectives = new ObjectiveGraph();
        public List<ScenarioEvent> Events = new List<ScenarioEvent>();
        public List<AiHint> AiHints = new List<AiHint>();
        public ScenarioRules Rules = new ScenarioRules();
        public List<string> Phases = new List<string>();
        public List<string> SecondaryObjectives = new List<string>();

        /// <summary>Errores del escenario (grafo, referencias internas).</summary>
        public List<string> Validate()
        {
            var errors = new List<string>(Objectives.Validate());
            var zoneIds = new HashSet<string>();
            foreach (var z in Zones) if (!zoneIds.Add(z.Id)) errors.Add($"zona duplicada '{z.Id}'");
            foreach (var n in Objectives.Nodes)
                if (n.Zone != null && !zoneIds.Contains(n.Zone)) errors.Add($"objetivo '{n.Id}' en zona desconocida '{n.Zone}'");
            var objIds = new HashSet<string>();
            foreach (var n in Objectives.Nodes) objIds.Add(n.Id);
            var eventIds = new HashSet<string>();
            foreach (var e in Events) eventIds.Add(e.Id);
            foreach (var e in Events)
            {
                foreach (var c in e.Conditions)
                {
                    if ((c.Type == "ObjectiveAchieved" || c.Type == "ObjectiveLost") && !objIds.Contains(c.Target))
                        errors.Add($"evento '{e.Id}': objetivo desconocido '{c.Target}'");
                    if (c.Type == "ZoneControlled" && !zoneIds.Contains(c.Target)) errors.Add($"evento '{e.Id}': zona desconocida '{c.Target}'");
                    if (c.Type == "EventFired" && !eventIds.Contains(c.Target)) errors.Add($"evento '{e.Id}': evento desconocido '{c.Target}'");
                }
                foreach (var a in e.Actions)
                    if ((a.Type == "EnableObjective" || a.Type == "DisableObjective") && !objIds.Contains(a.Target))
                        errors.Add($"evento '{e.Id}': objetivo desconocido '{a.Target}'");
            }
            if (Armies.Count != 2) errors.Add("un escenario necesita exactamente dos ejércitos");
            foreach (var s in Armies) foreach (var sq in s.Squads)
                if (sq.Zone != null && !zoneIds.Contains(sq.Zone)) errors.Add($"escuadra de '{sq.Unit}' en zona desconocida '{sq.Zone}'");
            if (Rules.Victory.Count == 0) errors.Add("sin condiciones de victoria");
            return errors;
        }
    }

    /// <summary>Referencias externas de un escenario (facciones, unidades, personalidades, dominios HTN).</summary>
    public sealed class ScenarioReferenceValidator : Mods.IReferenceValidator
    {
        public IEnumerable<string> Validate(Definition def, Mods.IContentLookup lookup)
        {
            if (!(def is ScenarioDefinition s)) yield break;
            foreach (var e in s.Validate()) yield return e;
            foreach (var a in s.Armies)
            {
                if (!lookup.TryGet<FactionDefinition>(a.Faction, out _)) yield return $"facción desconocida '{a.Faction}'";
                if (a.GeneralPersonality != null && !lookup.TryGet<PersonalityDefinition>(a.GeneralPersonality, out _))
                    yield return $"personalidad desconocida '{a.GeneralPersonality}'";
                if (a.HtnDomain != null && !lookup.TryGet<Strategy.HtnDomainDefinition>(a.HtnDomain, out _))
                    yield return $"dominio HTN desconocido '{a.HtnDomain}'";
                foreach (var c in a.Commanders)
                    if (c.Personality != null && !lookup.TryGet<PersonalityDefinition>(c.Personality, out _))
                        yield return $"personalidad desconocida '{c.Personality}'";
                foreach (var sq in a.Squads)
                    if (!lookup.TryGet<UnitDefinition>(sq.Unit, out _) && !lookup.TryGet<SiegeEngineDefinition>(sq.Unit, out _))
                        yield return $"unidad desconocida '{sq.Unit}'";
            }
        }
    }
}
