using System.Collections.Generic;
using Estandartes.Core;
using Estandartes.Core.Influence;
using Estandartes.Core.Navigation;
using Estandartes.Core.Spatial;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class SpatialNavigationTests
    {
        [Test]
        public void RejillaEspacialIgualQueFuerzaBruta()
        {
            var rng = new SimRandom(77);
            int n = 3000;
            var pos = new Vec2[n];
            for (int i = 0; i < n; i++) pos[i] = new Vec2(rng.Range(0f, 500f), rng.Range(0f, 500f));
            var grid = new SpatialGrid(0, 0, 500, 500, 4f);
            grid.Build(pos, n);
            var found = new List<int>();
            for (int q = 0; q < 50; q++)
            {
                var c = new Vec2(rng.Range(0f, 500f), rng.Range(0f, 500f));
                float r = rng.Range(1f, 20f);
                grid.Query(c, r, found);
                var expected = new List<int>();
                for (int i = 0; i < n; i++) if (Vec2.DistanceSquared(pos[i], c) <= r * r) expected.Add(i);
                found.Sort();
                CollectionAssert.AreEqual(expected, found);
            }
        }

        private static CostGrid MuroConHueco()
        {
            var g = new CostGrid(128, 128);
            g.FillRect(60, 0, 63, 127, CostGrid.Blocked); // muro vertical
            g.FillRect(60, 100, 63, 104, 1);              // hueco
            return g;
        }

        [Test]
        public void FlowFieldRodeaElMuro()
        {
            var g = MuroConHueco();
            var ff = new FlowField(g);
            int goal = g.Index(120, 10);
            ff.Compute(new[] { goal });
            int start = g.Index(5, 10);
            int steps = ff.Trace(start);
            Assert.Greater(steps, 170); // en línea recta serían 115: tiene que subir hasta el hueco y volver
            Assert.IsTrue(float.IsPositiveInfinity(ff.Integration[g.Index(61, 10)]));
            Assert.AreNotEqual(Vec2.Zero, ff.DirectionAt(start));
        }

        [Test]
        public void CorredorDeSectoresYFlowFieldLimitado()
        {
            var g = MuroConHueco();
            var sg = new SectorGraph(g, 32);
            int start = g.Index(5, 10), goal = g.Index(120, 10);
            var corridor = sg.FindCorridor(start, goal);
            Assert.IsNotNull(corridor);
            Assert.AreEqual(sg.SectorOf(start), corridor[0]);
            Assert.AreEqual(sg.SectorOf(goal), corridor[corridor.Count - 1]);
            Assert.Contains(sg.SectorOf(g.Index(61, 102)), corridor);
            var ff = new FlowField(g);
            ff.Compute(new[] { goal }, sg.CorridorMask(corridor));
            Assert.Greater(ff.Trace(start), 0);
        }

        [Test]
        public void SinCaminoNoHayCorredor()
        {
            var g = new CostGrid(64, 64);
            g.FillRect(30, 0, 33, 63, CostGrid.Blocked);
            var sg = new SectorGraph(g, 16);
            Assert.IsNull(sg.FindCorridor(g.Index(2, 2), g.Index(60, 2)));
        }

        [Test]
        public void MapaDeInfluenciaEstampaRecuerdaYSeApaga()
        {
            var m = new InfluenceMap(0, 0, 400, 400, 8f);
            m.BeginStamp();
            m.Stamp(new Vec2(200, 200), 100f, 40f);
            m.EndStamp(0.9f);
            float peak = m.Sample(new Vec2(200, 200));
            Assert.Greater(peak, 80f);
            Assert.AreEqual(0f, m.Sample(new Vec2(20, 20)));
            for (int i = 0; i < 5; i++) { m.BeginStamp(); m.EndStamp(0.9f); }
            float later = m.Sample(new Vec2(200, 200));
            Assert.Less(later, peak);
            Assert.Greater(later, 0f); // memoria
            Assert.Less(m.Gradient(new Vec2(230, 200)).X, 0f); // apunta hacia el centro
        }

        [Test]
        public void VulnerabilidadMaximaEnFrentesIgualados()
        {
            Assert.AreEqual(100f, InfluenceMap.Vulnerability(50f, 50f));
            Assert.AreEqual(20f, InfluenceMap.Vulnerability(90f, 10f));
        }

        [Test]
        public void CosteDeTerreno()
        {
            Assert.AreEqual(CostGrid.Blocked, CostGrid.TerrainCost(45f, 1f));
            Assert.Greater(CostGrid.TerrainCost(20f, 3f), CostGrid.TerrainCost(0f, 1f));
        }
    }
}
