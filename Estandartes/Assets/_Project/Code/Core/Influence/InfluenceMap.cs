using System;

namespace Estandartes.Core.Influence
{
    /// <summary>
    /// Capa de mapa de influencia (Vol. III §7): rejilla de baja resolución con estampado radial,
    /// memoria con decaimiento y difuminado, y consultas para la IA de alto nivel.
    /// </summary>
    public sealed class InfluenceMap
    {
        public readonly int Width, Height;
        public readonly float CellSize;
        public readonly float OriginX, OriginY;
        public float[] Values;
        private float[] _stamp;
        private float[] _scratch;

        public InfluenceMap(float originX, float originY, float sizeX, float sizeY, float cellSize = 8f)
        {
            CellSize = cellSize;
            OriginX = originX;
            OriginY = originY;
            Width = Math.Max(1, (int)Math.Ceiling(sizeX / cellSize));
            Height = Math.Max(1, (int)Math.Ceiling(sizeY / cellSize));
            Values = new float[Width * Height];
            _stamp = new float[Width * Height];
            _scratch = new float[Width * Height];
        }

        public int Index(int x, int y) => y * Width + x;

        public Vec2 CellCenter(int x, int y) => new Vec2(OriginX + (x + 0.5f) * CellSize, OriginY + (y + 0.5f) * CellSize);

        public float Sample(Vec2 p)
        {
            int x = SimMath.Clamp((int)((p.X - OriginX) / CellSize), 0, Width - 1);
            int y = SimMath.Clamp((int)((p.Y - OriginY) / CellSize), 0, Height - 1);
            return Values[Index(x, y)];
        }

        public void BeginStamp() => Array.Clear(_stamp, 0, _stamp.Length);

        /// <summary>Estampa una escuadra: I(c) += S · max(0, 1 − d/R)^k.</summary>
        public void Stamp(Vec2 center, float strength, float radius, float k = 1f)
        {
            int minX = SimMath.Clamp((int)((center.X - radius - OriginX) / CellSize), 0, Width - 1);
            int maxX = SimMath.Clamp((int)((center.X + radius - OriginX) / CellSize), 0, Width - 1);
            int minY = SimMath.Clamp((int)((center.Y - radius - OriginY) / CellSize), 0, Height - 1);
            int maxY = SimMath.Clamp((int)((center.Y + radius - OriginY) / CellSize), 0, Height - 1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float d = Vec2.Distance(CellCenter(x, y), center);
                    if (d >= radius) continue;
                    float f = 1f - d / radius;
                    _stamp[Index(x, y)] += strength * (k == 1f ? f : MathF.Pow(f, k));
                }
        }

        /// <summary>I_t = max(estampado, α · difuminado(I_{t−1})).</summary>
        public void EndStamp(float alpha)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    float sum = 0f; int n = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                            sum += Values[Index(nx, ny)]; n++;
                        }
                    _scratch[Index(x, y)] = Math.Max(_stamp[Index(x, y)], alpha * sum / n);
                }
            (Values, _scratch) = (_scratch, Values);
        }

        /// <summary>Tensión = propia + enemiga.</summary>
        public static float Tension(float own, float enemy) => own + enemy;

        /// <summary>Vulnerabilidad = tensión − |propia − enemiga|: frentes igualados.</summary>
        public static float Vulnerability(float own, float enemy) => own + enemy - Math.Abs(own - enemy);

        /// <summary>Gradiente (dirección de mayor aumento) en un punto, por diferencias centrales.</summary>
        public Vec2 Gradient(Vec2 p)
        {
            float h = CellSize;
            float dx = Sample(p + new Vec2(h, 0)) - Sample(p - new Vec2(h, 0));
            float dy = Sample(p + new Vec2(0, h)) - Sample(p - new Vec2(0, h));
            return new Vec2(dx, dy);
        }

        /// <summary>Celda de menor valor a lo largo de un segmento (para "flanco débil").</summary>
        public Vec2 MinAlong(Vec2 a, Vec2 b, int samples = 32)
        {
            Vec2 best = a; float bestV = float.MaxValue;
            for (int i = 0; i <= samples; i++)
            {
                var p = Vec2.Lerp(a, b, i / (float)samples);
                float v = Sample(p);
                if (v < bestV) { bestV = v; best = p; }
            }
            return best;
        }
    }
}
