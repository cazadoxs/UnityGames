using Estandartes.Core;
using Estandartes.Core.Combat;
using Estandartes.Core.Command;
using Estandartes.Core.Content;
using Estandartes.Core.Morale;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class CombatMoraleTests
    {
        [Test]
        public void DanoSegunArmaduraZonaEImpulso()
        {
            // Hacha danesa (corte 70, pen 0,30) contra hombre de armas (corte 0,60) en el torso
            float d = DamageModel.Compute(70f, 1f, 0.60f, 0.30f, HitZone.Torso, 1f);
            Assert.That(d, Is.EqualTo(70f * (1f - 0.6f * 0.7f)).Within(1e-3));
            Assert.Greater(DamageModel.Compute(70f, 1f, 0.6f, 0.3f, HitZone.Head, 1f), d);
            Assert.Less(DamageModel.Compute(70f, 1f, 0.6f, 0.3f, HitZone.Limbs, 1f), d);
            Assert.AreEqual(1f, DamageModel.ImpulseFromSpeed(2f));
            Assert.Greater(DamageModel.ImpulseFromSpeed(11f), 2f);
        }

        [Test]
        public void ReaccionDentroDeLimitesYPeorConFatigaYMiedo()
        {
            var rng = new SimRandom(5);
            float fresh = 0, tired = 0;
            for (int i = 0; i < 2000; i++)
            {
                float a = DuelModel.SampleReactionMs(ref rng, Difficulty.Veterano, 0.6f, 0f, 0f);
                float b = DuelModel.SampleReactionMs(ref rng, Difficulty.Veterano, 0.6f, 1f, 1f);
                Assert.That(a, Is.InRange(150f, 700f));
                fresh += a; tired += b;
            }
            Assert.Less(fresh, tired);
        }

        [Test]
        public void ProbabilidadDeBloqueoSegunDificultad()
        {
            float r = DuelModel.BlockDirectionProbability(Difficulty.Recluta, 0.5f, 0f, 0f);
            float m = DuelModel.BlockDirectionProbability(Difficulty.MaestroDeGuerra, 0.5f, 0f, 0f);
            Assert.Less(r, m);
            Assert.That(DuelModel.BlockDirectionProbability(Difficulty.Recluta, 0f, 1f, 1f), Is.GreaterThanOrEqualTo(0.05f));
        }

        [Test]
        public void SinTiempoNoHayDefensa()
        {
            var rng = new SimRandom(1);
            Assert.AreEqual(DefenseChoice.None, DuelModel.ChooseDefense(ref rng, 400f, 300f, 500f, 1f, 0.5f, true));
        }

        [Test]
        public void EleccionDeRivalConHisteresis()
        {
            var c = new[]
            {
                new TargetCandidate { Id = 1, Distance = 2f, Health01 = 1f },
                new TargetCandidate { Id = 2, Distance = 1.8f, Health01 = 1f },
                new TargetCandidate { Id = 3, Distance = 3f, AttackingMe = true, Health01 = 1f },
            };
            Assert.AreEqual(3, TargetSelection.Select(c, -1, 6f));
            // Con rival actual 1, el 2 es apenas mejor: no cambia
            var d = new[] { c[0], c[1] };
            Assert.AreEqual(1, TargetSelection.Select(d, 1, 6f));
        }

        [Test]
        public void MoralEventosAtenuadosPorValor()
        {
            float cobarde = MoraleModel.ApplyEvent(60f, -20f, 0f);
            float valiente = MoraleModel.ApplyEvent(60f, -20f, 1f);
            Assert.AreEqual(40f, cobarde, 1e-4);
            Assert.AreEqual(50f, valiente, 1e-4);
            Assert.AreEqual(70f, MoraleModel.ApplyEvent(60f, 10f, 0f), 1e-4);
        }

        [Test]
        public void ElMiedoSeContagiaMasRapidoQueElValor()
        {
            float down = 60f - MoraleModel.Contagion(60f, 30f, 0f, 1f);
            float up = MoraleModel.Contagion(60f, 90f, 0f, 1f) - 60f;
            Assert.AreEqual(3f, down / up, 1e-3);
            float disciplined = 60f - MoraleModel.Contagion(60f, 30f, 1f, 1f);
            Assert.Less(disciplined, down);
        }

        [Test]
        public void EstadosDeMoral()
        {
            Assert.AreEqual(MoraleState.Steady, MoraleModel.StateFor(70f, false));
            Assert.AreEqual(MoraleState.Wavering, MoraleModel.StateFor(50f, false));
            Assert.AreEqual(MoraleState.Retreating, MoraleModel.StateFor(30f, false));
            Assert.AreEqual(MoraleState.Routing, MoraleModel.StateFor(10f, false));
            Assert.AreEqual(MoraleState.Surrendered, MoraleModel.StateFor(10f, true));
            Assert.AreEqual(66f, MoraleModel.SquadMorale(60f, true, false), 1e-3);
        }

        [Test]
        public void ReagrupacionSoloSinPersecucionYConMando()
        {
            float m = MoraleModel.Rally(20f, pursued: true, leaderNearby: true, 10f, out bool r1);
            Assert.AreEqual(20f, m); Assert.IsFalse(r1);
            m = 20f;
            for (int i = 0; i < 20; i++) m = MoraleModel.Rally(m, false, true, 1f, out _);
            MoraleModel.Rally(m, false, true, 0f, out bool rallied);
            Assert.IsTrue(rallied);
        }

        [Test]
        public void CadenaDeMandoEligeElMedioMasRapido()
        {
            Assert.AreEqual(CommandMedium.Voice, CommandChain.Choose(OrderType.Charge, 20f, true, true));
            Assert.AreEqual(CommandMedium.Musician, CommandChain.Choose(OrderType.Charge, 200f, true, true));
            Assert.AreEqual(CommandMedium.Standard, CommandChain.Choose(OrderType.Charge, 200f, false, true));
            Assert.AreEqual(CommandMedium.Messenger, CommandChain.Choose(OrderType.Formation, 200f, true, true));
            var rng = new SimRandom(3);
            Assert.That(CommandChain.Delay(CommandMedium.Messenger, 500f, ref rng), Is.GreaterThanOrEqualTo(52f));
            Assert.AreEqual(130u, CommandChain.DeliverTick(100, 1f));
        }

        [Test]
        public void ArmaduraPorTipo()
        {
            var a = new ArmorValues { Cut = 0.6f, Pierce = 0.45f, Blunt = 0.3f };
            Assert.AreEqual(0.45f, a.For(DamageType.Pierce));
        }
    }
}
