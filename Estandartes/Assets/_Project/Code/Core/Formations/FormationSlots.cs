using System;
using System.Collections.Generic;
using Estandartes.Core.Content;

namespace Estandartes.Core.Formations
{
    /// <summary>Slot de formación en coordenadas locales (X lateral, Y hacia delante; el ancla en el centro de la primera fila).</summary>
    public struct Slot
    {
        public Vec2 Offset;
        public int Rank;
        /// <summary>Dirección hacia la que mira el soldado en local (erizo: hacia fuera).</summary>
        public Vec2 LocalFacing;
    }

    public struct FormationParams
    {
        public FormationGenerator Generator;
        public float SpacingX;
        public float SpacingY;
        public int DesiredRanks;
        public int Width;
        public float Jitter;
        public uint Seed;

        public static FormationParams From(FormationDefinition d, uint seed = 1) => new FormationParams
        {
            Generator = d.Generator, SpacingX = d.SpacingX, SpacingY = d.SpacingY,
            DesiredRanks = Math.Max(1, d.DesiredRanks), Width = d.Width, Jitter = d.Jitter, Seed = seed,
        };
    }

    /// <summary>Generadores de slots (Vol. III §9.2).</summary>
    public static class FormationSlots
    {
        public const int MinWidth = 4, MaxWidth = 60;

        public static void Generate(in FormationParams p, int count, List<Slot> output)
        {
            output.Clear();
            if (count <= 0) return;
            switch (p.Generator)
            {
                case FormationGenerator.Line:
                case FormationGenerator.ShieldWall:
                case FormationGenerator.Open:
                    GenerateLine(p, count, Math.Max(1, p.DesiredRanks), output);
                    break;
                case FormationGenerator.Column:
                    GenerateLineWithWidth(p, count, p.Width > 0 ? p.Width : 5, output);
                    break;
                case FormationGenerator.Wedge: GenerateWedge(p, count, output); break;
                case FormationGenerator.Schiltron: GenerateRings(p, count, output, maxRings: int.MaxValue); break;
                case FormationGenerator.Circle: GenerateRings(p, count, output, maxRings: 2); break;
            }
            if (p.Jitter > 0f)
            {
                for (int i = 0; i < output.Count; i++)
                {
                    var rng = SimRandom.For(i, 0, p.Seed);
                    var s = output[i];
                    s.Offset += new Vec2(rng.Range(-p.Jitter, p.Jitter), rng.Range(-p.Jitter, p.Jitter));
                    output[i] = s;
                }
            }
        }

        /// <summary>Anchura de línea: clamp(round(N / filas), 4, 60).</summary>
        public static int LineWidth(int count, int desiredRanks) =>
            SimMath.Clamp((int)Math.Round(count / (double)desiredRanks), Math.Min(MinWidth, count), MaxWidth);

        private static void GenerateLine(in FormationParams p, int count, int ranks, List<Slot> output) =>
            GenerateLineWithWidth(p, count, LineWidth(count, ranks), output);

        private static void GenerateLineWithWidth(in FormationParams p, int count, int width, List<Slot> output)
        {
            width = Math.Max(1, Math.Min(width, count));
            for (int i = 0; i < count; i++)
            {
                int rank = i / width;
                int col = i % width;
                int inRank = Math.Min(width, count - rank * width); // la última fila se centra
                float x = (col - (inRank - 1) / 2f) * p.SpacingX;
                output.Add(new Slot { Offset = new Vec2(x, -rank * p.SpacingY), Rank = rank, LocalFacing = Vec2.Forward });
            }
        }

        private static void GenerateWedge(in FormationParams p, int count, List<Slot> output)
        {
            int placed = 0;
            for (int j = 0; placed < count; j++)
            {
                int inRank = Math.Min(2 * j + 1, count - placed);
                for (int k = 0; k < inRank; k++, placed++)
                {
                    float x = (k - (inRank - 1) / 2f) * p.SpacingX;
                    output.Add(new Slot { Offset = new Vec2(x, -j * p.SpacingY), Rank = j, LocalFacing = Vec2.Forward });
                }
            }
        }

