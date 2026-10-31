using System;

namespace Estandartes.Core.Navigation
{
    /// <summary>Rejilla de costes de navegación (Vol. III §15.1). 255 = intransitable.</summary>
    public sealed class CostGrid
    {
        public const byte Blocked = 255;
        public readonly int Width, Height;
        public readonly float CellSize;
        public readonly byte[] Costs;

        public CostGrid(int width, int height, float cellSize = 1f, byte initial = 1)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            Costs = new byte[width * height];
            for (int i = 0; i < Costs.Length; i++) Costs[i] = initial;
        }

        public int Index(int x, int y) => y * Width + x;
        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public bool Walkable(int x, int y) => InBounds(x, y) && Costs[Index(x, y)] != Blocked;
        public byte Get(int x, int y) => Costs[Index(x, y)];
        public void Set(int x, int y, byte cost) => Costs[Index(x, y)] = cost;

        public void FillRect(int x0, int y0, int x1, int y1, byte cost)
        {
            for (int y = Math.Max(0, y0); y <= Math.Min(Height - 1, y1); y++)
                for (int x = Math.Max(0, x0); x <= Math.Min(Width - 1, x1); x++)
                    Costs[Index(x, y)] = cost;
        }

        /// <summary>Coste base por pendiente (grados) y material del terreno (Vol. I §10.3).</summary>
        public static byte TerrainCost(float slopeDegrees, float materialCost)
        {
            if (slopeDegrees > 40f) return Blocked;
            float c = materialCost * (1f + slopeDegrees / 15f);
            return (byte)SimMath.Clamp((int)Math.Round(c), 1, 254);
        }

        // 8 vecinos: E, NE, N, NO, O, SO, S, SE
        public static readonly int[] DX = { 1, 1, 0, -1, -1, -1, 0, 1 };
        public static readonly int[] DY = { 0, 1, 1, 1, 0, -1, -1, -1 };
    }
}
