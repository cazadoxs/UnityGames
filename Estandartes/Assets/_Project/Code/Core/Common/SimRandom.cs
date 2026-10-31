using System;

namespace Estandartes.Core
{
    /// <summary>
    /// Generador aleatorio determinista (xorshift32 con semilla mezclada). Se crea por entidad, tick y propósito
    /// (Vol. III §17.3) para que el resultado no dependa del orden de ejecución ni de los hilos.
    /// </summary>
    public struct SimRandom
    {
        private uint _state;

        public SimRandom(uint seed)
        {
            _state = seed == 0 ? 0x6E624EB7u : seed;
            NextUInt(); // descarta el primer valor (poco mezclado)
        }

        /// <summary>Semilla reproducible a partir de entidad, tick y propósito.</summary>
        public static SimRandom For(int entityIndex, uint tick, uint purpose)
        {
            uint h = Hash((uint)entityIndex);
            h = Hash(h ^ (tick * 0x9E3779B1u));
            h = Hash(h ^ (purpose * 0x85EBCA77u));
            return new SimRandom(h);
        }

        /// <summary>Finalizador de MurmurHash3: mezcla bien los bits de un entero.</summary>
        public static uint Hash(uint x)
        {
            x ^= x >> 16;
            x *= 0x85EBCA6Bu;
            x ^= x >> 13;
            x *= 0xC2B2AE35u;
            x ^= x >> 16;
            return x;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Flotante uniforme en [0, 1).</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        /// <summary>Entero uniforme en [min, max).</summary>
        public int Range(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min));
        }

        public bool Chance(float probability) => NextFloat() < probability;

        /// <summary>Normal estándar (Box-Muller).</summary>
        public float NextGaussian()
        {
            float u1 = 1f - NextFloat(); // (0, 1]
            float u2 = NextFloat();
            return MathF.Sqrt(-2f * MathF.Log(u1)) * MathF.Cos(2f * MathF.PI * u2);
        }

        /// <summary>Log-normal: exp(mu + sigma·N(0,1)).</summary>
        public float LogNormal(float mu, float sigma) => MathF.Exp(mu + sigma * NextGaussian());

        /// <summary>Punto uniforme dentro de un círculo.</summary>
        public Vec2 InsideCircle(float radius)
        {
            float r = radius * MathF.Sqrt(NextFloat());
            float a = 2f * MathF.PI * NextFloat();
            return new Vec2(r * MathF.Cos(a), r * MathF.Sin(a));
        }
    }
}
