using System;
using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Barra rápida: selecciona una de las primeras casillas con 1-9 o la rueda,
    /// muestra el objeto en la mano y permite usarlo o tirarlo sin abrir el inventario.
    /// </summary>
    [AddComponentMenu("Mecánicas/Inventario/Barra rápida y mano")]
    public class InventoryHotbar : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [Tooltip("Punto donde aparece el objeto en la mano (hijo de la cámara). Vacío = no se muestra.")]
        [SerializeField] private Transform handAnchor;

        [Header("Controles")]
        [SerializeField] private bool numberKeys = true;
        [SerializeField] private bool mouseWheel = true;
        [SerializeField] private bool invertWheel = false;
        [SerializeField] private KeyCode useKey = KeyCode.Mouse0;
        [Tooltip("Tirar una unidad. Con Mayús se tira la pila entera.")]
        [SerializeField] private KeyCode dropKey = KeyCode.G;

        [Header("Mano")]
        [Tooltip("Pequeño balanceo del objeto en la mano al moverse la cámara.")]
        [SerializeField] private bool sway = true;
        [SerializeField] private float swayAmount = 0.02f;
        [SerializeField] private float swaySmooth = 8f;

        public int SelectedIndex { get; private set; }
        public ItemStack SelectedStack => inventory != null ? inventory.GetSlot(SelectedIndex) : ItemStack.Empty;
        public Inventory Inventory => inventory;
        public KeyCode UseKey => useKey;
        public KeyCode DropKey => dropKey;

        /// <summary>Pon a false mientras haya menús abiertos.</summary>
        public bool InputEnabled { get; set; } = true;

        public event Action<int> SelectionChanged;

        private GameObject heldInstance;
        private ItemData heldItem;
        private Vector3 anchorBasePos;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponentInParent<Inventory>();
            if (handAnchor != null) anchorBasePos = handAnchor.localPosition;
        }

        private void OnEnable()
        {
            if (inventory != null) inventory.Changed += RefreshHeld;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.Changed -= RefreshHeld;
        }

        private void Start() => RefreshHeld();

        private void Update()
        {
            if (inventory == null || inventory.HotbarSize <= 0) return;

            if (InputEnabled)
            {
                if (numberKeys)
                {
                    int n = InventoryInput.NumberKeyDown();
                    if (n >= 0 && n < inventory.HotbarSize) Select(n);
                }
                if (mouseWheel)
                {
                    float s = InventoryInput.ScrollDelta();
                    if (invertWheel) s = -s;
                    if (s > 0.01f) Select(SelectedIndex - 1);
                    else if (s < -0.01f) Select(SelectedIndex + 1);
                }
                if (InventoryInput.GetKeyDown(useKey)) inventory.UseItem(SelectedIndex);
                if (InventoryInput.GetKeyDown(dropKey))
                    inventory.DropFromSlot(SelectedIndex, InventoryInput.ShiftHeld ? int.MaxValue : 1);
            }

            if (sway && handAnchor != null)
            {
                Vector2 d = InputEnabled ? InventoryInput.MouseDelta() : Vector2.zero;
                Vector3 target = anchorBasePos + new Vector3(-d.x, -d.y, 0f) * swayAmount;
                target = anchorBasePos + Vector3.ClampMagnitude(target - anchorBasePos, 0.08f);
                handAnchor.localPosition = Vector3.Lerp(handAnchor.localPosition, target, Time.deltaTime * swaySmooth);
            }
        }

        /// <summary>Selecciona una casilla de la barra (da la vuelta en los extremos).</summary>
        public void Select(int index)
        {
            int size = inventory != null ? inventory.HotbarSize : 0;
            if (size <= 0) return;
            index = ((index % size) + size) % size;
            if (index == SelectedIndex) return;
            SelectedIndex = index;
            RefreshHeld();
            SelectionChanged?.Invoke(SelectedIndex);
        }

        private void RefreshHeld()
        {
            if (handAnchor == null) return;
            var s = SelectedStack;
            ItemData wanted = s.IsEmpty ? null : s.item;
            if (wanted == heldItem && (heldInstance != null || wanted == null)) return;

            if (heldInstance != null) Destroy(heldInstance);
            heldInstance = null;
            heldItem = wanted;
            if (wanted == null || wanted.HeldPrefab == null) return;

            heldInstance = Instantiate(wanted.HeldPrefab, handAnchor);
            heldInstance.transform.localPosition = Vector3.zero;
            heldInstance.transform.localRotation = Quaternion.identity;
            heldInstance.transform.localScale *= wanted.HeldScale;
            MakeVisualOnly(heldInstance);
        }

        /// <summary>Quita física y lógica de recogida a la copia que se ve en la mano.</summary>
        private static void MakeVisualOnly(GameObject go)
        {
            foreach (var wi in go.GetComponentsInChildren<WorldItem>(true)) Destroy(wi);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; Destroy(rb); }
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
