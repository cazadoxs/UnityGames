using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Dibuja las celdas de un <see cref="BuildGrid"/> en el juego: libres, ocupadas, bloqueadas y la
    /// huella del objeto que se está colocando en verde o rojo. Todos los colores se cambian en el inspector.
    /// </summary>
    [RequireComponent(typeof(BuildGrid))]
    [AddComponentMenu("Mecánicas/Construcción/Visual de la cuadrícula")]
    public class BuildGridVisual : MonoBehaviour
    {
        public enum Visibility
        {
            /// <summary>Solo mientras se construye en modo cuadrícula.</summary>
            WhileBuildingGrid = 0,
            /// <summary>Mientras el modo construcción está activo (cualquier modo).</summary>
            WhileBuilding = 1,
            Always = 2,
            Never = 3
        }

        [SerializeField] private Visibility visibility = Visibility.WhileBuildingGrid;

        [Header("Colores de las celdas")]
        [SerializeField] private Color freeColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color occupiedColor = new Color(0.3f, 0.3f, 0.3f, 0.25f);
        [SerializeField] private Color blockedColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
        [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.35f, 0.55f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.55f);

        [Header("Forma")]
        [Tooltip("Hueco entre celdas, en proporción del tamaño de la celda (0 = sin separación).")]
        [Range(0f, 0.4f)] [SerializeField] private float cellGap = 0.06f;
        [Tooltip("Altura sobre la cuadrícula para que no parpadee con el suelo.")]
        [SerializeField] private float heightOffset = 0.015f;
        [Tooltip("Material opcional. Debe aceptar colores de vértice. Si está vacío se crea uno transparente.")]
        [SerializeField] private Material material;

        private BuildGrid _grid;
        private Mesh _mesh;
        private Color[] _colors;
        private GameObject _meshObject;
        private Material _ownedMaterial;
        private bool _hasHighlight;
        private CellArea _highlight;
        private bool _highlightValid;
        private bool _dirty = true;
        private bool _visible;

        public Visibility Mode { get => visibility; set => visibility = value; }

        private void Awake()
        {
            _grid = GetComponent<BuildGrid>();
        }

        private void OnEnable()
        {
            if (_grid == null) _grid = GetComponent<BuildGrid>();
            _grid.OccupancyChanged += OnOccupancyChanged;
            _dirty = true;
        }

        private void OnDisable()
        {
            if (_grid != null) _grid.OccupancyChanged -= OnOccupancyChanged;
            if (_meshObject != null) _meshObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_ownedMaterial != null) Destroy(_ownedMaterial);
            if (_meshObject != null) Destroy(_meshObject);
        }

        private void OnOccupancyChanged(BuildGrid grid) => _dirty = true;

        /// <summary>Marca la huella del objeto que se coloca. La llama el BuildSystem.</summary>
        public void SetHighlight(CellArea area, bool valid)
        {
            if (_hasHighlight && _highlight.Equals(area) && _highlightValid == valid) return;
            _hasHighlight = true;
            _highlight = area;
            _highlightValid = valid;
            _dirty = true;
        }

        public void ClearHighlight()
        {
            if (!_hasHighlight) return;
            _hasHighlight = false;
            _dirty = true;
        }

        private void LateUpdate()
        {
            BuildSystem system = BuildSystem.Current;
            bool building = system != null && system.IsActive;
            bool show;
            switch (visibility)
            {
                case Visibility.Always: show = true; break;
                case Visibility.Never: show = false; break;
                case Visibility.WhileBuilding: show = building; break;
                default: show = building && system.State == BuildState.Placing && system.Mode == PlacementMode.Grid; break;
            }

            if (!show)
            {
                if (_meshObject != null && _meshObject.activeSelf) _meshObject.SetActive(false);
                _visible = false;
                return;
            }

            EnsureMesh();
            if (!_meshObject.activeSelf) _meshObject.SetActive(true);
            if (!_visible) _dirty = true;
            _visible = true;
            if (_dirty) RefreshColors();
        }

        private void EnsureMesh()
        {
            if (_mesh != null && _meshObject != null) return;

            int w = _grid.Width;
            int d = _grid.Depth;
            float s = _grid.CellSize;
            float gap = s * cellGap * 0.5f;

            var vertices = new Vector3[w * d * 4];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[w * d * 6];
            _colors = new Color[vertices.Length];

            for (int z = 0; z < d; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    int cell = z * w + x;
                    int v = cell * 4;
                    float x0 = x * s + gap, x1 = (x + 1) * s - gap;
                    float z0 = z * s + gap, z1 = (z + 1) * s - gap;
                    vertices[v + 0] = new Vector3(x0, heightOffset, z0);
                    vertices[v + 1] = new Vector3(x0, heightOffset, z1);
                    vertices[v + 2] = new Vector3(x1, heightOffset, z1);
                    vertices[v + 3] = new Vector3(x1, heightOffset, z0);
                    uvs[v + 0] = new Vector2(0, 0);
                    uvs[v + 1] = new Vector2(0, 1);
                    uvs[v + 2] = new Vector2(1, 1);
                    uvs[v + 3] = new Vector2(1, 0);
                    int t = cell * 6;
                    triangles[t + 0] = v;
                    triangles[t + 1] = v + 1;
                    triangles[t + 2] = v + 2;
                    triangles[t + 3] = v;
                    triangles[t + 4] = v + 2;
                    triangles[t + 5] = v + 3;
                }
            }

            _mesh = new Mesh { name = "Celdas de construcción" };
            if (vertices.Length > 65000) _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;
            _mesh.colors = _colors;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _meshObject = new GameObject("Celdas (visual)");
            _meshObject.transform.SetParent(transform, false);
            _meshObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            MeshRenderer mr = _meshObject.AddComponent<MeshRenderer>();
            if (material == null)
            {
                _ownedMaterial = BuildVisualUtility.CreateTransparentMaterial(Color.white, "Celdas de construcción");
                mr.sharedMaterial = _ownedMaterial;
            }
            else
            {
                mr.sharedMaterial = material;
            }
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _dirty = true;
        }

        private void RefreshColors()
        {
            _dirty = false;
            int w = _grid.Width;
            int d = _grid.Depth;
            for (int z = 0; z < d; z++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c;
                    if (_hasHighlight && _highlight.Contains(x, z))
                    {
                        c = _highlightValid ? validColor : invalidColor;
                    }
                    else
                    {
                        switch (_grid.GetCellState(x, z))
                        {
                            case CellState.Occupied: c = occupiedColor; break;
                            case CellState.Blocked: c = blockedColor; break;
                            default: c = freeColor; break;
                        }
                    }
                    int v = (z * w + x) * 4;
                    _colors[v] = c;
                    _colors[v + 1] = c;
                    _colors[v + 2] = c;
                    _colors[v + 3] = c;
                }
            }
            _mesh.colors = _colors;
        }
    }
}
