using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Leyenda en pantalla mientras se construye. Lee el estado del <see cref="BuildSystem"/> y pinta las
    /// líneas de un <see cref="BuildLegendConfig"/>. Por defecto crea su propio Canvas; si quieres un
    /// diseño propio, asigna tu panel en "Panel propio" y tu prefab de línea en "Plantilla de línea".
    /// </summary>
    [AddComponentMenu("Mecánicas/Construcción/Leyenda de construcción")]
    public class BuildLegend : MonoBehaviour
    {
        [Tooltip("Sistema del que se lee el estado. Si está vacío, se usa el BuildSystem activo.")]
        [SerializeField] private BuildSystem buildSystem;
        [Tooltip("Textos, colores y líneas de la leyenda. Si está vacío, se usa la configuración por defecto.")]
        [SerializeField] private BuildLegendConfig config;

        [Header("Diseño propio (opcional)")]
        [Tooltip("Tu propio panel (con VerticalLayoutGroup). Si está vacío se crea uno automáticamente.")]
        [SerializeField] private RectTransform customPanel;
        [Tooltip("Tu propio prefab de línea con un BuildLegendEntryView. Si está vacío se crea una línea sencilla.")]
        [SerializeField] private BuildLegendEntryView entryTemplate;

        [Header("Leyenda oculta")]
        [Tooltip("Texto pequeño que queda cuando el jugador oculta la leyenda (vacío = nada).")]
        [SerializeField] private string hiddenHint = "[{tecla:leyenda}] Mostrar leyenda";

        /// <summary>
        /// Para añadir etiquetas propias: recibe el nombre (sin llaves) y devuelve el texto, o null si no
        /// es tuya. Ejemplo: legend.ResolveToken += t =&gt; t == "dinero" ? wallet.Money.ToString() : null;
        /// </summary>
        public event Func<string, string> ResolveToken;

        private readonly List<BuildLegendEntryView> _views = new List<BuildLegendEntryView>();
        private readonly StringBuilder _sb = new StringBuilder(256);
        private BuildLegendConfig _activeConfig;
        private RectTransform _panel;
        private Text _title;
        private BuildLegendEntryView _hintView;
        private GameObject _hintRoot;
        private Canvas _ownCanvas;
        private bool _built;

        public BuildLegendConfig Config => _activeConfig;

        /// <summary>Cambia la configuración en tiempo de ejecución y reconstruye la leyenda.</summary>
        public void SetConfig(BuildLegendConfig newConfig)
        {
            config = newConfig;
            Rebuild();
        }

        /// <summary>Vuelve a crear las líneas (llámalo si cambias la lista de líneas durante el juego).</summary>
        public void Rebuild()
        {
            Clear();
            Build();
        }

        private void Start()
        {
            Build();
        }

        private void OnDestroy()
        {
            Clear();
        }

        private BuildSystem System => buildSystem != null ? buildSystem : BuildSystem.Current;

        private void LateUpdate()
        {
            if (!_built) Build();
            BuildSystem sys = System;
            bool active = sys != null && sys.IsActive;
            bool show = active && sys.LegendVisible;

            SetActive(_panel.gameObject, show);
            if (_hintRoot != null)
            {
                bool hint = active && !sys.LegendVisible;
                SetActive(_hintRoot, hint);
                if (hint) _hintView.SetText(Format(hiddenHint, sys));
            }
            if (!show) return;

            BuildLegendConfig c = _activeConfig;
            for (int i = 0; i < _views.Count && i < c.entries.Count; i++)
            {
                BuildLegendConfig.LegendEntry entry = c.entries[i];
                BuildLegendEntryView view = _views[i];
                bool visible = IsVisible(entry.showWhen, sys);
                SetActive(view.gameObject, visible);
                if (!visible) continue;

                view.SetText(Format(entry.text, sys));
                view.SetIcon(entry.icon);
                switch (entry.swatch)
                {
                    case BuildLegendConfig.SwatchMode.Fixed: view.SetSwatch(true, entry.swatchColor); break;
                    case BuildLegendConfig.SwatchMode.ValidColor: view.SetSwatch(true, Opaque(sys.ValidColor)); break;
                    case BuildLegendConfig.SwatchMode.InvalidColor: view.SetSwatch(true, Opaque(sys.InvalidColor)); break;
                    case BuildLegendConfig.SwatchMode.PlacementState:
                        view.SetSwatch(true, Opaque(sys.CanPlaceNow ? sys.ValidColor : sys.InvalidColor));
                        break;
                    default: view.SetSwatch(false, Color.clear); break;
                }
            }
        }

        // ---------------- Lógica de visibilidad y etiquetas ----------------

        private static bool IsVisible(BuildLegendConfig.Condition condition, BuildSystem sys)
        {
            bool placing = sys.State == BuildState.Placing;
            switch (condition)
            {
                case BuildLegendConfig.Condition.Always: return true;
                case BuildLegendConfig.Condition.Placing: return placing;
                case BuildLegendConfig.Condition.GridMode: return placing && sys.Mode == PlacementMode.Grid;
                case BuildLegendConfig.Condition.FreeMode: return placing && sys.Mode == PlacementMode.Free;
                case BuildLegendConfig.Condition.Demolishing: return sys.State == BuildState.Demolishing;
                case BuildLegendConfig.Condition.WhenValid: return placing && sys.CanPlaceNow;
                case BuildLegendConfig.Condition.WhenInvalid: return placing && !sys.CanPlaceNow;
                default: return true;
            }
        }

        /// <summary>Sustituye las etiquetas {nombre} de un texto por sus valores actuales.</summary>
        public string Format(string template, BuildSystem sys)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;
            if (template.IndexOf('{') < 0) return template;

            _sb.Clear();
            int i = 0;
            while (i < template.Length)
            {
                char ch = template[i];
                if (ch == '{')
                {
                    int close = template.IndexOf('}', i + 1);
                    if (close > i)
                    {
                        string token = template.Substring(i + 1, close - i - 1);
                        string value = Resolve(token, sys);
                        if (value != null)
                        {
                            _sb.Append(value);
                            i = close + 1;
                            continue;
                        }
                    }
                }
                _sb.Append(ch);
                i++;
            }
            return _sb.ToString();
        }

        private string Resolve(string token, BuildSystem sys)
        {
            BuildLegendConfig c = _activeConfig;
            BuildableDefinition def = sys.Selected;

            if (token.StartsWith("tecla:", StringComparison.Ordinal))
            {
                return TryParseAction(token.Substring(6), out BuildAction action) ? sys.GetKeyName(action) : null;
            }

            switch (token)
            {
                case "objeto": return def != null ? def.DisplayNameOrAssetName : "-";
                case "descripcion": return def != null ? def.description ?? string.Empty : string.Empty;
                case "coste": return def == null || def.cost <= 0 ? c.freeCostText : string.Format(c.costFormat, def.cost);
                case "modo":
                    if (sys.State == BuildState.Demolishing) return c.demolishModeName;
                    return sys.Mode == PlacementMode.Grid ? c.gridModeName : c.freeModeName;
                case "estado": return sys.CanPlaceNow ? c.validText : c.invalidText;
                case "motivo": return c.GetReasonText(sys.LastResult, sys.LastRuleMessage);
                case "rotacion": return Mathf.RoundToInt(sys.RotationDegrees).ToString();
                case "catalogo": return BuildCatalog(sys, c);
                case "objetivo":
                    PlacedBuildable t = sys.DemolishTarget;
                    if (t == null) return c.demolishTargetNone;
                    return t.Definition != null ? t.Definition.DisplayNameOrAssetName : t.name;
            }

            if (ResolveToken != null)
            {
                foreach (Delegate d in ResolveToken.GetInvocationList())
                {
                    string v = ((Func<string, string>)d)(token);
                    if (v != null) return v;
                }
            }
            return null;
        }

        private static string BuildCatalog(BuildSystem sys, BuildLegendConfig c)
        {
            var sb = new StringBuilder();
            IReadOnlyList<BuildableDefinition> list = sys.Catalog;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                if (sb.Length > 0) sb.Append(c.catalogSeparator);
                string format = i == sys.SelectedIndex ? c.catalogSelectedFormat : c.catalogItemFormat;
                sb.AppendFormat(format, i + 1, list[i].DisplayNameOrAssetName);
            }
            return sb.ToString();
        }

        private static bool TryParseAction(string name, out BuildAction action)
        {
            switch (name.ToLowerInvariant())
            {
                case "construir": action = BuildAction.ToggleBuild; return true;
                case "colocar": action = BuildAction.Place; return true;
                case "cancelar": action = BuildAction.Cancel; return true;
                case "rotarizq": action = BuildAction.RotateLeft; return true;
                case "rotarder": action = BuildAction.RotateRight; return true;
                case "cambiarmodo": action = BuildAction.SwitchMode; return true;
                case "siguiente": action = BuildAction.Next; return true;
                case "anterior": action = BuildAction.Previous; return true;
                case "demoler": action = BuildAction.Demolish; return true;
                case "leyenda": action = BuildAction.ToggleLegend; return true;
            }
            return Enum.TryParse(name, true, out action);
        }

        private static Color Opaque(Color c) => new Color(c.r, c.g, c.b, 1f);

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        // ---------------- Construcción de la interfaz ----------------

        private void Build()
        {
            if (_built) return;
            _built = true;
            _activeConfig = config != null ? config : BuildLegendConfig.CreateDefault();
            BuildLegendConfig c = _activeConfig;

            if (customPanel != null)
            {
                _panel = customPanel;
            }
            else
            {
                _panel = CreateDefaultPanel(c);
            }

            if (c.showTitle && !string.IsNullOrEmpty(c.title) && customPanel == null)
            {
                _title = CreateText("Título", _panel, c, c.titleFontSize, c.titleColor);
                _title.text = c.title;
                _title.fontStyle = FontStyle.Bold;
            }

            foreach (BuildLegendConfig.LegendEntry entry in c.entries)
            {
                _views.Add(CreateEntry(_panel, c));
            }

            if (!string.IsNullOrEmpty(hiddenHint))
            {
                RectTransform hintPanel = customPanel == null ? CreateHintPanel(c) : null;
                if (hintPanel != null)
                {
                    _hintView = CreateEntry(hintPanel, c);
                    _hintView.SetSwatch(false, Color.clear);
                    _hintView.SetIcon(null);
                    _hintRoot = hintPanel.gameObject;
                    _hintRoot.SetActive(false);
                }
            }

            _panel.gameObject.SetActive(false);
        }

        private void Clear()
        {
            foreach (BuildLegendEntryView v in _views)
            {
                if (v != null) Destroy(v.gameObject);
            }
            _views.Clear();
            if (_title != null) Destroy(_title.gameObject);
            if (_hintRoot != null) Destroy(_hintRoot);
            if (customPanel == null && _panel != null) Destroy(_panel.gameObject);
            if (_ownCanvas != null) Destroy(_ownCanvas.gameObject);
            _ownCanvas = null;
            _panel = null;
            _title = null;
            _hintRoot = null;
            _hintView = null;
            _built = false;
        }

        private Transform GetCanvasRoot(BuildLegendConfig c)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null) return parentCanvas.transform;
            if (_ownCanvas != null) return _ownCanvas.transform;

            var go = new GameObject("Leyenda construcción (Canvas)", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _ownCanvas = go.AddComponent<Canvas>();
            _ownCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _ownCanvas.sortingOrder = c.sortingOrder;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        private RectTransform CreateDefaultPanel(BuildLegendConfig c)
        {
            Transform root = GetCanvasRoot(c);
            RectTransform panel = CreatePanelObject("Leyenda construcción", root, c);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)c.padding.x, (int)c.padding.y, (int)c.padding.z, (int)c.padding.w);
            layout.spacing = c.lineSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panel.sizeDelta = new Vector2(c.width, panel.sizeDelta.y);
            return panel;
        }

        private RectTransform CreateHintPanel(BuildLegendConfig c)
        {
            Transform root = GetCanvasRoot(c);
            RectTransform panel = CreatePanelObject("Leyenda oculta (aviso)", root, c);
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return panel;
        }

        private static RectTransform CreatePanelObject(string name, Transform parent, BuildLegendConfig c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Vector2 anchor;
            Vector2 sign;
            switch (c.corner)
            {
                case BuildLegendConfig.Corner.TopRight: anchor = new Vector2(1f, 1f); sign = new Vector2(-1f, -1f); break;
                case BuildLegendConfig.Corner.BottomLeft: anchor = new Vector2(0f, 0f); sign = new Vector2(1f, 1f); break;
                case BuildLegendConfig.Corner.BottomRight: anchor = new Vector2(1f, 0f); sign = new Vector2(-1f, 1f); break;
                default: anchor = new Vector2(0f, 1f); sign = new Vector2(1f, -1f); break;
            }
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = new Vector2(c.margin.x * sign.x, c.margin.y * sign.y);

            Image bg = go.AddComponent<Image>();
            bg.color = c.backgroundColor;
            bg.raycastTarget = false;
            if (c.backgroundSprite != null)
            {
                bg.sprite = c.backgroundSprite;
                bg.type = Image.Type.Sliced;
            }
            return rt;
        }

        private BuildLegendEntryView CreateEntry(RectTransform parent, BuildLegendConfig c)
        {
            if (entryTemplate != null)
            {
                BuildLegendEntryView v = Instantiate(entryTemplate, parent);
                v.gameObject.SetActive(true);
                return v;
            }

            var row = new GameObject("Línea", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            BuildLegendEntryView view = row.AddComponent<BuildLegendEntryView>();
            view.swatch = CreateImage("Color", row.transform, c.swatchSize);
            view.icon = CreateImage("Icono", row.transform, c.fontSize + 6f);
            view.text = CreateText("Texto", row.transform, c, c.fontSize, c.textColor);
            view.text.GetComponent<LayoutElement>().flexibleWidth = 1f;
            view.swatch.gameObject.SetActive(false);
            view.icon.gameObject.SetActive(false);
            return view;
        }

        private static Image CreateImage(string name, Transform parent, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.minWidth = size;
            le.minHeight = size;
            return img;
        }

        private static Text CreateText(string name, Transform parent, BuildLegendConfig c, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.font = c.font != null ? c.font : DefaultFont();
            t.fontSize = size;
            t.color = color;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.alignment = TextAnchor.MiddleLeft;
            go.AddComponent<LayoutElement>();
            return t;
        }

        private static Font _defaultFont;

        private static Font DefaultFont()
        {
            if (_defaultFont != null) return _defaultFont;
            try { _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (ArgumentException) { _defaultFont = null; }
            if (_defaultFont == null)
            {
                try { _defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (ArgumentException) { _defaultFont = null; }
            }
            return _defaultFont;
        }
    }
}
