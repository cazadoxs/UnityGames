using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Interfaz completa del inventario, generada por código al empezar (no hace falta
    /// montar nada a mano): ventana con rejilla, barra rápida inferior, tooltip,
    /// arrastrar y soltar, punto de mira, aviso de "Recoger" y mensajes.
    /// Todo el aspecto se cambia en "Theme". Para casillas con tu propio diseño,
    /// asigna un Prefab con InventorySlotUI en "Slot Prefab".
    /// </summary>
    [AddComponentMenu("Mecánicas/Inventario/UI/Interfaz del inventario")]
    public class InventoryUI : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Inventory inventory;
        [Tooltip("Opcional: para marcar la casilla seleccionada y bloquear su uso con el menú abierto.")]
        [SerializeField] private InventoryHotbar hotbar;
        [Tooltip("Opcional: para mostrar el aviso de recoger y bloquearlo con el menú abierto.")]
        [SerializeField] private InventoryInteractor interactor;
        [Tooltip("Opcional: muestra los mensajes de guardado.")]
        [SerializeField] private InventorySaver saver;
        [Tooltip("Opcional: Prefab de casilla propio (con InventorySlotUI).")]
        [SerializeField] private InventorySlotUI slotPrefab;

        [Header("Aspecto")]
        [SerializeField] private InventoryUITheme theme = new InventoryUITheme();

        [Header("Controles")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode alternateToggleKey = KeyCode.I;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;

        [Header("Comportamiento")]
        [Tooltip("Bloquea y oculta el cursor con el inventario cerrado (primera persona).")]
        [SerializeField] private bool lockCursorWhenClosed = true;
        [Tooltip("Pausa el juego (Time.timeScale = 0) con el inventario abierto.")]
        [SerializeField] private bool pauseWhenOpen = false;
        [SerializeField] private bool showHotbar = true;
        [SerializeField] private bool hideHotbarWhenOpen = true;
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private bool showPickupPrompt = true;
        [Tooltip("Muestra \"+3 Manzana\" al recoger.")]
        [SerializeField] private bool showPickupMessages = true;
        [SerializeField] private float messageDuration = 2.5f;
        [SerializeField] private int sortingOrder = 50;

        [Header("Eventos")]
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();

        /// <summary>true si cualquier inventario está abierto (úsalo en tu controlador para no girar la cámara).</summary>
        public static bool IsAnyOpen => openCount > 0;
        private static int openCount;

        public event Action<bool> OpenChanged;
        public bool IsOpen { get; private set; }
        public InventoryUITheme Theme => theme;
        public Inventory Inventory => inventory;

        // Vistas
        private RectTransform canvasRoot;
        private GameObject mainPanel;
        private RectTransform gridRoot;
        private RectTransform hotbarRoot;
        private Text weightText;
        private Text selectedNameText;
        private Text promptText;
        private Text messagesText;
        private GameObject crosshair;
        private RectTransform tooltip;
        private Text tooltipTitle, tooltipCategory, tooltipBody;
        private RectTransform dragGhost;
        private Image dragGhostIcon;
        private Text dragGhostText;

        private readonly List<InventorySlotUI> gridSlots = new List<InventorySlotUI>();
        private readonly List<InventorySlotUI> hotbarSlots = new List<InventorySlotUI>();
        private readonly List<KeyValuePair<string, float>> messages = new List<KeyValuePair<string, float>>();
        private InventorySlotUI hoveredSlot;
        private int dragFrom = -1;
        private bool dragOne;
        private bool dragAccepted;
        private float selectedNameTimer;
        private float timeScaleBeforeOpen = 1f;
        private Font font;
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => openCount = 0;

        private void Awake()
        {
            if (inventory == null)
#if UNITY_2023_1_OR_NEWER
                inventory = FindFirstObjectByType<Inventory>();
#else
                inventory = FindObjectOfType<Inventory>();
#endif
            if (inventory == null) { Debug.LogError("[Inventario] InventoryUI no encuentra ningún Inventory.", this); enabled = false; return; }
            if (hotbar == null) hotbar = inventory.GetComponentInChildren<InventoryHotbar>();
            if (interactor == null) interactor = inventory.GetComponentInChildren<InventoryInteractor>();
            if (saver == null) saver = inventory.GetComponentInChildren<InventorySaver>();
            font = theme.font != null ? theme.font : BuiltinFont();
            EnsureEventSystem();
            Build();
        }

        private void OnEnable()
        {
            if (inventory == null) return;
            inventory.SlotChanged += RefreshSlot;
            inventory.Changed += OnInventoryChanged;
            inventory.ItemAdded += OnItemAdded;
            inventory.InventoryFull += OnInventoryFull;
            if (hotbar != null) hotbar.SelectionChanged += OnSelectionChanged;
            if (interactor != null) interactor.Message += ShowMessage;
            if (saver != null) saver.Message += ShowMessage;
            if (built) RefreshAll();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.SlotChanged -= RefreshSlot;
                inventory.Changed -= OnInventoryChanged;
                inventory.ItemAdded -= OnItemAdded;
                inventory.InventoryFull -= OnInventoryFull;
            }
            if (hotbar != null) hotbar.SelectionChanged -= OnSelectionChanged;
            if (interactor != null) interactor.Message -= ShowMessage;
            if (saver != null) saver.Message -= ShowMessage;
            if (IsOpen) SetOpen(false);
        }

        private void Start()
        {
            ApplyCursor();
            RefreshAll();
        }

        private void Update()
        {
            if (InventoryInput.GetKeyDown(toggleKey) || InventoryInput.GetKeyDown(alternateToggleKey)) Toggle();
            else if (IsOpen && InventoryInput.GetKeyDown(closeKey)) SetOpen(false);

            if (promptText != null)
            {
                string p = (!IsOpen && showPickupPrompt && interactor != null) ? interactor.PromptText : string.Empty;
                if (promptText.text != p) promptText.text = p;
            }

            if (tooltip != null && tooltip.gameObject.activeSelf) PlaceNearMouse(tooltip, new Vector2(18f, -18f));

            if (selectedNameText != null && selectedNameTimer > 0f)
            {
                selectedNameTimer -= Time.unscaledDeltaTime;
                var c = selectedNameText.color;
                c.a = Mathf.Clamp01(selectedNameTimer);
                selectedNameText.color = c;
            }
            UpdateMessages();
        }

        // ================== API pública ==================
        public void Toggle() => SetOpen(!IsOpen);
        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        public void SetOpen(bool open)
        {
            if (IsOpen == open || !built) return;
            IsOpen = open;
            openCount = Mathf.Max(0, openCount + (open ? 1 : -1));

            mainPanel.SetActive(open);
            if (hotbarRoot != null) hotbarRoot.gameObject.SetActive(showHotbar && !(open && hideHotbarWhenOpen));
            if (crosshair != null) crosshair.SetActive(showCrosshair && !open);
            HideTooltip();
            CancelDrag();

            if (hotbar != null) hotbar.InputEnabled = !open;
            if (interactor != null) interactor.InteractionEnabled = !open;

            if (pauseWhenOpen)
            {
                if (open) { timeScaleBeforeOpen = Time.timeScale; Time.timeScale = 0f; }
                else Time.timeScale = timeScaleBeforeOpen;
            }
            ApplyCursor();
            OpenChanged?.Invoke(open);
            (open ? onOpened : onClosed).Invoke();
        }

        /// <summary>Muestra un mensaje temporal en pantalla (se puede llamar desde UnityEvents).</summary>
        public void ShowMessage(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            messages.Add(new KeyValuePair<string, float>(text, Time.unscaledTime + messageDuration));
            if (messages.Count > 5) messages.RemoveAt(0);
            UpdateMessages();
        }

        /// <summary>Vuelve a generar toda la interfaz (por ejemplo tras cambiar el tema por código).</summary>
        public void Rebuild()
        {
            if (canvasRoot != null) Destroy(canvasRoot.gameObject);
            gridSlots.Clear(); hotbarSlots.Clear();
            built = false;
            bool wasOpen = IsOpen;
            if (IsOpen) { IsOpen = false; openCount = Mathf.Max(0, openCount - 1); }
            font = theme.font != null ? theme.font : BuiltinFont();
            Build();
            RefreshAll();
            if (wasOpen) SetOpen(true);
        }

        // ================== Eventos del inventario ==================
        private void OnInventoryChanged()
        {
            if (gridSlots.Count != inventory.Capacity) { Rebuild(); return; }
            RefreshWeight();
            if (hoveredSlot != null) ShowTooltip(hoveredSlot);
        }

        private void OnItemAdded(ItemData item, int amount)
        {
            if (showPickupMessages) ShowMessage("+" + amount + " " + item.DisplayName);
        }

        private void OnInventoryFull(ItemData item) => ShowMessage("No cabe: " + item.DisplayName);

        private void OnSelectionChanged(int index)
        {
            for (int i = 0; i < inventory.HotbarSize; i++) RefreshSlot(i);
            var s = inventory.GetSlot(index);
            if (selectedNameText != null)
            {
                selectedNameText.text = s.IsEmpty ? string.Empty : s.item.DisplayName;
                selectedNameTimer = 2f;
            }
        }

        // ================== Ratón sobre casillas ==================
        internal void OnSlotHover(InventorySlotUI slot, bool enter)
        {
            if (enter) { hoveredSlot = slot; ShowTooltip(slot); }
            else if (hoveredSlot == slot) { hoveredSlot = null; HideTooltip(); }
        }

        internal void OnSlotClick(InventorySlotUI slot, PointerEventData e)
        {
            if (e.dragging) return;
            int i = slot.Index;
            switch (e.button)
            {
                case PointerEventData.InputButton.Right:
                    inventory.UseItem(i);
                    break;
                case PointerEventData.InputButton.Middle:
                    inventory.Split(i);
                    break;
                default:
                    if (InventoryInput.ShiftHeld) QuickMove(i);
                    else if (hotbar != null && i < inventory.HotbarSize) hotbar.Select(i);
                    break;
            }
        }

        internal void OnSlotBeginDrag(InventorySlotUI slot, PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            var s = inventory.GetSlot(slot.Index);
            if (s.IsEmpty) return;
            dragFrom = slot.Index;
            dragOne = InventoryInput.CtrlHeld;
            dragAccepted = false;
            dragGhost.gameObject.SetActive(true);
            dragGhostIcon.sprite = s.item.Icon;
            dragGhostIcon.color = s.item.Icon != null ? Color.white : s.item.IconColor;
            int n = dragOne ? 1 : s.amount;
            dragGhostText.text = n > 1 ? n.ToString() : (s.item.Icon == null ? InventorySlotUI.Initials(s.item.DisplayName) : string.Empty);
            dragGhost.position = e.position;
            HideTooltip();
        }

        internal void OnSlotDrag(InventorySlotUI slot, PointerEventData e)
        {
            if (dragFrom >= 0) dragGhost.position = e.position;
        }

        internal void OnSlotDrop(InventorySlotUI target, PointerEventData e)
        {
            if (dragFrom < 0) return;
            dragAccepted = true;
            if (target.Index == dragFrom) return;
            if (dragOne) inventory.MoveAmount(dragFrom, target.Index, 1);
            else inventory.MoveOrSwap(dragFrom, target.Index);
        }

        internal void OnSlotEndDrag(InventorySlotUI slot, PointerEventData e)
        {
            if (dragFrom < 0) return;
            bool overUI = e.pointerCurrentRaycast.gameObject != null;
            if (!dragAccepted && !overUI) inventory.DropFromSlot(dragFrom, dragOne ? 1 : int.MaxValue);
            CancelDrag();
        }

        private void CancelDrag()
        {
            dragFrom = -1;
            if (dragGhost != null) dragGhost.gameObject.SetActive(false);
        }

        /// <summary>Mayús+clic: pasa la pila de la barra rápida a la mochila o al revés.</summary>
        private void QuickMove(int index)
        {
            int hb = inventory.HotbarSize;
            bool fromHotbar = index < hb;
            int start = fromHotbar ? hb : 0;
            int end = fromHotbar ? inventory.Capacity : hb;
            if (end <= start) return;
            var item = inventory.GetSlot(index).item;
            if (item == null) return;

            // 1) Rellenar pilas iguales. 2) Primera casilla vacía.
            for (int pass = 0; pass < 2 && !inventory.GetSlot(index).IsEmpty; pass++)
            {
                for (int t = start; t < end && !inventory.GetSlot(index).IsEmpty; t++)
                {
                    var ts = inventory.GetSlot(t);
                    bool ok = pass == 0 ? (!ts.IsEmpty && ts.item == item && ts.SpaceLeft > 0) : ts.IsEmpty;
                    if (ok) inventory.MoveAmount(index, t, int.MaxValue);
                }
            }
        }

        // ================== Tooltip ==================
        private void ShowTooltip(InventorySlotUI slot)
        {
            if (tooltip == null || dragFrom >= 0) return;
            var s = inventory.GetSlot(slot.Index);
            if (s.IsEmpty) { HideTooltip(); return; }
            tooltipTitle.text = s.item.DisplayName;
            tooltipCategory.text = s.item.Category;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(s.item.Description)) sb.AppendLine(s.item.Description);
            if (s.item.IsStackable) sb.AppendLine("Cantidad: " + s.amount + " / " + s.item.MaxStack);
            if (s.item.Weight > 0f) sb.AppendLine("Peso: " + (s.item.Weight * s.amount).ToString("0.##"));
            string extra = s.item.GetTooltipExtra();
            if (!string.IsNullOrEmpty(extra)) sb.AppendLine(extra);
            if (s.item.Usable) sb.AppendLine("<i>Clic derecho para usar</i>");
            tooltipBody.text = sb.ToString().TrimEnd();
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();
            PlaceNearMouse(tooltip, new Vector2(18f, -18f));
        }

        private void HideTooltip()
        {
            if (tooltip != null) tooltip.gameObject.SetActive(false);
        }

        private void PlaceNearMouse(RectTransform rt, Vector2 offset)
        {
            Vector2 mouse = InventoryInput.MousePosition();
            rt.position = mouse + offset * CanvasScale();
            // Mantener dentro de la pantalla.
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 shift = Vector3.zero;
            if (corners[2].x > Screen.width) shift.x = -(corners[2].x - corners[0].x) - 2f * offset.x * CanvasScale();
            if (corners[0].y < 0f) shift.y = -corners[0].y;
            rt.position += shift;
        }

        private float CanvasScale() => canvasRoot != null ? canvasRoot.localScale.x : 1f;

        // ================== Refresco ==================
        private void RefreshAll()
        {
            if (!built || inventory == null) return;
            for (int i = 0; i < inventory.Capacity; i++) RefreshSlot(i);
            RefreshWeight();
        }

        private void RefreshSlot(int index)
        {
            if (!built) return;
            var s = inventory.GetSlot(index);
            bool selected = hotbar != null && index < inventory.HotbarSize && index == hotbar.SelectedIndex;
            if (index < gridSlots.Count) gridSlots[index].Refresh(s, selected);
            if (index < hotbarSlots.Count) hotbarSlots[index].Refresh(s, selected);
            if (hoveredSlot != null && hoveredSlot.Index == index) ShowTooltip(hoveredSlot);
        }

        private void RefreshWeight()
        {
            if (weightText == null) return;
            if (inventory.MaxWeight <= 0f) { weightText.text = string.Empty; return; }
            weightText.text = string.Format(theme.weightFormat, inventory.TotalWeight, inventory.MaxWeight);
            weightText.color = inventory.TotalWeight >= inventory.MaxWeight * 0.9f ? theme.warningColor : theme.secondaryTextColor;
        }

        private void UpdateMessages()
        {
            if (messagesText == null) return;
            float now = Time.unscaledTime;
            messages.RemoveAll(m => m.Value < now);
            var sb = new StringBuilder();
            foreach (var m in messages) sb.AppendLine(m.Key);
            string t = sb.ToString().TrimEnd();
            if (messagesText.text != t) messagesText.text = t;
        }

        private void ApplyCursor()
        {
            if (!lockCursorWhenClosed) return;
            bool free = IsAnyOpen;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        // ================== Construcción de la interfaz ==================
        private void Build()
        {
            var canvasGo = new GameObject("InventarioCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasRoot = (RectTransform)canvasGo.transform;

            BuildHud();
            BuildMainPanel();
            BuildTooltip();
            BuildDragGhost();

            mainPanel.SetActive(false);
            built = true;
        }

        private void BuildHud()
        {
            // Punto de mira
            var ch = NewRect("PuntoDeMira", canvasRoot);
            SetAnchored(ch, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));
            AddImage(ch, theme.crosshairColor, null, false);
            crosshair = ch.gameObject;
            crosshair.SetActive(showCrosshair);

            // Aviso de recoger
            var pr = NewRect("AvisoRecoger", canvasRoot);
            SetAnchored(pr, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 40f));
            promptText = AddText(pr, "", theme.fontSize + 2, TextAnchor.UpperCenter, theme.textColor);

            // Mensajes
            var msg = NewRect("Mensajes", canvasRoot);
            SetAnchored(msg, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 140f), new Vector2(600f, 200f));
            messagesText = AddText(msg, "", theme.fontSize, TextAnchor.LowerLeft, theme.textColor);

            // Barra rápida
            int hb = inventory.HotbarSize;
            if (hb <= 0) return;
            float w = hb * theme.slotSize + (hb - 1) * theme.slotSpacing + 2f * 8f;
            hotbarRoot = NewRect("BarraRapida", canvasRoot);
            SetAnchored(hotbarRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(w, theme.slotSize + 16f));
            AddImage(hotbarRoot, theme.panelColor, theme.panelSprite, true);
            var layout = hotbarRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = theme.slotSpacing;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            for (int i = 0; i < hb; i++)
            {
                var slot = CreateSlot(hotbarRoot, i, theme.hotbarSlotColor);
                if (slot.keyLabel != null) slot.keyLabel.text = (i + 1).ToString();
                hotbarSlots.Add(slot);
            }
            hotbarRoot.gameObject.SetActive(showHotbar);

            var sel = NewRect("NombreSeleccionado", canvasRoot);
            SetAnchored(sel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f + theme.slotSize + 24f), new Vector2(600f, 32f));
            selectedNameText = AddText(sel, "", theme.fontSize + 2, TextAnchor.MiddleCenter, theme.textColor);
        }

        private void BuildMainPanel()
        {
            int cols = Mathf.Max(1, theme.columns);
            int rows = Mathf.CeilToInt(inventory.Capacity / (float)cols);
            float pad = theme.panelPadding;
            float gridW = cols * theme.slotSize + (cols - 1) * theme.slotSpacing;
            float gridH = rows * theme.slotSize + (rows - 1) * theme.slotSpacing;
            const float header = 44f, footer = 70f;

            var panel = NewRect("Ventana", canvasRoot);
            SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f),
                new Vector2(gridW + 2f * pad, gridH + header + footer + 2f * pad));
            AddImage(panel, theme.panelColor, theme.panelSprite, true);
            mainPanel = panel.gameObject;

            var title = NewRect("Titulo", panel);
            SetTopLeft(title, new Vector2(pad, -pad), new Vector2(gridW * 0.6f, header - 8f));
            var tt = AddText(title, theme.title, theme.fontSize + 8, TextAnchor.MiddleLeft, theme.textColor);
            tt.fontStyle = FontStyle.Bold;

            var weight = NewRect("Peso", panel);
            SetTopLeft(weight, new Vector2(pad + gridW * 0.4f, -pad), new Vector2(gridW * 0.6f, header - 8f));
            weightText = AddText(weight, "", theme.fontSize, TextAnchor.MiddleRight, theme.secondaryTextColor);

            gridRoot = NewRect("Rejilla", panel);
            SetTopLeft(gridRoot, new Vector2(pad, -pad - header), new Vector2(gridW, gridH));
            var grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(theme.slotSize, theme.slotSize);
            grid.spacing = new Vector2(theme.slotSpacing, theme.slotSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            for (int i = 0; i < inventory.Capacity; i++)
            {
                var c = i < inventory.HotbarSize ? theme.hotbarSlotColor : theme.slotColor;
                var slot = CreateSlot(gridRoot, i, c);
                if (slot.keyLabel != null) slot.keyLabel.text = i < inventory.HotbarSize && i < 9 ? (i + 1).ToString() : string.Empty;
                gridSlots.Add(slot);
            }

            // Botón ordenar
            var btn = NewRect("BotonOrdenar", panel);
            SetTopLeft(btn, new Vector2(pad, -pad - header - gridH - 12f), new Vector2(140f, 36f));
            var bimg = AddImage(btn, theme.buttonColor, theme.slotSprite, true);
            var button = btn.gameObject.AddComponent<Button>();
            button.targetGraphic = bimg;
            button.onClick.AddListener(() => inventory.SortBackpack());
            var bt = NewRect("Texto", btn);
            Stretch(bt, 0f);
            AddText(bt, theme.sortButtonText, theme.fontSize, TextAnchor.MiddleCenter, theme.textColor);

            var help = NewRect("Ayuda", panel);
            SetTopLeft(help, new Vector2(pad + 152f, -pad - header - gridH - 8f), new Vector2(Mathf.Max(100f, gridW - 152f), footer - 8f));
            var ht = AddText(help, theme.helpText, Mathf.Max(10, theme.fontSize - 5), TextAnchor.UpperLeft, theme.secondaryTextColor);
            ht.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private void BuildTooltip()
        {
            tooltip = NewRect("Tooltip", canvasRoot);
            tooltip.anchorMin = tooltip.anchorMax = Vector2.zero;
            tooltip.pivot = new Vector2(0f, 1f);
            tooltip.sizeDelta = new Vector2(320f, 100f);
            AddImage(tooltip, theme.tooltipColor, theme.panelSprite, false);
            var v = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 10, 10);
            v.spacing = 4f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var fit = tooltip.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            tooltipTitle = AddText(NewRect("Nombre", tooltip), "", theme.fontSize + 2, TextAnchor.UpperLeft, theme.textColor);
            tooltipTitle.fontStyle = FontStyle.Bold;
            tooltipCategory = AddText(NewRect("Categoria", tooltip), "", Mathf.Max(10, theme.fontSize - 4), TextAnchor.UpperLeft, theme.selectedFrameColor);
            tooltipBody = AddText(NewRect("Descripcion", tooltip), "", Mathf.Max(10, theme.fontSize - 2), TextAnchor.UpperLeft, theme.secondaryTextColor);
            tooltipBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltip.gameObject.SetActive(false);
        }

        private void BuildDragGhost()
        {
            dragGhost = NewRect("Arrastrando", canvasRoot);
            dragGhost.anchorMin = dragGhost.anchorMax = Vector2.zero;
            dragGhost.pivot = new Vector2(0.5f, 0.5f);
            dragGhost.sizeDelta = Vector2.one * theme.slotSize * 0.8f;
            var cg = dragGhost.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false; cg.alpha = 0.85f;
            dragGhostIcon = AddImage(dragGhost, Color.white, null, false);
            dragGhostIcon.preserveAspect = true;
            var t = NewRect("Texto", dragGhost);
            Stretch(t, 0f);
            dragGhostText = AddText(t, "", theme.fontSize, TextAnchor.MiddleCenter, theme.textColor);
            dragGhostText.fontStyle = FontStyle.Bold;
            dragGhost.gameObject.SetActive(false);
        }

        private InventorySlotUI CreateSlot(RectTransform parent, int index, Color color)
        {
            InventorySlotUI slot;
            if (slotPrefab != null)
            {
                slot = Instantiate(slotPrefab, parent);
                ((RectTransform)slot.transform).sizeDelta = new Vector2(theme.slotSize, theme.slotSize);
            }
            else slot = BuildDefaultSlot(parent);
            slot.name = "Casilla " + index;
            slot.Bind(this, index, color);
            return slot;
        }

        private InventorySlotUI BuildDefaultSlot(RectTransform parent)
        {
            var rt = NewRect("Casilla", parent);
            rt.sizeDelta = new Vector2(theme.slotSize, theme.slotSize);
            var slot = rt.gameObject.AddComponent<InventorySlotUI>();
            slot.background = AddImage(rt, theme.slotColor, theme.slotSprite, true);

            // Marco de selección (4 bordes)
            var frame = NewRect("Seleccion", rt);
            Stretch(frame, 0f);
            const float b = 3f;
            Edge(frame, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -b), Vector2.zero);
            Edge(frame, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, b));
            Edge(frame, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(b, 0));
            Edge(frame, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-b, 0), Vector2.zero);
            frame.gameObject.SetActive(false);
            slot.selectedFrame = frame.gameObject;

            var icon = NewRect("Icono", rt);
            Stretch(icon, theme.slotSize * 0.14f);
            slot.icon = AddImage(icon, Color.white, null, false);
            slot.icon.enabled = false;

            var ini = NewRect("Iniciales", rt);
            Stretch(ini, 0f);
            slot.initials = AddText(ini, "", Mathf.RoundToInt(theme.slotSize * 0.28f), TextAnchor.MiddleCenter, new Color(0f, 0f, 0f, 0.75f));
            slot.initials.fontStyle = FontStyle.Bold;
            if (slot.initials.GetComponent<Shadow>() is Shadow sh) Destroy(sh);

            var amount = NewRect("Cantidad", rt);
            Stretch(amount, 5f);
            slot.amountText = AddText(amount, "", theme.fontSize, TextAnchor.LowerRight, theme.textColor);
            slot.amountText.fontStyle = FontStyle.Bold;

            var key = NewRect("Tecla", rt);
            Stretch(key, 4f);
            slot.keyLabel = AddText(key, "", Mathf.Max(9, theme.fontSize - 6), TextAnchor.UpperLeft, theme.secondaryTextColor);
            return slot;
        }

        private void Edge(RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var e = NewRect("Borde", parent);
            e.anchorMin = aMin; e.anchorMax = aMax; e.offsetMin = oMin; e.offsetMax = oMax;
            AddImage(e, theme.selectedFrameColor, null, false);
        }

        // ---------- Ayudantes de construcción ----------
        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void SetTopLeft(RectTransform rt, Vector2 pos, Vector2 size) =>
            SetAnchored(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static Image AddImage(RectTransform rt, Color color, Sprite sprite, bool raycast)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            return img;
        }

        private Text AddText(RectTransform rt, string text, int size, TextAnchor align, Color color)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
            sh.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        private static Font BuiltinFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<EventSystem>() != null) return;
#else
            if (FindObjectOfType<EventSystem>() != null) return;
#endif
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
