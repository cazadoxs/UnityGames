using System;

namespace BurgerKitchen
{
    public enum MessageKind { Info, Good, Bad }

    /// <summary>
    /// Eventos globales para enganchar interfaz, sonidos, logros o analítica sin tocar los sistemas.
    /// </summary>
    public static class KitchenEvents
    {
        public static event Action<Order> OrderCreated;
        public static event Action<Order, OrderLine> ItemDelivered;
        /// <summary>Pedido completado: pedido, dinero cobrado (precio), propina.</summary>
        public static event Action<Order, int, int> OrderCompleted;
        public static event Action<Order> OrderFailed;
        public static event Action<Customer> CustomerLeftAngry;
        public static event Action<int> MoneyChanged;
        public static event Action<int> DayStarted;
        public static event Action<int, DayStats> DayEnded;
        public static event Action<string, MessageKind> Message;

        internal static void RaiseOrderCreated(Order o) => OrderCreated?.Invoke(o);
        internal static void RaiseItemDelivered(Order o, OrderLine l) => ItemDelivered?.Invoke(o, l);
        internal static void RaiseOrderCompleted(Order o, int price, int tip) => OrderCompleted?.Invoke(o, price, tip);
        internal static void RaiseOrderFailed(Order o) => OrderFailed?.Invoke(o);
        internal static void RaiseCustomerLeftAngry(Customer c) => CustomerLeftAngry?.Invoke(c);
        internal static void RaiseMoneyChanged(int money) => MoneyChanged?.Invoke(money);
        internal static void RaiseDayStarted(int day) => DayStarted?.Invoke(day);
        internal static void RaiseDayEnded(int day, DayStats stats) => DayEnded?.Invoke(day, stats);

        public static void Post(string text, MessageKind kind = MessageKind.Info) => Message?.Invoke(text, kind);
    }

    [Serializable]
    public class DayStats
    {
        public int revenue;
        public int tips;
        public int spent;
        public int ordersCompleted;
        public int customersLost;
        public float satisfactionSum;
        public int satisfactionCount;

        public int Profit => revenue + tips - spent;
        public float AverageSatisfaction => satisfactionCount > 0 ? satisfactionSum / satisfactionCount : 1f;
    }
}
