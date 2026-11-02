using NUnit.Framework;
using UnityEngine;

namespace Mecanicas.Torretas.Tests
{
    public class MatematicaTorretaTests
    {
        [Test]
        public void Interceptacion_ObjetivoQuieto_TiempoEsDistanciaEntreVelocidad()
        {
            bool ok = MatematicaTorreta.PuntoInterceptacion(Vector3.zero, new Vector3(0, 0, 20), Vector3.zero, 10f, out var p, out var t);
            Assert.IsTrue(ok);
            Assert.AreEqual(2f, t, 1e-3f);
            Assert.AreEqual(20f, p.z, 1e-3f);
        }

        [Test]
        public void Interceptacion_ObjetivoMovil_LaBalaLlegaAlMismoSitioALaVez()
        {
            Vector3 origen = Vector3.zero, pos = new Vector3(10, 0, 20), vel = new Vector3(-3, 0, 1);
            float vBala = 25f;
            bool ok = MatematicaTorreta.PuntoInterceptacion(origen, pos, vel, vBala, out var punto, out var t);
            Assert.IsTrue(ok);
            Assert.AreEqual((pos + vel * t - punto).magnitude, 0f, 1e-3f);
            Assert.AreEqual(vBala * t, (punto - origen).magnitude, 1e-2f);
        }

        [Test]
        public void Interceptacion_ObjetivoMasRapidoQueHuye_DevuelveFalse()
        {
            bool ok = MatematicaTorreta.PuntoInterceptacion(Vector3.zero, new Vector3(0, 0, 10), new Vector3(0, 0, 50), 10f, out _, out _);
            Assert.IsFalse(ok);
        }

        [TestCase(20f, 0f, false)]
        [TestCase(20f, 5f, false)]
        [TestCase(15f, -3f, true)]
        public void Balistica_LaParabolaPasaPorElDestino(float x, float y, bool alto)
        {
            float v = 25f, g = 9.81f;
            Assert.IsTrue(MatematicaTorreta.AnguloBalistico(x, y, v, g, alto, out float ang));
            float rad = ang * Mathf.Deg2Rad;
            float t = x / (v * Mathf.Cos(rad));
            float yLlegada = v * Mathf.Sin(rad) * t - 0.5f * g * t * t;
            Assert.AreEqual(y, yLlegada, 1e-2f);
        }

        [Test]
        public void Balistica_FueraDeAlcance_DevuelveFalse()
        {
            Assert.IsFalse(MatematicaTorreta.AnguloBalistico(1000f, 0f, 10f, 9.81f, false, out _));
        }

        [Test]
        public void AnguloEnEje_GiroADerechaEsPositivo()
        {
            float a = MatematicaTorreta.AnguloEnEje(Vector3.forward, Vector3.right, Vector3.up);
            Assert.AreEqual(90f, a, 1e-3f);
        }

        [Test]
        public void NormalizarAngulo_QuedaEntreMenos180Y180()
        {
            Assert.AreEqual(-90f, MatematicaTorreta.NormalizarAngulo(270f), 1e-3f);
            Assert.AreEqual(10f, MatematicaTorreta.NormalizarAngulo(370f), 1e-3f);
        }
    }
}
