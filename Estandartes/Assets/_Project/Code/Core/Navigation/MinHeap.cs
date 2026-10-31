using System.Collections.Generic;

namespace Estandartes.Core.Navigation
{
    /// <summary>Montículo binario mínimo de (prioridad, valor) sin asignaciones por operación.</summary>
    public sealed class MinHeap
    {
        private readonly List<(float key, int value)> _items = new List<(float, int)>();
        public int Count => _items.Count;
        public void Clear() => _items.Clear();

        public void Push(float key, int value)
        {
            _items.Add((key, value));
            int i = _items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_items[parent].key <= _items[i].key) break;
                (_items[parent], _items[i]) = (_items[i], _items[parent]);
                i = parent;
            }
        }

        public (float key, int value) Pop()
        {
            var top = _items[0];
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, m = i;
                if (l < _items.Count && _items[l].key < _items[m].key) m = l;
                if (r < _items.Count && _items[r].key < _items[m].key) m = r;
                if (m == i) break;
                (_items[m], _items[i]) = (_items[i], _items[m]);
                i = m;
            }
            return top;
        }
    }
}
