using UnityEngine;
using UnityEngine.Events;

namespace BurgerKitchen
{
    /// <summary>
    /// Dispensador de un objeto (nevera de carne, caja de panes, pila de vasos, salsa...).
    /// Si llevas en la mano un contenedor que acepta el objeto, lo añade directamente
    /// (p. ej. con el papel de hamburguesa en la mano → añade queso).
    /// </summary>
    public class ItemSource : MonoBehaviour, IInteractable
    {
        public ItemDefinition item;
        public string stationName = "";
        [Tooltip("Existencias (-1 = infinitas).")]
        public int stock = -1;
        [Tooltip("Coste por unidad que se descuenta del dinero (0 = gratis).")]
        [Min(0)] public int costPerItem = 0;
        [Tooltip("Permite devolver el objeto si está limpio y sin procesar.")]
        public bool allowReturn = true;
        [Tooltip("Dónde aparece el objeto antes de ir a la mano.")]
        public Transform spawnPoint;

        public UnityEvent onItemTaken;

        string Title => string.IsNullOrEmpty(stationName) ? (item != null ? item.DisplayName : name) : stationName;
        bool HasStock => stock != 0;

        public bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            if (item == null) return false;
            prompt.Title = Title;
            if (stock >= 0) prompt.Info = "Quedan: " + stock;
            if (costPerItem > 0) prompt.Info = (prompt.Info != null ? prompt.Info + " · " : "") + "$" + costPerItem;
            var held = ctx.Held;
            if (!HasStock) { prompt.Info = "Agotado"; return true; }
            if (held == null) prompt.Primary = "Coger " + item.DisplayName;
            else if (CanAddInto(held)) prompt.Primary = "Añadir " + item.DisplayName;
            else if (CanReturn(held)) prompt.Primary = "Devolver";
            return true;
        }

        bool CanAddInto(Item held)
        {
            if (held.Container == null || !held.Container.IsActive || held.Container.IsFull) return false;
            return item.HasAnyTag(held.Definition.container.acceptedTags);
        }

        bool CanReturn(Item held) => allowReturn && held.Definition == item && !held.IsDirty
                                     && (held.Container == null || held.Container.Contents.Count == 0);

        public void Interact(InteractionContext ctx)
        {
            if (item == null) return;
            var held = ctx.Held;
            if (held != null && CanReturn(held) && !CanAddInto(held))
            {
                held.Consume();
                if (stock >= 0) stock++;
                if (costPerItem > 0) KitchenGameManager.Instance?.Refund(costPerItem);
                return;
            }
            if (!HasStock) return;
            if (held != null && !CanAddInto(held)) return;
            if (costPerItem > 0 && KitchenGameManager.Instance != null && !KitchenGameManager.Instance.TrySpend(costPerItem))
            {
                KitchenEvents.Post("No hay dinero suficiente", MessageKind.Bad);
                return;
            }

            var origin = spawnPoint != null ? spawnPoint : transform;
            var spawned = ItemFactory.Spawn(item, origin.position, origin.rotation);
            bool ok = held != null ? held.Container.TryAdd(spawned) : ctx.Interactor.TryPickUp(spawned);
            if (!ok)
            {
                Destroy(spawned.gameObject);
                return;
            }
            if (stock > 0) stock--;
            onItemTaken?.Invoke();
        }

        public void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime) { }
        public void Cycle(InteractionContext ctx, int direction) { }
    }
}
