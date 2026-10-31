using System;
using System.Collections.Generic;
using Estandartes.Core.Content;

namespace Estandartes.Core.Strategy
{
    /// <summary>Plan candidato listo para simular: órdenes para los grupos propios y etiquetas para la personalidad.</summary>
    public sealed class CandidatePlan
    {
        public string Id;
        public readonly List<string> Tags = new List<string>();
        /// <summary>Orden por índice de grupo propio en la batalla abstracta.</summary>
        public readonly Dictionary<int, GroupOrder> Orders = new Dictionary<int, GroupOrder>();
        public float DoctrineBias;
    }

    public struct PlanScore
    {
        public string PlanId;
        public float Mean;
        public float StdDev;
        public float Value;       // U(p) = media − λ·desviación
        public float Probability;
    }

    public struct UtilityWeights
    {
        public float Objective, EnemyLosses, OwnLosses, Time;
        public static UtilityWeights Default => new UtilityWeights { Objective = 1.0f, EnemyLosses = 1.0f, OwnLosses = 1.0f, Time = 0.05f };
    }

    /// <summary>
    /// Evaluación de planes por simulación Monte Carlo y elección softmax con sesgos de personalidad (Vol. III §6.5).
    /// </summary>
    public static class PlanEvaluator
    {
        public static float Utility(in BattleOutcome o, in UtilityWeights w, float casualtyAversion, float horizon) =>
            w.Objective * o.ObjectiveDelta + w.EnemyLosses * o.EnemyLosses
          - w.OwnLosses * casualtyAversion * o.OwnLosses - w.Time * (o.Time / Math.Max(horizon, 1f));

        public static PlanScore Evaluate(AbstractBattle baseline, int team, CandidatePlan plan, PersonalityDefinition personality,
                                         int rollouts, float horizon, float dt, uint seed, UtilityWeights weights, float positionNoise = 15f)
        {
            var values = new float[rollouts];
            for (int r = 0; r < rollouts; r++)
            {
                var rng = SimRandom.For(r, seed, 0xA11CEu);
                var sim = baseline.Clone();
                for (int i = 0; i < sim.Groups.Count; i++)
                {
                    if (plan.Orders.TryGetValue(i, out var order) && sim.Groups[i].Team == team) sim.Orders[i] = order;
                    if (sim.Groups[i].Team != team && positionNoise > 0)
                    {
                        // Creencias: la posición enemiga se muestrea dentro de su incertidumbre
                        var g = sim.Groups[i];
                        g.Position += rng.InsideCircle(positionNoise);
                        sim.Groups[i] = g;
                    }
                }
                sim.Run(horizon, dt, ref rng);
                values[r] = Utility(sim.Outcome(team), weights, personality.CasualtyAversion, horizon);
            }
            float mean = 0f;
            foreach (var v in values) mean += v;
            mean /= Math.Max(1, rollouts);
            float var2 = 0f;
            foreach (var v in values) var2 += (v - mean) * (v - mean);
            float std = MathF.Sqrt(var2 / Math.Max(1, rollouts));
            return new PlanScore { PlanId = plan.Id, Mean = mean, StdDev = std, Value = mean - personality.RiskAversion * std };
        }

        /// <summary>Sesgo de personalidad por etiquetas del plan ("deception", "attack") y doctrina.</summary>
        public static float PersonalityBias(CandidatePlan plan, PersonalityDefinition p)
        {
            float b = plan.DoctrineBias * p.DoctrineBias;
            if (plan.Tags.Contains("deception")) b += p.DeceptionBias;
            if (plan.Tags.Contains("attack")) b += p.AttackBias;
            return b;
        }

        /// <summary>P(p) = exp((U + sesgo)/T) / Σ exp(...). Rellena Probability y devuelve el índice elegido.</summary>
        public static int SoftmaxSelect(PlanScore[] scores, IReadOnlyList<CandidatePlan> plans, PersonalityDefinition personality,
                                        float temperature, ref SimRandom rng)
        {
            if (scores.Length == 0) return -1;
            float t = Math.Max(temperature, 1e-3f);
            double maxLogit = double.MinValue;
            var logits = new double[scores.Length];
            for (int i = 0; i < scores.Length; i++)
            {
                logits[i] = (scores[i].Value + PersonalityBias(plans[i], personality)) / t;
                maxLogit = Math.Max(maxLogit, logits[i]);
            }
            double sum = 0;
            for (int i = 0; i < scores.Length; i++) { logits[i] = Math.Exp(logits[i] - maxLogit); sum += logits[i]; }
            for (int i = 0; i < scores.Length; i++) scores[i].Probability = (float)(logits[i] / sum);
            float roll = rng.NextFloat();
            float acc = 0f;
            for (int i = 0; i < scores.Length; i++)
            {
                acc += scores[i].Probability;
                if (roll < acc) return i;
            }
            return scores.Length - 1;
        }

        /// <summary>Compromiso: solo se cambia de plan si el nuevo supera al actual en δ (paciencia).</summary>
        public static bool ShouldSwitch(float currentValue, float candidateValue, float patience) => candidateValue > currentValue + patience;
    }
}
