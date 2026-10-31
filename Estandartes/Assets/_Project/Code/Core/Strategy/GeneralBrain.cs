using System;
using System.Collections.Generic;
using Estandartes.Core.Content;

namespace Estandartes.Core.Strategy
{
    /// <summary>Rol táctico de un grupo para traducir tareas primitivas a órdenes.</summary>
    public enum GroupRole { Infantry, Ranged, Cavalry, LightCavalry, Elite, Artillery }

    /// <summary>
    /// Cerebro del general (Vol. III §6): hechos → planes HTN → traducción a órdenes → Monte Carlo → softmax con
    /// personalidad → compromiso. Vive en el hilo estratégico; trabaja sobre una copia abstracta de la batalla.
    /// </summary>
    public sealed class GeneralBrain
    {
        public readonly int Team;
        public readonly HtnDomainDefinition Domain;
        public readonly PersonalityDefinition Personality;
        public int Rollouts = 32;
        public float Horizon = 240f;
        public float Dt = 2f;
        public float TemperatureScale = 1f;

        public string CurrentPlanId { get; private set; }
        public float CurrentValue { get; private set; } = float.MinValue;
        public PlanScore[] LastScores { get; private set; } = Array.Empty<PlanScore>();

        public GeneralBrain(int team, HtnDomainDefinition domain, PersonalityDefinition personality)
        {
            Team = team; Domain = domain; Personality = personality;
        }

        /// <summary>Decide. Devuelve el plan elegido (o el actual si no merece la pena cambiar).</summary>
        public CandidatePlan Decide(AbstractBattle snapshot, IReadOnlyList<GroupRole> roles, IReadOnlyDictionary<string, float> facts,
                                    Vec2 enemyCenter, Vec2 weakFlank, Vec2 objective, uint seed)
        {
            var htnPlans = HtnPlanner.Plan(Domain, facts);
            var candidates = new List<CandidatePlan>();
            foreach (var hp in htnPlans) candidates.Add(Translate(hp, snapshot, roles, enemyCenter, weakFlank, objective));
            if (candidates.Count == 0) return null;

            var scores = new PlanScore[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
                scores[i] = PlanEvaluator.Evaluate(snapshot, Team, candidates[i], Personality, Rollouts, Horizon, Dt, seed + (uint)i * 7919u, UtilityWeights.Default);

            var rng = SimRandom.For(Team, seed, 0x5E1EC7u);
            int chosen = PlanEvaluator.SoftmaxSelect(scores, candidates, Personality, Personality.Temperature * TemperatureScale, ref rng);
            LastScores = scores;

            // Compromiso con el plan actual
            int current = candidates.FindIndex(c => c.Id == CurrentPlanId);
            if (current >= 0 && chosen != current && !PlanEvaluator.ShouldSwitch(scores[current].Value, scores[chosen].Value, Personality.Patience))
                chosen = current;
            CurrentPlanId = candidates[chosen].Id;
            CurrentValue = scores[chosen].Value;
            return candidates[chosen];
        }

        /// <summary>Traduce las primitivas del plan a órdenes por rol de grupo.</summary>
        public CandidatePlan Translate(HtnPlan plan, AbstractBattle battle, IReadOnlyList<GroupRole> roles, Vec2 enemyCenter, Vec2 weakFlank, Vec2 objective)
        {
            var cp = new CandidatePlan { Id = plan.Id };
            cp.Tags.AddRange(plan.Tags);
            float delay = 0f;
            foreach (var step in plan.Steps)
            {
                delay += step.Delay;
                for (int i = 0; i < battle.Groups.Count; i++)
                {
                    if (battle.Groups[i].Team != Team || i >= roles.Count || !RoleMatches(step.Role, roles[i])) continue;
                    if (step.Role == "rest" && cp.Orders.ContainsKey(i)) continue;
                    Vec2 point = step.Target switch
                    {
                        "enemyCenter" => enemyCenter,
                        "enemyWeakFlank" => weakFlank,
                        "objective" => objective,
                        "rear" => battle.Groups[i].Position + (battle.Groups[i].Position - enemyCenter).Normalized * 150f,
                        _ => battle.Groups[i].Position,
                    };
                    var order = step.Task switch
                    {
                        PrimitiveTasks.Hold => new GroupOrder { Type = GroupOrderType.Hold, TargetGroup = -1 },
                        PrimitiveTasks.Reserve => new GroupOrder { Type = GroupOrderType.Hold, TargetGroup = -1 },
                        PrimitiveTasks.Harass => new GroupOrder { Type = GroupOrderType.Harass, TargetGroup = -1, Point = point },
                        PrimitiveTasks.Flank => new GroupOrder { Type = GroupOrderType.Flank, Point = point },
                        PrimitiveTasks.Envelop => new GroupOrder { Type = GroupOrderType.Flank, Point = point },
                        PrimitiveTasks.Retreat => new GroupOrder { Type = GroupOrderType.Retreat, Point = point },
                        PrimitiveTasks.Capture => new GroupOrder { Type = GroupOrderType.MoveTo, Point = objective },
                        PrimitiveTasks.Feint => new GroupOrder { Type = GroupOrderType.Harass, TargetGroup = -1, Point = point },
                        _ => new GroupOrder { Type = GroupOrderType.Attack, TargetGroup = -1, Point = point },
                    };
                    order.Delay = delay;
                    cp.Orders[i] = order;
                }
            }
            return cp;
        }

        private static bool RoleMatches(string role, GroupRole r)
        {
            switch (role)
            {
                case "all": case "rest": return true;
                case "infantry": return r == GroupRole.Infantry || r == GroupRole.Elite;
                case "ranged": return r == GroupRole.Ranged;
                case "cavalry": return r == GroupRole.Cavalry || r == GroupRole.LightCavalry;
                case "lightCavalry": return r == GroupRole.LightCavalry;
                case "elite": return r == GroupRole.Elite;
                case "artillery": return r == GroupRole.Artillery;
                default: return false;
            }
        }
    }
}
