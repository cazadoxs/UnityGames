using System.Collections.Generic;

namespace BurgerKitchen
{
    public class OrderLine
    {
        public readonly ProductDefinition Product;
        public bool Delivered;

        public OrderLine(ProductDefinition product) { Product = product; }
    }

    /// <summary>Pedido de un cliente. Se puede entregar por partes (o todo junto en bandeja).</summary>
    public class Order
    {
        public readonly int Id;
        public readonly List<OrderLine> Lines = new List<OrderLine>();
        /// <summary>Cliente que lo pidió (puede ser null en pruebas).</summary>
        public readonly Customer Customer;

        public Order(int id, IEnumerable<ProductDefinition> products, Customer customer = null)
        {
            Id = id;
            Customer = customer;
            foreach (var p in products)
                if (p != null) Lines.Add(new OrderLine(p));
        }

        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < Lines.Count; i++)
                    if (!Lines[i].Delivered) return false;
                return true;
            }
        }

        public int TotalPrice
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Lines.Count; i++) total += Lines[i].Product.price;
                return total;
            }
        }

        public float ExtraPatience
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < Lines.Count; i++) total += Lines[i].Product.extraPatience;
                return total;
            }
        }

        /// <summary>Marca como entregada la primera línea pendiente que coincide.</summary>
        public bool TryDeliver(ItemSnapshot snapshot, out OrderLine line)
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var l = Lines[i];
                if (!l.Delivered && l.Product.Matches(snapshot))
                {
                    l.Delivered = true;
                    line = l;
                    return true;
                }
            }
            line = null;
            return false;
        }

        public bool Wants(ItemSnapshot snapshot)
        {
            for (int i = 0; i < Lines.Count; i++)
                if (!Lines[i].Delivered && Lines[i].Product.Matches(snapshot)) return true;
            return false;
        }
    }
}
