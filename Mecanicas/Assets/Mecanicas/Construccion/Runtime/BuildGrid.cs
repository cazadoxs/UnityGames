using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Cuadrícula de construcción en 3D. Es un rectángulo plano de <see cref="width"/> x <see cref="depth"/>
    /// celdas que empieza en la posición de este objeto y se extiende hacia +X y +Z locales.
    /// Puedes girarlo y moverlo; puedes tener varias en la misma escena.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecánicas/Construcción/Cuadrícula de construcción")]
    public class BuildGrid : MonoBehaviour
    {
        private static readonly List<BuildGrid> ActiveGrids = new List<BuildGrid>();

        /// <summary>Todas las cuadrículas activas de la escena.</summary>
        public static IReadOnlyList<BuildGrid> All => ActiveGrids;

        [Tooltip("Número de celdas a lo ancho (eje X local).")]
        [Min(1)] [SerializeField] private int width = 9;

        [Tooltip("Número de celdas de fondo (eje Z local).")]
        [Min(1)] [SerializeField] private int depth = 5;

        [Tooltip("Tamaño de cada celda en metros.")]
        [Min(0.05f)] [SerializeField] private float cellSize = 1f;

        [Tooltip("Celdas en las que nunca se puede construir (x, z), contando desde 0.")]
        [SerializeField] private List<Vector2Int> blockedCells = new List<Vector2Int>();

        [Tooltip("Comprobar también colisiones físicas en esta cuadrícula (rocas, árboles u objetos que no son de la cuadrícula).")]
        [SerializeField] private bool checkPhysicsOverlap = true;

        [Tooltip("Dibuja la cuadrícula en la vista de escena.")]
        [SerializeField] private bool drawGizmos = true;

        private GridOccupancy _occupancy;
        private readonly Dictionary<int, PlacedBuildable> _placed = new Dictionary<int, PlacedBuildable>();

        /// <summary>Se lanza cada vez que cambia la ocupación (algo se coloca o se quita).</summary>
        public event Action<BuildGrid> OccupancyChanged;

        public int Width => width;
        public int Depth => depth;
        public float CellSize => cellSize;
        public bool CheckPhysicsOverlap => checkPhysicsOverlap;
        public Vector3 Up => transform.up;

        public GridOccupancy Occupancy
        {
            get
            {
                EnsureOccupancy();
                return _occupancy;
            }
        }

        public IEnumerable<PlacedBuildable> PlacedObjects => _placed.Values;

        private void OnEnable()
        {
            if (!ActiveGrids.Contains(this)) ActiveGrids.Add(this);
            EnsureOccupancy();
        }

        private void OnDisable()
        {
            ActiveGrids.Remove(this);
        }

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            depth = Mathf.Max(1, depth);
            cellSize = Mathf.Max(0.05f, cellSize);
            if (Application.isPlaying && _occupancy != null && (_occupancy.Width != width || _occupancy.Depth != depth))
            {
                Debug.LogWarning("[Construcción] Cambiar el tamaño de la cuadrícula durante el juego no tiene efecto hasta reiniciar.", this);
            }
        }

        private void EnsureOccupancy()
        {
            if (_occupancy != null) return;
            _occupancy = new GridOccupancy(width, depth);
            foreach (Vector2Int c in blockedCells) _occupancy.SetBlocked(c.x, c.y, true);
        }

        // ---------- Conversión de coordenadas ----------

        /// <summary>Punto local de la cuadrícula medido en celdas (x, z).</summary>
        public Vector2 WorldToCellSpace(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return new Vector2(local.x / cellSize, local.z / cellSize);
        }

        /// <summary>Celda que contiene el punto (puede estar fuera de la cuadrícula).</summary>
        public Vector2Int WorldToCell(Vector3 world)
        {
            Vector2 p = WorldToCellSpace(world);
            return new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        }

        public Vector3 CellCenterWorld(int x, int z)
        {
            return transform.TransformPoint(new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize));
        }

        /// <summary>Centro en el mundo de un área de celdas, a la altura de la cuadrícula.</summary>
        public Vector3 AreaCenterWorld(CellArea area)
        {
            return transform.TransformPoint(new Vector3(
                (area.X + area.SizeX * 0.5f) * cellSize,
                0f,
                (area.Z + area.SizeZ * 0.5f) * cellSize));
        }

        /// <summary>Área de celdas que ocuparía un objeto de <paramref name="rotatedSize"/> celdas apuntando a <paramref name="world"/>.</summary>
        public CellArea AreaFromWorldPoint(Vector3 world, Vector2Int rotatedSize)
        {
            Vector2 p = WorldToCellSpace(world);
            return Footprint.AreaFromPoint(p.x, p.y, rotatedSize.x, rotatedSize.y);
        }

        /// <summary>Corta un rayo con el plano de la cuadrícula. Devuelve false si no lo toca hacia delante.</summary>
        public bool Raycast(Ray ray, float maxDistance, out Vector3 point, out float distance)
        {
            Plane plane = new Plane(transform.up, transform.position);
            if (plane.Raycast(ray, out distance) && distance <= maxDistance)
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = default;
            distance = 0f;
            return false;
        }

        /// <summary>True si el punto cae encima de alguna celda de esta cuadrícula.</summary>
        public bool ContainsPoint(Vector3 world)
        {
            Vector2 p = WorldToCellSpace(world);
            return p.x >= 0f && p.y >= 0f && p.x < width && p.y < depth;
        }

        // ---------- Ocupación ----------

        public CellState GetCellState(int x, int z) => Occupancy.GetState(x, z);

        public bool IsAreaInside(CellArea area) => Occupancy.IsInside(area);

        public bool IsAreaFree(CellArea area) => Occupancy.IsAreaFree(area);

        /// <summary>Bloquea o desbloquea una celda durante el juego.</summary>
        public void SetCellBlocked(int x, int z, bool blocked)
        {
            Occupancy.SetBlocked(x, z, blocked);
            OccupancyChanged?.Invoke(this);
        }

        /// <summary>El objeto que ocupa una celda, o null.</summary>
        public PlacedBuildable GetOccupant(int x, int z)
        {
            int id = Occupancy.GetOccupant(x, z);
            return id != 0 && _placed.TryGetValue(id, out PlacedBuildable p) ? p : null;
        }

        /// <summary>Registra un objeto en un área. Lo llama el BuildSystem; úsalo si colocas objetos por código.</summary>
        public bool Register(PlacedBuildable placed, CellArea area)
        {
            if (placed == null) return false;
            int id = Occupancy.Occupy(area);
            if (id == 0) return false;
            _placed[id] = placed;
            placed.AttachToGrid(this, area, id);
            OccupancyChanged?.Invoke(this);
            return true;
        }

        /// <summary>Libera las celdas de un objeto.</summary>
        public void Unregister(PlacedBuildable placed)
        {
            if (placed == null || placed.Grid != this) return;
            int id = placed.GridId;
            if (Occupancy.Release(id)) _placed.Remove(id);
            placed.DetachFromGrid();
            OccupancyChanged?.Invoke(this);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
            float w = width * cellSize;
            float d = depth * cellSize;
            for (int x = 0; x <= width; x++) Gizmos.DrawLine(new Vector3(x * cellSize, 0f, 0f), new Vector3(x * cellSize, 0f, d));
            for (int z = 0; z <= depth; z++) Gizmos.DrawLine(new Vector3(0f, 0f, z * cellSize), new Vector3(w, 0f, z * cellSize));

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.45f);
            foreach (Vector2Int c in blockedCells)
            {
                if (c.x < 0 || c.y < 0 || c.x >= width || c.y >= depth) continue;
                Gizmos.DrawCube(new Vector3((c.x + 0.5f) * cellSize, 0.01f, (c.y + 0.5f) * cellSize), new Vector3(cellSize * 0.9f, 0.01f, cellSize * 0.9f));
            }

            Gizmos.matrix = old;
        }
    }
}
