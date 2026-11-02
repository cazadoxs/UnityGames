using System;
using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>Una casilla del inventario: qué objeto hay y cuántas unidades.</summary>
    [Serializable]
    public struct ItemStack
    {
        public ItemData item;
        [Min(0)] public int amount;

        public ItemStack(ItemData item, int amount)
        {
            this.item = item;
            this.amount = item == null ? 0 : Mathf.Max(0, amount);
        }

        public static ItemStack Empty => new ItemStack(null, 0);

        public bool IsEmpty => item == null || amount <= 0;
        public int SpaceLeft => IsEmpty ? 0 : Mathf.Max(0, item.MaxStack - amount);
        public float TotalWeight => IsEmpty ? 0f : item.Weight * amount;

        public override string ToString() => IsEmpty ? "(vacía)" : item.DisplayName + " x" + amount;
    }
}
