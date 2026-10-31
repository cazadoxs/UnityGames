using System;
using System.Collections.Generic;

namespace Estandartes.Core.Content
{
    /// <summary>
    /// Catálogo de todo el contenido cargado (juego base y mods), indexado por ID estable (Vol. V §4.3).
    /// Ningún sistema debe referenciar contenido concreto si no es a través de este registro.
    /// </summary>
    public sealed class ContentRegistry
    {
        public sealed class Entry
        {
            public Definition Definition;
            public string SourceMod;
            public readonly List<string> ModifiedBy = new List<string>();
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public int Count => _entries.Count;

        /// <summary>Añade o reemplaza (el último en cargarse gana). Devuelve true si reemplazó algo.</summary>
        public bool Register(Definition def, string sourceMod)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            var id = ContentId.Parse(def.Id);
            if (id.Type != def.Kind)
                throw new ArgumentException($"El ID '{def.Id}' es de tipo '{id.Type}' pero la definición es '{def.Kind}'");
            bool replaced = _entries.TryGetValue(def.Id, out var existing);
            var entry = new Entry { Definition = def, SourceMod = replaced ? existing.SourceMod : sourceMod };
            if (replaced)
            {
                entry.ModifiedBy.AddRange(existing.ModifiedBy);
                entry.ModifiedBy.Add(sourceMod);
            }
            _entries[def.Id] = entry;
            return replaced;
        }

        public bool Contains(string id) => id != null && _entries.ContainsKey(id);

        public bool TryGet<T>(string id, out T def) where T : Definition
        {
            def = null;
            if (id == null || !_entries.TryGetValue(id, out var e)) return false;
            def = e.Definition as T;
            return def != null;
        }

        public T Get<T>(string id) where T : Definition
        {
            if (!TryGet<T>(id, out var def)) throw new KeyNotFoundException($"Contenido no encontrado: {id} ({typeof(T).Name})");
            return def;
        }

        public Entry GetEntry(string id) => _entries.TryGetValue(id, out var e) ? e : null;

        public IEnumerable<T> All<T>() where T : Definition
        {
            foreach (var e in _entries.Values)
                if (e.Definition is T t) yield return t;
        }

        public IEnumerable<Entry> Entries => _entries.Values;
    }
}
