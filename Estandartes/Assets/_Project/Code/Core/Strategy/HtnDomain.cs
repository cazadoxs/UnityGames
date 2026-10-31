using System;
using System.Collections.Generic;
using System.Globalization;
using Estandartes.Core.Content;

namespace Estandartes.Core.Strategy
{
    /// <summary>Tareas primitivas que entienden los comandantes. Los mods solo pueden usar estas (Vol. III §18.1).</summary>
    public static class PrimitiveTasks
    {
        public const string Hold = "Hold", FixCenter = "FixCenter", Harass = "Harass", Flank = "Flank", Assault = "Assault",
            FeignedRetreat = "FeignedRetreat", Feint = "Feint", Envelop = "Envelop", Sacrifice = "Sacrifice", Counterattack = "Counterattack",
            Bombard = "Bombard", Engineer = "Engineer", Reserve = "Reserve", Retreat = "Retreat", Neutralize = "Neutralize", Capture = "Capture";

        public static readonly HashSet<string> All = new HashSet<string>
        {
            Hold, FixCenter, Harass, Flank, Assault, FeignedRetreat, Feint, Envelop, Sacrifice, Counterattack,
            Bombard, Engineer, Reserve, Retreat, Neutralize, Capture
        };
    }

    /// <summary>Hechos del estado abstracto que pueden usar las precondiciones.</summary>
    public static class WorldFacts
    {
        public static readonly HashSet<string> Known = new HashSet<string>
        {
            "forceRatio", "cavalryRatio", "lightCavalryRatio", "rangedRatio", "flankRoute", "enemyOnHighGround",
            "enemyDiscipline", "openTerrain", "timeFavorsUs", "enemyMustAttack", "objectiveChain", "routesToObjective",
            "breachableWall", "artilleryAvailable", "heroDominant", "ownMorale", "enemyMorale", "enemyFatigue", "isDefender"
        };
    }

    [Serializable]
    public class HtnSubtask
    {
        public string Task;
        /// <summary>Rol de los grupos: infantry, ranged, cavalry, lightCavalry, elite, artillery, all, rest.</summary>
        public string Role = "all";
        /// <summary>enemyCenter, enemyWeakFlank, enemyNearest, objective, holdPosition, rear.</summary>
        public string Target = "enemyNearest";
        public float Delay;
        public int Repeat = 1;
    }

    [Serializable]
    public class HtnMethod
    {
        public string Name;
        public List<string> Tags = new List<string>();
        public List<string> Pre = new List<string>();
        public List<HtnSubtask> Subtasks = new List<HtnSubtask>();
    }

    /// <summary>Dominio HTN como contenido editable y modificable (Vol. III §6.4 y §18).</summary>
    [Serializable]
    public class HtnDomainDefinition : Definition
    {
        [Newtonsoft.Json.JsonIgnore] public override string Kind => "htn";
        public string Root = "WinBattle";
        public Dictionary<string, List<HtnMethod>> Compound = new Dictionary<string, List<HtnMethod>>();
        public const int MaxDepth = 8;

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (!Compound.ContainsKey(Root)) errors.Add($"falta la tarea raíz '{Root}'");
            foreach (var kv in Compound)
                foreach (var m in kv.Value)
                {
                    foreach (var pre in m.Pre)
                        if (!Precondition.TryParse(pre, out var pc, out var err)) errors.Add($"{kv.Key}/{m.Name}: {err}");
                        else if (!WorldFacts.Known.Contains(pc.Fact)) errors.Add($"{kv.Key}/{m.Name}: hecho desconocido '{pc.Fact}'");
                    foreach (var st in m.Subtasks)
                        if (!Compound.ContainsKey(st.Task) && !PrimitiveTasks.All.Contains(st.Task))
                            errors.Add($"{kv.Key}/{m.Name}: tarea desconocida '{st.Task}'");
                }
            if (HasCycleOrTooDeep(out string where)) errors.Add($"ciclo o profundidad excesiva en '{where}'");
            return errors;
        }

