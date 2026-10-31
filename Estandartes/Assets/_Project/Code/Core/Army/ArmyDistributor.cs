using System;
using System.Collections.Generic;

namespace Estandartes.Core.Army
{
    public enum UnitCategory { Infantry, Ranged, Cavalry, Siege, Command }

    public enum DistributionMode { Balanced, Counter, Historical }

    /// <summary>Una entrada de la doctrina para el reparto.</summary>
    public struct UnitShare
    {
        public string UnitId;
        public UnitCategory Category;
        public float DoctrineWeight;
        public int SquadSize;
        /// <summary>Modificador del mapa (campo abierto: caballería ×1,2…). Se ignora en modo histórico.</summary>
        public float MapModifier;
        /// <summary>Modificador del rol atacante/defensor.</summary>
        public float RoleModifier;
        /// <summary>Sesgo de contrarresto (solo modo Contrarrestar), por ejemplo +0,3.</summary>
        public float CounterBias;
    }

    public sealed class DistributionRequest
    {
        public int Total;
        public DistributionMode Mode = DistributionMode.Balanced;
        public List<UnitShare> Shares = new List<UnitShare>();
        /// <summary>General + escolta que se reservan antes del reparto.</summary>
        public int CommandReserve = 21;
        /// <summary>Soldados reservados para dotaciones de asedio (0 si no se ataca una fortificación).</summary>
        public int SiegeReserve;
        /// <summary>Margen de tamaño de escuadra para repartir sobrantes (±20 %).</summary>
        public float SquadSizeTolerance = 0.2f;
    }

    public sealed class SquadAllocation
    {
        public string UnitId;
        public UnitCategory Category;
        public int Squads;
        public int SquadSize;
        /// <summary>Soldados extra repartidos (como +1 en algunas escuadras).</summary>
        public int Extra;
        public int Soldiers => Squads * SquadSize + Extra;
        public float NormalizedWeight;
        public float IdealQuota;
    }

    public sealed class DistributionResult
    {
        public readonly List<SquadAllocation> Allocations = new List<SquadAllocation>();
        public int Command;
        public int SiegeCrew;
        public int Unassigned;

        public int Total
        {
            get
            {
                int t = Command + SiegeCrew + Unassigned;
                foreach (var a in Allocations) t += a.Soldiers;
                return t;
            }
        }

        public SquadAllocation For(string unitId) => Allocations.Find(a => a.UnitId == unitId);
    }

    /// <summary>
    /// Reparto automático del Modo Libre — "pon dos números" (Vol. I, Anexo B):
    /// pesos ajustados → cuotas → escuadras completas → mayor resto → sobrantes repartidos → restricciones.
    /// </summary>
    public static class ArmyDistributor
    {
        public static DistributionResult Distribute(DistributionRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            var result = new DistributionResult();
            if (req.Total <= 0 || req.Shares.Count == 0) { result.Unassigned = Math.Max(0, req.Total); return result; }

            // 1. Pesos ajustados y normalizados
            var shares = req.Shares;
            var weights = new float[shares.Count];
            float sum = 0f;
            for (int i = 0; i < shares.Count; i++)
            {
                var s = shares[i];
                float w = s.DoctrineWeight;
                if (req.Mode != DistributionMode.Historical) w *= s.MapModifier <= 0 ? 1f : s.MapModifier;
                w *= s.RoleModifier <= 0 ? 1f : s.RoleModifier;
                if (req.Mode == DistributionMode.Counter) w *= 1f + s.CounterBias;
                weights[i] = Math.Max(0f, w);
                sum += weights[i];
            }
            if (sum <= 0f) { result.Unassigned = req.Total; return result; }

            // 2. Reservas fijas
            result.Command = Math.Min(req.CommandReserve, req.Total);
            result.SiegeCrew = Math.Min(req.SiegeReserve, req.Total - result.Command);
            int remaining = req.Total - result.Command - result.SiegeCrew;

            // 3–4. Cuotas y escuadras completas
            var fractions = new float[shares.Count];
            for (int i = 0; i < shares.Count; i++)
            {
                var a = new SquadAllocation
                {
                    UnitId = shares[i].UnitId,
                    Category = shares[i].Category,
                    SquadSize = Math.Max(1, shares[i].SquadSize),
                    NormalizedWeight = weights[i] / sum,
                };
                a.IdealQuota = a.NormalizedWeight * remaining;
                float ideal = a.IdealQuota / a.SquadSize;
                a.Squads = (int)Math.Floor(ideal);
                fractions[i] = ideal - a.Squads;
                result.Allocations.Add(a);
            }
            int leftover = remaining;
            foreach (var a in result.Allocations) leftover -= a.Squads * a.SquadSize;

            // 5. Mayor resto: escuadras extra a las fracciones mayores mientras quepan
            var order = new List<int>();
            for (int i = 0; i < shares.Count; i++) if (weights[i] > 0) order.Add(i);
            order.Sort((x, y) =>
            {
                int c = fractions[y].CompareTo(fractions[x]);
                return c != 0 ? c : string.CompareOrdinal(shares[x].UnitId, shares[y].UnitId);
            });
            foreach (int i in order)
            {
                var a = result.Allocations[i];
                if (a.SquadSize <= leftover) { a.Squads++; leftover -= a.SquadSize; }
            }

            // 7 (antes de repartir sobrantes). Mínimo una escuadra de cada tipo con peso, si es posible
            foreach (int i in order)
            {
                var a = result.Allocations[i];
                if (a.Squads > 0) continue;
                if (a.SquadSize <= leftover) { a.Squads = 1; leftover -= a.SquadSize; continue; }
                // Tomar una escuadra del tipo con más escuadras (si le sobra) y convertirla
                SquadAllocation donor = null;
                foreach (var b in result.Allocations) if (b.Squads > 1 && (donor == null || b.Squads > donor.Squads)) donor = b;
                if (donor != null && donor.SquadSize + leftover >= a.SquadSize)
                {
                    donor.Squads--;
                    leftover += donor.SquadSize - a.SquadSize;
                    a.Squads = 1;
                }
            }

            // 6. Sobrantes: +1 soldado por escuadra, por orden de peso, sin superar la tolerancia
            var byWeight = new List<SquadAllocation>(result.Allocations);
            byWeight.Sort((x, y) => y.NormalizedWeight.CompareTo(x.NormalizedWeight));
            bool progress = true;
            while (leftover > 0 && progress)
            {
                progress = false;
                foreach (var a in byWeight)
                {
                    if (leftover == 0) break;
                    int cap = (int)Math.Floor(a.Squads * a.SquadSize * req.SquadSizeTolerance);
                    if (a.Squads > 0 && a.Extra < cap) { a.Extra++; leftover--; progress = true; }
                }
            }
            result.Unassigned = leftover;
            return result;
        }
    }
}
