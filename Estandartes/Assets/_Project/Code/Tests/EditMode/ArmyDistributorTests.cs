using System.Collections.Generic;
using Estandartes.Core.Army;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class ArmyDistributorTests
    {
        private static DistributionRequest EjemploAnexoB() => new DistributionRequest
        {
            Total = 2000,
            Mode = DistributionMode.Balanced,
            CommandReserve = 21,
            Shares = new List<UnitShare>
            {
                new UnitShare { UnitId = "arco_largo", Category = UnitCategory.Ranged, DoctrineWeight = 0.40f, SquadSize = 60, MapModifier = 1f, RoleModifier = 1.1f },
                new UnitShare { UnitId = "hombres_de_armas", Category = UnitCategory.Infantry, DoctrineWeight = 0.30f, SquadSize = 60, MapModifier = 1f, RoleModifier = 1f },
                new UnitShare { UnitId = "lanceros", Category = UnitCategory.Infantry, DoctrineWeight = 0.10f, SquadSize = 80, MapModifier = 1f, RoleModifier = 1f },
                new UnitShare { UnitId = "levas", Category = UnitCategory.Infantry, DoctrineWeight = 0.10f, SquadSize = 80, MapModifier = 1f, RoleModifier = 1f },
                new UnitShare { UnitId = "caballeria_pesada", Category = UnitCategory.Cavalry, DoctrineWeight = 0.07f, SquadSize = 25, MapModifier = 1.2f, RoleModifier = 0.9f },
                new UnitShare { UnitId = "caballeria_ligera", Category = UnitCategory.Cavalry, DoctrineWeight = 0.03f, SquadSize = 30, MapModifier = 1.2f, RoleModifier = 0.9f },
            }
        };

        [Test]
        public void EjemploResueltoDelAnexoB()
        {
            var r = ArmyDistributor.Distribute(EjemploAnexoB());
            Assert.AreEqual(14, r.For("arco_largo").Squads);
            Assert.AreEqual(10, r.For("hombres_de_armas").Squads);
            Assert.AreEqual(2, r.For("lanceros").Squads);
            Assert.AreEqual(2, r.For("levas").Squads);
            Assert.AreEqual(6, r.For("caballeria_pesada").Squads);
            Assert.AreEqual(2, r.For("caballeria_ligera").Squads);
            int extra = 0;
            foreach (var a in r.Allocations) extra += a.Extra;
            Assert.AreEqual(9, extra);
            Assert.AreEqual(21, r.Command);
            Assert.AreEqual(0, r.Unassigned);
            Assert.AreEqual(2000, r.Total);
        }

        [TestCase(100)] [TestCase(537)] [TestCase(2000)] [TestCase(3500)] [TestCase(20000)]
        public void ElTotalSiempreCuadra(int total)
        {
            var req = EjemploAnexoB();
            req.Total = total;
            var r = ArmyDistributor.Distribute(req);
            Assert.AreEqual(total, r.Total);
            foreach (var a in r.Allocations) Assert.GreaterOrEqual(a.Squads, total >= 500 ? 1 : 0, a.UnitId);
        }

        [Test]
        public void ModoHistoricoIgnoraElMapa()
        {
            var req = EjemploAnexoB();
            req.Mode = DistributionMode.Historical;
            var r = ArmyDistributor.Distribute(req);
            var cab = r.For("caballeria_pesada");
            var bal = ArmyDistributor.Distribute(EjemploAnexoB()).For("caballeria_pesada");
            Assert.Less(cab.NormalizedWeight, bal.NormalizedWeight);
        }

        [Test]
        public void ReservaDeAsedio()
        {
            var req = EjemploAnexoB();
            req.SiegeReserve = 120;
            var r = ArmyDistributor.Distribute(req);
            Assert.AreEqual(120, r.SiegeCrew);
            Assert.AreEqual(2000, r.Total);
        }
    }
}