        private bool HasCycleOrTooDeep(out string where)
        {
            where = null;
            var stack = new HashSet<string>();
            string bad = null;
            bool Visit(string task, int depth)
            {
                if (!Compound.TryGetValue(task, out var methods)) return false;
                if (depth > MaxDepth || !stack.Add(task)) { bad = task; return true; }
                foreach (var m in methods)
                    foreach (var st in m.Subtasks)
                        if (Visit(st.Task, depth + 1)) return true;
                stack.Remove(task);
                return false;
            }
            foreach (var k in Compound.Keys) if (Visit(k, 0)) { where = bad; return true; }
            return false;
        }
    }

    public struct Precondition
    {
        public string Fact;
        public string Op;
        public float Value;

        public static bool TryParse(string text, out Precondition p, out string error)
        {
            p = default; error = null;
            var parts = (text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) { error = $"precondición '{text}' no tiene la forma 'hecho op valor'"; return false; }
            if (Array.IndexOf(new[] { ">=", "<=", ">", "<", "==", "!=" }, parts[1]) < 0) { error = $"operador '{parts[1]}' no válido"; return false; }
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) { error = $"valor '{parts[2]}' no numérico"; return false; }
            p = new Precondition { Fact = parts[0], Op = parts[1], Value = v };
            return true;
        }

        public bool Holds(IReadOnlyDictionary<string, float> facts)
        {
            if (!facts.TryGetValue(Fact, out float f)) return false;
            switch (Op)
            {
                case ">=": return f >= Value;
                case "<=": return f <= Value;
                case ">": return f > Value;
                case "<": return f < Value;
                case "==": return Math.Abs(f - Value) < 1e-4f;
                default: return Math.Abs(f - Value) >= 1e-4f;
            }
        }
    }

    /// <summary>Plan descompuesto: secuencia de primitivas con el camino de métodos y sus etiquetas.</summary>
    public sealed class HtnPlan
    {
        public readonly List<string> Methods = new List<string>();
        public readonly List<string> Tags = new List<string>();
        public readonly List<HtnSubtask> Steps = new List<HtnSubtask>();
        public string Id => string.Join(">", Methods);
    }

    /// <summary>Planificador HTN: enumera todas las descomposiciones válidas (hasta un máximo) según los hechos.</summary>
    public static class HtnPlanner
    {
        public static List<HtnPlan> Plan(HtnDomainDefinition domain, IReadOnlyDictionary<string, float> facts, int maxPlans = 16)
        {
            var results = new List<HtnPlan>();
            foreach (var p in Expand(domain, domain.Root, facts, 0))
            {
                results.Add(p);
                if (results.Count >= maxPlans) break;
            }
            return results;
        }

        private static IEnumerable<HtnPlan> Expand(HtnDomainDefinition d, string task, IReadOnlyDictionary<string, float> facts, int depth)
        {
            if (depth > HtnDomainDefinition.MaxDepth || !d.Compound.TryGetValue(task, out var methods)) yield break;
            foreach (var m in methods)
            {
                bool ok = true;
                foreach (var pre in m.Pre)
                    if (!Precondition.TryParse(pre, out var pc, out _) || !pc.Holds(facts)) { ok = false; break; }
                if (!ok) continue;

                // Producto cartesiano de las alternativas de cada subtarea compuesta
                var partials = new List<HtnPlan> { NewPlan(m) };
                foreach (var st in m.Subtasks)
                {
                    var next = new List<HtnPlan>();
                    if (d.Compound.ContainsKey(st.Task))
                    {
                        var subs = new List<HtnPlan>(Expand(d, st.Task, facts, depth + 1));
                        foreach (var part in partials)
                            foreach (var sub in subs) next.Add(Merge(part, sub));
                    }
                    else
                    {
                        foreach (var part in partials)
                        {
                            for (int r = 0; r < Math.Max(1, st.Repeat); r++) part.Steps.Add(st);
                            next.Add(part);
                        }
                    }
                    partials = next;
                    if (partials.Count == 0) break;
                }
                foreach (var p in partials) yield return p;
            }
        }

        private static HtnPlan NewPlan(HtnMethod m)
        {
            var p = new HtnPlan();
            p.Methods.Add(m.Name);
            p.Tags.AddRange(m.Tags);
            return p;
        }

        private static HtnPlan Merge(HtnPlan a, HtnPlan b)
        {
            var p = new HtnPlan();
            p.Methods.AddRange(a.Methods); p.Methods.AddRange(b.Methods);
            p.Tags.AddRange(a.Tags); foreach (var t in b.Tags) if (!p.Tags.Contains(t)) p.Tags.Add(t);
            p.Steps.AddRange(a.Steps); p.Steps.AddRange(b.Steps);
            return p;
        }
    }

    /// <summary>Validación de dominios HTN al cargar contenido o mods.</summary>
    public sealed class HtnReferenceValidator : Mods.IReferenceValidator
    {
        public IEnumerable<string> Validate(Definition def, Mods.IContentLookup lookup)
        {
            if (def is HtnDomainDefinition d) foreach (var e in d.Validate()) yield return e;
        }
    }
}
