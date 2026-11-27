using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Lista de todos los objetos que existen en el juego. Hace falta para cargar
    /// partidas guardadas (el guardado solo almacena el Id de cada objeto).
    /// Crear con clic derecho > Create > Mecánicas > Inventario > Base de datos de objetos.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecánicas/Inventario/Base de datos de objetos", fileName = "BaseDeDatosObjetos", order = 1)]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        private Dictionary<string, ItemData> lookup;

        public IReadOnlyList<ItemData> Items => items;

        public ItemData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (lookup == null || lookup.Count != items.Count) Rebuild();
            lookup.TryGetValue(id, out var item);
            return item;
        }

        public void Add(ItemData item)
        {
            if (item != null && !items.Contains(item)) { items.Add(item); lookup = null; }
        }

        private void Rebuild()
        {
            lookup = new Dictionary<string, ItemData>();
            foreach (var item in items)
            {
                if (item == null) continue;
                if (lookup.ContainsKey(item.Id))
                    Debug.LogWarning($"[Inventario] Id repetido en la base de datos: '{item.Id}'.", this);
                else
                    lookup.Add(item.Id, item);
            }
        }

        private void OnEnable() => lookup = null;
    }
}
