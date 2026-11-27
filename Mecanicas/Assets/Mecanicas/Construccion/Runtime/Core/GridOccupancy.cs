using System;
using System.Collections.Generic;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Rectángulo de celdas de la cuadrícula (origen en la esquina de X y Z mínimos).
    /// C# puro, sin Unity, para poder probarlo sin el editor.
    /// </summary>
    [Serializable]
    public struct CellArea : IEquatable<CellArea>
    {
        public int X;
        public int Z;
        public int SizeX;
        public int SizeZ;

        public CellArea(int x, int z, int sizeX, int sizeZ)
        {
            X = x;
            Z = z;
            SizeX = sizeX;
            SizeZ = sizeZ;
        }

        public int MaxXExclusive => X + SizeX;
        public int MaxZExclusive => Z + SizeZ;

        public bool Contains(int x, int z)
        {
            return x >= X && x < MaxXExclusive && z >= Z && z < MaxZExclusive;
        }

        public bool Equals(CellArea other)
        {
            return X == other.X && Z == other.Z && SizeX == other.SizeX && SizeZ == other.SizeZ;
        }

        public override bool Equals(object obj) => obj is CellArea other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = X;
                h = (h * 397) ^ Z;
                h = (h * 397) ^ SizeX;
                h = (h * 397) ^ SizeZ;
                return h;
            }
        }

        public override string ToString() => $"({X},{Z}) {SizeX}x{SizeZ}";
    }

    /// <summary>
    /// Cálculos de huella (footprint) de un objeto en la cuadrícula.
    /// </summary>
    public static class Footprint
    {
        /// <summary>Normaliza un número de cuartos de vuelta a 0..3.</summary>
        public static int NormalizeQuarterTurns(int quarterTurns)
        {
            int q = quarterTurns % 4;
            return q < 0 ? q + 4 : q;
        }

        /// <summary>Tamaño de la huella tras girar el objeto en cuartos de vuelta (90° intercambia X y Z).</summary>
        public static void Rotate(int sizeX, int sizeZ, int quarterTurns, out int rotatedX, out int rotatedZ)
        {
            bool swap = NormalizeQuarterTurns(quarterTurns) % 2 == 1;
            rotatedX = swap ? sizeZ : sizeX;
            rotatedZ = swap ? sizeX : sizeZ;
        }

        /// <summary>
        /// Primera celda de una huella de <paramref name="size"/> celdas centrada lo más cerca posible
        /// del punto <paramref name="pointInCells"/> (coordenada local medida en celdas).
        /// Tamaños impares: el punto queda dentro de la celda central.
        /// Tamaños pares: la huella se centra en la línea de cuadrícula más cercana.
        /// </summary>
        public static int OriginFromPoint(double pointInCells, int size)
        {
            return (int)Math.Floor(pointInCells - size * 0.5 + 0.5);
        }

        /// <summary>Área de celdas para un punto local (en celdas) y una huella ya girada.</summary>
        public static CellArea AreaFromPoint(double pointXInCells, double pointZInCells, int rotatedX, int rotatedZ)
        {
            return new CellArea(
                OriginFromPoint(pointXInCells, rotatedX),
                OriginFromPoint(pointZInCells, rotatedZ),
                rotatedX,
                rotatedZ);
        }
    }

    /// <summary>Estado de una celda.</summary>
    public enum CellState
    {
        Free = 0,
        Occupied = 1,
        Blocked = 2,
        Outside = 3
    }

    /// <summary>
    /// Ocupación de una cuadrícula de ancho x fondo celdas. Cada celda está libre, bloqueada
    /// (no se puede construir nunca) u ocupada por un objeto con un identificador.
    /// </summary>
    public sealed class GridOccupancy
    {
        private const int FreeId = 0;
        private const int BlockedId = -1;

        private readonly int[] _cells;
        private readonly Dictionary<int, CellArea> _areas = new Dictionary<int, CellArea>();
        private int _nextId = 1;

        public int Width { get; }
        public int Depth { get; }
        public int OccupiedCount { get; private set; }

        public GridOccupancy(int width, int depth)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));
            Width = width;
            Depth = depth;
            _cells = new int[width * depth];
        }

        public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth;

        public bool IsInside(CellArea area)
        {
            return area.SizeX > 0 && area.SizeZ > 0
                && area.X >= 0 && area.Z >= 0
                && area.MaxXExclusive <= Width && area.MaxZExclusive <= Depth;
        }

        public CellState GetState(int x, int z)
        {
            if (!InBounds(x, z)) return CellState.Outside;
            int id = _cells[Index(x, z)];
            if (id == FreeId) return CellState.Free;
            if (id == BlockedId) return CellState.Blocked;
            return CellState.Occupied;
        }

        /// <summary>Identificador del objeto que ocupa la celda, o 0 si no hay ninguno.</summary>
        public int GetOccupant(int x, int z)
        {
            if (!InBounds(x, z)) return FreeId;
            int id = _cells[Index(x, z)];
            return id > 0 ? id : FreeId;
        }

        /// <summary>True si toda el área está dentro de la cuadrícula y libre.</summary>
        public bool IsAreaFree(CellArea area)
        {
            if (!IsInside(area)) return false;
            for (int z = area.Z; z < area.MaxZExclusive; z++)
            {
                for (int x = area.X; x < area.MaxXExclusive; x++)
                {
                    if (_cells[Index(x, z)] != FreeId) return false;
                }
            }
            return true;
        }

        /// <summary>Marca una celda como no construible (o la vuelve a liberar).</summary>
        public void SetBlocked(int x, int z, bool blocked)
        {
            if (!InBounds(x, z)) return;
            int i = Index(x, z);
            if (blocked)
            {
                if (_cells[i] == FreeId) _cells[i] = BlockedId;
            }
            else if (_cells[i] == BlockedId)
            {
                _cells[i] = FreeId;
            }
        }

        /// <summary>Ocupa el área si está libre. Devuelve el identificador asignado, o 0 si no se pudo.</summary>
        public int Occupy(CellArea area)
        {
            if (!IsAreaFree(area)) return FreeId;
            int id = _nextId++;
            for (int z = area.Z; z < area.MaxZExclusive; z++)
            {
                for (int x = area.X; x < area.MaxXExclusive; x++)
                {
                    _cells[Index(x, z)] = id;
                }
            }
            _areas[id] = area;
            OccupiedCount += area.SizeX * area.SizeZ;
            return id;
        }

        /// <summary>Libera las celdas de un objeto. Devuelve false si el identificador no existe.</summary>
        public bool Release(int id)
        {
            if (!_areas.TryGetValue(id, out CellArea area)) return false;
            for (int z = area.Z; z < area.MaxZExclusive; z++)
            {
                for (int x = area.X; x < area.MaxXExclusive; x++)
                {
                    int i = Index(x, z);
                    if (_cells[i] == id) _cells[i] = FreeId;
                }
            }
            _areas.Remove(id);
            OccupiedCount -= area.SizeX * area.SizeZ;
            return true;
        }

        public bool TryGetArea(int id, out CellArea area) => _areas.TryGetValue(id, out area);

        private int Index(int x, int z) => z * Width + x;
    }
}
