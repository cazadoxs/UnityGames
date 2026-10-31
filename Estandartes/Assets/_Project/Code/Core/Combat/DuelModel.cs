using System;

namespace Estandartes.Core.Combat
{
    public enum Difficulty { Recluta, Veterano, MaestroDeGuerra }

    public enum DefenseChoice { None, Block, Parry, StepBack, Counter }

    /// <summary>Modelo de duelo cuerpo a cuerpo de la IA (Vol. III §10.3), con las mismas reglas que el jugador.</summary>
    public static class DuelModel
    {
        public const float ReactionMinMs = 150f;
        public const float ReactionMaxMs = 700f;
        public const float ParryWindowMs = 200f;

        /// <summary>Error de lectura de dirección por dificultad (Vol. I §8.9).</summary>
        public static float DirectionError(Difficulty d) => d == Difficulty.Recluta ? 0.35f : (d == Difficulty.Veterano ? 0.20f : 0.10f);

        /// <summary>Mediana del tiempo de reacción (ms) por dificultad, antes de destreza, fatiga y miedo.</summary>
        public static float BaseReactionMs(Difficulty d) => d == Difficulty.Recluta ? 450f : (d == Difficulty.Veterano ? 340f : 270f);

        /// <summary>Tiempo de reacción en ms ~ LogNormal, recortado a [150, 700].</summary>
        public static float SampleReactionMs(ref SimRandom rng, Difficulty d, float skill, float fatigue, float fear)
        {
            float median = BaseReactionMs(d) * (1.25f - 0.5f * SimMath.Clamp01(skill)) * (1f + 0.4f * SimMath.Clamp01(fatigue)) * (1f + 0.3f * SimMath.Clamp01(fear));
            float ms = rng.LogNormal(MathF.Log(median), 0.25f);
            return SimMath.Clamp(ms, ReactionMinMs, ReactionMaxMs);
        }

        /// <summary>p_dir = clamp(p_base − 0,3·fatiga − 0,2·miedo, 0,05, 0,98), p_base según destreza y dificultad.</summary>
        public static float BlockDirectionProbability(Difficulty d, float skill, float fatigue, float fear)
        {
            float pBase = (1f - DirectionError(d)) * (0.75f + 0.25f * SimMath.Clamp01(skill));
            return SimMath.Clamp(pBase - 0.3f * SimMath.Clamp01(fatigue) - 0.2f * SimMath.Clamp01(fear), 0.05f, 0.98f);
        }

        /// <summary>Decide la defensa ante un ataque entrante.</summary>
        public static DefenseChoice ChooseDefense(ref SimRandom rng, float reactionMs, float windupRemainingMs,
                                                  float ownAttackMs, float stamina01, float skill, bool hasSpaceBehind)
        {
            if (reactionMs >= windupRemainingMs) return DefenseChoice.None;
            float spare = windupRemainingMs - reactionMs;
            if (ownAttackMs + 50f < spare && rng.Chance(0.3f + 0.4f * skill)) return DefenseChoice.Counter;
            if (spare >= ParryWindowMs && rng.Chance(0.15f + 0.5f * skill)) return DefenseChoice.Parry;
            if (stamina01 < 0.2f && hasSpaceBehind) return DefenseChoice.StepBack;
            return DefenseChoice.Block;
        }

        /// <summary>Probabilidad de atacar en la dirección contraria a la guardia del rival.</summary>
        public static float OffGuardProbability(float skill) => 0.4f + 0.5f * SimMath.Clamp01(skill);

        public static bool ShouldFeint(ref SimRandom rng, float skill) => skill > 0.7f && rng.Chance(0.15f);

        /// <summary>¿Ataca ahora? Solo con apertura o con estamina suficiente.</summary>
        public static bool ShouldAttack(float stamina01, bool opponentRecovering, bool opponentStaggered, bool opponentDisordered, float aggression)
        {
            bool opening = opponentRecovering || opponentStaggered || opponentDisordered;
            if (stamina01 < 0.2f) return opening;
            return opening || stamina01 > 0.5f - 0.3f * aggression;
        }
    }
}
