using System.Collections.Generic;
using System.Linq;
using Estandartes.Core.Army;
using Estandartes.Core.Content;
using Estandartes.Core.Maps;
using Estandartes.Core.Mods;
using Estandartes.Core.Strategy;
using NUnit.Framework;

namespace Estandartes.Tests
{
    /// <summary>El contenido base completo se carga con el mismo cargador que los mods (Vol. V §2).</summary>
    public class BaseContentTests
    {
        private static LoadResult Load() =>
            GameContent.CreateLoader().Load(new IModSource[] { new DirectoryModSource(TestPaths.BaseMod) }, GameContent.GameVersion);

        [Test]
        public void ElContenidoBaseCargaSinErrores()
        {
            var r = Load();
            var errors = r.Messages.Where(m => m.Severity == LoadSeverity.Error).ToList();
            Assert.IsEmpty(errors, string.Join("\n", errors));
            Assert.AreEqual(1, r.Loaded.Count);
            Assert.GreaterOrEqual(r.Registry.All<UnitDefinition>().Count(), 20);
            Assert.AreEqual(12, r.Registry.All<FactionDefinition>().Count());
            Assert.AreEqual(4, r.Registry.All<HtnDomainDefinition>().Count());
        }

        [TestCase("hastings")] [TestCase("chateau_gaillard")] [TestCase("las_navas")] [TestCase("stirling")] [TestCase("agincourt")]
        public void LosCincoNivelesSonValidos(string level)
        {
            var r = Load();
            var s = r.Registry.Get<ScenarioDefinition>("estandartes:scenario/" + level);
            CollectionAssert.IsEmpty(s.Validate());
            Assert.AreEqual(1, s.Objectives.Nodes.Count(n => n.Final));
            Assert.IsTrue(s.Armies.Exists(a => a.PlayerSide));
        }

        [Test]
        public void CadaFaccionPuedeRepartirUnEjercito()
        {
            var r = Load();
            foreach (var f in r.Registry.All<FactionDefinition>())
            {
                var doctrine = r.Registry.Get<DoctrineDefinition>(f.Doctrine);
                var req = new DistributionRequest { Total = 3000 };
                foreach (var kv in doctrine.Weights)
                {
                    var u = r.Registry.Get<UnitDefinition>(kv.Key);
                    req.Shares.Add(new UnitShare { UnitId = u.Id, DoctrineWeight = kv.Value, SquadSize = u.SquadSize, MapModifier = 1, RoleModifier = 1 });
                }
                var res = ArmyDistributor.Distribute(req);
                Assert.AreEqual(3000, res.Total, f.Id);
                Assert.IsTrue(res.Allocations.All(a => a.Squads >= 1), f.Id);
            }
        }

        [Test]
        public void LosPlanesPreferidosDeLasDoctrinasExistenEnLosDominios()
        {
            var r = Load();
            var methods = new HashSet<string>();
            foreach (var d in r.Registry.All<HtnDomainDefinition>())
                foreach (var kv in d.Compound) foreach (var m in kv.Value) methods.Add(m.Name);
            foreach (var doc in r.Registry.All<DoctrineDefinition>())
                foreach (var plan in doc.PreferredPlans.Keys)
                    Assert.IsTrue(methods.Contains(plan), $"{doc.Id}: plan '{plan}'");
        }
    }
}
