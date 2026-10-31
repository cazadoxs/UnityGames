using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Contenido de un objeto contenedor (papel de hamburguesa, bandeja, tarrina).
    /// Se añade solo a los objetos cuya definición es contenedor.
    /// </summary>
    [DisallowMultipleComponent]
    public class ItemContainer : MonoBehaviour, IItemHolder
    {
        readonly List<Item> contents = new List<Item>();
        Item owner;

        public IReadOnlyList<Item> Contents => contents;
        public Item Owner => owner;
        ContainerSettings Settings => owner != null ? owner.Definition.container : null;
        public bool IsActive => Settings != null && Settings.isContainer;
        public bool IsFull => IsActive && contents.Count >= Settings.capacity;

        internal void Bind(Item item) => owner = item;

        public bool CanAccept(Item item)
        {
            if (!IsActive || item == null || item == owner || IsFull) return false;
            if (item.IsDirty || item.Definition.isFailedState) return false;
            if (!item.Definition.HasAnyTag(Settings.acceptedTags)) return false;
            // Evita meter un contenedor dentro de algo que él mismo contiene.
            if (item.Container != null && item.Container.Contains(owner)) return false;
            return true;
        }

        public bool Contains(Item item)
        {
            foreach (var c in contents)
                if (c == item || (c.Container != null && c.Container.Contains(item))) return true;
            return false;
        }

        public bool TryAdd(Item item)
        {
            if (!CanAccept(item)) return false;
            contents.Add(item);
            item.AttachTo(owner.transform, this, Vector3.zero, Quaternion.identity);
            Layout();
            return true;
        }

        public void OnItemRemoved(Item item)
        {
            if (contents.Remove(item)) Layout();
        }

        /// <summary>Destruye todo el contenido (tirar a la basura conservando el envase).</summary>
        public void Clear()
        {
            for (int i = contents.Count - 1; i >= 0; i--)
                if (contents[i] != null) contents[i].Consume();
            contents.Clear();
        }

        void Layout()
        {
            var s = Settings;
            if (s == null) return;
            float y = 0f;
            for (int i = 0; i < contents.Count; i++)
            {
                var t = contents[i].transform;
                if (s.layout == ContainerLayout.Grid)
                {
                    int cols = Mathf.Max(1, s.gridColumns);
                    int rows = Mathf.CeilToInt(s.capacity / (float)cols);
                    int col = i % cols, row = i / cols;
                    float x = (col - (cols - 1) * 0.5f) * s.gridSpacing;
                    float z = (row - (rows - 1) * 0.5f) * s.gridSpacing;
                    t.localPosition = s.contentOffset + new Vector3(x, 0f, z);
                }
                else
                {
                    t.localPosition = s.contentOffset + new Vector3(0f, y, 0f);
                    y += contents[i].Definition.stackHeight;
                }
                t.localRotation = Quaternion.identity;
            }
        }
    }
}
