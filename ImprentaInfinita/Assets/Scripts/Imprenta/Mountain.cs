using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Imprenta
{
    /// <summary>
    /// La Montaña (doc 4.1 y 4.6): un campo de alturas de 25 m hecho de columnas de resmas, dibujado con
    /// instancias en GPU. Cada hoja que coges agranda el semicírculo libre frente al puesto de trabajo.
    /// La física de hojas sueltas y el sistema de millones de hojas son el riesgo técnico a probar después.
    /// </summary>
    public class Mountain : MonoBehaviour
    {
        public const float FrontZ = 22f;
                const float XMin = -58f, XMax = 58f, ZMax = 228f;
        static readonly Vector2 Center = new Vector2(0, FrontZ);

        // La montaña es un relieve continuo cubierto de capas de papel (textura) y, encima, miles de folios
        // sueltos de verdad, curvados y con texto, que es lo que se ve de cerca. Se trocea en parcelas de 16 m
        // para recalcular solo las que cambian al ganar terreno y para darle colisión real.
        const int Chunk = 16;
        const float Step = 1f;
        Material pileMat;
        readonly Dictionary<Vector2Int, (Mesh mesh, MeshCollider col, float sig)> chunks = new Dictionary<Vector2Int, (Mesh, MeshCollider, float)>();
        // Hojas de verdad: cada parcela guarda las matrices de sus folios (finos y, para lejos, menos y más grandes)
        class SheetChunk
        {
            public readonly List<Matrix4x4>[] fine = { new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>() };
            public readonly List<Matrix4x4>[] coarse = { new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>() };
            public readonly List<uint>[] ids = { new List<uint>(), new List<uint>(), new List<uint>() };
            // segunda capa de folios: solo se dibuja cerca, de lejos basta la primera
            public readonly List<Matrix4x4>[] top = { new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>() };
            public readonly List<uint>[] topIds = { new List<uint>(), new List<uint>(), new List<uint>() };
            public Bounds bounds;
            public int Count => fine[0].Count + fine[1].Count + fine[2].Count + top[0].Count + top[1].Count + top[2].Count;
        }
        readonly Dictionary<Vector2Int, SheetChunk> sheetChunks = new Dictionary<Vector2Int, SheetChunk>();
        Material[] sheetMats;
        float builtRadius = -1f, builtBand = -1f;
        float radius, band, lastRebuild = -10f;
        readonly Plane[] planes = new Plane[6];
        const float FineCell = 0.21f, CoarseCell = 0.5f, CoarseScale = 2.7f;
        const int Layers = 2;   // dos capas de folios: no se ve nada debajo
        const float NearDist = 12f, TopDist = 24f, MidDist = 32f;
        public int SheetTotal { get; private set; }
        public int SheetsActivated { get; private set; }
        // folios que ya se han convertido en objetos físicos: no vuelven a dibujarse como instancia
        readonly HashSet<uint> activated = new HashSet<uint>();
        float activateTimer;
        public int SheetsDrawn { get; private set; }

        public float Radius => radius;
        public float Band => band;
        Vector2 CenterNow => new Vector2(0, FrontZ + band);

        void Awake()
        {
            // debajo de las hojas, solo un relleno liso color papel en sombra (lo que asoma entre hoja y hoja)
            pileMat = Build.Mat(Build.Hex("#8a8274"), 0.05f);
            sheetMats = new[] { Paper.BlankMat, Paper.WrittenMat, Paper.KraftMat };
        }

        /// <summary>Radio del semicírculo libre a partir de los m² ganados a la montaña.</summary>
        public static float RadiusFor(double freeArea)
        {
            Progress.Shape(freeArea, out float r, out _);
            return r;
        }

        public void SetArea(double freeArea)
        {
            Progress.Shape(freeArea, out radius, out band);
            bool changed = Mathf.Abs(radius - builtRadius) > 0.04f || Mathf.Abs(band - builtBand) > 0.04f;
            if (changed && (builtRadius < 0 || Time.time - lastRebuild > 1f || Mathf.Abs(radius - builtRadius) > 0.6f && Time.time - lastRebuild > 0.1f)) Rebuild();
        }

        static float BaseHeight(float x, float z)
        {
            if (z < FrontZ || z > ZMax || x < XMin || x > XMax) return 0;
            float d = z - FrontZ;
            float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(d / 38f));
            float back = Mathf.SmoothStep(0, 1, Mathf.Clamp01((ZMax - z) / 30f));
            float side = Mathf.SmoothStep(0, 1, Mathf.Clamp01((XMax - Mathf.Abs(x)) / 22f));
            float n = Mathf.PerlinNoise(x * 0.06f + 13f, z * 0.06f + 7f) * 5f + Mathf.PerlinNoise(x * 0.23f, z * 0.23f) * 1.4f
                      + Mathf.PerlinNoise(x * 0.9f + 3f, z * 0.9f) * 0.25f;
            return 1.3f + 21f * rise * back * (0.45f + 0.55f * side) + n * (0.3f + rise);
        }

        /// <summary>Altura de la montaña en (x, z), ya con el hueco que ha ido ganando el jugador.</summary>
        public float HeightAt(float x, float z)
        {
            float h = BaseHeight(x, z);
            if (h <= 0) return 0;
            if (band > 0 && z < FrontZ + band) return 0;
            float dist = Vector2.Distance(new Vector2(x, z), CenterNow);
            if (dist < radius) return 0;
            // talud de papel derrumbado: más tendido que antes, como un montón que se desmorona
            return h * Mathf.SmoothStep(0, 1, Mathf.Clamp01((dist - radius) / 3.2f));
        }

        void Rebuild()
        {
            builtRadius = radius; builtBand = band; lastRebuild = Time.time;
            int cx0 = Mathf.FloorToInt(XMin / Chunk), cx1 = Mathf.CeilToInt(XMax / Chunk);
            int cz0 = Mathf.FloorToInt((FrontZ - 2) / Chunk), cz1 = Mathf.CeilToInt(ZMax / Chunk);
            for (int cx = cx0; cx < cx1; cx++)
                for (int cz = cz0; cz < cz1; cz++)
                    BuildChunk(new Vector2Int(cx, cz));
            SheetTotal = 0;
            foreach (var sc in sheetChunks.Values) SheetTotal += sc.Count;
        }

        void BuildChunk(Vector2Int c)
        {
            int n = Mathf.RoundToInt(Chunk / Step) + 1;
            float x0 = c.x * Chunk, z0 = c.y * Chunk;
            var verts = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            float sig = 0, maxH = 0;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = x0 + i * Step, z = z0 + j * Step;
                    // el borde exterior baja un poco por debajo del suelo para que no se vea la costura
                    float h = HeightAt(x, z);
                    h = h > 0.02f ? h : -0.05f;
                    verts[j * n + i] = new Vector3(x, h, z);
                    uvs[j * n + i] = new Vector2(x / 4.5f, z / 4.5f + h / 4.5f);
                    sig += h * (1 + i * 0.37f + j * 0.11f);
                    if (h > maxH) maxH = h;
                }
            chunks.TryGetValue(c, out var ch);
            if (ch.mesh != null && Mathf.Abs(ch.sig - sig) < 0.01f) return;
            if (maxH <= 0.02f)
            {
                if (sheetChunks.TryGetValue(c, out var emp)) { foreach (var l in emp.fine) l.Clear(); foreach (var l in emp.coarse) l.Clear(); foreach (var l in emp.top) l.Clear(); foreach (var l in emp.ids) l.Clear(); foreach (var l in emp.topIds) l.Clear(); }
                if (ch.col != null) ch.col.gameObject.SetActive(false);
                chunks[c] = (ch.mesh, ch.col, sig);
                return;
            }
            if (ch.mesh == null)
            {
                ch.mesh = new Mesh { name = "Montaña_" + c };
                var tris = new int[(n - 1) * (n - 1) * 6];
                int t = 0;
                for (int j = 0; j < n - 1; j++)
                    for (int i = 0; i < n - 1; i++)
                    {
                        int a = j * n + i, b = a + 1, d = a + n, e = d + 1;
                        tris[t++] = a; tris[t++] = d; tris[t++] = b;
                        tris[t++] = b; tris[t++] = d; tris[t++] = e;
                    }
                ch.mesh.vertices = verts; ch.mesh.uv = uvs; ch.mesh.triangles = tris;
                var go = new GameObject("Parcela");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = ch.mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = pileMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ch.col = go.AddComponent<MeshCollider>();
            }
            else { ch.mesh.vertices = verts; ch.mesh.uv = uvs; }
            ch.mesh.RecalculateNormals();
            ch.mesh.RecalculateTangents();
            ch.mesh.RecalculateBounds();
            ch.col.gameObject.SetActive(true);
            ch.col.sharedMesh = null;
            ch.col.sharedMesh = ch.mesh;
            chunks[c] = (ch.mesh, ch.col, sig);
            GenerateSheets(c, verts, n, maxH);
        }

        static float Bilinear(Vector3[] v, int n, float lx, float lz)
        {
            int i = Mathf.Clamp((int)lx, 0, n - 2), j = Mathf.Clamp((int)lz, 0, n - 2);
            float fx = lx - i, fz = lz - j;
            float a = v[j * n + i].y, b = v[j * n + i + 1].y, c = v[(j + 1) * n + i].y, d = v[(j + 1) * n + i + 1].y;
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }

        /// <summary>Cubre la parcela de folios A4 sueltos, apoyados en la ladera con giros al azar.</summary>
        void GenerateSheets(Vector2Int c, Vector3[] v, int n, float maxH)
        {
            if (!sheetChunks.TryGetValue(c, out var sc)) { sc = new SheetChunk(); sheetChunks[c] = sc; }
            foreach (var l in sc.fine) l.Clear();
            foreach (var l in sc.coarse) l.Clear();
            foreach (var l in sc.ids) l.Clear();
            foreach (var l in sc.top) l.Clear();
            foreach (var l in sc.topIds) l.Clear();
            float x0 = c.x * Chunk, z0 = c.y * Chunk;
            sc.bounds = new Bounds(new Vector3(x0 + Chunk / 2f, maxH / 2f, z0 + Chunk / 2f), new Vector3(Chunk + 1, maxH + 1, Chunk + 1));
            var cn = CenterNow;
            for (int pass = 0; pass < 2; pass++)
            {
                float cell = pass == 0 ? FineCell : CoarseCell;
                float scale = pass == 0 ? 1f : CoarseScale;
                var dst = pass == 0 ? sc.fine : sc.coarse;
                int cells = Mathf.RoundToInt(Chunk / cell);
                for (int layer = 0; layer < (pass == 0 ? Layers : 1); layer++)
                for (int j = 0; j < cells; j++)
                    for (int i = 0; i < cells; i++)
                    {
                        uint hk = Phrases.Hash((uint)(c.x * 4096 + i + 70000) * 92821u ^ (uint)(c.y * 4096 + j + 50000) * 68917u ^ (uint)pass * 7u ^ (uint)layer * 1000003u);
                        float lx = (i + (hk & 1023) / 1023f) * cell, lz = (j + ((hk >> 10) & 1023) / 1023f) * cell;
                        float h = Bilinear(v, n, lx / Step, lz / Step);
                        float px = x0 + lx, pz = z0 + lz;
                        Vector3 nrm;
                        if (h < 0.03f)
                        {
                            // al pie del montón, hojas tiradas por el suelo del hueco
                            if (pass == 1) continue;
                            float d = Vector2.Distance(new Vector2(px, pz), cn);
                            if (d < radius - 1.6f || d > radius + 0.6f || ((hk >> 20) & 7) > 2) continue;
                            h = 0f; nrm = Vector3.up;
                        }
                        else
                        {
                            float e = 0.35f;
                            float hx = Bilinear(v, n, (lx + e) / Step, lz / Step) - Bilinear(v, n, (lx - e) / Step, lz / Step);
                            float hz = Bilinear(v, n, lx / Step, (lz + e) / Step) - Bilinear(v, n, lx / Step, (lz - e) / Step);
                            nrm = new Vector3(-hx / (2 * e), 1f, -hz / (2 * e)).normalized;
                        }
                        var rot = Quaternion.FromToRotation(Vector3.up, nrm) *
                                  Quaternion.Euler((((hk >> 22) & 15) - 7.5f) * (pass == 0 ? 1f : 0.25f), ((hk >> 4) & 511) * 0.7045f, (((hk >> 14) & 15) - 7.5f) * (pass == 0 ? 1f : 0.25f));
                        float lift = 0.006f + layer * 0.016f + ((hk >> 26) & 7) * 0.004f;
                        var m = Matrix4x4.TRS(new Vector3(px, h + lift, pz), rot, new Vector3(scale, 1, scale));
                        int kind = (int)((hk >> 27) % 13);
                        int grp = kind < 9 ? 0 : kind < 12 ? 1 : 2;
                        if (pass == 0)
                        {
                            if (activated.Contains(hk)) continue;
                            if (layer > 0) { sc.topIds[grp].Add(hk); sc.top[grp].Add(m); continue; }
                            sc.ids[grp].Add(hk);
                        }
                        dst[grp].Add(m);
                    }
            }
        }

        public bool Empty => band >= (float)Progress.MountainDepth - 0.5f;

        void Update()
        {
            if ((Mathf.Abs(radius - builtRadius) > 0.04f || Mathf.Abs(band - builtBand) > 0.04f) && Time.time - lastRebuild > 1f) Rebuild();
            var cam = Camera.main;
            if (cam == null) return;
            activateTimer -= Time.deltaTime;
            if (activateTimer <= 0f)
            {
                activateTimer = 0.15f;
                ActivateNear(cam.transform.position, 2.6f, 60);
                // y donde mira el jugador, si la ladera está a mano
                if (Raycast(cam.transform.position, cam.transform.forward, 7f, out var look)) ActivateNear(look, 2.2f, 60);
            }
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            var cp = cam.transform.position;
            int drawn = 0;
            foreach (var sc in sheetChunks.Values)
            {
                if (sc.fine[0].Count == 0 && sc.fine[1].Count == 0 && sc.top[0].Count == 0) continue;
                float dist = Mathf.Sqrt(sc.bounds.SqrDistance(cp));
                bool visible = GeometryUtility.TestPlanesAABB(planes, sc.bounds);
                if (!visible && dist > NearDist) continue;     // fuera de cámara (salvo sombras cercanas)
                for (int g = 0; g < 3; g++)
                {
                    if (dist < TopDist)
                        drawn += Draw(sc.top[g], dist < NearDist ? Paper.LowCurledMesh : Paper.QuadMesh, sheetMats[g], ShadowCastingMode.Off);
                    if (dist < NearDist)
                        drawn += Draw(sc.fine[g], Paper.LowCurledMesh, sheetMats[g], ShadowCastingMode.On);
                    else if (dist < MidDist)
                        drawn += Draw(sc.fine[g], Paper.QuadMesh, sheetMats[g], ShadowCastingMode.Off);
                    else
                        drawn += Draw(sc.coarse[g], Paper.QuadMesh, sheetMats[g], ShadowCastingMode.Off);
                }
            }
            SheetsDrawn = drawn;
        }

        /// <summary>
        /// Los folios de la montaña son instancias en la GPU; los que quedan al alcance del jugador se convierten en
        /// objetos de verdad (cuerpo rígido, colisión, se pueden coger, patear y aspirar). Así hay cientos de miles de
        /// hojas sueltas y solo pagan física las que se pueden tocar.
        /// </summary>
        public void ActivateNear(Vector3 p, float r, int max)
        {
            int made = 0;
            float r2 = r * r;
            int cx0 = Mathf.FloorToInt((p.x - r) / Chunk), cx1 = Mathf.FloorToInt((p.x + r) / Chunk);
            int cz0 = Mathf.FloorToInt((p.z - r) / Chunk), cz1 = Mathf.FloorToInt((p.z + r) / Chunk);
            for (int cx = cx0; cx <= cx1; cx++)
                for (int cz = cz0; cz <= cz1; cz++)
                {
                    if (!sheetChunks.TryGetValue(new Vector2Int(cx, cz), out var sc)) continue;
                    for (int gg = 0; gg < 6; gg++)
                    {
                        int g = gg % 3;
                        var list = gg < 3 ? sc.top[g] : sc.fine[g]; var ids = gg < 3 ? sc.topIds[g] : sc.ids[g];
                        for (int i = list.Count - 1; i >= 0 && made < max; i--)
                        {
                            var m = list[i];
                            var pos = new Vector3(m.m03, m.m13, m.m23);
                            float dx = pos.x - p.x, dz = pos.z - p.z, dy = pos.y - p.y;
                            if (dx * dx + dz * dz > r2 || dy > 1.2f || dy < -2.4f) continue;
                            var rot = m.rotation;
                            PageData page = null;
                            if (g > 0) page = new PageData { text = Phrases.Get(ids[i] % 2000000, 91u, 0, out _), quality = 1, value = Econ.RecycleValue * 4 };
                            var ls = Paper.Spawn(pos + Vector3.up * 0.004f, rot, Vector3.zero, page, g == 2);
                            ls.Sleep();
                            activated.Add(ids[i]);
                            int last = list.Count - 1;
                            list[i] = list[last]; list.RemoveAt(last);
                            ids[i] = ids[last]; ids.RemoveAt(last);
                            made++;
                        }
                    }
                }
            SheetsActivated += made;
        }

        int Draw(List<Matrix4x4> list, Mesh mesh, Material mat, ShadowCastingMode shadows)
        {
            if (list.Count == 0) return 0;
            var rp = new RenderParams(mat) { shadowCastingMode = shadows, receiveShadows = true };
            for (int i = 0; i < list.Count; i += 1023)
                Graphics.RenderMeshInstanced(rp, mesh, 0, list, Mathf.Min(1023, list.Count - i), i);
            return list.Count;
        }

        /// <summary>Marcha a lo largo del rayo hasta tocar la superficie de la montaña.</summary>
        public bool Raycast(Vector3 origin, Vector3 dir, float maxDist, out Vector3 hit)
        {
            for (float t = 0.2f; t < maxDist; t += 0.06f)
            {
                var p = origin + dir * t;
                if (p.y < HeightAt(p.x, p.z)) { hit = p; return true; }
            }
            hit = Vector3.zero;
            return false;
        }
    }
}
