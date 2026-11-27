using NUnit.Framework;

namespace Mecanicas.Construccion.Tests
{
    public class GridOccupancyTests
    {
        [Test]
        public void Rotate_SwapsSizeOnOddQuarterTurns()
        {
            Footprint.Rotate(2, 3, 0, out int x0, out int z0);
            Footprint.Rotate(2, 3, 1, out int x1, out int z1);
            Footprint.Rotate(2, 3, 2, out int x2, out int z2);
            Footprint.Rotate(2, 3, -1, out int x3, out int z3);
            Assert.AreEqual((2, 3), (x0, z0));
            Assert.AreEqual((3, 2), (x1, z1));
            Assert.AreEqual((2, 3), (x2, z2));
            Assert.AreEqual((3, 2), (x3, z3));
        }

        [TestCase(3.7, 1, 3)]
        [TestCase(3.0, 1, 3)]
        [TestCase(3.7, 2, 3)]
        [TestCase(3.2, 2, 2)]
        [TestCase(3.7, 3, 2)]
        [TestCase(-0.2, 1, -1)]
        public void OriginFromPoint_CentersFootprint(double point, int size, int expected)
        {
            Assert.AreEqual(expected, Footprint.OriginFromPoint(point, size));
        }

        [Test]
        public void OddFootprint_ContainsPointedCell()
        {
            for (double p = 0.01; p < 10; p += 0.13)
            {
                int origin = Footprint.OriginFromPoint(p, 3);
                int cell = (int)System.Math.Floor(p);
                Assert.AreEqual(cell, origin + 1, "punto " + p);
            }
        }

        [Test]
        public void Occupy_BlocksOverlapAndReleaseFrees()
        {
            var g = new GridOccupancy(5, 4);
            var a = new CellArea(0, 0, 2, 2);
            int id = g.Occupy(a);
            Assert.Greater(id, 0);
            Assert.AreEqual(4, g.OccupiedCount);
            Assert.AreEqual(CellState.Occupied, g.GetState(1, 1));
            Assert.AreEqual(id, g.GetOccupant(1, 0));

            Assert.IsFalse(g.IsAreaFree(new CellArea(1, 1, 2, 2)));
            Assert.AreEqual(0, g.Occupy(new CellArea(1, 1, 2, 2)));
            Assert.IsTrue(g.IsAreaFree(new CellArea(2, 0, 2, 2)));

            Assert.IsTrue(g.Release(id));
            Assert.IsFalse(g.Release(id));
            Assert.AreEqual(0, g.OccupiedCount);
            Assert.IsTrue(g.IsAreaFree(a));
        }

        [Test]
        public void AreaOutsideBounds_IsNotFree()
        {
            var g = new GridOccupancy(3, 3);
            Assert.IsFalse(g.IsAreaFree(new CellArea(2, 0, 2, 1)));
            Assert.IsFalse(g.IsAreaFree(new CellArea(-1, 0, 1, 1)));
            Assert.IsFalse(g.IsAreaFree(new CellArea(0, 0, 0, 1)));
            Assert.AreEqual(CellState.Outside, g.GetState(3, 0));
            Assert.IsTrue(g.IsAreaFree(new CellArea(0, 0, 3, 3)));
        }

        [Test]
        public void BlockedCells_CannotBeUsedUntilUnblocked()
        {
            var g = new GridOccupancy(3, 3);
            g.SetBlocked(1, 1, true);
            Assert.AreEqual(CellState.Blocked, g.GetState(1, 1));
            Assert.IsFalse(g.IsAreaFree(new CellArea(0, 0, 2, 2)));
            Assert.AreEqual(0, g.Occupy(new CellArea(1, 1, 1, 1)));
            g.SetBlocked(1, 1, false);
            Assert.Greater(g.Occupy(new CellArea(1, 1, 1, 1)), 0);
            // Bloquear una celda ocupada no la libera.
            g.SetBlocked(1, 1, true);
            Assert.AreEqual(CellState.Occupied, g.GetState(1, 1));
        }
    }
}
