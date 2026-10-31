using System;

namespace Estandartes.Core.Content
{
    /// <summary>
    /// Identificador estable de contenido con la forma <c>espacio:tipo/nombre</c>
    /// (por ejemplo <c>estandartes:unit/lancero_castellano</c>). Ver Vol. V §4.3.
    /// </summary>
    public readonly struct ContentId : IEquatable<ContentId>
    {
        public readonly string Namespace;
        public readonly string Type;
        public readonly string Name;

        private ContentId(string ns, string type, string name)
        {
            Namespace = ns;
            Type = type;
            Name = name;
        }

        public string Full => $"{Namespace}:{Type}/{Name}";

        public static bool TryParse(string text, out ContentId id)
        {
            id = default;
            if (string.IsNullOrEmpty(text)) return false;
            int colon = text.IndexOf(':');
            int slash = text.IndexOf('/', colon + 1);
            if (colon <= 0 || slash <= colon + 1 || slash == text.Length - 1) return false;
            string ns = text.Substring(0, colon);
            string type = text.Substring(colon + 1, slash - colon - 1);
            string name = text.Substring(slash + 1);
            if (!IsValidPart(ns) || !IsValidPart(type) || !IsValidPart(name)) return false;
            id = new ContentId(ns, type, name);
            return true;
        }

        public static ContentId Parse(string text)
        {
            if (!TryParse(text, out var id))
                throw new FormatException($"ID de contenido no válido: '{text}' (formato espacio:tipo/nombre en minúsculas)");
            return id;
        }

        /// <summary>Solo minúsculas ASCII, dígitos y guion bajo.</summary>
        public static bool IsValidPart(string part)
        {
            if (string.IsNullOrEmpty(part)) return false;
            foreach (char c in part)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        public bool Equals(ContentId other) => Full == other.Full;
        public override bool Equals(object obj) => obj is ContentId o && Equals(o);
        public override int GetHashCode() => Full.GetHashCode();
        public override string ToString() => Full;
    }
}
