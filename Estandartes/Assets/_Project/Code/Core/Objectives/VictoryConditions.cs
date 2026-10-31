using System;
using System.Collections.Generic;

namespace Estandartes.Core.Objectives
{
    public enum VictoryType { Annihilation, Domination, Core, Resist, Decapitation }

    [Serializable]
    public class VictoryCondition
    {
        public VictoryType Type;
        /// <summary>Bando que gana con esta condición (-1 = cualquiera).</summary>
        public int Team = -1;
        /// <summary>Resistir: segundos; Dominación: tickets de partida.</summary>
        public float Value;
    }

    /// <summary>Resumen del estado de la batalla para evaluar las condiciones de victoria.</summary>
    public struct BattleStatus
    {
        public float Elapsed;
        public int Alive0, Alive1;
        public int Reinforcements0, Reinforcements1;
        public float Morale0, Morale1;           // moral global 0..100
        public bool GeneralAlive0, GeneralAlive1;
        public float Tickets0, Tickets1;          // dominación
    }

    public static class VictoryEvaluator
    {
        public const float RoutMorale = 15f;

        /// <summary>Devuelve el bando ganador (0 o 1) o -1 si la batalla sigue.</summary>
        public static int Evaluate(IReadOnlyList<VictoryCondition> conditions, in BattleStatus st, ObjectiveTracker objectives)
        {
            foreach (var c in conditions)
            {
                switch (c.Type)
                {
                    case VictoryType.Annihilation:
                        bool broken0 = (st.Alive0 + st.Reinforcements0) == 0 || st.Morale0 < RoutMorale;
                        bool broken1 = (st.Alive1 + st.Reinforcements1) == 0 || st.Morale1 < RoutMorale;
                        if (broken0 && !broken1 && (c.Team < 0 || c.Team == 1)) return 1;
                        if (broken1 && !broken0 && (c.Team < 0 || c.Team == 0)) return 0;
                        break;
                    case VictoryType.Domination:
                        if (st.Tickets0 <= 0 && (c.Team < 0 || c.Team == 1)) return 1;
                        if (st.Tickets1 <= 0 && (c.Team < 0 || c.Team == 0)) return 0;
                        break;
                    case VictoryType.Core:
                        if (objectives != null)
                            for (int t = 0; t < 2; t++)
                                if ((c.Team < 0 || c.Team == t) && objectives.FinalAchievedBy(t)) return t;
                        break;
                    case VictoryType.Resist:
                        if (c.Team >= 0 && st.Elapsed >= c.Value) return c.Team;
                        break;
                    case VictoryType.Decapitation:
                        if (!st.GeneralAlive0 && (c.Team < 0 || c.Team == 1)) return 1;
                        if (!st.GeneralAlive1 && (c.Team < 0 || c.Team == 0)) return 0;
                        break;
                }
            }
            return -1;
        }

        /// <summary>Drenaje de tickets por dominación: cada punto controlado drena <paramref name="ratePerPoint"/>/s al rival.</summary>
        public static void DrainTickets(ref BattleStatus st, int pointsHeld0, int pointsHeld1, float ratePerPoint, float dt)
        {
            int diff = pointsHeld0 - pointsHeld1;
            if (diff > 0) st.Tickets1 = Math.Max(0f, st.Tickets1 - diff * ratePerPoint * dt);
            else if (diff < 0) st.Tickets0 = Math.Max(0f, st.Tickets0 + diff * ratePerPoint * dt);
        }
    }
}
