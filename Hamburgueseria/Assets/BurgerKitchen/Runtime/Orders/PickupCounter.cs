using UnityEngine;
using UnityEngine.Events;

namespace BurgerKitchen
{
    /// <summary>Mostrador de entrega: con un producto o una bandeja en la mano, se entrega a los pedidos abiertos.</summary>
    public class PickupCounter : MonoBehaviour, IInteractable
    {
        public string stationName = "Entrega de pedidos";
        public UnityEvent onDelivered;
        public UnityEvent onRejected;

        public bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            prompt.Title = stationName;
            var om = OrderManager.Instance;
            int open = om != null ? om.ActiveOrders.Count : 0;
            prompt.Info = open == 1 ? "1 pedido abierto" : open + " pedidos abiertos";
            var held = ctx.Held;
            if (held != null)
                prompt.Primary = om != null && om.AnyOrderWants(held) ? "Entregar " + held.Definition.DisplayName : "Entregar (no coincide)";
            return true;
        }

        public void Interact(InteractionContext ctx)
        {
            var held = ctx.Held;
            if (held == null || OrderManager.Instance == null) return;
            var r = OrderManager.Instance.Deliver(held);
            if (r.ItemsDelivered > 0) onDelivered?.Invoke(); else onRejected?.Invoke();
        }

        public void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime) { }
        public void Cycle(InteractionContext ctx, int direction) { }
    }
}
