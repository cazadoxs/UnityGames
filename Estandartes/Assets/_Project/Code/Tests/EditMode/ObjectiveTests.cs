using System.Collections.Generic;
using Estandartes.Core.Objectives;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class ObjectiveTests
    {
        /// <summary>El ejemplo de castillo del Vol. I §5.1.</summary>
        public static ObjectiveGraph Castillo() => new ObjectiveGraph
        {
            Nodes = new List<ObjectiveNode>
            {
                new ObjectiveNode { Id = "puerta", Type = ObjectiveType.Open, Team = 0, PointOfNoReturn = true, InitialOwner = 1 },
                new ObjectiveNode { Id = "brecha", Type = ObjectiveType.Destroy, Team = 0, PointOfNoReturn = true, InitialOwner = 1 },
                new ObjectiveNode { Id = "patio", Requires = { "puerta", "brecha" }, RequireMode = RequireMode.Or, InitialOwner = 1 },
                new ObjectiveNode { Id = "torreta_izq", Requires = { "patio" }, InitialOwner = 1 },
                new ObjectiveNode { Id = "torreta_der", Requires = { "patio" }, InitialOwner = 1 },
                new ObjectiveNode { Id = "homenaje", Requires = { "torreta_izq", "torreta_der" }, RequireMode = RequireMode.And, Final = true, InitialOwner = 1 },
            }
        };

        [Test]
        public void GrafoDelCastilloEsValido() => CollectionAssert.IsEmpty(Castillo().Validate());

        [Test]
        public void DetectaCiclosYReferenciasRotas()
        {
            var g = new ObjectiveGraph { Nodes = { new ObjectiveNode { Id = "a", Requires = { "b" } }, new ObjectiveNode { Id = "b", Requires = { "a" } },
                                                   new ObjectiveNode { Id = "c", Requires = { "x" } } } };
            var e = g.Validate();
            Assert.That(e, Has.Some.Contains("ciclo"));
            Assert.That(e, Has.Some.Contains("'x'"));
        }

        [Test]
        public void DesbloqueoYO()
        {
            var t = new ObjectiveTracker(Castillo());
            Assert.IsFalse(t.IsUnlockedFor("patio", 0));
            t.Complete("brecha", 0); // basta con una ruta (O)
            Assert.IsTrue(t.IsUnlockedFor("patio", 0));
            t.Complete("patio", 0);
            t.Complete("torreta_izq", 0);
            Assert.IsFalse(t.IsUnlockedFor("homenaje", 0)); // hacen falta las dos (Y)
            t.Complete("torreta_der", 0);
            Assert.IsTrue(t.IsUnlockedFor("homenaje", 0));
            Assert.IsFalse(t.FinalAchievedBy(0));
            t.Complete("homenaje", 0);
            Assert.IsTrue(t.FinalAchievedBy(0));
        }

        [Test]
        public void CapturaSinOposicionTardaElTiempoBaseConElUmbral()
        {
            var node = new ObjectiveNode { Id = "p", CaptureTime = 60f };
            var s = new ObjectiveState { Owner = -1, Capturer = -1 };
            float t = 0f;
            while (!CaptureRules.Step(ref s, node, 0.5f, 10f, 0f, true, true, threshold: 10f) && t < 1000f) t += 0.5f;
            Assert.That(t + 0.5f, Is.EqualTo(60f).Within(1f));
            Assert.AreEqual(0, s.Owner);
        }

        [Test]
        public void MasGenteCapturaMasRapidoConRendimientosDecrecientes()
        {
            Assert.AreEqual(1f, CaptureRules.SpeedFactor(10f, 10f), 1e-4);
            Assert.AreEqual(2f, CaptureRules.SpeedFactor(20f, 10f), 1e-4);
            Assert.AreEqual(3f, CaptureRules.SpeedFactor(1000f, 10f), 1e-4);
        }

        [Test]
        public void DisputadoSeCongelaSalvoDosAUno()
        {
            var node = new ObjectiveNode { Id = "p", CaptureTime = 30f };
            var s = new ObjectiveState { Owner = -1, Capturer = -1 };
            CaptureRules.Step(ref s, node, 1f, 15f, 10f, true, true);
            Assert.AreEqual(0f, s.Progress);
            CaptureRules.Step(ref s, node, 1f, 25f, 10f, true, true);
            Assert.Greater(s.Progress, 0f);
            Assert.AreEqual(0, s.Capturer);
        }

        [Test]
        public void PuntoBloqueadoNoAvanzaYPuntoDeNoRetornoNoSeRecupera()
        {
            var node = new ObjectiveNode { Id = "p", CaptureTime = 10f, PointOfNoReturn = true, InitialOwner = 1 };
            var s = new ObjectiveState { Owner = 1, Capturer = -1 };
            CaptureRules.Step(ref s, node, 1f, 50f, 0f, unlocked0: false, unlocked1: true);
            Assert.AreEqual(0f, s.Progress);
            for (int i = 0; i < 30; i++) CaptureRules.Step(ref s, node, 1f, 50f, 0f, true, true);
            Assert.AreEqual(0, s.Owner);
            for (int i = 0; i < 30; i++) CaptureRules.Step(ref s, node, 1f, 0f, 50f, true, true);
            Assert.AreEqual(0, s.Owner);
        }

        [Test]
        public void VaciaRevierteLentamente()
        {
            var node = new ObjectiveNode { Id = "p", CaptureTime = 20f };
            var s = new ObjectiveState { Owner = -1, Capturer = -1 };
            for (int i = 0; i < 10; i++) CaptureRules.Step(ref s, node, 1f, 10f, 0f, true, true);
            float p = s.Progress;
            CaptureRules.Step(ref s, node, 1f, 0f, 0f, true, true);
            Assert.Less(s.Progress, p);
            Assert.Greater(s.Progress, p - 0.05f);
        }

        [Test]
        public void CondicionesDeVictoria()
        {
            var conds = new List<VictoryCondition>
            {
                new VictoryCondition { Type = VictoryType.Annihilation },
                new VictoryCondition { Type = VictoryType.Resist, Team = 1, Value = 600f },
            };
            var st = new BattleStatus { Alive0 = 100, Alive1 = 100, Morale0 = 60, Morale1 = 60, GeneralAlive0 = true, GeneralAlive1 = true, Elapsed = 100 };
            Assert.AreEqual(-1, VictoryEvaluator.Evaluate(conds, st, null));
            st.Morale1 = 10;
            Assert.AreEqual(0, VictoryEvaluator.Evaluate(conds, st, null));
            st.Morale1 = 60; st.Elapsed = 601;
            Assert.AreEqual(1, VictoryEvaluator.Evaluate(conds, st, null));
        }
    }
}
