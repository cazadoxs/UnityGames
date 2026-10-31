using System.Collections.Generic;
using Estandartes.Core.Content;
using Estandartes.Core.Mods;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class ModLoaderTests
    {
        private static MemoryModSource Base() => new MemoryModSource("base")
            .Add("mod.json", @"{ ""id"": ""estandartes"", ""name"": ""Base"", ""version"": ""0.1.0"" }")
            .Add("data/weapons/espada.json", @"{ ""id"": ""estandartes:weapon/espada"", ""family"": ""Blade"", ""damageType"": ""Cut"", ""baseDamage"": 40, ""penetration"": 0.15, ""range"": 1.0 }")
            .Add("data/weapons/arco_largo.json", @"{ ""id"": ""estandartes:weapon/arco_largo"", ""family"": ""Bow"", ""damageType"": ""Pierce"", ""baseDamage"": 55, ""penetration"": 0.35, ""range"": 250, ""ranged"": true }")
            .Add("data/units/hombre_de_armas.json", @"{ ""id"": ""estandartes:unit/hombre_de_armas"", ""archetype"": ""ShockInfantry"", ""capabilities"": [""Shield""],
                ""weapons"": [""estandartes:weapon/espada""], ""armor"": { ""cut"": 0.6, ""pierce"": 0.45, ""blunt"": 0.3 }, ""squadSize"": 60, ""cost"": 12 }");

        private static ModManifest M(string json) => Json.Deserialize<ModManifest>(json);

        [Test]
        public void VersionesYRangos()
        {
            Assert.IsTrue(VersionRange.Parse(">=1.1.0 <2.0.0").Satisfies(SemVer.Parse("1.5.3")));
            Assert.IsFalse(VersionRange.Parse(">=1.1.0 <2.0.0").Satisfies(SemVer.Parse("2.0.0")));
            Assert.IsTrue(VersionRange.Parse("^1.2.0").Satisfies(SemVer.Parse("1.9.0")));
            Assert.IsFalse(VersionRange.Parse("~1.2.0").Satisfies(SemVer.Parse("1.3.0")));
            Assert.IsTrue(VersionRange.Parse("*").Satisfies(SemVer.Parse("9.9.9")));
        }

        [Test]
        public void ContentIdValido()
        {
            Assert.IsTrue(ContentId.TryParse("almogavares:unit/almogavar", out var id));
            Assert.AreEqual("almogavares", id.Namespace);
            Assert.IsFalse(ContentId.TryParse("Almogavares:unit/x", out _));
            Assert.IsFalse(ContentId.TryParse("a:unit/", out _));
            Assert.IsFalse(ContentId.TryParse("sin_tipo", out _));
        }

        [Test]
        public void CargaElJuegoBaseComoUnModMas()
        {
            var r = GameContent.CreateLoader().Load(new IModSource[] { Base() }, GameContent.GameVersion);
            Assert.AreEqual(0, r.Messages.FindAll(m => m.Severity == LoadSeverity.Error).Count, string.Join("\n", r.Messages));
            Assert.AreEqual(3, r.Registry.Count);
            Assert.AreEqual(12, r.Registry.Get<UnitDefinition>("estandartes:unit/hombre_de_armas").Cost);
            Assert.IsFalse(r.AnyRuleChanges);
        }

        [Test]
        public void ModConUnidadNuevaParcheYDependencia()
        {
            var mod = new MemoryModSource("almogavares")
                .Add("mod.json", @"{ ""id"": ""almogavares"", ""name"": ""Almogávares"", ""version"": ""1.0.0"", ""gameVersion"": "">=0.1.0 <1.0.0"",
                                     ""dependencies"": [ { ""id"": ""estandartes"", ""version"": "">=0.1.0"" } ] }")
                .Add("data/weapons/azcona.json", @"{ ""id"": ""almogavares:weapon/azcona"", ""family"": ""Thrown"", ""damageType"": ""Pierce"", ""baseDamage"": 45, ""range"": 30, ""ranged"": true }")
                .Add("data/units/almogavar.json", @"{ ""id"": ""almogavares:unit/almogavar"", ""archetype"": ""Skirmisher"", ""capabilities"": [""Javelins""],
                    ""weapons"": [""almogavares:weapon/azcona"", ""estandartes:weapon/espada""], ""squadSize"": 40, ""cost"": 9 }")
                .Add("data/weapons/arco.patch.json", @"{ ""patch"": ""estandartes:weapon/arco_largo"", ""set"": { ""baseDamage"": 50, ""range"": 230 } }")
                .Add("localization/es.json", @"{ ""unit.almogavar"": ""Almogávar"" }");
            var r = GameContent.CreateLoader().Load(new IModSource[] { mod, Base() }, GameContent.GameVersion);
            Assert.AreEqual(0, r.Messages.FindAll(m => m.Severity == LoadSeverity.Error).Count, string.Join("\n", r.Messages));
            Assert.AreEqual("estandartes", r.Loaded[0].Manifest.Id); // el juego base va primero aunque llegue después
            var arco = r.Registry.Get<WeaponDefinition>("estandartes:weapon/arco_largo");
            Assert.AreEqual(50f, arco.BaseDamage);
            Assert.AreEqual(0.35f, arco.Penetration, 1e-5); // campo no tocado se conserva
            CollectionAssert.Contains(r.Registry.GetEntry(arco.Id).ModifiedBy, "almogavares");
            Assert.IsTrue(r.AnyRuleChanges);
            Assert.AreEqual("Almogávar", r.Localization["es"]["unit.almogavar"]);
        }

        [Test]
        public void ModConErroresSeDesactivaSinRomperNada()
        {
            var roto = new MemoryModSource("roto")
                .Add("mod.json", @"{ ""id"": ""roto"", ""name"": ""Roto"", ""version"": ""1.0.0"" }")
                .Add("data/units/x.json", @"{ ""id"": ""roto:unit/x"", ""archetype"": ""Ranged"", ""weapons"": [""roto:weapon/no_existe""] }")
                .Add("data/weapons/y.json", @"{ ""id"": ""roto:weapon/y"", ""family"": ""Bow"", ""damageType"": ""Pierce"", ""baseDamage"": 10 }");
            var r = GameContent.CreateLoader().Load(new IModSource[] { Base(), roto }, GameContent.GameVersion);
            Assert.IsTrue(r.Disabled.ContainsKey("roto"));
            Assert.IsFalse(r.Registry.Contains("roto:weapon/y")); // nada del mod roto se aplica
            Assert.IsTrue(r.Registry.Contains("estandartes:unit/hombre_de_armas"));
            Assert.That(r.Messages.Find(m => m.ModId == "roto" && m.Severity == LoadSeverity.Error).Text, Does.Contain("no_existe"));
        }

        [Test]
        public void CampoMalEscritoEsError()
        {
            var mod = new MemoryModSource("typo")
                .Add("mod.json", @"{ ""id"": ""typo"", ""name"": ""T"", ""version"": ""1.0.0"" }")
                .Add("data/weapons/w.json", @"{ ""id"": ""typo:weapon/w"", ""family"": ""Bow"", ""damageType"": ""Pierce"", ""baseDamge"": 10 }");
            var r = GameContent.CreateLoader().Load(new IModSource[] { Base(), mod }, GameContent.GameVersion);
            Assert.IsTrue(r.Disabled.ContainsKey("typo"));
        }

        [Test]
        public void DependenciasQueFaltanCiclosYVersionDelJuego()
        {
            var a = new MemoryModSource("a").Add("mod.json", @"{ ""id"": ""a"", ""name"": ""A"", ""version"": ""1.0.0"", ""dependencies"": [ { ""id"": ""falta"" } ] }");
            var b = new MemoryModSource("b").Add("mod.json", @"{ ""id"": ""b"", ""name"": ""B"", ""version"": ""1.0.0"", ""loadAfter"": [""c""] }");
            var c = new MemoryModSource("c").Add("mod.json", @"{ ""id"": ""c"", ""name"": ""C"", ""version"": ""1.0.0"", ""loadAfter"": [""b""] }");
            var d = new MemoryModSource("d").Add("mod.json", @"{ ""id"": ""d"", ""name"": ""D"", ""version"": ""1.0.0"", ""gameVersion"": "">=2.0.0"" }");
            var e = new MemoryModSource("e").Add("mod.json", @"{ ""id"": ""e"", ""name"": ""E"", ""version"": ""1.0.0"", ""dependencies"": [ { ""id"": ""a"" } ] }");
            var r = GameContent.CreateLoader().Load(new IModSource[] { Base(), a, b, c, d, e }, GameContent.GameVersion);
            foreach (var id in new[] { "a", "b", "c", "d", "e" }) Assert.IsTrue(r.Disabled.ContainsKey(id), id);
            Assert.AreEqual(1, r.Loaded.Count);
        }

        [Test]
        public void OrdenDelUsuarioYReemplazo()
        {
            string W(string mod, float dmg) => $@"{{ ""id"": ""estandartes:weapon/espada"", ""family"": ""Blade"", ""damageType"": ""Cut"", ""baseDamage"": {dmg} }}";
            var x = new MemoryModSource("x").Add("mod.json", @"{ ""id"": ""x"", ""name"": ""X"", ""version"": ""1.0.0"" }").Add("data/weapons/e.json", W("x", 41));
            var y = new MemoryModSource("y").Add("mod.json", @"{ ""id"": ""y"", ""name"": ""Y"", ""version"": ""1.0.0"" }").Add("data/weapons/e.json", W("y", 42));
            var r1 = GameContent.CreateLoader().Load(new IModSource[] { Base(), x, y }, GameContent.GameVersion, new List<string> { "y", "x" });
            Assert.AreEqual(41f, r1.Registry.Get<WeaponDefinition>("estandartes:weapon/espada").BaseDamage); // x carga último y gana
            var r2 = GameContent.CreateLoader().Load(new IModSource[] { Base(), x, y }, GameContent.GameVersion, new List<string> { "x", "y" });
            Assert.AreEqual(42f, r2.Registry.Get<WeaponDefinition>("estandartes:weapon/espada").BaseDamage);
        }

        [Test]
        public void RutaFueraDelModSeRechaza()
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "estandartes_mod_test");
            System.IO.Directory.CreateDirectory(dir);
            var src = new DirectoryModSource(dir);
            Assert.Throws<System.UnauthorizedAccessException>(() => src.ReadText("../../etc/passwd"));
        }
    }
}
