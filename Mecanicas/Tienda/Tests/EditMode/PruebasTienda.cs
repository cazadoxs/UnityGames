using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Mecanicas.Tienda.Tests
{
    public class PruebasReglas
    {
        [Test] public void ConDineroSuficienteSePuedeComprar()
        { Assert.AreEqual(MotivoCompra.Ok, ReglasCompra.Validar(100, 50, -1, 0, 0)); }

        [Test] public void SinDineroNoSePuede()
        { Assert.AreEqual(MotivoCompra.SinDinero, ReglasCompra.Validar(10, 50, -1, 0, 0)); }

        [Test] public void GratisSinDineroSiSePuede()
        { Assert.AreEqual(MotivoCompra.Ok, ReglasCompra.Validar(0, 0, -1, 0, 0)); }

        [Test] public void SinStockNoSePuede()
        { Assert.AreEqual(MotivoCompra.SinStock, ReglasCompra.Validar(100, 5, 0, 0, 0)); }

        [Test] public void LimiteDeComprasSeRespeta()
        { Assert.AreEqual(MotivoCompra.LimiteAlcanzado, ReglasCompra.Validar(100, 5, -1, 1, 1)); }

        [Test] public void StockInfinitoNoBaja()
        {
            Assert.AreEqual(-1, ReglasCompra.StockTrasCompra(-1));
            Assert.AreEqual(2, ReglasCompra.StockTrasCompra(3));
            Assert.AreEqual(0, ReglasCompra.StockTrasCompra(0));
        }
    }

    public class PruebasGestor
    {
        class MonederoFalso : IMonedero
        {
            public int saldo;
            public int Saldo(string moneda) { return saldo; }
            public bool Gastar(string moneda, int c) { if (saldo < c) return false; saldo -= c; return true; }
            public void Anadir(string moneda, int c) { saldo += c; }
            public event Action<string, int> SaldoCambiado { add { } remove { } }
        }

        class ReceptorFalso : IReceptorCompra
        {
            public bool falla;
            public int entregados;
            public bool PuedeEntregar(ArticuloTienda a, int c) { return true; }
            public bool Entregar(ArticuloTienda a, int c, List<GameObject> creados) { if (falla) return false; entregados += c; return true; }
        }

        class InventarioFalso : IInventarioCompras
        {
            public int capacidad = 1, dentro;
            public bool PuedeAnadir(ArticuloTienda a, int c) { return dentro + c <= capacidad; }
            public bool Anadir(ArticuloTienda a, int c) { dentro += c; return true; }
        }

        GameObject go;
        GestorTienda gestor;
        MonederoFalso monedero;
        ReceptorFalso receptor;

        [SetUp]
        public void Preparar()
        {
            go = new GameObject("Prueba tienda");
            gestor = go.AddComponent<GestorTienda>();
            gestor.buscarInventarioEnEscena = false;
            monedero = new MonederoFalso { saldo = 100 };
            gestor.Monedero = monedero;
            receptor = new ReceptorFalso();
        }

        [TearDown]
        public void Limpiar() { UnityEngine.Object.DestroyImmediate(go); }

        static ArticuloTienda Art(int precio)
        {
            var a = ArticuloTienda.Crear("prueba", "Prueba", precio, null);
            a.modoEntrega = ModoEntrega.Aparecer;
            return a;
        }

        [Test]
        public void CompraCobraYEntrega()
        {
            var r = gestor.Comprar(Art(30), receptor);
            Assert.IsTrue(r.Exito);
            Assert.AreEqual(70, monedero.saldo);
            Assert.AreEqual(1, receptor.entregados);
        }

        [Test]
        public void SiLaEntregaFallaSeDevuelveElDinero()
        {
            receptor.falla = true;
            var r = gestor.Comprar(Art(30), receptor);
            Assert.AreEqual(MotivoCompra.EntregaImposible, r.Motivo);
            Assert.AreEqual(100, monedero.saldo);
        }

        [Test]
        public void SinDineroNoCobraNiEntrega()
        {
            var r = gestor.Comprar(Art(300), receptor);
            Assert.AreEqual(MotivoCompra.SinDinero, r.Motivo);
            Assert.AreEqual(100, monedero.saldo);
            Assert.AreEqual(0, receptor.entregados);
        }

        [Test]
        public void StockSeAgota()
        {
            var a = Art(10);
            a.stockInicial = 1;
            Assert.IsTrue(gestor.Comprar(a, receptor).Exito);
            Assert.AreEqual(MotivoCompra.SinStock, gestor.Comprar(a, receptor).Motivo);
            Assert.AreEqual(90, monedero.saldo);
        }

        [Test]
        public void CompraUnicaSoloUnaVez()
        {
            var a = Art(10);
            a.maxCompras = 1;
            Assert.IsTrue(gestor.Comprar(a, receptor).Exito);
            Assert.AreEqual(MotivoCompra.LimiteAlcanzado, gestor.Comprar(a, receptor).Motivo);
        }

        [Test]
        public void InventarioLlenoNoCobra()
        {
            var inv = new InventarioFalso { capacidad = 1 };
            gestor.Inventario = inv;
            var a = Art(10);
            a.modoEntrega = ModoEntrega.Inventario;
            Assert.IsTrue(gestor.Comprar(a).Exito);
            Assert.AreEqual(MotivoCompra.EntregaImposible, gestor.Comprar(a).Motivo);
            Assert.AreEqual(90, monedero.saldo);
            Assert.AreEqual(1, inv.dentro);
        }

        [Test]
        public void InventarioOAparecerCaeAlSueloSiNoCabe()
        {
            gestor.Inventario = new InventarioFalso { capacidad = 0 };
            var a = Art(10);
            a.modoEntrega = ModoEntrega.InventarioOAparecer;
            var r = gestor.Comprar(a, receptor);
            Assert.IsTrue(r.Exito);
            Assert.IsFalse(r.EntregadoEnInventario);
            Assert.AreEqual(1, receptor.entregados);
        }

        [Test]
        public void SoloEventoNoNecesitaReceptor()
        {
            var a = Art(10);
            a.modoEntrega = ModoEntrega.SoloEvento;
            ArticuloTienda recibido = null;
            gestor.alComprar.AddListener(x => recibido = x);
            Assert.IsTrue(gestor.Comprar(a).Exito);
            Assert.AreSame(a, recibido);
        }

        [Test]
        public void MensajeSinDineroUsaLaPlantilla()
        {
            var r = gestor.Comprar(Art(300), receptor);
            StringAssert.Contains("300", r.Mensaje);
            StringAssert.Contains("100", r.Mensaje);
        }
    }
}
