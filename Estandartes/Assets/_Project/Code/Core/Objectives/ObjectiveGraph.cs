using System;
using System.Collections.Generic;

namespace Estandartes.Core.Objectives
{
    /// <summary>Tipos de objetivo (Vol. I §5.1).</summary>
    public enum ObjectiveType
    {
        Capture, Hold, Destroy, Open, Defend, Escort, KillLeader, Recover, Resist, Cross, Burn, Infiltrate, Build
    }

    public enum RequireMode { And, Or }

    [Serializable]
    public class ObjectiveEffect
    {
        /// <summary>OpenPath, MoveReinforcementPoint, DisableDefenses, Victory, UnlockZone, Message…</summary>
        public string Type;
        public string Target;
        public float Value;
    }

    /// <summary>Nodo del grafo de objetivos (datos; el estado vive en <see cref="ObjectiveState"/>).</summary>
    [Serializable]
    public class ObjectiveNode
    {
        public string Id;
        public string Name;
        public ObjectiveType Type = ObjectiveType.Capture;
        public List<string> Requires = new List<string>();
        public RequireMode RequireMode = RequireMode.And;
        /// <summary>Bando que debe cumplirlo (0 o 1); -1 = cualquiera (puntos de dominación).</summary>
        public int Team = -1;
        public int InitialOwner = -1;
        /// <summary>Una vez cumplido no se puede revertir (puerta destruida).</summary>
        public bool PointOfNoReturn;
        public bool Final;
        public float CaptureTime = 60f;
        public float Radius = 20f;
        public float HoldTime;
        public string Zone;
        public float X, Y;
        public List<ObjectiveEffect> Effects = new List<ObjectiveEffect>();
    }

    /// <summary>Grafo de objetivos con dependencias Y/O, rutas alternativas y varias vías de victoria.</summary>
    [Serializable]
    public class ObjectiveGraph
    {
        public List<ObjectiveNode> Nodes = new List<ObjectiveNode>();

        public ObjectiveNode Find(string id) => Nodes.Find(n => n.Id == id);

        /// <summary>Errores estructurales: duplicados, referencias rotas, ciclos, sin objetivo final alcanzable.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var n in Nodes)
            {
                if (string.IsNullOrEmpty(n.Id)) { errors.Add("objetivo sin id"); continue; }
                if (!ids.Add(n.Id)) errors.Add($"objetivo duplicado '{n.Id}'");
            }
            foreach (var n in Nodes)
                foreach (var r in n.Requires)
                {
                    if (!ids.Contains(r)) errors.Add($"'{n.Id}' depende de '{r}', que no existe");
                    if (r == n.Id) errors.Add($"'{n.Id}' depende de sí mismo");
                }
            if (HasCycle()) errors.Add("el grafo de objetivos tiene un ciclo");
            foreach (var n in Nodes)
                if (n.Final && !IsEventuallyReachable(n.Id, new HashSet<string>()))
                    errors.Add($"el objetivo final '{n.Id}' no se puede cumplir");
            return errors;
        }

        private bool HasCycle()
        {
            var state = new Dictionary<string, int>(); // 0 sin visitar, 1 en pila, 2 terminado
            bool Visit(ObjectiveNode n)
            {
                state.TryGetValue(n.Id, out int s);
                if (s == 1) return true;
                if (s == 2) return false;
                state[n.Id] = 1;
                foreach (var r in n.Requires)
                {
                    var dep = Find(r);
                    if (dep != null && Visit(dep)) return true;
                }
                state[n.Id] = 2;
                return false;
            }
            foreach (var n in Nodes) if (Visit(n)) return true;
            return false;
        }

        private bool IsEventuallyReachable(string id, HashSet<string> visiting)
        {
            var n = Find(id);
            if (n == null || !visiting.Add(id)) return false;
            if (n.Requires.Count == 0) return true;
            bool any = false, all = true;
            foreach (var r in n.Requires)
            {
                bool ok = IsEventuallyReachable(r, new HashSet<string>(visiting));
                any |= ok;
                all &= ok;
            }
            return n.RequireMode == RequireMode.And ? all : any;
        }
    }

    /// <summary>Estado en partida de un objetivo.</summary>
    public struct ObjectiveState
    {
        public int Owner;           // -1 nadie
        public int Capturer;        // bando que está capturando, -1 nadie
        public float Progress;      // 0..1 hacia el capturador
        public bool Completed;      // cumplido por Owner (para tipos no de captura)
        public bool Locked;         // bloqueado para ambos bandos
        public float HeldTime;
    }

    /// <summary>Estado del grafo completo y cálculo de desbloqueos.</summary>
    public sealed class ObjectiveTracker
    {
        public readonly ObjectiveGraph Graph;
        private readonly Dictionary<string, int> _index = new Dictionary<string, int>();
        public readonly ObjectiveState[] States;

        public ObjectiveTracker(ObjectiveGraph graph)
        {
            Graph = graph;
            States = new ObjectiveState[graph.Nodes.Count];
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                _index[graph.Nodes[i].Id] = i;
                States[i] = new ObjectiveState { Owner = graph.Nodes[i].InitialOwner, Capturer = -1 };
            }
        }

        public int IndexOf(string id) => _index.TryGetValue(id, out int i) ? i : -1;

        public ref ObjectiveState State(string id) => ref States[_index[id]];

        /// <summary>¿Lo ha conseguido el bando? (capturado y en su poder, o completado por él).</summary>
        public bool IsAchievedBy(string id, int team)
        {
            return States[_index[id]].Owner == team;
        }

        /// <summary>¿Puede el bando trabajar en este objetivo? Requisitos Y/O cumplidos por ese bando.</summary>
        public bool IsUnlockedFor(string id, int team)
        {
            int i = _index[id];
            var node = Graph.Nodes[i];
            if (States[i].Locked) return false;
            if (node.Team >= 0 && node.Team != team && States[i].Owner != team) return false;
            if (node.Requires.Count == 0) return true;
            int met = 0;
            foreach (var r in node.Requires) if (IsAchievedBy(r, team)) met++;
            return node.RequireMode == RequireMode.And ? met == node.Requires.Count : met > 0;
        }

        /// <summary>Marca un objetivo no de captura como cumplido (puerta rota, máquina destruida…).</summary>
        public void Complete(string id, int team)
        {
            ref var s = ref State(id);
            s.Owner = team;
            s.Completed = true;
            s.Progress = 1f;
        }

        public bool FinalAchievedBy(int team)
        {
            foreach (var n in Graph.Nodes) if (n.Final && IsAchievedBy(n.Id, team)) return true;
            return false;
        }
    }
}
