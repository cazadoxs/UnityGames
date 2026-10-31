using System;

namespace Estandartes.Core
{
    /// <summary>Funciones matemáticas pequeñas usadas por toda la simulación.</summary>
    public static class SimMath
    {
        public const float Sqrt2 = 1.41421356f;

        public static float Clamp01(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);
        public static float Clamp(float x, float min, float max) => x < min ? min : (x > max ? max : x);
        public static int Clamp(int x, int min, int max) => x < min ? min : (x > max ? max : x);
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float InverseLerp(float a, float b, float x) => Math.Abs(b - a) < 1e-9f ? 0f : (x - a) / (b - a);
        public static float Logistic(float x) => 1f / (1f + MathF.Exp(-x));
        public static float Log2(float x) => MathF.Log(x) / MathF.Log(2f);

        /// <summary>Media exponencial móvil: avanza <paramref name="current"/> hacia <paramref name="sample"/>.</summary>
        public static float Ema(float current, float sample, float alpha) => current + (sample - current) * alpha;
    }
}
