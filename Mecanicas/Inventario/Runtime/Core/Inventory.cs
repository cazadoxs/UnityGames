using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Inventario
{
    [Serializable] public class ItemAmountEvent : UnityEvent<ItemData, int> { }
    [Serializable] public class ItemSlotEvent : UnityEvent<ItemData, int> { }
    [Serializable] public class ItemEvent : UnityEvent<ItemData> { }

    /// <summary>
    /// Inventario de un personaje (o de un cofre). Ponlo en el jugador.
    /// Las primeras "Hotbar Size" casillas forman la barra rápida.
    /// Todo lo que cambia el contenido pasa por aquí y lanza eventos
    /// (C# y UnityEvent) para que la interfaz y tu juego reaccionen.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecánicas/Inventario/Inventario")]
    public class Inventory : MonoBehaviour
    {
        [Header("Tamaño")]
        [Tooltip("Número total de casillas.")]
        [Min(1)] [SerializeField] private int capacity = 24;
        [Tooltip("Cuántas de las primeras casillas forman la barra rápida (0 = sin barra rápida). Máximo 9 para usar las teclas 1-9.")]
        [Min(0)] [SerializeField] private int hotbarSize = 6;
        [Tooltip("Peso máximo que se puede llevar. 0 = sin límite.")]
        [Min(0f)] [SerializeField] private float maxWeight = 0f;

        [Header("Contenido inicial")]
        [SerializeField] private List<ItemStack> startingItems = new List<ItemStack>();

        [Header("Soltar objetos")]
        [Tooltip("Desde dónde se tiran los objetos. Si está vacío se usa la cámara principal.")]
        [SerializeField] private Transform dropOrigin;
        [SerializeField] private float dropDistance = 1.0f;
        [SerializeField] private float dropForce = 2.5f;

        [Header("Guardado")]
        [Tooltip("Necesaria para cargar partidas guardadas (traduce Id -> objeto).")]
        [SerializeField] private ItemDatabase database;

        [Header("Eventos (para conectar desde el Inspector)")]
        public ItemAmountEvent onItemAdded = new ItemAmountEvent();
        public ItemAmountEvent onItemRemoved = new ItemAmountEvent();
        public ItemSlotEvent onItemUsed = new ItemSlotEvent();
        public ItemAmountEvent onItemDropped = new ItemAmountEvent();
        [Tooltip("Se lanza cuando se intenta añadir algo y no cabe (por espacio o peso).")]
        public ItemEvent onInventoryFull = new ItemEvent();
        public UnityEvent onChanged = new UnityEvent();

        // Eventos C# (más cómodos desde código).
        public event Action<ItemData, int> ItemAdded;
        public event Action<ItemData, int> ItemRemoved;
        /// <summary>(objeto, casilla). Se lanza después de un uso válido.</summary>
        public event Action<ItemData, int> ItemUsed;
        public event Action<ItemData, int> ItemDropped;
        public event Action<ItemData> InventoryFull;
        public event Action<int> SlotChanged;
        public event Action Changed;

        private InventoryModel model;

        public InventoryModel Model { get { EnsureModel(); return model; } }
        public int Capacity => Model.Capacity;
        public int HotbarSize => Mathf.Clamp(hotbarSize, 0, Capacity);
        public float MaxWeight => Model.MaxWeight;
        public float TotalWeight => Model.TotalWeight;
        public ItemDatabase Database { get => database; set => database = value; }
        public Transform DropOrigin { get => dropOrigin; set => dropOrigin = value; }

        private void Awake()
        {
            EnsureModel();
            foreach (var s in startingItems)
                if (!s.IsEmpty) Add(s.item, s.amount, false);
        }

        private void EnsureModel()
        {
            if (model != null) return;
            model = new InventoryModel(capacity, maxWeight);
            model.SlotChanged += i => SlotChanged?.Invoke(i);
            model.Changed += () => { Changed?.Invoke(); onChanged.Invoke(); };
        }

        // ---------- Consultas ----------
        public ItemStack GetSlot(int index) => Model.GetSlot(index);
        public int Count(ItemData item) => Model.Count(item);
        public bool Contains(ItemData item, int amount = 1) => Model.Contains(item, amount);
        public bool CanAdd(ItemData item, int amount = 1) => Model.CanAdd(item, amount);
        public int SpaceFor(ItemData item) => Model.SpaceFor(item);

        // ---------- Añadir y quitar ----------
        /// <summary>Añade unidades. Devuelve las que NO han cabido (0 = todo dentro).</summary>
        public int Add(ItemData item, int amount = 1) => Add(item, amount, true);

        private int Add(ItemData item, int amount, bool notifyFull)
        {
            if (item == null || amount <= 0) return 0;
            int left = Model.Add(item, amount);
            int added = amount - left;
            if (added > 0) { ItemAdded?.Invoke(item, added); onItemAdded.Invoke(item, added); }
            if (left > 0 && notifyFull) { InventoryFull?.Invoke(item); onInventoryFull.Invoke(item); }
            return left;
        }

        /// <summary>Añade todo o nada. Devuelve true si se ha añadido.</summary>
        public bool TryAddAll(ItemData item, int amount = 1)
        {
            if (!CanAdd(item, amount)) { InventoryFull?.Invoke(item); onInventoryFull.Invoke(item); return false; }
            Add(item, amount);
            return true;
        }

        /// <summary>Quita unidades. Devuelve cuántas se han quitado.</summary>
        public int Remove(ItemData item, int amount = 1)
        {
            int removed = Model.Remove(item, amount);
            if (removed > 0) { ItemRemoved?.Invoke(item, removed); onItemRemoved.Invoke(item, removed); }
            return removed;
        }

        /// <summary>Quita solo si hay suficientes (útil para llaves, recetas, pagos).</summary>
        public bool TryConsume(ItemData item, int amount = 1)
        {
            if (!Contains(item, amount)) return false;
            Remove(item, amount);
            return true;
        }

        public ItemStack RemoveFromSlot(int index, int amount)
        {
            var taken = Model.RemoveFromSlot(index, amount);
            if (!taken.IsEmpty) { ItemRemoved?.Invoke(taken.item, taken.amount); onItemRemoved.Invoke(taken.item, taken.amount); }
            return taken;
        }

        // ---------- Organizar ----------
        public void MoveOrSwap(int from, int to) => Model.MoveOrSwap(from, to);
        public int MoveAmount(int from, int to, int amount) => Model.MoveAmount(from, to, amount);
        public int Split(int index) => Model.Split(index);
        /// <summary>Ordena la mochila sin tocar la barra rápida.</summary>
        public void SortBackpack() => Model.SortRange(HotbarSize, Capacity - HotbarSize);
        public void SortAll() => Model.Sort();
        public void Clear() => Model.Clear();

        /// <summary>Cambia el número de casillas; lo que no quepa se tira al suelo.</summary>
        public void SetCapacity(int newCapacity)
        {
            capacity = Mathf.Max(1, newCapacity);
            foreach (var s in Model.Resize(capacity)) SpawnDropped(s.item, s.amount);
        }

        /// <summary>Mueve una casilla a otro inventario (cofre). Devuelve lo que se ha movido.</summary>
        public int TransferTo(Inventory other, int index, int amount = int.MaxValue)
        {
            if (other == null || other == this) return 0;
            var s = GetSlot(index);
            if (s.IsEmpty) return 0;
            int want = Mathf.Min(amount, s.amount);
            int fit = Mathf.Min(want, other.SpaceFor(s.item));
            if (fit <= 0) { other.InventoryFull?.Invoke(s.item); other.onInventoryFull.Invoke(s.item); return 0; }
            RemoveFromSlot(index, fit);
            other.Add(s.item, fit);
            return fit;
        }

        // ---------- Usar y soltar ----------
        public bool UseItem(int index)
        {
            var s = GetSlot(index);
            if (s.IsEmpty || !s.item.CanUse(this)) return false;
            var item = s.item;
            if (!item.Use(this, index)) return false;
            if (item.ConsumeOnUse)
            {
                // Puede que Use() haya movido cosas: se gasta de esa casilla si sigue ahí, si no de cualquiera.
                if (GetSlot(index).item == item) RemoveFromSlot(index, 1);
                else Remove(item, 1);
            }
            ItemUsed?.Invoke(item, index);
            onItemUsed.Invoke(item, index);
            return true;
        }

        /// <summary>Tira unidades de una casilla al mundo delante del jugador.</summary>
        public bool DropFromSlot(int index, int amount = 1)
        {
            var s = GetSlot(index);
            if (s.IsEmpty || !s.item.CanDrop) return false;
            var taken = RemoveFromSlot(index, amount);
            if (taken.IsEmpty) return false;
            SpawnDropped(taken.item, taken.amount);
            ItemDropped?.Invoke(taken.item, taken.amount);
            onItemDropped.Invoke(taken.item, taken.amount);
            return true;
        }

        private void SpawnDropped(ItemData item, int amount)
        {
            var origin = dropOrigin != null ? dropOrigin : (Camera.main != null ? Camera.main.transform : transform);
            Vector3 pos = origin.position + origin.forward * dropDistance;
            // Evita meter el objeto dentro de una pared.
            if (Physics.Raycast(origin.position, origin.forward, out var hit, dropDistance, ~0, QueryTriggerInteraction.Ignore))
                pos = hit.point - origin.forward * 0.25f;
            WorldItem.Spawn(item, amount, pos, Quaternion.LookRotation(origin.forward), origin.forward * dropForce + Vector3.up * 1f);
        }

        // ---------- Guardar y cargar ----------
        public string ToJson()
        {
            var data = new InventorySaveData { capacity = Capacity };
            for (int i = 0; i < Capacity; i++)
            {
                var s = GetSlot(i);
                if (s.IsEmpty) continue;
                data.slots.Add(new InventorySaveData.Slot { index = i, id = s.item.Id, amount = s.amount });
            }
            return JsonUtility.ToJson(data);
        }

        /// <summary>Carga desde JSON. Necesita la base de datos asignada. Devuelve false si falla.</summary>
        public bool LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            if (database == null) { Debug.LogError("[Inventario] Falta asignar la base de datos de objetos para cargar.", this); return false; }
            InventorySaveData data;
            try { data = JsonUtility.FromJson<InventorySaveData>(json); }
            catch (Exception e) { Debug.LogError("[Inventario] JSON no válido: " + e.Message, this); return false; }
            if (data == null) return false;
            if (data.capacity > 0 && data.capacity != Capacity) { capacity = data.capacity; Model.Resize(capacity); }
            Model.Clear();
            foreach (var s in data.slots)
            {
                var item = database.Find(s.id);
                if (item == null) { Debug.LogWarning($"[Inventario] El objeto '{s.id}' no está en la base de datos; se ignora.", this); continue; }
                Model.SetSlot(s.index, new ItemStack(item, s.amount));
            }
            return true;
        }

        public void SaveToPlayerPrefs(string key = "inventario")
        {
            PlayerPrefs.SetString(key, ToJson());
            PlayerPrefs.Save();
        }

        public bool LoadFromPlayerPrefs(string key = "inventario") =>
            PlayerPrefs.HasKey(key) && LoadFromJson(PlayerPrefs.GetString(key));

        private void OnValidate()
        {
            if (hotbarSize > capacity) hotbarSize = capacity;
        }
    }

    /// <summary>Formato del guardado (JSON).</summary>
    [Serializable]
    public class InventorySaveData
    {
        [Serializable]
        public class Slot { public int index; public string id; public int amount; }
        public int capacity;
        public List<Slot> slots = new List<Slot>();
    }
}
