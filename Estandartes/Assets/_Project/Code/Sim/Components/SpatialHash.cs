// PENDIENTE DE COMPILAR EN UNITY. Hash espacial como singleton (Vol. I §9.6; referencia: Core/Spatial/SpatialGrid.cs).
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Estandartes.Sim
{
    public struct SpatialEntry
    {
        public Entity Entity;
        public float3 Position;
        public byte Team;
    }

    public struct SpatialHash : IComponentData
    {
        public NativeParallelMultiHashMap<int, SpatialEntry> Map;
        public float CellSize;

        public static int Key(float3 p, float cellSize)
        {
            int2 c = (int2)math.floor(p.xz / cellSize);
            return (c.x & 0xFFFF) | (c.y << 16);
        }

        public static int Key(int2 cell) => (cell.x & 0xFFFF) | (cell.y << 16);
    }
}
