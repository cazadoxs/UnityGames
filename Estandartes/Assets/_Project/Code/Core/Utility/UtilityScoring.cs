using System;

namespace Estandartes.Core.Utility
{
    /// <summary>Puntuación de opciones de utility AI (Vol. III §8.2).</summary>
    public static class UtilityScoring
    {
        public const float ContinuityBonus = 0.15f;

        /// <summary>Media geométrica: una consideración a 0 anula la opción.</summary>
        public static float GeometricMean(ReadOnlySpan<float> considerations)
        {
            if (considerations.Length == 0) return 0f;
            double logSum = 0;
            for (int i = 0; i < considerations.Length; i++)
            {
                float c = considerations[i];
                if (c <= 0f) return 0f;
                logSum += Math.Log(Math.Min(c, 1f));
            }
            return (float)Math.Exp(logSum / considerations.Length);
        }

        public static float Score(float priority, float planWeight, ReadOnlySpan<float> considerations, bool isCurrentTask) =>
            priority * planWeight * GeometricMean(considerations) * (isCurrentTask ? 1f + ContinuityBonus : 1f);
    }
}
