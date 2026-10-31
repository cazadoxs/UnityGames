using System;

namespace Estandartes.Core.Utility
{
    public enum CurveType { Linear, Polynomial, Logistic, InverseLogistic, SmoothStep, Constant }

    /// <summary>Curva de respuesta de una consideración de utility AI (Vol. III §8.2).</summary>
    [Serializable]
    public struct ResponseCurve
    {
        public CurveType Type;
        public float Slope;      // pendiente (lineal) o k (logística)
        public float Exponent;   // polinómica
        public float X0;         // centro (logística, en la entrada normalizada)
        public float InputMin;
        public float InputMax;
        public bool Invert;

        public static ResponseCurve Linear(float min = 0f, float max = 1f, bool invert = false) =>
            new ResponseCurve { Type = CurveType.Linear, Slope = 1f, InputMin = min, InputMax = max, Invert = invert };

        public static ResponseCurve Logistic(float x0Raw, float k, float min, float max, bool invert = false) =>
            new ResponseCurve { Type = CurveType.Logistic, Slope = k, X0 = (x0Raw - min) / (max - min), InputMin = min, InputMax = max, Invert = invert };

        public static ResponseCurve Polynomial(float exponent, float min = 0f, float max = 1f, bool invert = false) =>
            new ResponseCurve { Type = CurveType.Polynomial, Exponent = exponent, InputMin = min, InputMax = max, Invert = invert };

        /// <summary>Evalúa la curva: entrada en unidades reales, salida en [0, 1].</summary>
        public float Evaluate(float raw)
        {
            float x = SimMath.Clamp01(SimMath.InverseLerp(InputMin, InputMax, raw));
            float y;
            switch (Type)
            {
                case CurveType.Linear: y = SimMath.Clamp01(Slope * x); break;
                case CurveType.Polynomial: y = MathF.Pow(x, Exponent <= 0 ? 1f : Exponent); break;
                case CurveType.Logistic: y = SimMath.Logistic((Slope == 0 ? 10f : Slope) * (x - X0)); break;
                case CurveType.InverseLogistic: y = 1f - SimMath.Logistic((Slope == 0 ? 10f : Slope) * (x - X0)); break;
                case CurveType.SmoothStep: y = x * x * (3f - 2f * x); break;
                default: y = 1f; break;
            }
            return Invert ? 1f - y : y;
        }

        /// <summary>Tabla de 64 muestras para evaluación rápida en Burst.</summary>
        public float[] Bake(int samples = 64)
        {
            var lut = new float[samples];
            for (int i = 0; i < samples; i++)
                lut[i] = Evaluate(SimMath.Lerp(InputMin, InputMax, i / (float)(samples - 1)));
            return lut;
        }

        /// <summary>Evalúa una tabla horneada con interpolación lineal.</summary>
        public static float EvaluateBaked(ReadOnlySpan<float> lut, float min, float max, float raw)
        {
            float x = SimMath.Clamp01(SimMath.InverseLerp(min, max, raw)) * (lut.Length - 1);
            int i = (int)x;
            if (i >= lut.Length - 1) return lut[lut.Length - 1];
            return SimMath.Lerp(lut[i], lut[i + 1], x - i);
        }
    }
}
