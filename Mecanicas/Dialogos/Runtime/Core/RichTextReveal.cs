using System.Collections.Generic;
using System.Text;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Ayuda para el efecto máquina de escribir respetando las etiquetas de rich text
    /// (&lt;b&gt;, &lt;i&gt;, &lt;color&gt;, &lt;size&gt;): las etiquetas no cuentan como letras y nunca se cortan.
    /// El resto del texto se añade invisible para que la caja no cambie de tamaño mientras se escribe.
    /// </summary>
    public static class RichTextReveal
    {
        public static int CountVisible(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '<' && TryTag(s, i, out int end, out _, out _)) { i = end; continue; }
                count++;
            }
            return count;
        }

        public static string Reveal(string s, int visible, bool keepHiddenSpace = true)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 32);
            var open = new List<string>();
            int shown = 0, i = 0;
            for (; i < s.Length; i++)
            {
                if (s[i] == '<' && TryTag(s, i, out int end, out string name, out bool closing))
                {
                    if (shown >= visible) break;
                    sb.Append(s, i, end - i + 1);
                    if (closing) { int k = open.LastIndexOf(name); if (k >= 0) open.RemoveAt(k); }
                    else open.Add(name);
                    i = end;
                    continue;
                }
                if (shown >= visible) break;
                sb.Append(s[i]);
                shown++;
            }
            if (i >= s.Length) return sb.ToString();

            for (int k = open.Count - 1; k >= 0; k--) sb.Append("</").Append(open[k]).Append('>');
            if (keepHiddenSpace)
            {
                string rest = Strip(s.Substring(i));
                if (rest.Length > 0) sb.Append("<color=#00000000>").Append(rest).Append("</color>");
            }
            return sb.ToString();
        }

        public static string Strip(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '<' && TryTag(s, i, out int end, out _, out _)) { i = end; continue; }
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        static readonly HashSet<string> Known = new HashSet<string> { "b", "i", "color", "size" };

        static bool TryTag(string s, int start, out int end, out string name, out bool closing)
        {
            end = s.IndexOf('>', start + 1);
            name = null;
            closing = false;
            if (end < 0) return false;
            string inner = s.Substring(start + 1, end - start - 1);
            if (inner.StartsWith("/")) { closing = true; inner = inner.Substring(1); }
            int cut = inner.IndexOfAny(new[] { '=', ' ' });
            name = (cut >= 0 ? inner.Substring(0, cut) : inner).Trim().ToLowerInvariant();
            return Known.Contains(name);
        }
    }
}
