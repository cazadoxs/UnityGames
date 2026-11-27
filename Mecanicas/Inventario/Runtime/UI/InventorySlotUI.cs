using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Una casilla de la interfaz. La crea InventoryUI automáticamente, pero puedes
    /// hacer tu propio Prefab de casilla con este componente y asignarlo en
    /// InventoryUI > Slot Prefab (rellena las referencias de abajo).
    /// </summary>
    [AddComponentMenu("Mecánicas/Inventario/UI/Casilla")]
    public class InventorySlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Tooltip("Fondo de la casilla (cambia de color al pasar el ratón).")]
        public Image background;
        [Tooltip("Icono del objeto.")]
        public Image icon;
        [Tooltip("Iniciales cuando el objeto no tiene icono (opcional).")]
        public Text initials;
        [Tooltip("Cantidad de la pila.")]
        public Text amountText;
        [Tooltip("Marco de selección de la barra rápida (opcional).")]
        public GameObject selectedFrame;
        [Tooltip("Número de tecla 1-9 (opcional).")]
        public Text keyLabel;

        public int Index { get; private set; } = -1;
        public InventoryUI Owner { get; private set; }

        private Color baseColor;
        private bool hovered;

        public void Bind(InventoryUI owner, int index, Color slotColor)
        {
            Owner = owner;
            Index = index;
            baseColor = slotColor;
            if (background != null) background.color = baseColor;
        }

        public void Refresh(ItemStack stack, bool selected)
        {
            bool has = !stack.IsEmpty;
            if (icon != null)
            {
                icon.enabled = has;
                if (has)
                {
                    icon.sprite = stack.item.Icon;
                    icon.color = stack.item.Icon != null ? Color.white : stack.item.IconColor;
                    icon.preserveAspect = true;
                }
            }
            if (initials != null)
            {
                bool show = has && stack.item.Icon == null;
                initials.enabled = show;
                if (show) initials.text = Initials(stack.item.DisplayName);
            }
            if (amountText != null)
            {
                amountText.enabled = has && stack.amount > 1;
                if (amountText.enabled) amountText.text = stack.amount.ToString();
            }
            if (selectedFrame != null && selectedFrame.activeSelf != selected) selectedFrame.SetActive(selected);
            if (background != null) background.color = hovered && Owner != null ? Owner.Theme.slotHoverColor : baseColor;
        }

        public static string Initials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
            return name.Substring(0, Mathf.Min(2, name.Length)).ToUpperInvariant();
        }

        // ---------- Ratón (todo se delega en InventoryUI) ----------
        public void OnPointerEnter(PointerEventData e)
        {
            hovered = true;
            if (background != null && Owner != null) background.color = Owner.Theme.slotHoverColor;
            Owner?.OnSlotHover(this, true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            hovered = false;
            if (background != null) background.color = baseColor;
            Owner?.OnSlotHover(this, false);
        }

        public void OnPointerClick(PointerEventData e) => Owner?.OnSlotClick(this, e);
        public void OnBeginDrag(PointerEventData e) => Owner?.OnSlotBeginDrag(this, e);
        public void OnDrag(PointerEventData e) => Owner?.OnSlotDrag(this, e);
        public void OnEndDrag(PointerEventData e) => Owner?.OnSlotEndDrag(this, e);
        public void OnDrop(PointerEventData e) => Owner?.OnSlotDrop(this, e);
    }
}
