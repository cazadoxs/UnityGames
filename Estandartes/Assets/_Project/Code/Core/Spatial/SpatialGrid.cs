using System;
using System.Collections.Generic;

namespace Estandartes.Core.Spatial
{
    /// <summary>
    /// Rejilla espacial uniforme reconstruida por ordenación por conteo (Vol. I §9.6).
    /// Implementación de referencia: el sistema ECS la reproduce con jobs Burst y NativeArrays.
    /// </summary>
    public sealed class SpatialGrid
    {
        public readonly float CellSize;
        public readonly float OriginX, OriginY;
        public readonly int Width, Height;

        private int[] _cellStart;   // Width*Height + 1
        private int[] _sorted;      // índices de agentes ordenados por celda
        private int[] _cellOf;
        private Vec2[] _positions;

        public SpatialGrid(float originX, float originY, float sizeX, float sizeY, float cellSize)
        {
            CellSize = cellSize;
            OriginX = originX;
            OriginY = originY;
            Width = Math.Max(1, (int)Math.Ceiling(sizeX / cellSize));
            Height = Math.Max(1, (int)Math.Ceiling(sizeY / cellSize));
            _cellStart = new int[Width * Height + 1];
        }

        public int CellIndex(Vec2 p)
        {
            int cx = SimMath.Clamp((int)((p.X - OriginX) / CellSize), 0, Width - 1);
            int cy = SimMath.Clamp((int)((p.Y - OriginY) / CellSize), 0, Height - 1);
            return cy * Width + cx;
        }

        /// <summary>Reconstruye la rejilla: conteo → prefijos → colocación. O(n + celdas).</summary>
        public void Build(Vec2[] positions, int count)
        {
            _positions = positions;
            if (_sorted == null || _sorted.Length < count) { _sorted = new int[count]; _cellOf = new int[count]; }
            Array.Clear(_cellStart, 0, _cellStart.Length);
            for (int i = 0; i < count; i++)
            {
                int c = CellIndex(positions[i]);
                _cellOf[i] = c;
                _cellStart[c + 1]++;
            }
            for (int c = 0; c < Width * Height; c++) _cellStart[c + 1] += _cellStart[c];
            var cursor = new int[Width * Height];
            Array.Copy(_cellStart, cursor, Width * Height);
            for (int i = 0; i < count; i++) _sorted[cursor[_cellOf[i]]++] = i;
        }

        /// <summary>Agentes a menos de <paramref name="radius"/> de <paramref name="center"/> (excluye <paramref name="exclude"/>).</summary>
        public void Query(Vec2 center, float radius, List<int> results, int exclude = -1)
        {
            results.Clear();
            float r2 = radius * radius;
            int minX = SimMath.Clamp((int)((center.X - radius - OriginX) / CellSize), 0, Width - 1);
            int maxX = SimMath.Clamp((int)((center.X + radius - OriginX) / CellSize), 0, Width - 1);
            int minY = SimMath.Clamp((int)((center.Y - radius - OriginY) / CellSize), 0, Height - 1);
            int maxY = SimMath.Clamp((int)((center.Y + radius - OriginY) / CellSize), 0, Height - 1);
            for (int cy = minY; cy <= maxY; cy++)
                for (int cx = minX; cx <= maxX; cx++)
                {
                    int c = cy * Width + cx;
                    for (int k = _cellStart[c]; k < _cellStart[c + 1]; k++)
                    {
                        int i = _sorted[k];
                        if (i != exclude && Vec2.DistanceSquared(_positions[i], center) <= r2) results.Add(i);
                    }
                }
        }

        public int CountInCell(int cell) => _cellStart[cell + 1] - _cellStart[cell];
    }
}
