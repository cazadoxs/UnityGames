using System;
using System.Collections.Generic;

namespace Estandartes.Core.Mods
{
    /// <summary>Versión semántica MAYOR.MENOR.PARCHE (sin etiquetas de pre-lanzamiento).</summary>
    public readonly struct SemVer : IComparable<SemVer>, IEquatable<SemVer>
    {
        public readonly int Major, Minor, Patch;

        public SemVer(int major, int minor, int patch) { Major = major; Minor = minor; Patch = patch; }

        public static bool TryParse(string s, out SemVer v)
        {
            v = default;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Trim().Split('.');
            if (parts.Length < 1 || parts.Length > 3) return false;
            int[] n = new int[3];
            for (int i = 0; i < parts.Length; i++)
                if (!int.TryParse(parts[i], out n[i]) || n[i] < 0) return false;
            v = new SemVer(n[0], n[1], n[2]);
            return true;
        }

        public static SemVer Parse(string s) => TryParse(s, out var v) ? v : throw new FormatException($"Versión no válida: '{s}'");

        public int CompareTo(SemVer o)
        {
            if (Major != o.Major) return Major.CompareTo(o.Major);
            if (Minor != o.Minor) return Minor.CompareTo(o.Minor);
            return Patch.CompareTo(o.Patch);
        }

        public bool Equals(SemVer o) => CompareTo(o) == 0;
        public override bool Equals(object obj) => obj is SemVer o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);
        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }

    /// <summary>
    /// Rango de versiones: comparadores separados por espacios que deben cumplirse todos
    /// (<c>&gt;=1.1.0 &lt;2.0.0</c>), además de <c>^1.2.0</c>, <c>~1.2.0</c>, <c>=1.0.0</c>, <c>1.0.0</c> o <c>*</c>.
    /// </summary>
    public sealed class VersionRange
    {
        private readonly List<(string op, SemVer v)> _comparators = new List<(string, SemVer)>();
        public string Text { get; }

        private VersionRange(string text) { Text = text; }

        public static bool TryParse(string text, out VersionRange range, out string error)
        {
            range = new VersionRange(text ?? "*");
            error = null;
            if (string.IsNullOrWhiteSpace(text) || text.Trim() == "*") return true;
            foreach (var raw in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string op = "=";
                string ver = raw;
                foreach (var candidate in new[] { ">=", "<=", ">", "<", "=", "^", "~" })
                {
                    if (raw.StartsWith(candidate, StringComparison.Ordinal)) { op = candidate; ver = raw.Substring(candidate.Length); break; }
                }
                if (!SemVer.TryParse(ver, out var v)) { error = $"Comparador no válido '{raw}'"; return false; }
                if (op == "^")
                {
                    range._comparators.Add((">=", v));
                    range._comparators.Add(("<", v.Major > 0 ? new SemVer(v.Major + 1, 0, 0) : new SemVer(0, v.Minor + 1, 0)));
                }
                else if (op == "~")
                {
                    range._comparators.Add((">=", v));
                    range._comparators.Add(("<", new SemVer(v.Major, v.Minor + 1, 0)));
                }
                else range._comparators.Add((op, v));
            }
            return true;
        }

        public static VersionRange Parse(string text) =>
            TryParse(text, out var r, out var e) ? r : throw new FormatException(e);

        public bool Satisfies(SemVer v)
        {
            foreach (var (op, c) in _comparators)
            {
                int cmp = v.CompareTo(c);
                bool ok = op switch
                {
                    ">=" => cmp >= 0,
                    "<=" => cmp <= 0,
                    ">" => cmp > 0,
                    "<" => cmp < 0,
                    _ => cmp == 0,
                };
                if (!ok) return false;
            }
            return true;
        }

        public override string ToString() => Text;
    }
}
