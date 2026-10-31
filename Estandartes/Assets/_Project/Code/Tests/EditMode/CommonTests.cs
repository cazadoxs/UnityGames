using Estandartes.Core;
using NUnit.Framework;

namespace Estandartes.Tests
{
    public class CommonTests
    {
        [Test]
        public void SimRandom_EsDeterminista()
        {
            var a = SimRandom.For(42, 1000, 7);
            var b = SimRandom.For(42, 1000, 7);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void SimRandom_CambiaConPropositoYTick()
        {
            Assert.AreNotEqual(SimRandom.For(1, 1, 1).NextUInt(), SimRandom.For(1, 1, 2).NextUInt());
            Assert.AreNotEqual(SimRandom.For(1, 1, 1).NextUInt(), SimRandom.For(1, 2, 1).NextUInt());
        }

        [Test]
        public void SimRandom_FloatEnRangoYMediaRazonable()
        {
            var r = new SimRandom(123);
            double sum = 0;
            for (int i = 0; i < 20000; i++)
            {
                float f = r.NextFloat();
                Assert.That(f, Is.InRange(0f, 0.9999999f));
                sum += f;
            }
            Assert.That(sum / 20000, Is.EqualTo(0.5).Within(0.02));
        }

        [Test]
        public void Vec2_RotacionLocalIdaYVuelta()
        {
            var facing = new Vec2(1, 1).Normalized;
            var local = new Vec2(3, -2);
            var world = Vec2.RotateLocal(local, facing);
            var back = Vec2.ToLocal(world, facing);
            Assert.That(back.X, Is.EqualTo(local.X).Within(1e-4));
            Assert.That(back.Y, Is.EqualTo(local.Y).Within(1e-4));
        }
    }
}
