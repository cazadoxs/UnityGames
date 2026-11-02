using NUnit.Framework;
using UnityEngine;

namespace Mecanicas.Patrulla.Tests
{
    public class RutaDePuntosTests
    {
        GameObject raiz;

        [TearDown]
        public void Limpiar()
        {
            if (raiz != null) Object.DestroyImmediate(raiz);
        }

        RutaDePuntos Crear(bool cerrada, TipoEsquina esquina, float radio, params Vector3[] puntos)
        {
            raiz = new GameObject("Ruta de prueba");
            var ruta = raiz.AddComponent<RutaDePuntos>();
            ruta.cerrada = cerrada;
            ruta.esquinaPorDefecto = esquina;
            ruta.radioPorDefecto = radio;
            foreach (var p in puntos)
            {
                var go = new GameObject("Punto");
                go.transform.SetParent(raiz.transform, false);
                go.transform.position = p;
            }
            ruta.Reconstruir();
            return ruta;
        }

        static readonly Vector3[] Cuadrado =
        {
            new Vector3(0, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 10), new Vector3(0, 0, 10)
        };

        [Test]
        public void RutaAbiertaEnPicoMideLaSumaDeLosTramos()
        {
            var ruta = Crear(false, TipoEsquina.Pico, 0f, Cuadrado);
            Assert.AreEqual(30f, ruta.Longitud, 1e-3f);
            Assert.AreEqual(0f, ruta.DistanciaDePunto(0), 1e-4f);
            Assert.AreEqual(10f, ruta.DistanciaDePunto(1), 1e-3f);
            Assert.AreEqual(30f, ruta.DistanciaDePunto(3), 1e-3f);
        }

        [Test]
        public void CircuitoEnPicoMideElPerimetro()
        {
            var ruta = Crear(true, TipoEsquina.Pico, 0f, Cuadrado);
            Assert.AreEqual(40f, ruta.Longitud, 1e-3f);
        }

        [Test]
        public void EsquinasSuavesAcortanLaRutaYNoPasanPorElVertice()
        {
            float pico = Crear(true, TipoEsquina.Pico, 0f, Cuadrado).Longitud;
            Object.DestroyImmediate(raiz);
            var ruta = Crear(true, TipoEsquina.Suave, 3f, Cuadrado);
            Assert.Less(ruta.Longitud, pico);
            // Con radio 3 la curva pasa lejos del vértice (10,0,0).
            float masCerca = float.MaxValue;
            foreach (var m in ruta.Muestras) masCerca = Mathf.Min(masCerca, Vector3.Distance(m, new Vector3(10, 0, 0)));
            Assert.Greater(masCerca, 0.5f);
        }

        [Test]
        public void ElRadioSeRecortaALaMitadDelTramo()
        {
            // Tramos de 10 m: un radio de 50 se recorta a 5 y la curva sigue dentro del cuadrado.
            var ruta = Crear(true, TipoEsquina.Suave, 50f, Cuadrado);
            Assert.Greater(ruta.Longitud, 0f);
            Assert.Less(ruta.Longitud, 40f);
            foreach (var m in ruta.Muestras)
            {
                Assert.GreaterOrEqual(m.x, -1e-3f); Assert.LessOrEqual(m.x, 10.001f);
                Assert.GreaterOrEqual(m.z, -1e-3f); Assert.LessOrEqual(m.z, 10.001f);
            }
        }

        [Test]
        public void PuntoConEsquinaPropiaGanaALaDeLaRuta()
        {
            var ruta = Crear(true, TipoEsquina.Suave, 3f, Cuadrado);
            var ajustes = ruta.ObtenerPunto(1).gameObject.AddComponent<PuntoDeRuta>();
            ajustes.usarEsquinaPropia = true;
            ajustes.esquina = TipoEsquina.Pico;
            ruta.Reconstruir();
            Assert.AreEqual(TipoEsquina.Pico, ruta.EsquinaDe(1));
            // Un punto en pico se pisa exactamente.
            Vector3 p = ruta.PosicionEn(ruta.DistanciaDePunto(1));
            Assert.Less(Vector3.Distance(p, new Vector3(10, 0, 0)), 1e-3f);
        }

        [Test]
        public void EvaluarDaLaVueltaEnCircuitosYRecortaEnAbiertas()
        {
            var circuito = Crear(true, TipoEsquina.Pico, 0f, Cuadrado);
            Vector3 a = circuito.PosicionEn(5f), b = circuito.PosicionEn(5f + circuito.Longitud);
            Assert.Less(Vector3.Distance(a, b), 1e-3f);
            Object.DestroyImmediate(raiz);

            var abierta = Crear(false, TipoEsquina.Pico, 0f, Cuadrado);
            Assert.Less(Vector3.Distance(abierta.PosicionEn(999f), new Vector3(0, 0, 10)), 1e-3f);
            Assert.Less(Vector3.Distance(abierta.PosicionEn(-5f), Vector3.zero), 1e-3f);
        }

        [Test]
        public void DireccionSigueElTramo()
        {
            var ruta = Crear(false, TipoEsquina.Pico, 0f, Cuadrado);
            Assert.Greater(Vector3.Dot(ruta.DireccionEn(5f), Vector3.right), 0.999f);
            Assert.Greater(Vector3.Dot(ruta.DireccionEn(15f), Vector3.forward), 0.999f);
        }

        [Test]
        public void DistanciaMasCercanaEncuentraElTramo()
        {
            var ruta = Crear(false, TipoEsquina.Pico, 0f, Cuadrado);
            Assert.AreEqual(4f, ruta.DistanciaMasCercana(new Vector3(4, 0, -3)), 1e-3f);
            Assert.AreEqual(2, ruta.PuntoMasCercano(new Vector3(11, 0, 11)));
        }

        [Test]
        public void BloqueoPorSentido()
        {
            var go = new GameObject("Punto suelto");
            try
            {
                var p = go.AddComponent<PuntoDeRuta>();
                p.bloqueado = true;
                p.sentidoBloqueo = PuntoDeRuta.SentidoBloqueo.SoloIda;
                Assert.IsTrue(p.EstaBloqueadoPara(1));
                Assert.IsFalse(p.EstaBloqueadoPara(-1));
                p.bloqueado = false;
                Assert.IsFalse(p.EstaBloqueadoPara(1));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
