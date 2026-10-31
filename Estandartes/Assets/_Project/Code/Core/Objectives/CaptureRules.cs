using System;

namespace Estandartes.Core.Objectives
{
    /// <summary>Pesos de presencia para capturar (Vol. I §5.2).</summary>
    public struct PresenceCount
    {
        public int Infantry;
        public int Cavalry;
        public int SiegeCrew;
        public int Heroes;

        public float Weighted => Infantry * 1f + Cavalry * 0.5f + SiegeCrew * 0f + Heroes * 3f;
    }

    /// <summary>Reglas de captura de un punto (Vol. I §5.2). Funciones puras, llamables desde Burst.</summary>
    public static class CaptureRules
    {
        public const float DisputeRatio = 2f;
        public const float DisputeSpeedFactor = 1f / 3f;
        public const float RevertSpeedFactor = 0.25f;
        public const float MinSpeedFactor = 0.25f;
        public const float MaxSpeedFactor = 3f;

        /// <summary>Factor de velocidad por presencia: min(1 + log2(p / umbral), 3), con mínimo 0,25.</summary>
        public static float SpeedFactor(float presence, float threshold)
        {
            if (presence <= 0f) return 0f;
            float f = 1f + SimMath.Log2(presence / Math.Max(threshold, 0.001f));
            return SimMath.Clamp(f, MinSpeedFactor, MaxSpeedFactor);
        }

        /// <summary>
        /// Avanza el estado de captura un paso. <paramref name="presence0"/>/<paramref name="presence1"/>: presencia
        /// ponderada de cada bando dentro del área. <paramref name="unlocked0"/>/<paramref name="unlocked1"/>: si el punto
        /// está desbloqueado para cada bando. Devuelve true si cambia de dueño en este paso.
        /// </summary>
        public static bool Step(ref ObjectiveState s, ObjectiveNode node, float dt,
                                float presence0, float presence1, bool unlocked0, bool unlocked1, float threshold = 10f)
        {
            if (s.Locked) return false;
            if (node.PointOfNoReturn && s.Owner >= 0 && s.Owner != node.InitialOwner) return false;

            float baseRate = 1f / Math.Max(node.CaptureTime, 1f);
            float p0 = unlocked0 ? presence0 : 0f;
            float p1 = unlocked1 ? presence1 : 0f;

            int attacker = -1;
            float rate = 0f;
            if (p0 > 0f && p1 <= 0f) { attacker = 0; rate = baseRate * SpeedFactor(p0, threshold); }
            else if (p1 > 0f && p0 <= 0f) { attacker = 1; rate = baseRate * SpeedFactor(p1, threshold); }
            else if (p0 > 0f && p1 > 0f)
            {
                // Disputado: solo avanza quien supera 2:1, a un tercio de la velocidad
                if (p0 >= DisputeRatio * p1) { attacker = 0; rate = baseRate * SpeedFactor(p0 - p1, threshold) * DisputeSpeedFactor; }
                else if (p1 >= DisputeRatio * p0) { attacker = 1; rate = baseRate * SpeedFactor(p1 - p0, threshold) * DisputeSpeedFactor; }
                else return false; // congelado
            }

            if (attacker < 0)
            {
                // Vacío: el progreso parcial vuelve poco a poco
                if (s.Capturer >= 0 && s.Progress > 0f)
                {
                    s.Progress = Math.Max(0f, s.Progress - baseRate * RevertSpeedFactor * dt);
                    if (s.Progress <= 0f) s.Capturer = -1;
                }
                return false;
            }

            if (attacker == s.Owner)
            {
                // El dueño "limpia" la captura enemiga en curso
                if (s.Capturer >= 0 && s.Capturer != attacker)
                {
                    s.Progress = Math.Max(0f, s.Progress - rate * dt);
                    if (s.Progress <= 0f) s.Capturer = -1;
                }
                return false;
            }

            if (s.Capturer != attacker)
            {
                // Primero hay que deshacer la captura del otro bando
                if (s.Capturer >= 0 && s.Progress > 0f)
                {
                    s.Progress -= rate * dt;
                    if (s.Progress > 0f) return false;
                    s.Progress = 0f;
                }
                s.Capturer = attacker;
            }

            s.Progress += rate * dt;
            if (s.Progress >= 1f)
            {
                s.Owner = attacker;
                s.Capturer = -1;
                s.Progress = 0f;
                s.Completed = true;
                return true;
            }
            return false;
        }
    }
}
