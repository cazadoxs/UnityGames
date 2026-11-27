using System;
using System.Globalization;

namespace Imprenta
{
    /// <summary>Notación legible de números: 1.234 → 1,23 K, luego M, B, T y letras (aa, ab...).</summary>
    public static class Fmt
    {
        static readonly CultureInfo Es = new CultureInfo("es-ES");
        static readonly string[] Suffix = { "", " K", " M", " B", " T" };

        public static string Money(double v) => Num(v) + " $";

        public static string Num(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "∞";
            if (v < 0) return "-" + Num(-v);
            if (v < 100) return v.ToString(v < 10 ? "0.00" : "0.0", Es);
            if (v < 1000) return v.ToString("0", Es);
            int tier = (int)Math.Floor(Math.Log10(v) / 3.0);
            double scaled = v / Math.Pow(1000, tier);
            string s = scaled.ToString(scaled < 10 ? "0.00" : scaled < 100 ? "0.0" : "0", Es);
            if (tier < Suffix.Length) return s + Suffix[tier];
            int idx = tier - Suffix.Length; // 0 → aa
            char a = (char)('a' + (idx / 26) % 26);
            char b = (char)('a' + idx % 26);
            return s + " " + a + b;
        }

        public static string Int(long v) => v.ToString("N0", Es);

        public static string Area(double m2) => m2.ToString("N0", Es) + " m²";
    }
}
