using System;

namespace Estandartes.Core.Command
{
    public enum CommandMedium { Voice, Musician, Standard, Messenger }

    public enum OrderType
    {
        Move, Hold, Attack, Charge, Formation, FireAtWill, CeaseFire, Volley, Retreat, FeignedRetreat, Follow, Signal
    }

    /// <summary>Medios de transmisión de órdenes y sus retrasos (Vol. III §12).</summary>
    public static class CommandChain
    {
        public const float VoiceRange = 30f, MusicianRange = 400f, StandardRange = 600f, MessengerSpeed = 10f;

        /// <summary>Órdenes que se pueden codificar con cuerno o tambor.</summary>
        public static bool IsMusicCoded(OrderType t) =>
            t == OrderType.Move || t == OrderType.Charge || t == OrderType.Hold || t == OrderType.Retreat || t == OrderType.Signal;

        public static bool IsSimple(OrderType t) => t != OrderType.Formation && t != OrderType.Volley;

        /// <summary>El medio más rápido disponible para esa orden y distancia.</summary>
        public static CommandMedium Choose(OrderType order, float distance, bool hasMusician, bool lineOfSightToStandard)
        {
            if (distance <= VoiceRange) return CommandMedium.Voice;
            if (hasMusician && distance <= MusicianRange && IsMusicCoded(order)) return CommandMedium.Musician;
            if (lineOfSightToStandard && distance <= StandardRange && IsSimple(order)) return CommandMedium.Standard;
            return CommandMedium.Messenger;
        }

        /// <summary>Retraso en segundos, con variación aleatoria determinista.</summary>
        public static float Delay(CommandMedium medium, float distance, ref SimRandom rng)
        {
            switch (medium)
            {
                case CommandMedium.Voice: return rng.Range(0.3f, 1f);
                case CommandMedium.Musician: return rng.Range(0.5f, 1.5f);
                case CommandMedium.Standard: return rng.Range(1f, 3f);
                default: return distance / MessengerSpeed + 2f + rng.Range(0f, 1f);
            }
        }

        /// <summary>Tick de entrega a 30 Hz.</summary>
        public static uint DeliverTick(uint issueTick, float delaySeconds, float tickRate = 30f) =>
            issueTick + (uint)Math.Ceiling(delaySeconds * tickRate);

        /// <summary>Segundos hasta que se promueve un nuevo capitán: 15 s → 5 s con la experiencia.</summary>
        public static float PromotionDelay(float bestExperience01) => SimMath.Lerp(15f, 5f, SimMath.Clamp01(bestExperience01));
    }
}
