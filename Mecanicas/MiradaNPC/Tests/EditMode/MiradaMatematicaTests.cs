using NUnit.Framework;
using UnityEngine;

namespace Mecanicas.MiradaNPC.Tests
{
    public class MiradaMatematicaTests
    {
        [Test]
        public void Frente_DaCeroGrados()
        {
            MiradaMatematica.DireccionAAngulos(Vector3.forward, out float yaw, out float pitch);
            Assert.AreEqual(0f, yaw, 0.01f);
            Assert.AreEqual(0f, pitch, 0.01f);
        }

        [Test]
        public void Derecha_DaYawPositivo()
        {
            MiradaMatematica.DireccionAAngulos(Vector3.right, out float yaw, out _);
            Assert.AreEqual(90f, yaw, 0.01f);
        }

        [Test]
        public void Arriba45_DaPitchPositivo()
        {
            MiradaMatematica.DireccionAAngulos(new Vector3(0f, 1f, 1f), out _, out float pitch);
            Assert.AreEqual(45f, pitch, 0.01f);
        }

        [Test]
        public void Detras_DaYaw180()
        {
            MiradaMatematica.DireccionAAngulos(Vector3.back, out float yaw, out _);
            Assert.AreEqual(180f, Mathf.Abs(yaw), 0.01f);
        }

        [Test]
        public void RotacionInversa_ApuntaALaDireccion()
        {
            Vector3 dir = new Vector3(0.4f, 0.3f, 0.8f).normalized;
            MiradaMatematica.DireccionAAngulos(dir, out float yaw, out float pitch);
            Vector3 resultado = MiradaMatematica.AngulosARotacion(yaw, pitch) * Vector3.forward;
            Assert.Less(Vector3.Angle(dir, resultado), 0.1f);
        }

        [Test]
        public void Limitar_RecortaCadaLado()
        {
            var l = new LimitesMirada(30f, 60f, 20f, 10f);
            float yaw = -80f, pitch = -50f;
            MiradaMatematica.Limitar(ref yaw, ref pitch, l);
            Assert.AreEqual(-30f, yaw);
            Assert.AreEqual(-10f, pitch);
            yaw = 100f; pitch = 40f;
            MiradaMatematica.Limitar(ref yaw, ref pitch, l);
            Assert.AreEqual(60f, yaw);
            Assert.AreEqual(20f, pitch);
        }

        [Test]
        public void DentroDeLimites_RespetaElMargen()
        {
            var l = new LimitesMirada(70f, 70f, 40f, 40f);
            Assert.IsTrue(MiradaMatematica.DentroDeLimites(69f, 0f, l));
            Assert.IsFalse(MiradaMatematica.DentroDeLimites(75f, 0f, l));
            Assert.IsTrue(MiradaMatematica.DentroDeLimites(75f, 0f, l, 10f));
        }
    }
}
