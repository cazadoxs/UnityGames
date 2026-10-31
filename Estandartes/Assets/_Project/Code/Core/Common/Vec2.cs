using System;

namespace Estandartes.Core
{
    /// <summary>Vector 2D blittable (plano XZ del mundo). Sin dependencias de Unity para poder usarse en Core y desde Burst.</summary>
    [Serializable]
    public struct Vec2 : IEquatable<Vec2>
    {
        public float X;
        public float Y;

        public Vec2(float x, float y) { X = x; Y = y; }

        public static readonly Vec2 Zero = new Vec2(0f, 0f);
        public static readonly Vec2 Forward = new Vec2(0f, 1f);

        [Newtonsoft.Json.JsonIgnore] public float LengthSquared => X * X + Y * Y;
        [Newtonsoft.Json.JsonIgnore] public float Length => MathF.Sqrt(LengthSquared);

        [Newtonsoft.Json.JsonIgnore] public Vec2 Normalized
        {
            get
            {
                float l = Length;
                return l > 1e-6f ? new Vec2(X / l, Y / l) : Zero;
            }
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(float s, Vec2 a) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.X / s, a.Y / s);

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static float Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static float DistanceSquared(Vec2 a, Vec2 b) => (a - b).LengthSquared;
        public static Vec2 Lerp(Vec2 a, Vec2 b, float t) => a + (b - a) * t;

        /// <summary>Rota un desplazamiento local (X lateral, Y hacia delante) según una orientación mundial normalizada.</summary>
        public static Vec2 RotateLocal(Vec2 local, Vec2 facing)
        {
            // facing = eje "delante" del sistema local; derecha = (facing.Y, -facing.X)
            var right = new Vec2(facing.Y, -facing.X);
            return right * local.X + facing * local.Y;
        }

        /// <summary>Inversa de <see cref="RotateLocal"/>: pasa un vector mundial al sistema local.</summary>
        public static Vec2 ToLocal(Vec2 world, Vec2 facing)
        {
            var right = new Vec2(facing.Y, -facing.X);
            return new Vec2(Dot(world, right), Dot(world, facing));
        }

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}
