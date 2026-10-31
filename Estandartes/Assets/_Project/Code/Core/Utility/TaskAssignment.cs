using System;
using System.Collections.Generic;

namespace Estandartes.Core.Utility
{
    /// <summary>Capacidad de una tarea: cuántas escuadras admite.</summary>
    public struct TaskCapacity
    {
        public int Min;
        public int Max;
    }

    /// <summary>Asignación de escuadras a tareas (Vol. III §8.3).</summary>
    public static class TaskAssignment
    {
        /// <summary>
        /// Subasta voraz. <paramref name="scores"/>[escuadra, tarea] ≥ 0 (0 = no válida). Devuelve la tarea de cada escuadra (-1 = ninguna).
        /// Primero garantiza los mínimos de cada tarea y luego reparte el resto por puntuación.
        /// </summary>
        public static int[] Greedy(float[,] scores, TaskCapacity[] capacity)
        {
            int squads = scores.GetLength(0), tasks = scores.GetLength(1);
            var result = new int[squads];
            for (int i = 0; i < squads; i++) result[i] = -1;
            var used = new int[tasks];

            var pairs = new List<(float score, int squad, int task)>();
            for (int s = 0; s < squads; s++)
                for (int t = 0; t < tasks; t++)
                    if (scores[s, t] > 0f) pairs.Add((scores[s, t], s, t));
            pairs.Sort((a, b) =>
            {
                int c = b.score.CompareTo(a.score);
                if (c != 0) return c;
                c = a.squad.CompareTo(b.squad);
                return c != 0 ? c : a.task.CompareTo(b.task);
            });

            // Pasada 1: mínimos
            foreach (var p in pairs)
            {
                if (result[p.squad] >= 0 || used[p.task] >= capacity[p.task].Min) continue;
                result[p.squad] = p.task;
                used[p.task]++;
            }
            // Pasada 2: hasta el máximo
            foreach (var p in pairs)
            {
                if (result[p.squad] >= 0 || used[p.task] >= Math.Max(capacity[p.task].Max, capacity[p.task].Min)) continue;
                result[p.squad] = p.task;
                used[p.task]++;
            }
            return result;
        }

        /// <summary>
        /// Asignación óptima uno a uno (algoritmo húngaro) maximizando la puntuación total. Para pocas escuadras (≤ 12).
        /// Devuelve la tarea de cada escuadra o -1.
        /// </summary>
        public static int[] Hungarian(float[,] scores)
        {
            int rows = scores.GetLength(0), cols = scores.GetLength(1);
            int n = Math.Max(rows, cols);
            float max = 0f;
            for (int i = 0; i < rows; i++) for (int j = 0; j < cols; j++) max = Math.Max(max, scores[i, j]);
            // Coste = max − puntuación (matriz cuadrada rellena con max)
            var cost = new double[n + 1, n + 1];
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= n; j++)
                    cost[i, j] = (i <= rows && j <= cols) ? max - scores[i - 1, j - 1] : max;

            var u = new double[n + 1]; var v = new double[n + 1];
            var p = new int[n + 1]; var way = new int[n + 1];
            for (int i = 1; i <= n; i++)
            {
                p[0] = i;
                int j0 = 0;
                var minv = new double[n + 1];
                var used = new bool[n + 1];
                for (int j = 0; j <= n; j++) minv[j] = double.PositiveInfinity;
                do
                {
                    used[j0] = true;
                    int i0 = p[j0], j1 = 0;
                    double delta = double.PositiveInfinity;
                    for (int j = 1; j <= n; j++)
                    {
                        if (used[j]) continue;
                        double cur = cost[i0, j] - u[i0] - v[j];
                        if (cur < minv[j]) { minv[j] = cur; way[j] = j0; }
                        if (minv[j] < delta) { delta = minv[j]; j1 = j; }
                    }
                    for (int j = 0; j <= n; j++)
                    {
                        if (used[j]) { u[p[j]] += delta; v[j] -= delta; }
                        else minv[j] -= delta;
                    }
                    j0 = j1;
                } while (p[j0] != 0);
                do { int j1 = way[j0]; p[j0] = p[j1]; j0 = j1; } while (j0 != 0);
            }

            var result = new int[rows];
            for (int i = 0; i < rows; i++) result[i] = -1;
            for (int j = 1; j <= n; j++)
            {
                int i = p[j];
                if (i >= 1 && i <= rows && j <= cols && scores[i - 1, j - 1] > 0f) result[i - 1] = j - 1;
            }
            return result;
        }
    }
}
