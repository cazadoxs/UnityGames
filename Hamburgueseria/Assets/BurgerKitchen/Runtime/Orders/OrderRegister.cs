using UnityEngine;
using UnityEngine.Events;

namespace BurgerKitchen
{
    /// <summary>Caja registradora: toma nota al cliente que espera delante.</summary>
    public class OrderRegister : MonoBehaviour, IInteractable
    {
        public string stationName = "Caja";
        public CustomerFlow flow;
        public UnityEvent onOrderTaken;

        void Awake()
        {
            if (flow == null) flow = FindFirstObjectByType<CustomerFlow>();
        }

        public bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            prompt.Title = stationName;
            var c = flow != null ? flow.CustomerReadyToOrder : null;
            if (c != null) prompt.Primary = "Tomar nota";
            else prompt.Info = flow != null && flow.QueueCount > 0 ? "El cliente se está acercando" : "No hay nadie esperando";
            return true;
        }

        public void Interact(InteractionContext ctx)
        {
            var c = flow != null ? flow.CustomerReadyToOrder : null;
            if (c == null || OrderManager.Instance == null) return;
            if (OrderManager.Instance.TakeOrder(c) != null) onOrderTaken?.Invoke();
        }

        public void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime) { }
        public void Cycle(InteractionContext ctx, int direction) { }
    }
}
