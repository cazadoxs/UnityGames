using System;
using System.Collections.Generic;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Lógica pura del inventario (sin MonoBehaviour): casillas, pilas, peso,
    /// mover, dividir, ordenar. El componente Inventory la envuelve.
    /// Se puede usar suelta (cofres, tiendas, NPCs) creando un "new InventoryModel(20)".
    /// </summary>
    public class InventoryModel
    {
        private ItemStack[] slots;

        /// <summary>Peso máximo. 0 o menos = sin límite.</summary>
        public float MaxWeight { get; set; }

        /// <summary>Se lanza cuando cambia una casilla concreta (índice).</summary>
        public event Action<int> SlotChanged;
        /// <summary>Se lanza una vez por cada operación que cambie algo.</summary>
        public event Action Changed;

        public InventoryModel(int capacity, float maxWeight = 0f)
        {
            slots = new ItemStack[Math.Max(1, capacity)];
            MaxWeight = maxWeight;
        }

        public int Capacity => slots.Length;
        public bool HasWeightLimit => MaxWeight > 0f;

        public ItemStack GetSlot(int index) => IsValid(index) ? slots[index] : ItemStack.Empty;
        public bool IsValid(int index) => index >= 0 && index < slots.Length;

        public float TotalWeight
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < slots.Length; i++) total += slots[i].TotalWeight;
                return total;
            }
        }

        public int EmptySlotCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < slots.Length; i++) if (slots[i].IsEmpty) n++;
                return n;
            }
        }

        public bool IsFull => EmptySlotCount == 0;

        /// <summary>Cuántas unidades de un objeto hay en total.</summary>
        public int Count(ItemData item)
        {
            if (item == null) return 0;
            int n = 0;
            for (int i = 0; i < slots.Length; i++) if (slots[i].item == item) n += slots[i].amount;
            return n;
        }

        public bool Contains(ItemData item, int amount = 1) => Count(item) >= amount;

        /// <summary>Primera casilla que contiene el objeto, o -1.</summary>
        public int IndexOf(ItemData item)
        {
            for (int i = 0; i < slots.Length; i++) if (!slots[i].IsEmpty && slots[i].item == item) return i;
            return -1;
        }

        public int FirstEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++) if (slots[i].IsEmpty) return i;
            return -1;
        }

        /// <summary>Cuántas unidades caben por peso (int.MaxValue si no hay límite).</summary>
        public int MaxAddableByWeight(ItemData item)
        {
            if (item == null) return 0;
            if (!HasWeightLimit || item.Weight <= 0f) return int.MaxValue;
            float free = MaxWeight - TotalWeight;
            if (free <= 0f) return 0;
            return (int)Math.Floor(free / item.Weight + 0.0001f);
        }

        /// <summary>Cuántas unidades de este objeto se podrían añadir ahora mismo (espacio y peso).</summary>
        public int SpaceFor(ItemData item)
        {
            if (item == null) return 0;
            long space = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty) space += item.MaxStack;
                else if (slots[i].item == item) space += slots[i].SpaceLeft;
            }
            return (int)Math.Min(space, MaxAddableByWeight(item));
        }

        public bool CanAdd(ItemData item, int amount = 1) => SpaceFor(item) >= amount;

        /// <summary>
        /// Añade unidades: primero rellena pilas existentes y luego casillas vacías.
        /// Devuelve las unidades que NO han cabido (0 = todo añadido).
        /// </summary>
        public int Add(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return Math.Max(0, amount);
            int toAdd = Math.Min(amount, MaxAddableByWeight(item));
            int remaining = toAdd;
            bool changed = false;

            if (item.IsStackable)
            {
                for (int i = 0; i < slots.Length && remaining > 0; i++)
                {
                    if (slots[i].IsEmpty || slots[i].item != item) continue;
                    int put = Math.Min(remaining, slots[i].SpaceLeft);
                    if (put <= 0) continue;
                    slots[i].amount += put;
                    remaining -= put;
                    changed = true;
                    SlotChanged?.Invoke(i);
                }
            }

            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;
                int put = Math.Min(remaining, item.MaxStack);
                slots[i] = new ItemStack(item, put);
                remaining -= put;
                changed = true;
                SlotChanged?.Invoke(i);
            }

            if (changed) Changed?.Invoke();
            return amount - toAdd + remaining;
        }

        /// <summary>Añade en una casilla concreta. Devuelve lo que no ha cabido.</summary>
        public int AddToSlot(int index, ItemData item, int amount)
        {
            if (!IsValid(index) || item == null || amount <= 0) return Math.Max(0, amount);
            var s = slots[index];
            if (!s.IsEmpty && s.item != item) return amount;
            int room = s.IsEmpty ? item.MaxStack : s.SpaceLeft;
            int put = Math.Min(Math.Min(amount, room), MaxAddableByWeight(item));
            if (put <= 0) return amount;
            slots[index] = new ItemStack(item, (s.IsEmpty ? 0 : s.amount) + put);
            SlotChanged?.Invoke(index);
            Changed?.Invoke();
            return amount - put;
        }

        /// <summary>Quita unidades de un objeto (empezando por las últimas casillas). Devuelve cuántas se han quitado.</summary>
        public int Remove(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return 0;
            int remaining = amount;
            for (int i = slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                if (slots[i].IsEmpty || slots[i].item != item) continue;
                int take = Math.Min(remaining, slots[i].amount);
                slots[i].amount -= take;
                if (slots[i].amount <= 0) slots[i] = ItemStack.Empty;
                remaining -= take;
                SlotChanged?.Invoke(i);
            }
            int removed = amount - remaining;
            if (removed > 0) Changed?.Invoke();
            return removed;
        }

        /// <summary>Quita unidades de una casilla. Devuelve lo que se ha quitado (con qué objeto).</summary>
        public ItemStack RemoveFromSlot(int index, int amount)
        {
            if (!IsValid(index) || slots[index].IsEmpty || amount <= 0) return ItemStack.Empty;
            var s = slots[index];
            int take = Math.Min(amount, s.amount);
            s.amount -= take;
            slots[index] = s.amount <= 0 ? ItemStack.Empty : s;
            SlotChanged?.Invoke(index);
            Changed?.Invoke();
            return new ItemStack(s.item, take);
        }

        /// <summary>
        /// Mueve una casilla sobre otra: si es el mismo objeto apilable, junta las pilas
        /// (lo que sobra se queda en el origen); si no, las intercambia.
        /// </summary>
        public void MoveOrSwap(int from, int to)
        {
            if (!IsValid(from) || !IsValid(to) || from == to) return;
            var a = slots[from];
            var b = slots[to];
            if (a.IsEmpty) return;

            if (!b.IsEmpty && a.item == b.item && a.item.IsStackable)
            {
                int put = Math.Min(a.amount, b.SpaceLeft);
                if (put <= 0) { slots[from] = b; slots[to] = a; }
                else
                {
                    b.amount += put;
                    a.amount -= put;
                    slots[to] = b;
                    slots[from] = a.amount <= 0 ? ItemStack.Empty : a;
                }
            }
            else
            {
                slots[from] = b;
                slots[to] = a;
            }
            SlotChanged?.Invoke(from);
            SlotChanged?.Invoke(to);
            Changed?.Invoke();
        }

        /// <summary>
        /// Mueve solo parte de una pila a otra casilla (vacía o con el mismo objeto).
        /// Devuelve cuántas unidades se han movido.
        /// </summary>
        public int MoveAmount(int from, int to, int amount)
        {
            if (!IsValid(from) || !IsValid(to) || from == to || amount <= 0) return 0;
            var a = slots[from];
            var b = slots[to];
            if (a.IsEmpty) return 0;
            if (!b.IsEmpty && b.item != a.item) return 0;
            int room = b.IsEmpty ? a.item.MaxStack : b.SpaceLeft;
            int put = Math.Min(Math.Min(amount, a.amount), room);
            if (put <= 0) return 0;
            slots[to] = new ItemStack(a.item, (b.IsEmpty ? 0 : b.amount) + put);
            a.amount -= put;
            slots[from] = a.amount <= 0 ? ItemStack.Empty : a;
            SlotChanged?.Invoke(from);
            SlotChanged?.Invoke(to);
            Changed?.Invoke();
            return put;
        }

        /// <summary>Divide una pila por la mitad hacia la primera casilla vacía. Devuelve la casilla destino o -1.</summary>
        public int Split(int index)
        {
            if (!IsValid(index) || slots[index].amount < 2) return -1;
            int empty = FirstEmptySlot();
            if (empty < 0) return -1;
            MoveAmount(index, empty, slots[index].amount / 2);
            return empty;
        }

        /// <summary>Junta pilas iguales y ordena por categoría y nombre (casillas vacías al final).</summary>
        public void Sort() => SortRange(0, slots.Length);

        /// <summary>Igual que Sort pero solo entre las casillas [start, start+count).</summary>
        public void SortRange(int start, int count)
        {
            start = Math.Max(0, start);
            int end = Math.Min(slots.Length, start + Math.Max(0, count));
            if (end - start < 1) return;

            var totals = new Dictionary<ItemData, int>();
            var order = new List<ItemData>();
            for (int i = start; i < end; i++)
            {
                var s = slots[i];
                if (s.IsEmpty) continue;
                if (!totals.ContainsKey(s.item)) { totals[s.item] = 0; order.Add(s.item); }
                totals[s.item] += s.amount;
            }
            order.Sort((x, y) =>
            {
                int c = string.CompareOrdinal(x.Category ?? "", y.Category ?? "");
                return c != 0 ? c : string.CompareOrdinal(x.DisplayName ?? "", y.DisplayName ?? "");
            });

            int w = start;
            var overflow = new List<ItemStack>();
            foreach (var item in order)
            {
                int left = totals[item];
                while (left > 0)
                {
                    int put = Math.Min(left, item.MaxStack);
                    if (w < end) slots[w++] = new ItemStack(item, put);
                    else overflow.Add(new ItemStack(item, put));
                    left -= put;
                }
            }
            for (int i = w; i < end; i++) slots[i] = ItemStack.Empty;
            // (No debería haber overflow: juntar pilas nunca necesita más casillas.)
            for (int i = start; i < end; i++) SlotChanged?.Invoke(i);
            Changed?.Invoke();
        }

        /// <summary>Pone directamente el contenido de una casilla (para cargar partidas o herramientas).</summary>
        public void SetSlot(int index, ItemStack stack)
        {
            if (!IsValid(index)) return;
            slots[index] = stack.IsEmpty ? ItemStack.Empty : new ItemStack(stack.item, Math.Min(stack.amount, stack.item.MaxStack));
            SlotChanged?.Invoke(index);
            Changed?.Invoke();
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++) slots[i] = ItemStack.Empty;
            for (int i = 0; i < slots.Length; i++) SlotChanged?.Invoke(i);
            Changed?.Invoke();
        }

        /// <summary>
        /// Cambia el número de casillas (mochilas, mejoras). Si se reduce, lo que
        /// no quepa se devuelve en la lista para que decidas qué hacer (tirarlo al suelo, etc).
        /// </summary>
        public List<ItemStack> Resize(int newCapacity)
        {
            newCapacity = Math.Max(1, newCapacity);
            var overflow = new List<ItemStack>();
            var old = slots;
            slots = new ItemStack[newCapacity];
            for (int i = 0; i < old.Length; i++)
            {
                if (old[i].IsEmpty) continue;
                if (i < newCapacity) slots[i] = old[i];
                else overflow.Add(old[i]);
            }
            // Intenta recolocar lo que sobra en huecos libres.
            for (int k = overflow.Count - 1; k >= 0; k--)
            {
                var s = overflow[k];
                int left = AddSilently(s.item, s.amount);
                if (left <= 0) overflow.RemoveAt(k);
                else overflow[k] = new ItemStack(s.item, left);
            }
            for (int i = 0; i < slots.Length; i++) SlotChanged?.Invoke(i);
            Changed?.Invoke();
            return overflow;
        }

        private int AddSilently(ItemData item, int amount)
        {
            int remaining = amount;
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].item != item) continue;
                int put = Math.Min(remaining, slots[i].SpaceLeft);
                slots[i].amount += put; remaining -= put;
            }
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;
                int put = Math.Min(remaining, item.MaxStack);
                slots[i] = new ItemStack(item, put); remaining -= put;
            }
            return remaining;
        }

        /// <summary>Copia del contenido de todas las casillas.</summary>
        public ItemStack[] Snapshot() => (ItemStack[])slots.Clone();
    }
}
