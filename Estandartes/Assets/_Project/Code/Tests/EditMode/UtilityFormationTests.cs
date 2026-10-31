using System.Collections.Generic;
using Estandartes.Core;
using Estandartes.Core.Content;
using Estandartes.Core.Formations;
using Estandartes.Core.Utility;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class UtilityFormationTests
    {
        [Test]
        public void CurvasDeRespuesta()
        {
            var lin = ResponseCurve.Linear(0f, 100f);
            Assert.AreEqual(0.5f, lin.Evaluate(50f), 1e-4);
            var inv = ResponseCurve.Logistic(250f, 10f, 0f, 600f, invert: true);
            Assert.Greater(inv.Evaluate(100f), inv.Evaluate(400f));
            Assert.AreEqual(0.5f, inv.Evaluate(250f), 1e-3);
            var lut = inv.Bake();
            Assert.AreEqual(64, lut.Length);
            Assert.AreEqual(inv.Evaluate(300f), ResponseCurve.EvaluateBaked(lut, 0f, 600f, 300f), 0.02);
        }

        [Test]
        public void MediaGeometricaYEjemploDelVolumenIII()
        {
            Assert.AreEqual(0f, UtilityScoring.GeometricMean(new[] { 0.9f, 0f, 1f }));
            float g = UtilityScoring.GeometricMean(new[] { 0.82f, 1f, 0.91f, 0.95f, 0.60f, 0.75f });
            Assert.AreEqual(0.83f, g, 0.01);
            Assert.AreEqual(g * 1.15f, UtilityScoring.Score(1f, 1f, new[] { 0.82f, 1f, 0.91f, 0.95f, 0.60f, 0.75f }, true), 1e-4);
        }

        [Test]
        public void SubastaRespetaMinimosYMaximos()
        {
            var scores = new float[,] { { 0.9f, 0.1f }, { 0.8f, 0.2f }, { 0.7f, 0.3f } };
            var cap = new[] { new TaskCapacity { Min = 0, Max = 2 }, new TaskCapacity { Min = 1, Max = 1 } };
            var r = TaskAssignment.Greedy(scores, cap);
            int t0 = 0, t1 = 0;
            foreach (var x in r) { if (x == 0) t0++; if (x == 1) t1++; }
            Assert.AreEqual(2, t0);
            Assert.AreEqual(1, t1);
        }

        [Test]
        public void HungaroEsOptimo()
        {
            // Voraz elegiría (0,0)=0.9 y luego (1,1)=0.1 → 1.0; lo óptimo es (0,1)+(1,0) = 0.8+0.85
            var scores = new float[,] { { 0.9f, 0.8f }, { 0.85f, 0.1f } };
            var r = TaskAssignment.Hungarian(scores);
            Assert.AreEqual(1, r[0]);
            Assert.AreEqual(0, r[1]);
        }

        private static FormationParams P(FormationGenerator g, int ranks = 4) =>
            new FormationParams { Generator = g, SpacingX = 1f, SpacingY = 1.2f, DesiredRanks = ranks };

        [TestCase(FormationGenerator.Line)] [TestCase(FormationGenerator.ShieldWall)] [TestCase(FormationGenerator.Column)]
        [TestCase(FormationGenerator.Wedge)] [TestCase(FormationGenerator.Schiltron)] [TestCase(FormationGenerator.Circle)]
        public void TodosLosGeneradoresDanUnSlotPorSoldadoSinSolapes(FormationGenerator g)
        {
            var slots = new List<Slot>();
            FormationSlots.Generate(P(g), 97, slots);
            Assert.AreEqual(97, slots.Count);
            for (int i = 0; i < slots.Count; i++)
                for (int j = i + 1; j < slots.Count; j++)
                    Assert.Greater(Vec2.Distance(slots[i].Offset, slots[j].Offset), 0.3f, $"{g}: {i} y {j} se solapan");
        }

        [Test]
        public void LineaDe100ConCuatroFilas()
        {
            var slots = new List<Slot>();
            FormationSlots.Generate(P(FormationGenerator.Line), 100, slots);
            Assert.AreEqual(25, FormationSlots.LineWidth(100, 4));
            Assert.AreEqual(3, slots[99].Rank);
            Assert.AreEqual(0f, slots[12].Offset.X, 1e-4); // centro de la primera fila
        }

        [Test]
        public void ErizoMiraHaciaFuera()
        {
            var slots = new List<Slot>();
            FormationSlots.Generate(P(FormationGenerator.Schiltron), 60, slots);
            foreach (var s in slots) Assert.Greater(Vec2.Dot(s.LocalFacing, s.Offset.Normalized), 0.99f);
        }

        [Test]
        public void AsignacionPoneLosPrioritariosDelanteYSinCruces()
        {
            var slots = new List<Slot>();
            FormationSlots.Generate(P(FormationGenerator.Line, 2), 8, slots);
            var pos = new List<Vec2>();
            var prio = new List<float>();
            for (int i = 0; i < 8; i++) { pos.Add(new Vec2(i, -5)); prio.Add(i % 2 == 0 ? 1f : 0f); }
            var map = SlotAssignment.Assign(slots, pos, prio);
            for (int s = 0; s < slots.Count; s++)
            {
                if (slots[s].Rank == 0) Assert.AreEqual(1f, prio[map[s]], "primera fila con los prioritarios");
            }
            // En la primera fila, de izquierda a derecha, las X de origen crecen (sin cruces)
            float last = float.MinValue;
            var front = new List<(float x, int soldier)>();
            for (int s = 0; s < slots.Count; s++) if (slots[s].Rank == 0) front.Add((slots[s].Offset.X, map[s]));
            front.Sort((a, b) => a.x.CompareTo(b.x));
            foreach (var f in front) { Assert.Greater(pos[f.soldier].X, last); last = pos[f.soldier].X; }
        }

        [Test]
        public void Cohesion()
        {
            var a = new List<Vec2> { new Vec2(0, 0), new Vec2(1, 0) };
            Assert.AreEqual(1f, SlotAssignment.Cohesion(a, a, 1f), 1e-4);
            var b = new List<Vec2> { new Vec2(0, 3), new Vec2(1, 3) };
            Assert.AreEqual(0f, SlotAssignment.Cohesion(b, a, 1f), 1e-4);
            Assert.AreEqual(4, SlotAssignment.MaxRotations(25));
            Assert.IsTrue(SlotAssignment.ShouldRotate(0.7f, 1f, false, 0.2f));
            Assert.IsFalse(SlotAssignment.ShouldRotate(0.7f, 1f, true, 0.2f));
        }
    }
}
