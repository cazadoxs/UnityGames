using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Texturas procedurales para la montaña: caras de pilas de resmas (envoltorios blancos, kraft y de color,
    /// con su ranura entre paquetes) y su mapa de normales. Una cara del bloque = 2 × 2 m.
    /// </summary>
    public static class PaperTextures
    {
        public static Material MakeReamMaterial(int seed, out Texture2D albedo)
        {
            const int N = 512;
            var rnd = new System.Random(seed);
            var col = new Color[N * N];
            var h = new float[N * N];
            float pxPerM = N / 2f;
            float reamH = 0.05f * pxPerM;      // 12,8 px
            float reamL = 0.297f * pxPerM;     // 76 px
            int rows = Mathf.CeilToInt(N / reamH);
            for (int i = 0; i < col.Length; i++) { col[i] = new Color(0.8f, 0.79f, 0.76f, 1); h[i] = 0.5f; }
            for (int r = 0; r < rows; r++)
            {
                int y0 = Mathf.RoundToInt(r * reamH), y1 = Mathf.RoundToInt((r + 1) * reamH);
                float offset = (float)rnd.NextDouble() * reamL;
                bool looseRow = rnd.NextDouble() < 0.07;
                float x = -offset;
                while (x < N)
                {
                    float rw = reamL * (0.85f + (float)rnd.NextDouble() * 0.3f);
                    double t = rnd.NextDouble();
                    Color baseC;
                    if (looseRow) baseC = new Color(0.93f, 0.92f, 0.89f);
                    else if (t < 0.68) { float w = 0.9f + (float)rnd.NextDouble() * 0.07f; baseC = new Color(w, w * 0.995f, w * 0.975f); }
                    else if (t < 0.84) baseC = new Color(0.66f, 0.53f, 0.38f) * (0.9f + (float)rnd.NextDouble() * 0.15f);
                    else if (t < 0.94)
                    {
                        Color[] pastel = { new Color(0.78f, 0.86f, 0.93f), new Color(0.95f, 0.83f, 0.85f), new Color(0.95f, 0.92f, 0.7f), new Color(0.8f, 0.92f, 0.8f) };
                        baseC = pastel[rnd.Next(pastel.Length)];
                    }
                    else baseC = new Color(0.9f, 0.9f, 0.88f);
                    bool band = !looseRow && rnd.NextDouble() < 0.18;
                    Color bandC = rnd.NextDouble() < 0.5 ? new Color(0.2f, 0.35f, 0.62f) : new Color(0.72f, 0.18f, 0.16f);
                    float tilt = ((float)rnd.NextDouble() - 0.5f) * 1.5f;
                    int xa = Mathf.RoundToInt(x), xb = Mathf.RoundToInt(x + rw);
                    x += rw;
                    for (int px = xa; px < xb; px++)
                    {
                        if (px < 0 || px >= N) continue;
                        for (int py = y0; py < y1 && py < N; py++)
                        {
                            int i = py * N + px;
                            float ly = (py - y0) / (float)(y1 - y0);
                            bool gap = py == y0 || px == xa;
                            float n = (float)rnd.NextDouble() * 0.035f;
                            Color c = baseC * (0.97f + n);
                            if (looseRow) c *= ((py + (int)(px * 0.02f * tilt)) % 2 == 0) ? 1f : 0.86f;
                            if (band && px > xa + (xb - xa) * 0.35f && px < xa + (xb - xa) * 0.5f) c = Color.Lerp(c, bandC, 0.85f);
                            // las esquinas del paquete se redondean y oscurecen un poco
                            float edge = Mathf.Min(ly, 1 - ly);
                            c *= Mathf.Lerp(0.82f, 1f, Mathf.Clamp01(edge * 5f));
                            if (gap) c *= 0.62f;
                            c.a = 1;
                            col[i] = c;
                            h[i] = gap ? 0f : Mathf.Clamp01(edge * 6f) * 0.8f + 0.2f + (looseRow ? (py % 2) * 0.1f : 0);
                        }
                    }
                }
            }
            albedo = new Texture2D(N, N, TextureFormat.RGBA32, true, false) { name = "ResmasColor", wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
            albedo.SetPixels(col); albedo.Apply(true, false);

            // Normales a partir de la altura, en formato DXT5nm (x en alfa, y en verde), que es lo que URP espera en PC
            var nrm = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float hl = h[y * N + (x + N - 1) % N], hr = h[y * N + (x + 1) % N];
                    float hd = h[((y + N - 1) % N) * N + x], hu = h[((y + 1) % N) * N + x];
                    var v = new Vector3((hl - hr) * 2.2f, (hd - hu) * 2.2f, 1f).normalized;
                    nrm[y * N + x] = new Color(1f, v.y * 0.5f + 0.5f, 0f, v.x * 0.5f + 0.5f);
                }
            var normal = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "ResmasNormal", wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
            normal.SetPixels(nrm); normal.Apply(true, false);

            var baseNormal = Resources.Load<Material>("M_ImprentaNormal");
            var m = baseNormal != null ? new Material(baseNormal) : new Material(Build.Mat(Color.white, 0.18f));
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", normal);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_BumpScale", 1.2f);
            m.SetFloat("_Smoothness", 0.22f);
            m.enableInstancing = true;
            return m;
        }

        /// <summary>Hoja suelta: folio con texto gris tenue para que se lea «papel escrito» de lejos.</summary>
        public static Material MakeSheetMaterial()
        {
            const int W = 128, H = 180;
            var rnd = new System.Random(5);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float c = 0.95f + (float)rnd.NextDouble() * 0.02f;
                    bool text = x > 12 && x < W - 12 && y > 18 && y < H - 22 && (y % 7) < 2 && rnd.NextDouble() < 0.55;
                    if (text) c *= 0.55f;
                    px[y * W + x] = new Color(c, c, c * 0.985f, 1);
                }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true, false) { name = "Folio", wrapMode = TextureWrapMode.Clamp };
            t.SetPixels(px); t.Apply(true, false);
            var m = new Material(Build.Mat(Color.white, 0.2f));
            m.SetTexture("_BaseMap", t);
            m.SetColor("_BaseColor", Color.white);
            m.enableInstancing = true;
            return m;
        }

        /// <summary>
        /// Montón de papel visto de cerca: cientos de folios superpuestos en todas direcciones, algunos escritos,
        /// algunos kraft o de color, con sombra bajo cada borde. Repetible (los folios dan la vuelta por los lados).
        /// Una repetición = 3 × 3 m.
        /// </summary>
        public static Material MakePileMaterial(int seed)
        {
            const int N = 1024;
            var rnd = new System.Random(seed);
            var col = new Color[N * N];
            var h = new float[N * N];
            for (int i = 0; i < col.Length; i++) { col[i] = new Color(0.72f, 0.71f, 0.67f, 1); h[i] = 0; }
            float pxPerM = N / 4.5f;
            float sw = 0.21f * pxPerM, sl = 0.297f * pxPerM;
            int count = 1000;
            for (int k = 0; k < count; k++)
            {
                float cx = (float)rnd.NextDouble() * N, cy = (float)rnd.NextDouble() * N;
                float ang = (float)rnd.NextDouble() * Mathf.PI;
                float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
                double t = rnd.NextDouble();
                Color baseC = t < 0.62 ? new Color(0.95f, 0.94f, 0.91f) : t < 0.8 ? new Color(0.92f, 0.9f, 0.84f) : t < 0.9 ? new Color(0.78f, 0.66f, 0.5f)
                    : t < 0.95 ? new Color(0.86f, 0.9f, 0.95f) : new Color(0.96f, 0.92f, 0.74f);
                bool written = rnd.NextDouble() < 0.55;
                float crumple = (float)rnd.NextDouble() * 0.12f;
                float layer = k / (float)count;
                int r = Mathf.CeilToInt(Mathf.Sqrt(sw * sw + sl * sl) / 2) + 2;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        float u = dx * ca + dy * sa, v = -dx * sa + dy * ca;
                        float au = Mathf.Abs(u) / (sw / 2), av = Mathf.Abs(v) / (sl / 2);
                        int x = ((int)cx + dx + N) % N, y = ((int)cy + dy + N) % N;
                        int i = y * N + x;
                        if (au > 1 || av > 1)
                        {
                            // sombra que proyecta el folio sobre lo de debajo
                            if (au < 1.12f && av < 1.08f && h[i] < layer) col[i] *= 0.93f;
                            continue;
                        }
                        float edge = Mathf.Min(1 - au, 1 - av);
                        Color c = baseC * (0.94f + (float)rnd.NextDouble() * 0.04f);
                        // pliegues suaves
                        c *= 1f - crumple * Mathf.Abs(Mathf.Sin(u * 0.08f + k) * Mathf.Sin(v * 0.05f + k * 0.3f));
                        if (written && av < 0.8f && au < 0.78f)
                        {
                            int line = Mathf.FloorToInt((v + sl / 2) / 4.2f);
                            float inLine = ((v + sl / 2) % 4.2f);
                            if (inLine < 1.4f && (Phrases.Hash((uint)(k * 131 + line * 7 + (int)((u + sw / 2) / 5))) & 3) != 0) c *= 0.62f;
                        }
                        if (edge < 0.03f) c *= 0.88f;
                        c.a = 1;
                        col[i] = c;
                        h[i] = layer;
                    }
            }
            // oclusión: las zonas más hundidas (folios de debajo) más oscuras
            for (int i = 0; i < col.Length; i++) col[i] *= Mathf.Lerp(0.9f, 1.04f, h[i]);
            var albedo = new Texture2D(N, N, TextureFormat.RGBA32, true, false) { name = "MontonPapel", wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
            albedo.SetPixels(col); albedo.Apply(true, false);
            var nrm = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float hl = h[y * N + (x + N - 1) % N], hr = h[y * N + (x + 1) % N];
                    float hd = h[((y + N - 1) % N) * N + x], hu = h[((y + 1) % N) * N + x];
                    float sx = Mathf.Clamp(hl - hr, -0.02f, 0.02f) * 40f, sy = Mathf.Clamp(hd - hu, -0.02f, 0.02f) * 40f;
                    var v = new Vector3(sx, sy, 1f).normalized;
                    nrm[y * N + x] = new Color(1f, v.y * 0.5f + 0.5f, 0f, v.x * 0.5f + 0.5f);
                }
            var normal = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "MontonPapelNormal", wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
            normal.SetPixels(nrm); normal.Apply(true, false);
            var baseNormal = Resources.Load<Material>("M_ImprentaNormal");
            var m = baseNormal != null ? new Material(baseNormal) : new Material(Build.Mat(Color.white, 0.18f));
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BumpMap", normal);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_BumpScale", 0.55f);
            m.SetFloat("_Smoothness", 0.12f);
            return m;
        }
    }
}
