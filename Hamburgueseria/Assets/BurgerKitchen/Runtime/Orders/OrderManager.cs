using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    public struct DeliveryResult
    {
        public int ItemsDelivered;
        public int OrdersCompleted;
        public int ItemsRejected;
    }

    /// <summary>Pedidos activos: los crea al tomar nota, recibe entregas, cobra y gestiona los fallos.</summary>
    [DefaultExecutionOrder(-50)]
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        [Tooltip("Si se deja vacío usa la carta del KitchenGameManager.")]
        public MenuDefinition menuOverride;
        [Tooltip("Semilla (0 = aleatoria).")]
        public int randomSeed;

        readonly List<Order> active = new List<Order>();
        public IReadOnlyList<Order> ActiveOrders => active;

        System.Random rng;
        int nextId = 1;

        void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning("[BurgerKitchen] Hay dos OrderManager en la escena."); }
            Instance = this;
            rng = randomSeed != 0 ? new System.Random(randomSeed) : new System.Random();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        MenuDefinition Menu => menuOverride != null ? menuOverride
            : KitchenGameManager.Instance != null ? KitchenGameManager.Instance.menu : null;

        int Day => KitchenGameManager.Instance != null ? KitchenGameManager.Instance.Day : 1;

        /// <summary>Toma nota al cliente que está en la caja.</summary>
        public Order TakeOrder(Customer customer)
        {
            if (customer == null || customer.Order != null) return null;
            var menu = Menu;
            if (menu == null) { Debug.LogError("[BurgerKitchen] No hay MenuDefinition asignada."); return null; }

            var products = OrderGenerator.Generate(menu, Day, rng);
            var order = new Order(nextId++, products, customer);
            active.Add(order);

            var settings = KitchenGameManager.Instance != null ? KitchenGameManager.Instance.settings : null;
            float patience = settings != null
                ? settings.basePatience + settings.patiencePerItem * order.Lines.Count + order.ExtraPatience
                : 60f + order.ExtraPatience;
            customer.AssignOrder(order, patience);

            var flow = FindFlow(customer);
            if (flow != null) flow.MoveToWaiting(customer);

            KitchenEvents.RaiseOrderCreated(order);
            return order;
        }

        static CustomerFlow cachedFlow;
        static CustomerFlow FindFlow(Customer c)
        {
            if (cachedFlow == null) cachedFlow = FindFirstObjectByType<CustomerFlow>();
            return cachedFlow;
        }

        /// <summary>¿Algún pedido quiere este objeto (o algo de esta bandeja)?</summary>
        public bool AnyOrderWants(Item item)
        {
            if (item == null) return false;
            var s = item.ToSnapshot();
            foreach (var o in active) if (o.Wants(s)) return true;
            if (item.Definition.HasAnyTag(ItemTags.Tray) && item.Container != null)
                foreach (var c in item.Container.Contents)
                {
                    var cs = c.ToSnapshot();
                    foreach (var o in active) if (o.Wants(cs)) return true;
                }
            return false;
        }

        /// <summary>Entrega un objeto. Las bandejas entregan cada cosa que llevan.</summary>
        public DeliveryResult Deliver(Item item)
        {
            var result = new DeliveryResult();
            if (item == null) return result;

            bool isTray = item.Definition.HasAnyTag(ItemTags.Tray) && item.Container != null;
            if (isTray)
            {
                var contents = new List<Item>(item.Container.Contents);
                foreach (var c in contents) DeliverSingle(c, ref result);
                if (item.Container.Contents.Count == 0 && result.ItemsDelivered > 0) item.Consume();
            }
            else DeliverSingle(item, ref result);

            if (result.ItemsDelivered == 0)
                KitchenEvents.Post(item.IsDirty ? "¡Eso está sucio!" : "Nadie ha pedido eso", MessageKind.Bad);
            return result;
        }

        void DeliverSingle(Item item, ref DeliveryResult result)
        {
            var snapshot = item.ToSnapshot();
            foreach (var order in active)
            {
                if (!order.TryDeliver(snapshot, out var line)) continue;
                result.ItemsDelivered++;
                item.Consume();
                KitchenEvents.RaiseItemDelivered(order, line);
                if (order.IsComplete) { Complete(order); result.OrdersCompleted++; }
                return;
            }
            result.ItemsRejected++;
        }

        void Complete(Order order)
        {
            active.Remove(order);
            float satisfaction = order.Customer != null ? order.Customer.PatienceFraction : 1f;
            int price = order.TotalPrice;
            var gm = KitchenGameManager.Instance;
            int tip = gm != null && gm.settings != null ? Mathf.RoundToInt(price * gm.settings.maxTipFraction * satisfaction) : 0;
            if (gm != null) gm.RegisterSale(price, tip, satisfaction);
            KitchenEvents.RaiseOrderCompleted(order, price, tip);
            KitchenEvents.Post($"Pedido #{order.Id} servido  +${price}" + (tip > 0 ? $" (+${tip} propina)" : ""), MessageKind.Good);
            if (order.Customer != null) FindFlow(order.Customer)?.Release(order.Customer, true);
        }

        /// <summary>Un cliente se cansa de esperar (en cola o esperando la comida).</summary>
        public void CustomerGaveUp(Customer c)
        {
            if (c == null) return;
            if (c.Order != null && active.Remove(c.Order))
                KitchenEvents.RaiseOrderFailed(c.Order);
            KitchenGameManager.Instance?.RegisterLostCustomer();
            KitchenEvents.RaiseCustomerLeftAngry(c);
            KitchenEvents.Post(c.Order != null ? $"El cliente del pedido #{c.Order.Id} se ha ido enfadado" : "Un cliente se ha cansado de esperar", MessageKind.Bad);
            FindFlow(c)?.Release(c, false);
        }

        public void ClearAll()
        {
            active.Clear();
            nextId = 1;
        }
    }
}
