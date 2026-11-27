using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Construccion
{
    [Serializable] public class PlacedBuildableEvent : UnityEvent<PlacedBuildable> { }
    [Serializable] public class BuildableDefinitionEvent : UnityEvent<BuildableDefinition> { }
    [Serializable] public class PlacementModeEvent : UnityEvent<PlacementMode> { }

    /// <summary>
    /// Controlador principal del sistema de construcción. Ponlo en el jugador (o en cualquier objeto),
    /// asígnale la cámara y un catálogo de <see cref="BuildableDefinition"/>. Se encarga de:
    /// entrar y salir del modo construcción, mover la vista previa, comprobar si se puede construir
    /// (verde/rojo), colocar, girar, cambiar entre cuadrícula y libre, y demoler.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecánicas/Construcción/Sistema de construcción")]
    public class BuildSystem : MonoBehaviour
    {
        public enum AimMode
        {
            /// <summary>Apunta con el centro de la pantalla (primera persona).</summary>
            ScreenCenter = 0,
            /// <summary>Apunta con el ratón (vista desde arriba o cursor libre).</summary>
            MousePosition = 1
        }

        /// <summary>El último BuildSystem activado. Lo usan la leyenda y el visual de la cuadrícula.</summary>
        public static BuildSystem Current { get; private set; }

        [Header("Catálogo")]
        [Tooltip("Objetos que se pueden construir. Las teclas 1-9 eligen por orden.")]
        [SerializeField] private List<BuildableDefinition> catalog = new List<BuildableDefinition>();
        [SerializeField] private int startIndex = 0;

        [Header("Apuntado")]
        [Tooltip("Cámara desde la que se apunta. Si está vacía se usa Camera.main.")]
        [SerializeField] private Camera aimCamera;
        [SerializeField] private AimMode aimMode = AimMode.ScreenCenter;
        [Tooltip("Distancia máxima a la que se puede construir o demoler, en metros.")]
        [Min(0.5f)] [SerializeField] private float maxDistance = 12f;
        [Tooltip("Capas que cuentan como suelo en modo libre. Si incluyen los objetos construidos, se puede apilar encima.")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Tooltip("Capas que impiden construir si se superponen con el objeto.")]
        [SerializeField] private LayerMask obstacleMask = ~0;
        [Tooltip("Margen que se resta a la caja del objeto para que dos objetos que solo se tocan no cuenten como superpuestos.")]
        [Range(0f, 0.25f)] [SerializeField] private float overlapTolerance = 0.03f;
        [Tooltip("En modo libre, redondea la posición a pasos de este tamaño (0 = sin redondeo).")]
        [Min(0f)] [SerializeField] private float freeSnapStep = 0f;

        [Header("Comportamiento")]
        [SerializeField] private PlacementMode startMode = PlacementMode.Grid;
        [Tooltip("Seguir construyendo después de colocar un objeto (si no, se sale del modo construcción).")]
        [SerializeField] private bool keepBuildingAfterPlace = true;
        [Tooltip("Solo se puede construir dentro de zonas BuildZone de tipo Permitida.")]
        [SerializeField] private bool requireAllowedZone = false;
        [Tooltip("Permite el modo demoler.")]
        [SerializeField] private bool allowDemolish = true;
        [Tooltip("Padre de los objetos construidos (opcional, para ordenar la jerarquía).")]
        [SerializeField] private Transform placedParent;
        [Tooltip("Opcional: conecta aquí tu sistema de dinero o recursos.")]
        [SerializeField] private BuildCostProvider costProvider;

        [Header("Vista previa (verde y rojo)")]
        [SerializeField] private Color validColor = new Color(0.2f, 1f, 0.35f, 0.45f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.45f);
        [Tooltip("Material opcional para la vista previa válida. Si está vacío se crea con el color de arriba.")]
        [SerializeField] private Material validMaterial;
        [Tooltip("Material opcional para la vista previa no válida y para resaltar al demoler.")]
        [SerializeField] private Material invalidMaterial;
        [Tooltip("Muestra una marca en el suelo bajo la vista previa en modo libre.")]
        [SerializeField] private bool showFreeFootprint = true;
        [SerializeField] private Color footprintValidColor = new Color(0.2f, 1f, 0.35f, 0.3f);
        [SerializeField] private Color footprintInvalidColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        [Tooltip("Capa de la vista previa (2 = Ignore Raycast).")]
        [SerializeField] private int previewLayer = 2;

        [Header("Controles")]
        [Tooltip("Desactívalo si quieres manejar el sistema solo por código (Begin, TryPlace, Rotate...).")]
        [SerializeField] private bool readInput = true;
        [SerializeField] private BuildInputBindings input = new BuildInputBindings();

        [Header("Eventos")]
        public UnityEvent onBuildStarted = new UnityEvent();
        public UnityEvent onBuildEnded = new UnityEvent();
        public PlacedBuildableEvent onPlaced = new PlacedBuildableEvent();
        public PlacedBuildableEvent onDemolished = new PlacedBuildableEvent();
        public BuildableDefinitionEvent onSelectionChanged = new BuildableDefinitionEvent();
        public PlacementModeEvent onModeChanged = new PlacementModeEvent();
        [Tooltip("Se lanza al intentar colocar donde no se puede.")]
        public UnityEvent onPlaceFailed = new UnityEvent();

        // Eventos de C# (equivalentes a los de arriba) para conectar desde código.
        public event Action<BuildState> StateChanged;
        public event Action<BuildableDefinition> SelectionChanged;
        public event Action<PlacementMode> ModeChanged;
        public event Action<PlacedBuildable> Placed;
        public event Action<PlacedBuildable> Demolished;

        private readonly Collider[] _overlapBuffer = new Collider[64];
        private readonly List<BuildRule> _rules = new List<BuildRule>();
        private readonly Dictionary<Renderer, Material[]> _demolishOriginals = new Dictionary<Renderer, Material[]>();

        private BuildPreview _preview;
        private Material _validRuntime;
        private Material _invalidRuntime;
        private int _selectedIndex = -1;
        private int _quarterTurns;
        private float _freeYaw;
        private BuildGrid _highlightedGrid;
        private PlacedBuildable _demolishTarget;

        // Última evaluación
        private bool _hasCandidate;
        private PlacementCandidate _candidate;

        public BuildState State { get; private set; } = BuildState.Inactive;
        public bool IsActive => State != BuildState.Inactive;
        public PlacementMode Mode { get; private set; }
        public BuildableDefinition Selected => _selectedIndex >= 0 && _selectedIndex < catalog.Count ? catalog[_selectedIndex] : null;
        public int SelectedIndex => _selectedIndex;
        public IReadOnlyList<BuildableDefinition> Catalog => catalog;
        public PlacementResult LastResult { get; private set; } = PlacementResult.NoSurface;
        public bool CanPlaceNow => LastResult == PlacementResult.Valid;
        /// <summary>Mensaje de la regla personalizada que ha fallado (si <see cref="LastResult"/> es CustomRule).</summary>
        public string LastRuleMessage { get; private set; }
        public PlacedBuildable DemolishTarget => _demolishTarget;
        public BuildInputBindings Input => input;
        public BuildCostProvider CostProvider { get => costProvider; set => costProvider = value; }
        public bool LegendVisible { get; set; } = true;
        public Color ValidColor => validColor;
        public Color InvalidColor => invalidColor;
        public float RotationDegrees => Mode == PlacementMode.Grid ? Footprint.NormalizeQuarterTurns(_quarterTurns) * 90f : Mathf.Repeat(_freeYaw, 360f);

        // ---------------- Ciclo de vida ----------------

        private void Awake()
        {
            Mode = startMode;
            if (catalog.Count > 0) _selectedIndex = Mathf.Clamp(startIndex, 0, catalog.Count - 1);
            GetComponentsInChildren(true, _rules);
        }

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (IsActive) End();
            if (Current == this) Current = null;
        }

        private void OnDestroy()
        {
            DestroyPreview();
            if (_validRuntime != null) Destroy(_validRuntime);
            if (_invalidRuntime != null) Destroy(_invalidRuntime);
        }

        private void Update()
        {
            if (readInput) HandleInput();

            switch (State)
            {
                case BuildState.Placing:
                    UpdatePlacing();
                    break;
                case BuildState.Demolishing:
                    UpdateDemolishing();
                    break;
            }
        }

        // ---------------- API pública ----------------

        /// <summary>Entra en el modo construcción con el objeto seleccionado.</summary>
        public void Begin()
        {
            if (State == BuildState.Placing) return;
            if (_selectedIndex < 0 && catalog.Count > 0) _selectedIndex = 0;
            SetState(BuildState.Placing);
            EnsureModeAllowed();
            onBuildStarted.Invoke();
        }

        /// <summary>Entra en el modo construcción con un objeto concreto (se añade al catálogo si no está).</summary>
        public void Begin(BuildableDefinition definition)
        {
            if (definition != null)
            {
                int i = catalog.IndexOf(definition);
                if (i < 0)
                {
                    catalog.Add(definition);
                    i = catalog.Count - 1;
                }
                Select(i);
            }
            Begin();
        }

        /// <summary>Sale del modo construcción.</summary>
        public void End()
        {
            if (State == BuildState.Inactive) return;
            ClearDemolishTarget();
            ClearGridHighlight();
            if (_preview != null) _preview.Visible = false;
            _hasCandidate = false;
            SetState(BuildState.Inactive);
            onBuildEnded.Invoke();
        }

        public void Toggle()
        {
            if (IsActive) End();
            else Begin();
        }

        /// <summary>Elige el objeto número <paramref name="index"/> del catálogo.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= catalog.Count || index == _selectedIndex) return;
            _selectedIndex = index;
            DestroyPreview();
            EnsureModeAllowed();
            SelectionChanged?.Invoke(Selected);
            onSelectionChanged.Invoke(Selected);
        }

        public void SelectNext(int direction = 1)
        {
            if (catalog.Count == 0) return;
            int start = _selectedIndex < 0 ? 0 : _selectedIndex;
            int i = ((start + direction) % catalog.Count + catalog.Count) % catalog.Count;
            Select(i);
        }

        /// <summary>Sustituye el catálogo (por ejemplo, al desbloquear objetos).</summary>
        public void SetCatalog(IEnumerable<BuildableDefinition> definitions, int selectIndex = 0)
        {
            catalog = new List<BuildableDefinition>(definitions);
            _selectedIndex = -1;
            DestroyPreview();
            if (catalog.Count > 0) Select(Mathf.Clamp(selectIndex, 0, catalog.Count - 1));
        }

        /// <summary>Cambia a cuadrícula o libre. Devuelve false si el objeto actual no admite ese modo.</summary>
        public bool SetMode(PlacementMode mode)
        {
            if (Mode == mode) return true;
            if (Selected != null && !Selected.Allows(mode)) return false;
            Mode = mode;
            ClearGridHighlight();
            ModeChanged?.Invoke(Mode);
            onModeChanged.Invoke(Mode);
            return true;
        }

        public bool ToggleMode()
        {
            return SetMode(Mode == PlacementMode.Grid ? PlacementMode.Free : PlacementMode.Grid);
        }

        /// <summary>Gira la vista previa: en cuadrícula 90° por paso, en libre lo que diga el objeto.</summary>
        public void Rotate(int direction)
        {
            if (direction == 0) return;
            if (Mode == PlacementMode.Grid)
            {
                _quarterTurns = Footprint.NormalizeQuarterTurns(_quarterTurns + (direction > 0 ? 1 : -1));
            }
            else
            {
                float step = Selected != null ? Selected.freeRotationStep : 15f;
                _freeYaw = Mathf.Repeat(_freeYaw + step * Mathf.Sign(direction), 360f);
            }
        }

        /// <summary>Activa o desactiva el modo demoler.</summary>
        public void ToggleDemolish()
        {
            if (!allowDemolish) return;
            if (State == BuildState.Demolishing)
            {
                ClearDemolishTarget();
                SetState(BuildState.Placing);
            }
            else
            {
                if (State == BuildState.Inactive) onBuildStarted.Invoke();
                ClearGridHighlight();
                if (_preview != null) _preview.Visible = false;
                SetState(BuildState.Demolishing);
            }
        }

        /// <summary>Intenta colocar el objeto donde está la vista previa. Devuelve el objeto creado o null.</summary>
        public PlacedBuildable TryPlace()
        {
            if (State != BuildState.Placing) return null;
            if (!_hasCandidate || LastResult != PlacementResult.Valid)
            {
                onPlaceFailed.Invoke();
                return null;
            }
            return PlaceAt(_candidate);
        }

        /// <summary>
        /// Coloca un objeto sin pasar por la vista previa (para cargar partidas o generar niveles).
        /// En modo cuadrícula registra las celdas; no comprueba reglas ni cobra.
        /// </summary>
        public PlacedBuildable PlaceDirect(BuildableDefinition definition, BuildGrid grid, CellArea area, int quarterTurns)
        {
            if (definition == null || definition.prefab == null || grid == null) return null;
            Quaternion rot = grid.transform.rotation * Quaternion.Euler(0f, Footprint.NormalizeQuarterTurns(quarterTurns) * 90f, 0f);
            Vector3 pos = grid.AreaCenterWorld(area) + rot * definition.positionOffset;
            var candidate = new PlacementCandidate
            {
                Definition = definition,
                Mode = PlacementMode.Grid,
                Position = pos,
                Rotation = rot,
                Grid = grid,
                Area = area
            };
            return Spawn(candidate, false);
        }

        /// <summary>Quita un objeto construido, devuelve parte del coste y libera sus celdas.</summary>
        public bool Demolish(PlacedBuildable target)
        {
            if (target == null || !target.CanBeDemolished) return false;
            if (target == _demolishTarget) ClearDemolishTarget();

            BuildableDefinition def = target.Definition;
            if (costProvider != null && def != null && def.cost > 0)
            {
                int refund = Mathf.RoundToInt(def.cost * def.refundOnDemolish);
                if (refund > 0) costProvider.Refund(def, refund);
            }

            if (target.Grid != null) target.Grid.Unregister(target);
            Demolished?.Invoke(target);
            onDemolished.Invoke(target);
            Destroy(target.gameObject);
            return true;
        }

        /// <summary>Nombre legible de la tecla de una acción (para la leyenda).</summary>
        public string GetKeyName(BuildAction action) => InputCompat.KeyDisplayName(input.Get(action));

        // ---------------- Entrada ----------------

        private void HandleInput()
        {
            if (InputCompat.GetKeyDown(input.toggleBuild))
            {
                Toggle();
                return;
            }
            if (!IsActive) return;

            if (InputCompat.GetKeyDown(input.toggleLegend)) LegendVisible = !LegendVisible;

            if (InputCompat.GetKeyDown(input.cancel) || InputCompat.GetKeyDown(input.cancelAlt))
            {
                if (State == BuildState.Demolishing && catalog.Count > 0) ToggleDemolish();
                else End();
                return;
            }

            if (allowDemolish && InputCompat.GetKeyDown(input.demolish))
            {
                ToggleDemolish();
                return;
            }

            if (State == BuildState.Demolishing)
            {
                if (InputCompat.GetKeyDown(input.place) && _demolishTarget != null) Demolish(_demolishTarget);
                return;
            }

            if (InputCompat.GetKeyDown(input.switchMode)) ToggleMode();
            if (InputCompat.GetKeyDown(input.next)) SelectNext(1);
            if (InputCompat.GetKeyDown(input.previous)) SelectNext(-1);
            if (input.numberKeysSelect)
            {
                for (int i = 0; i < 9 && i < catalog.Count; i++)
                {
                    if (InputCompat.GetKeyDown(KeyCode.Alpha1 + i)) Select(i);
                }
            }

            if (InputCompat.GetKeyDown(input.rotateLeft)) Rotate(-1);
            if (InputCompat.GetKeyDown(input.rotateRight)) Rotate(1);
            if (input.rotateWithScroll)
            {
                float scroll = InputCompat.ScrollDelta();
                if (scroll != 0f) Rotate(scroll > 0f ? 1 : -1);
            }

            if (InputCompat.GetKeyDown(input.place))
            {
                // La comprobación usa la posición de este mismo fotograma.
                UpdatePlacing();
                TryPlace();
            }
        }

        // ---------------- Colocación ----------------

        private Ray GetAimRay(out bool ok)
        {
            Camera cam = aimCamera != null ? aimCamera : Camera.main;
            ok = cam != null;
            if (!ok) return default;
            return aimMode == AimMode.ScreenCenter
                ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : cam.ScreenPointToRay(InputCompat.MousePosition());
        }

        private void UpdatePlacing()
        {
            BuildableDefinition def = Selected;
            _hasCandidate = false;
            LastRuleMessage = null;

            if (def == null || def.prefab == null)
            {
                LastResult = PlacementResult.NoSurface;
                if (_preview != null) _preview.Visible = false;
                ClearGridHighlight();
                return;
            }

            Ray ray = GetAimRay(out bool rayOk);
            EnsurePreview(def);

            bool found = rayOk && (Mode == PlacementMode.Grid ? EvaluateGrid(def, ray) : EvaluateFree(def, ray));
            if (!found)
            {
                _preview.Visible = false;
                ClearGridHighlight();
                return;
            }

            _hasCandidate = true;
            bool valid = LastResult == PlacementResult.Valid;
            _preview.SetPose(_candidate.Position, _candidate.Rotation);
            _preview.SetValid(valid);
            _preview.Visible = true;
        }

        private bool EvaluateGrid(BuildableDefinition def, Ray ray)
        {
            BuildGrid best = null;
            Vector3 bestPoint = default;
            float bestDist = float.MaxValue;
            bool bestInside = false;
            IReadOnlyList<BuildGrid> grids = BuildGrid.All;
            for (int i = 0; i < grids.Count; i++)
            {
                if (!grids[i].Raycast(ray, maxDistance, out Vector3 p, out float d)) continue;
                bool inside = grids[i].ContainsPoint(p);
                // Gana la cuadrícula que tiene el punto encima; si ninguna, la más cercana.
                if ((inside && !bestInside) || (inside == bestInside && d < bestDist))
                {
                    best = grids[i];
                    bestPoint = p;
                    bestDist = d;
                    bestInside = inside;
                }
            }

            if (best == null || !bestInside)
            {
                LastResult = best == null ? PlacementResult.NoSurface : PlacementResult.OutsideGrid;
                ClearGridHighlight();
                return false;
            }

            Footprint.Rotate(def.gridSize.x, def.gridSize.y, _quarterTurns, out int sx, out int sz);
            CellArea area = best.AreaFromWorldPoint(bestPoint, new Vector2Int(sx, sz));
            Quaternion rot = best.transform.rotation * Quaternion.Euler(0f, Footprint.NormalizeQuarterTurns(_quarterTurns) * 90f, 0f);
            Vector3 pos = best.AreaCenterWorld(area) + rot * def.positionOffset;

            _candidate = new PlacementCandidate
            {
                Definition = def,
                Mode = PlacementMode.Grid,
                Position = pos,
                Rotation = rot,
                Grid = best,
                Area = area
            };

            PlacementResult result;
            if (!def.Allows(PlacementMode.Grid)) result = PlacementResult.ModeNotAllowed;
            else if (!best.IsAreaInside(area)) result = PlacementResult.OutsideGrid;
            else if (!best.IsAreaFree(area)) result = PlacementResult.CellsOccupied;
            else if (best.CheckPhysicsOverlap && Overlaps(pos, rot, null)) result = PlacementResult.Overlapping;
            else result = CommonChecks(def, pos);
            LastResult = result;

            // Visual de celdas en verde/rojo; si la cuadrícula no tiene visual, se usa la marca del suelo.
            if (_highlightedGrid != null && _highlightedGrid != best) ClearGridHighlight();
            BuildGridVisual visual = best.GetComponent<BuildGridVisual>();
            if (visual != null && visual.isActiveAndEnabled)
            {
                visual.SetHighlight(area, result == PlacementResult.Valid);
                _highlightedGrid = best;
                _preview.SetFootprintVisible(false);
            }
            else
            {
                _preview.SetFootprintVisible(true);
                _preview.SetFootprintSize(new Vector2(def.gridSize.x * best.CellSize, def.gridSize.y * best.CellSize), -def.positionOffset);
            }
            return true;
        }

        private bool EvaluateFree(BuildableDefinition def, Ray ray)
        {
            ClearGridHighlight();
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                LastResult = PlacementResult.NoSurface;
                return false;
            }

            Vector3 point = hit.point;
            if (freeSnapStep > 0f)
            {
                point.x = Mathf.Round(point.x / freeSnapStep) * freeSnapStep;
                point.z = Mathf.Round(point.z / freeSnapStep) * freeSnapStep;
            }

            Quaternion yaw = Quaternion.Euler(0f, _freeYaw, 0f);
            Quaternion rot = def.alignToSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) * yaw : yaw;
            Vector3 pos = point + rot * def.positionOffset;

            _candidate = new PlacementCandidate
            {
                Definition = def,
                Mode = PlacementMode.Free,
                Position = pos,
                Rotation = rot,
                Surface = hit.collider
            };

            PlacementResult result;
            if (!def.Allows(PlacementMode.Free)) result = PlacementResult.ModeNotAllowed;
            else if (Vector3.Angle(hit.normal, Vector3.up) > def.maxSlope) result = PlacementResult.TooSteep;
            else if (Overlaps(pos, rot, hit.collider)) result = PlacementResult.Overlapping;
            else result = CommonChecks(def, pos);
            LastResult = result;

            Bounds b = _preview.LocalBounds;
            _preview.SetFootprintVisible(showFreeFootprint);
            _preview.SetFootprintSize(new Vector2(b.size.x, b.size.z), new Vector3(b.center.x, 0f, b.center.z));
            return true;
        }

        private PlacementResult CommonChecks(BuildableDefinition def, Vector3 pos)
        {
            if (!BuildZone.IsPointAllowed(pos, requireAllowedZone)) return PlacementResult.OutsideBuildZone;
            for (int i = 0; i < _rules.Count; i++)
            {
                BuildRule rule = _rules[i];
                if (rule == null || !rule.isActiveAndEnabled) continue;
                if (!rule.IsAllowed(_candidate))
                {
                    LastRuleMessage = rule.FailMessage;
                    return PlacementResult.CustomRule;
                }
            }
            if (costProvider != null && def.cost > 0 && !costProvider.CanAfford(def, def.cost)) return PlacementResult.CannotAfford;
            return PlacementResult.Valid;
        }

        /// <summary>True si la caja del objeto en esa posición choca con algo (excepto el suelo apuntado).</summary>
        private bool Overlaps(Vector3 pos, Quaternion rot, Collider ignore)
        {
            Bounds b = _preview.LocalBounds;
            float t = overlapTolerance;
            Vector3 half = b.extents - new Vector3(t, t, t);
            half = Vector3.Max(half, new Vector3(0.005f, 0.005f, 0.005f));
            // Se sube un poco la caja para que el suelo sobre el que se apoya no cuente.
            Vector3 center = pos + rot * (b.center + Vector3.up * t);

            int count = Physics.OverlapBoxNonAlloc(center, half, _overlapBuffer, rot, obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = _overlapBuffer[i];
                if (c == null || c == ignore || c is TerrainCollider) continue;
                if (_preview.Root != null && c.transform.IsChildOf(_preview.Root.transform)) continue;
                return true;
            }
            return false;
        }

        private PlacedBuildable PlaceAt(PlacementCandidate candidate)
        {
            PlacedBuildable placed = Spawn(candidate, true);
            if (placed != null && !keepBuildingAfterPlace) End();
            return placed;
        }

        private PlacedBuildable Spawn(PlacementCandidate candidate, bool charge)
        {
            BuildableDefinition def = candidate.Definition;
            GameObject go = Instantiate(def.prefab, candidate.Position, candidate.Rotation, placedParent);
            go.name = def.prefab.name;
            PlacedBuildable placed = go.GetComponent<PlacedBuildable>();
            if (placed == null) placed = go.AddComponent<PlacedBuildable>();
            placed.Initialize(def, candidate.Mode);

            if (candidate.Mode == PlacementMode.Grid && candidate.Grid != null && !candidate.Grid.Register(placed, candidate.Area))
            {
                Debug.LogWarning($"[Construcción] Las celdas {candidate.Area} ya estaban ocupadas; se cancela la colocación de {def.name}.", this);
                Destroy(go);
                return null;
            }

            if (charge && costProvider != null && def.cost > 0) costProvider.Spend(def, def.cost);

            Placed?.Invoke(placed);
            onPlaced.Invoke(placed);
            return placed;
        }

        // ---------------- Demoler ----------------

        private void UpdateDemolishing()
        {
            Ray ray = GetAimRay(out bool ok);
            PlacedBuildable target = null;
            if (ok && Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                target = hit.collider.GetComponentInParent<PlacedBuildable>();
                if (target != null && !target.CanBeDemolished) target = null;
            }

            if (target == _demolishTarget) return;
            ClearDemolishTarget();
            _demolishTarget = target;
            if (target == null) return;

            Material red = GetInvalidMaterial();
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>())
            {
                Material[] original = r.sharedMaterials;
                _demolishOriginals[r] = original;
                var tinted = new Material[Mathf.Max(1, original.Length)];
                for (int i = 0; i < tinted.Length; i++) tinted[i] = red;
                r.sharedMaterials = tinted;
            }
        }

        private void ClearDemolishTarget()
        {
            foreach (KeyValuePair<Renderer, Material[]> kv in _demolishOriginals)
            {
                if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            }
            _demolishOriginals.Clear();
            _demolishTarget = null;
        }

        // ---------------- Utilidades ----------------

        private void SetState(BuildState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }

        private void EnsureModeAllowed()
        {
            BuildableDefinition def = Selected;
            if (def == null || def.Allows(Mode)) return;
            SetMode(Mode == PlacementMode.Grid ? PlacementMode.Free : PlacementMode.Grid);
        }

        private void EnsurePreview(BuildableDefinition def)
        {
            if (_preview != null && _preview.Definition == def) return;
            DestroyPreview();
            _preview = new BuildPreview(def, GetValidMaterial(), GetInvalidMaterial(), true,
                footprintValidColor, footprintInvalidColor, previewLayer);
        }

        private void DestroyPreview()
        {
            if (_preview == null) return;
            _preview.Destroy();
            _preview = null;
        }

        private void ClearGridHighlight()
        {
            if (_highlightedGrid == null) return;
            BuildGridVisual visual = _highlightedGrid.GetComponent<BuildGridVisual>();
            if (visual != null) visual.ClearHighlight();
            _highlightedGrid = null;
        }

        private Material GetValidMaterial()
        {
            if (validMaterial != null) return validMaterial;
            if (_validRuntime == null) _validRuntime = BuildVisualUtility.CreateTransparentMaterial(validColor, "Vista previa válida");
            return _validRuntime;
        }

        private Material GetInvalidMaterial()
        {
            if (invalidMaterial != null) return invalidMaterial;
            if (_invalidRuntime == null) _invalidRuntime = BuildVisualUtility.CreateTransparentMaterial(invalidColor, "Vista previa no válida");
            return _invalidRuntime;
        }

        private void OnValidate()
        {
            if (_validRuntime != null) BuildVisualUtility.SetColor(_validRuntime, validColor);
            if (_invalidRuntime != null) BuildVisualUtility.SetColor(_invalidRuntime, invalidColor);
        }
    }
}
