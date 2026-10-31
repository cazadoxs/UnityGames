using System;

namespace Estandartes.Core.Combat
{
    /// <summary>Error de tiro de la IA (Vol. III §10.4).</summary>
    public static class AimModel
    {
        /// <summary>σ = σ0(destreza)·(1+0,8·fatiga)·(1+0,5·miedo)·(1+k_v·|v|)·clima, en radianes.</summary>
        public static float AngularSigma(float skill, float fatigue, float fear, float targetSpeed, float weatherFactor, float kv = 0.08f)
        {
            float sigma0 = SimMath.Lerp(0.045f, 0.012f, SimMath.Clamp01(skill));
            return sigma0 * (1f + 0.8f * SimMath.Clamp01(fatigue)) * (1f + 0.5f * SimMath.Clamp01(fear)) * (1f + kv * Math.Abs(targetSpeed)) * Math.Max(weatherFactor, 1f);
        }

        /// <summary>Corrección tras fallar al mismo blanco: bias ← bias − η·desvío.</summary>
        public static Vec2 CorrectBias(Vec2 bias, Vec2 observedMiss, float learningRate = 0.5f) => bias - observedMiss * learningRate;

        /// <summary>Instante de suelta de cada tirador en una andanada: t + U(0; 0,6 s).</summary>
        public static float VolleyReleaseTime(ref SimRandom rng, float volleyTime) => volleyTime + rng.Range(0f, 0.6f);
    }
}