        private static void GenerateRings(in FormationParams p, int count, List<Slot> output, int maxRings)
        {
            // Anillos desde fuera hacia dentro: el anillo 0 (exterior) es la "primera fila".
            var rings = new List<int>();
            float r0 = Math.Max(p.SpacingY, 1f);
            int remaining = count;
            // Primero se calcula cuántos anillos hacen falta creciendo desde dentro.
            var radii = new List<float>();
            for (int k = 0; remaining > 0; k++)
            {
                float r = r0 + k * p.SpacingY;
                int n = Math.Max(1, (int)Math.Floor(2 * Math.PI * r / p.SpacingX));
                if (k + 1 >= maxRings && remaining > n)
                {
                    n = remaining; // último anillo permitido: se agranda para que quepan todos
                    r = Math.Max(r, (float)(n * p.SpacingX / (2 * Math.PI)));
                }
                n = Math.Min(n, remaining);
                rings.Add(n);
                radii.Add(r);
                remaining -= n;
            }
            int outer = rings.Count - 1;
            for (int k = outer; k >= 0; k--)
            {
                int n = rings[k];
                for (int m = 0; m < n; m++)
                {
                    float a = (float)(2 * Math.PI * m / n);
                    var dir = new Vec2(MathF.Sin(a), MathF.Cos(a));
                    output.Add(new Slot { Offset = dir * radii[k], Rank = outer - k, LocalFacing = dir });
                }
            }
        }
    }

    /// <summary>Asignación de soldados a slots y cohesión (Vol. III §9.3 y §9.5).</summary>
    public static class SlotAssignment
    {
        /// <summary>
        /// Empareja soldados y slots en O(n log n): slots por fila y posición lateral; soldados por prioridad de fila
        /// (mayor = más adelante) y por su posición lateral proyectada en la nueva formación.
        /// Devuelve, para cada slot, el índice del soldado asignado.
        /// </summary>
        public static int[] Assign(IReadOnlyList<Slot> slots, IReadOnlyList<Vec2> soldierLocalPositions, IReadOnlyList<float> frontPriority)
        {
            int n = Math.Min(slots.Count, soldierLocalPositions.Count);
            var slotOrder = new int[slots.Count];
            for (int i = 0; i < slotOrder.Length; i++) slotOrder[i] = i;
            Array.Sort(slotOrder, (a, b) =>
            {
                int c = slots[a].Rank.CompareTo(slots[b].Rank);
                return c != 0 ? c : slots[a].Offset.X.CompareTo(slots[b].Offset.X);
            });

            var soldierOrder = new int[soldierLocalPositions.Count];
            for (int i = 0; i < soldierOrder.Length; i++) soldierOrder[i] = i;
            Array.Sort(soldierOrder, (a, b) =>
            {
                int c = frontPriority[b].CompareTo(frontPriority[a]);
                return c != 0 ? c : a.CompareTo(b);
            });

            var result = new int[slots.Count];
            for (int i = 0; i < result.Length; i++) result[i] = -1;

            // Por bloques de fila: los soldados con más prioridad llenan las primeras filas,
            // y dentro de cada fila se ordenan lateralmente para minimizar cruces.
            int cursor = 0;
            int si = 0;
            while (si < n)
            {
                int rank = slots[slotOrder[si]].Rank;
                int end = si;
                while (end < n && slots[slotOrder[end]].Rank == rank) end++;
                int len = end - si;
                var group = new int[len];
                Array.Copy(soldierOrder, cursor, group, 0, len);
                Array.Sort(group, (a, b) =>
                {
                    int c = soldierLocalPositions[a].X.CompareTo(soldierLocalPositions[b].X);
                    return c != 0 ? c : a.CompareTo(b);
                });
                for (int k = 0; k < len; k++) result[slotOrder[si + k]] = group[k];
                cursor += len;
                si = end;
            }
            return result;
        }

        /// <summary>Cohesión = 1 − clamp(media(|p − p_slot|) / (2·s_x), 0, 1).</summary>
        public static float Cohesion(IReadOnlyList<Vec2> positions, IReadOnlyList<Vec2> slotPositions, float spacingX)
        {
            int n = Math.Min(positions.Count, slotPositions.Count);
            if (n == 0) return 1f;
            float sum = 0f;
            for (int i = 0; i < n; i++) sum += Vec2.Distance(positions[i], slotPositions[i]);
            return 1f - SimMath.Clamp01(sum / n / (2f * Math.Max(spacingX, 0.1f)));
        }

        /// <summary>Máximo de intercambios de rotación de filas simultáneos: ceil(W / 8).</summary>
        public static int MaxRotations(int width) => Math.Max(1, (width + 7) / 8);

        /// <summary>¿Debe rotar un soldado de primera fila? (Vol. III §9.4)</summary>
        public static bool ShouldRotate(float fatigue01, float health01, bool midStrike, float behindFatigue01) =>
            !midStrike && (fatigue01 > 0.6f || health01 < 0.35f) && behindFatigue01 < fatigue01;
    }
}
