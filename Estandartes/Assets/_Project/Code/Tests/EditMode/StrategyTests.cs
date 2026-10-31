using System.Collections.Generic;
using Estandartes.Core;
using Estandartes.Core.Content;
using Estandartes.Core.Strategy;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class StrategyTests
    {
        private static AbstractGroup G(int team, float x, float y, float n, float q = 1f, bool cav = false, float range = 0f, float r = 0f) =>
            new AbstractGroup
            {
                Team = team, Position = new Vec2(x, y), Soldiers = n, StartSoldiers = n, MeleeQuality = q, Speed = cav ? 6f : 1.3f,
                Spacing = 1f, Morale = 70f, Cavalry = cav, Range = range, RangedRate = r,
            };

        [Test]
        public void ElGrupoMasFuerteGanaElChoque()
        {
            var b = new AbstractBattle();
            b.Add(G(0, 0, 0, 300), new GroupOrder { Type = GroupOrderType.Attack, TargetGroup = 1 });
            b.Add(G(1, 0, 60, 100), new GroupOrder { Type = GroupOrderType.Hold, TargetGroup = -1 });
            var rng = new SimRandom(1);
            b.Run(600f, 1f, ref rng);
            var o = b.Outcome(0);
            Assert.Greater(o.EnemyLosses, o.OwnLosses);
            Assert.IsTrue(b.Decided());
        }

        [Test]
        public void ElFlanqueoAumentaLasBajas()
        {
            float Losses(bool flank)
            {
                var b = new AbstractBattle();
                b.Add(G(0, 0, 0, 200), new GroupOrder { Type = GroupOrderType.Attack, TargetGroup = 2 });
                b.Add(G(0, flank ? 40 : -200, 30, 100), new GroupOrder { Type = flank ? GroupOrderType.Attack : GroupOrderType.Hold, TargetGroup = 2 });
                b.Add(G(1, 0, 20, 200), new GroupOrder { Type = GroupOrderType.Hold, TargetGroup = -1 });
                var rng = new SimRandom(2);
                b.Run(120f, 1f, ref rng);
                return b.Outcome(0).EnemyLosses;
            }
            Assert.Greater(Losses(true), Losses(false));
        }

        [Test]
        public void MonteCarloPrefiereElPlanMejor()
        {
            var b = new AbstractBattle();
            b.Add(G(0, 0, 0, 200), new GroupOrder { Type = GroupOrderType.Hold });
            b.Add(G(0, 50, 0, 80, cav: true, q: 1.5f), new GroupOrder { Type = GroupOrderType.Hold });
            b.Add(G(1, 0, 150, 220), new GroupOrder { Type = GroupOrderType.Hold });
            b.Objectives.Add((new Vec2(0, 150), 1f, 60f));

            var holdPlan = new CandidatePlan { Id = "esperar" };
            var attackPlan = new CandidatePlan { Id = "yunque_y_martillo" };
            attackPlan.Tags.Add("attack");
            attackPlan.Orders[0] = new GroupOrder { Type = GroupOrderType.Attack, TargetGroup = 2 };
            attackPlan.Orders[1] = new GroupOrder { Type = GroupOrderType.Flank, Point = new Vec2(120, 150) };

            var pers = new PersonalityDefinition { Id = "estandartes:personality/prueba", RiskAversion = 0.5f, CasualtyAversion = 1f, Temperature = 0.05f };
            var s0 = PlanEvaluator.Evaluate(b, 0, holdPlan, pers, 16, 300f, 2f, 9, UtilityWeights.Default);
            var s1 = PlanEvaluator.Evaluate(b, 0, attackPlan, pers, 16, 300f, 2f, 9, UtilityWeights.Default);
            Assert.Greater(s1.Value, s0.Value);

            var scores = new[] { s0, s1 };
            var plans = new List<CandidatePlan> { holdPlan, attackPlan };
            var rng = new SimRandom(4);
            int picks = 0;
            for (int i = 0; i < 100; i++) if (PlanEvaluator.SoftmaxSelect(scores, plans, pers, pers.Temperature, ref rng) == 1) picks++;
            Assert.Greater(picks, 90);
            Assert.AreEqual(1f, scores[0].Probability + scores[1].Probability, 1e-4);
        }

        [Test]
        public void EvaluacionReproducibleConLaMismaSemilla()
        {
            var b = new AbstractBattle();
            b.Add(G(0, 0, 0, 100), new GroupOrder { Type = GroupOrderType.Attack, TargetGroup = 1 });
            b.Add(G(1, 0, 50, 100), new GroupOrder { Type = GroupOrderType.Hold });
            var plan = new CandidatePlan { Id = "x" };
            var pers = new PersonalityDefinition { Id = "estandartes:personality/p" };
            var a = PlanEvaluator.Evaluate(b, 0, plan, pers, 8, 120f, 2f, 42, UtilityWeights.Default);
            var c = PlanEvaluator.Evaluate(b, 0, plan, pers, 8, 120f, 2f, 42, UtilityWeights.Default);
            Assert.AreEqual(a.Value, c.Value);
        }

        public static HtnDomainDefinition DominioCampal() => new HtnDomainDefinition
        {
            Id = "estandartes:htn/prueba",
            Root = "WinBattle",
            Compound = new Dictionary<string, List<HtnMethod>>
            {
                ["WinBattle"] = new List<HtnMethod>
                {
                    new HtnMethod { Name = "anvil_hammer", Tags = { "attack" }, Pre = { "cavalryRatio >= 0.15", "flankRoute == 1" },
                        Subtasks = { new HtnSubtask { Task = "FixCenter", Role = "infantry", Target = "enemyCenter" },
                                     new HtnSubtask { Task = "Harass", Role = "ranged", Target = "enemyCenter" },
                                     new HtnSubtask { Task = "Flank", Role = "cavalry", Target = "enemyWeakFlank", Delay = 20 } } },
                    new HtnMethod { Name = "feigned_retreat", Tags = { "deception" }, Pre = { "enemyOnHighGround == 1", "enemyDiscipline < 0.5" },
                        Subtasks = { new HtnSubtask { Task = "Harass", Role = "ranged" }, new HtnSubtask { Task = "FeignedRetreat", Role = "cavalry", Repeat = 3 },
                                     new HtnSubtask { Task = "Assault", Role = "all" } } },
                    new HtnMethod { Name = "frontal", Tags = { "attack" }, Pre = { "forceRatio >= 1.5" }, Subtasks = { new HtnSubtask { Task = "Assault", Role = "all" } } },
                    new HtnMethod { Name = "wait", Subtasks = { new HtnSubtask { Task = "Hold", Role = "all" } } },
                }
            }
        };

        [Test]
        public void HtnValidoYDescomposicionSegunHechos()
        {
            var d = DominioCampal();
            CollectionAssert.IsEmpty(d.Validate());
            var facts = new Dictionary<string, float> { ["cavalryRatio"] = 0.2f, ["flankRoute"] = 1, ["forceRatio"] = 1f, ["enemyOnHighGround"] = 0 };
            var plans = HtnPlanner.Plan(d, facts);
            var ids = plans.ConvertAll(p => p.Id);
            CollectionAssert.AreEquivalent(new[] { "anvil_hammer", "wait" }, ids);
            var fr = HtnPlanner.Plan(d, new Dictionary<string, float> { ["enemyOnHighGround"] = 1, ["enemyDiscipline"] = 0.3f });
            var feigned = fr.Find(p => p.Id == "feigned_retreat");
            Assert.IsNotNull(feigned);
            Assert.AreEqual(5, feigned.Steps.Count); // la retirada fingida se repite 3 veces
        }

        [Test]
        public void HtnDetectaErrores()
        {
            var d = DominioCampal();
            d.Compound["WinBattle"][0].Subtasks.Add(new HtnSubtask { Task = "Teletransportar" });
            d.Compound["WinBattle"][0].Pre.Add("hechoInventado > 1");
            d.Compound["Loop"] = new List<HtnMethod> { new HtnMethod { Name = "l", Subtasks = { new HtnSubtask { Task = "Loop" } } } };
            var e = d.Validate();
            Assert.That(e, Has.Some.Contains("Teletransportar"));
            Assert.That(e, Has.Some.Contains("hechoInventado"));
            Assert.That(e, Has.Some.Contains("ciclo"));
        }

        [Test]
        public void ElGeneralEligeYSeCompromete()
        {
            var b = new AbstractBattle();
            b.Add(G(0, 0, 0, 250), new GroupOrder { Type = GroupOrderType.Hold });
            b.Add(G(0, 30, 0, 60, range: 150f, r: 0.004f), new GroupOrder { Type = GroupOrderType.Hold });
            b.Add(G(0, 80, 0, 80, cav: true, q: 1.5f), new GroupOrder { Type = GroupOrderType.Hold });
            b.Add(G(1, 0, 200, 220), new GroupOrder { Type = GroupOrderType.Hold });
            var roles = new List<GroupRole> { GroupRole.Infantry, GroupRole.Ranged, GroupRole.Cavalry, GroupRole.Infantry };
            var pers = new PersonalityDefinition { Id = "estandartes:personality/astuto", DeceptionBias = 0.3f, Patience = 0.1f, Temperature = 0.1f };
            var brain = new GeneralBrain(0, DominioCampal(), pers) { Rollouts = 8, Horizon = 240f };
            var facts = new Dictionary<string, float> { ["cavalryRatio"] = 0.2f, ["flankRoute"] = 1, ["forceRatio"] = 1.6f };
            var plan = brain.Decide(b, roles, facts, new Vec2(0, 200), new Vec2(120, 200), new Vec2(0, 200), 11);
            Assert.IsNotNull(plan);
            Assert.AreEqual(3, brain.LastScores.Length);
            Assert.AreNotEqual("wait", plan.Id);
            var again = brain.Decide(b, roles, facts, new Vec2(0, 200), new Vec2(120, 200), new Vec2(0, 200), 12);
            Assert.AreEqual(plan.Id, again.Id); // compromiso
        }
    }
}
