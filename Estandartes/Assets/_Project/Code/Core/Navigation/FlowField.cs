using System;
using System.Collections.Generic;

namespace Estandartes.Core.Navigation
{
    /// <summary>
    /// Flow field (Vol. III §15.2): campo de integración (Dijkstra desde los destinos) y campo de direcciones.
    /// Implementación de referencia; el sistema ECS la ejecuta por sectores en jobs Burst.
    /// </summary>
    public sealed class FlowField
    {
        public const byte NoDirection = 255;
        public readonly CostGrid Grid;
        public readonly float[] Integration;
        public readonly byte[] Directions;

        public FlowField(CostGrid grid)
        {
            Grid = grid;
            Integration = new float[grid.Width * grid.Height];
            Directions = new byte[grid.Width * grid.Height];
        }

        /// <summary>
        /// Calcula integración y direcciones hacia <paramref name="goals"/> (índices de celda).
        /// <paramref name="mask"/> opcional limita el cálculo a ciertas celdas (corredor de sectores).
        /// </summary>
        public void Compute(IReadOnlyList<int> goals, bool[] mask = null)
        {
            var g = Grid;
            for (int i = 0; i < Integration.Length; i++) { Integration[i] = float.PositiveInfinity; Directions[i] = NoDirection; }
            var heap = new MinHeap();
            foreach (int goal in goals)
            {
                if (g.Costs[goal] == CostGrid.Blocked) continue;
                Integration[goal] = 0f;
                heap.Push(0f, goal);
            }
            while (heap.Count > 0)
            {
                var (d, idx) = heap.Pop();
                if (d > Integration[idx]) continue;
                int x = idx % g.Width, y = idx / g.Width;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + CostGrid.DX[k], ny = y + CostGrid.DY[k];
                    if (!g.Walkable(nx, ny)) continue;
                    bool diagonal = (k & 1) == 1;
                    // Sin cortar esquinas por diagonales bloqueadas
                    if (diagonal && (!g.Walkable(x + CostGrid.DX[k], y) || !g.Walkable(x, y + CostGrid.DY[k]))) continue;
                    int n = g.Index(nx, ny);
                    if (mask != null && !mask[n]) continue;
                    float step = g.Costs[n] * (diagonal ? SimMath.Sqrt2 : 1f);
                    float nd = d + step;
                    if (nd < Integration[n]) { Integration[n] = nd; heap.Push(nd, n); }
                }
            }
            // Direcciones: hacia el vecino de menor integración
            for (int idx = 0; idx < Integration.Length; idx++)
            {
                if (float.IsPositiveInfinity(Integration[idx]) || Integration[idx] == 0f) continue;
                int x = idx % g.Width, y = idx / g.Width;
                float best = Integration[idx];
                byte dir = NoDirection;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + CostGrid.DX[k], ny = y + CostGrid.DY[k];
                    if (!g.InBounds(nx, ny)) continue;
                    if ((k & 1) == 1 && (!g.Walkable(x + CostGrid.DX[k], y) || !g.Walkable(x, y + CostGrid.DY[k]))) continue;
                    float v = Integration[g.Index(nx, ny)];
                    if (v < best) { best = v; dir = (byte)k; }
                }
                Directions[idx] = dir;
            }
        }

        /// <summary>Dirección mundial normalizada en una celda (cero si no hay).</summary>
        public Vec2 DirectionAt(int cellIndex)
        {
            byte d = Directions[cellIndex];
            if (d == NoDirection) return Vec2.Zero;
            return new Vec2(CostGrid.DX[d], CostGrid.DY[d]).Normalized;
        }

        /// <summary>Sigue el campo desde una celda; devuelve el número de pasos hasta el destino o -1.</summary>
        public int Trace(int startCell, int maxSteps = 100000)
        {
            int idx = startCell, steps = 0;
            while (Integration[idx] > 0f)
            {
                byte d = Directions[idx];
                if (d == NoDirection || ++steps > maxSteps) return -1;
                idx = Grid.Index(idx % Grid.Width + CostGrid.DX[d], idx / Grid.Width + CostGrid.DY[d]);
            }
            return steps;
        }
    }
}
