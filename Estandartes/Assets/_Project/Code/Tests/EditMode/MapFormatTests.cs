using System.Collections.Generic;
using System.IO;
using Estandartes.Core;
using Estandartes.Core.Maps;
using Estandartes.Core.Objectives;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class MapFormatTests
    {
        private static ZoneData Square(string id, ZoneType t, int owner, float x) => new ZoneData
        {
            Id = id, Type = t, Owner = owner,
            Polygon = new List<Vec2> { new Vec2(x, 0), new Vec2(x + 50, 0), new Vec2(x + 50, 50), new Vec2(x, 50) }
        };

        private static MapPackage Sample()
        {
            var p = new MapPackage();
            p.Manifest = new MapManifest { Id = "valle_del_rio", Name = "Valle del río", Author = "pruebas" };
            p.Zones.Add(Square("despliegue_azul", ZoneType.Deployment, 0, 0));
            p.Zones.Add(Square("despliegue_rojo", ZoneType.Deployment, 1, 500));
            p.Zones.Add(Square("puente", ZoneType.Conquest, -1, 250));
            p.Objectives = ObjectiveTests.Castillo();
            p.AiHints.Add(new AiHint { Type = "Chokepoint", Points = { new Vec2(275, 25) }, Source = "auto" });
            p.Binaries["terrain/height.r16"] = new byte[] { 1, 2, 3, 4 };
            return p;
        }

        [Test]
        public void GuardarYCargarIdaYVuelta()
        {
            var ms = new MemoryStream();
            Sample().Save(ms);
            ms.Position = 0;
            var errors = new List<string>();
            var p = MapPackage.Load(ms, errors);
            CollectionAssert.IsEmpty(errors);
            Assert.AreEqual("valle_del_rio", p.Manifest.Id);
            Assert.AreEqual(3, p.Zones.Count);
            Assert.AreEqual(6, p.Objectives.Nodes.Count);
            Assert.AreEqual("auto", p.AiHints[0].Source);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, p.Binaries["terrain/height.r16"]);
            CollectionAssert.IsEmpty(p.Validate());
        }

        [Test]
        public void ValidadorExigeDespliegues()
        {
            var p = Sample();
            p.Zones.RemoveAt(1);
            Assert.That(p.Validate(), Has.Some.Contains("despliegue"));
        }

        [Test]
        public void RutasPeligrosasSeRechazan()
        {
            Assert.IsFalse(MapPackage.IsSafePath("../fuera.json"));
            Assert.IsFalse(MapPackage.IsSafePath("/abs.json"));
            Assert.IsFalse(MapPackage.IsSafePath("C:/x.json"));
            Assert.IsTrue(MapPackage.IsSafePath("gameplay/zones.json"));

            var ms = new MemoryStream();
            using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                using (var w = new StreamWriter(zip.CreateEntry("manifest.json").Open())) w.Write("{}");
                using (var w = new StreamWriter(zip.CreateEntry("../../malo.json").Open())) w.Write("{}");
            }
            ms.Position = 0;
            var errors = new List<string>();
            Assert.IsNull(MapPackage.Load(ms, errors));
            Assert.That(errors, Has.Some.Contains("no permitida"));
        }
    }
}
