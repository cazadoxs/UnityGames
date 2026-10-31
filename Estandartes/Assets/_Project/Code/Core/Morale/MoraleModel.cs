using System;

namespace Estandartes.Core.Morale
{
    public enum MoraleState { Steady, Wavering, Retreating, Routing, Surrendered }

    /// <summary>Fuentes continuas de moral activas para un soldado (Vol. III §11.1).</summary>
    public struct MoraleSources
    {
        public bool MusicianPlaying;
        public bool StandardNearby;      // estandarte visible a menos de 30 m
        public bool HeroFightingNearby;  // héroe a menos de 15 m
        public float Fatigue01;
        public bool SustainedFlankThreat;
    }

    /// <summary>Moral individual, contagio, estados y moral de escuadra. Funciones puras.</summary>
    public static class MoraleModel
    {
        public const float SteadyMin = 60f, WaveringMin = 40f, RetreatMin = 25f;
        public const float ContagionDown = 0.6f, ContagionUp = 0.2f;
        public const float RallyThreshold = 45f, RallyRate = 2f;

        /// <summary>Golpe puntual. Los negativos se atenúan por el valor: ×(1 − 0,5·valor).</summary>
        public static float ApplyEvent(float morale, float magnitude, float courage01)
        {
            float delta = magnitude < 0f ? magnitude * (1f - 0.5f * SimMath.Clamp01(courage01)) : magnitude;
            return SimMath.Clamp(morale + delta, 0f, 100f);
        }

        /// <summary>Tasa continua (puntos por segundo).</summary>
        public static float ContinuousRate(in MoraleSources s)
        {
            float r = 0f;
            if (s.MusicianPlaying) r += 0.5f;
            if (s.StandardNearby) r += 0.3f;
            if (s.HeroFightingNearby) r += 0.3f;
            r -= 0.4f * SimMath.Clamp01(s.Fatigue01);
            if (s.SustainedFlankThreat) r -= 0.5f;
            return r;
        }

        /// <summary>Contagio asimétrico con la media de los vecinos (el miedo se contagia 3 veces más rápido).</summary>
        public static float Contagion(float morale, float neighborsMean, float discipline01, float dt)
        {
            float k = neighborsMean < morale ? ContagionDown : ContagionUp;
            return morale + dt * k * (neighborsMean - morale) * (1f - 0.5f * SimMath.Clamp01(discipline01));
        }

        public static float Step(float morale, in MoraleSources s, float neighborsMean, bool hasNeighbors, float discipline01, float dt)
        {
            float m = morale + ContinuousRate(s) * dt;
            if (hasNeighbors) m = Contagion(m, neighborsMean, discipline01, dt);
            return SimMath.Clamp(m, 0f, 100f);
        }

        public static MoraleState StateFor(float morale, bool surrounded)
        {
            if (morale >= SteadyMin) return MoraleState.Steady;
            if (morale >= WaveringMin) return MoraleState.Wavering;
            if (morale >= RetreatMin) return MoraleState.Retreating;
            return surrounded ? MoraleState.Surrendered : MoraleState.Routing;
        }

        /// <summary>Moral de escuadra = media × 1,1 si vive el capitán × 1,1 si el estandarte está en pie.</summary>
        public static float SquadMorale(float average, bool captainAlive, bool standardUp)
        {
            float m = average * (captainAlive ? 1.1f : 1f) * (standardUp ? 1.1f : 1f);
            return Math.Min(100f, m);
        }

        /// <summary>Recuperación de una escuadra en huida si nadie la persigue y hay mando cerca.</summary>
        public static float Rally(float morale, bool pursued, bool leaderNearby, float dt, out bool rallied)
        {
            rallied = false;
            if (pursued || !leaderNearby) return morale;
            morale = Math.Min(100f, morale + RallyRate * dt);
            rallied = morale >= RallyThreshold;
            return morale;
        }
    }
}
