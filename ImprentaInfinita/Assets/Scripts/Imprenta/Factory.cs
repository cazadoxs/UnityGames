using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Imprenta
{
    /// <summary>
    /// La producción de los actos 2 a 8 (doc 4.7, 4.9, 4.10, 6.2): coloca cada productor comprado en su zona
    /// de la nave, lo anima, mueve hojas visibles por las cintas hasta el muelle y suma dinero y terreno.
    /// </summary>
    public class Factory : MonoBehaviour
    {
        public static Factory I;
        Game g;
        Transform root;
        readonly Dictionary<string, List<Unit>> units = new Dictionary<string, List<Unit>>();
        readonly Dictionary<string, TextMesh> signs = new Dictionary<string, TextMesh>();
        public double income, pagesPerSec, sheetsPerSec;
        float recalc, earnTick, scrollCheck, dropCooldown;
        double earnBuffer;
        bool built;

        // Cintas y hojas visibles
        class Path { public Vector3[] pts; public float[] cum; public float len; }
        class Sheet { public Path path; public float d; public float speed; public Quaternion rot; public float lateral; }
        readonly List<Sheet> sheets = new List<Sheet>();
        readonly List<Matrix4x4> sheetMatrices = new List<Matrix4x4>();
        readonly Matrix4x4[] batch = new Matrix4x4[1023];
        Mesh quad;
        Material sheetMat;
        readonly Dictionary<string, Path> zonePaths = new Dictionary<string, Path>();
        public Vector3 DockPoint = new Vector3(24f, 0.9f, -5f);
        Transform dockStack;

        public static Factory Create(Game g, Transform world)
        {
            var go = new GameObject("Fabrica");
            go.transform.SetParent(world, false);
            var f = go.AddComponent<Factory>();
            f.g = g; f.root = go.transform;
            I = f;
            return f;
        }

        void Awake()
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quad = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            sheetMat = Build.Mat(Build.Paper, 0.2f);
        }

        /// <summary>Monta lo que corresponde a la partida cargada (cintas troncales, muelle y unidades).</summary>
        public void BuildAll()
        {
            if (built) return;
            built = true;
            BuildTrunk();
            foreach (var p in Progress.Producers) Sync(p);
            Recalc();
        }

        // ------------------------------------------------------------ zonas
        struct Zone { public Vector3 origin, col, row; public int perRow; public float yaw; public string line; }

        static Zone ZoneFor(string key)
        {
            switch (key)
            {
                case "mecanografo": return new Zone { origin = new Vector3(-8.5f, 0, 14.5f), col = new Vector3(-2.3f, 0, 0), row = new Vector3(0, 0, -3.4f), perRow = 6, line = "izq" };
                case "equipo": return new Zone { origin = new Vector3(-26f, 0, 15f), col = new Vector3(-6f, 0, 0), row = new Vector3(0, 0, -6.5f), perRow = 3, line = "izq" };
                case "pc": return new Zone { origin = new Vector3(14f, 0, 18.5f), col = new Vector3(2.4f, 0, 0), row = new Vector3(0, 0, -3.4f), perRow = 6, line = "der" };
                case "matricial": return new Zone { origin = new Vector3(14f, 0, 5.5f), col = new Vector3(2.4f, 0, 0), row = new Vector3(0, 0, -2.6f), perRow = 5, line = "der" };
                case "inyeccion": return new Zone { origin = new Vector3(30f, 0, 18.5f), col = new Vector3(2.0f, 0, 0), row = new Vector3(0, 0, -3f), perRow = 5, line = "der" };
                case "fotocopiadora": return new Zone { origin = new Vector3(30f, 0, 7f), col = new Vector3(2.2f, 0, 0), row = new Vector3(0, 0, -3f), perRow = 5, line = "der" };
                case "duplicadora": return new Zone { origin = new Vector3(43f, 0, 18.5f), col = new Vector3(1.6f, 0, 0), row = new Vector3(0, 0, -3f), perRow = 4, line = "der" };
                case "dependiente": return new Zone { origin = new Vector3(37f, 0, -6.4f), col = new Vector3(2.2f, 0, 0), row = Vector3.zero, perRow = 4, yaw = 180, line = null };
                case "plotter": return new Zone { origin = new Vector3(-12f, 0, 32f), col = new Vector3(-3.2f, 0, 0), row = new Vector3(0, 0, 5f), perRow = 4, line = "fondoIzq" };
                case "fotolab": return new Zone { origin = new Vector3(-12f, 0, 44f), col = new Vector3(-3.6f, 0, 0), row = new Vector3(0, 0, 5f), perRow = 3, line = "fondoIzq" };
                case "offset": return new Zone { origin = new Vector3(14f, 0, 32f), col = new Vector3(4f, 0, 0), row = new Vector3(0, 0, 6f), perRow = 3, line = "fondoDer" };
                case "rotativa": return new Zone { origin = new Vector3(-14f, 0, 66f), col = new Vector3(-6f, 0, 0), row = Vector3.zero, perRow = 3, line = "fondoIzq" };
                case "ia": return new Zone { origin = new Vector3(14f, 0, 50f), col = new Vector3(3f, 0, 0), row = new Vector3(0, 0, 4f), perRow = 3, line = "fondoDer" };
                case "redia": return new Zone { origin = new Vector3(-20f, 0, 110f), col = new Vector3(-1.0f, 0, 0), row = new Vector3(0, 0, 3.5f), perRow = 5, line = "fondoIzq" };
                case "cuantica": return new Zone { origin = new Vector3(14f, 0, 120f), col = new Vector3(9f, 0, 0), row = Vector3.zero, perRow = 3, line = "fondoDer" };
                case "fractal": return new Zone { origin = new Vector3(-18f, 0, 165f), col = new Vector3(-10f, 0, 0), row = Vector3.zero, perRow = 2, line = "fondoIzq" };
                case "babel": return new Zone { origin = new Vector3(18f, 0, 185f), col = Vector3.zero, row = Vector3.zero, perRow = 1, line = "fondoDer" };
            }
            return new Zone { origin = new Vector3(-40, 0, 0), col = new Vector3(2, 0, 0), row = new Vector3(0, 0, 2), perRow = 5 };
        }

        /// <summary>Puntos del frente de la montaña donde trabajan los recogedores.</summary>
        Vector3 PickerSpot(ProducerDef def, int i, int count, out float yaw)
        {
            Progress.Shape(Econ.FreeArea(g.S), out float r, out float band);
            float inset = def.key == "brazo" ? 1.4f : def.key == "grua" ? 2.5f : def.key == "excavadora" ? 4.5f : def.key == "aspirador" ? 6f : 1.5f;
            // reparto por el arco, con los recogedores de cada tipo intercalados
            int typeIdx = def.key == "brazo" ? 0 : def.key == "grua" ? 1 : def.key == "excavadora" ? 2 : def.key == "aspirador" ? 3 : 4;
            float t = (i + 0.5f + typeIdx * 0.21f) / Mathf.Max(1, count);
            float ang = Mathf.Lerp(Mathf.PI * 0.12f, Mathf.PI * 0.88f, t);
            float rad = Mathf.Max(2f, r - inset);
            var c = new Vector3(0, 0, Mountain.FrontZ + band);
            var p = c + new Vector3(Mathf.Cos(ang) * rad, 0, Mathf.Sin(ang) * rad);
            p.x = Mathf.Clamp(p.x, -56, 56);
            var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
            yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            return p;
        }

        Vector3 SlotPos(ProducerDef def, int i, out float yaw)
        {
            if (def.IsPicker) return PickerSpot(def, i, Mathf.Min(g.S.Count(def.key), def.visualCap), out yaw);
            var z = ZoneFor(def.key);
            yaw = z.yaw;
            int c = i % z.perRow, r = i / z.perRow;
            return z.origin + z.col * c + z.row * r;
        }

        /// <summary>Crea las unidades que falten (hasta el tope visual) y el cartel con el total.</summary>
        public void Sync(ProducerDef def)
        {
            int n = g.S.Count(def.key);
            if (!units.TryGetValue(def.key, out var list)) units[def.key] = list = new List<Unit>();
            int want = Mathf.Min(n, def.visualCap);
            while (list.Count < want)
            {
                int i = list.Count;
                var pos = SlotPos(def, i, out float yaw);
                var u = Props.Create(def, root, pos, yaw, i);
                list.Add(u);
                if (!def.IsPicker)
                {
                    var zone = ZoneFor(def.key);
                    if (zone.line != null && zonePaths.TryGetValue(zone.line, out var trunk))
                    {
                        EnsureLine(zone.line);
                        var outW = u.root.TransformPoint(u.outLocal);
                        DrawFeeder(outW, Eval(trunk, NearestAlong(trunk, outW), out _));
                    }
                }
                if (Time.timeSinceLevelLoad > 1f) { Sfx.PlayAt("buy", pos, 0.5f); Build.AddColliders(root); }
            }
            if (def.IsPicker) RepositionPickers(def);
            if (n > 0 && def.visualCap > 0 && !def.IsPicker)
            {
                if (!signs.TryGetValue(def.key, out var tm))
                {
                    var zone = ZoneFor(def.key);
                    var pos = zone.origin + new Vector3(0, 2.6f, 0) - zone.col.normalized * 1.6f;
                    var post = Build.Empty("Cartel_" + def.key, root, pos, new Vector3(0, zone.yaw, 0)).transform;
                    Build.Box("Placa", post, new Vector3(0, 0, 0.02f), new Vector3(2.2f, 0.5f, 0.04f), Build.Mat(Build.Hex("#20252a"), 0.4f, 0.4f), false);
                    tm = Build.Text("Texto", post, new Vector3(0, 0, -0.01f), "", 0.09f, Build.Hex("#e8b923"));
                    tm.transform.localEulerAngles = new Vector3(0, 180, 0);
                    signs[def.key] = tm;
                }
                tm.text = def.name.ToUpperInvariant() + "\n×" + n + (Progress.MilestoneMult(n) > 1 ? "   (×" + Progress.MilestoneMult(n) + ")" : "");
            }
        }

        void RepositionPickers(ProducerDef def)
        {
            if (!units.TryGetValue(def.key, out var list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                var p = SlotPos(def, i, out float yaw);
                list[i].basePos = p;
                list[i].root.position = p;
                list[i].root.rotation = Quaternion.Euler(0, yaw, 0);
            }
        }

        public void Clear()
        {
            foreach (var kv in units) foreach (var u in kv.Value) if (u.root) Destroy(u.root.gameObject);
            units.Clear();
            foreach (var kv in signs) if (kv.Value) Destroy(kv.Value.transform.parent.gameObject);
            signs.Clear();
            sheets.Clear();
        }

        // ------------------------------------------------------------ cintas troncales y muelle
        readonly Dictionary<string, Vector3[]> lineSegs = new Dictionary<string, Vector3[]>();

        void BuildTrunk()
        {
            // Las cintas no existen al principio: cada tramo se monta cuando llega la primera máquina que lo usa.
            // La troncal acaba en el borde del muelle: lo que llega se cae al suelo y espera al camión.
            var lineIzq = new[] { new Vector3(-6f, 0.8f, 16f), new Vector3(-6f, 0.8f, 3f), new Vector3(-6f, 2.6f, 0f), new Vector3(-6f, 2.6f, -2.5f), new Vector3(22f, 2.6f, -2.5f), new Vector3(23.5f, 0.9f, -4.5f), DockPoint };
            var lineDer = new[] { new Vector3(12.4f, 0.8f, 19f), new Vector3(12.4f, 0.8f, 3f), new Vector3(12.4f, 2.6f, 0f), new Vector3(12.4f, 2.6f, -2.5f), new Vector3(22f, 2.6f, -2.5f), new Vector3(23.5f, 0.9f, -4.5f), DockPoint };
            var lineFondoIzq = new[] { new Vector3(-6f, 0.8f, 200f), new Vector3(-6f, 0.8f, 16f) };
            var lineFondoDer = new[] { new Vector3(12.4f, 0.8f, 200f), new Vector3(12.4f, 0.8f, 19f) };
            zonePaths["izq"] = MakePath(lineIzq);
            zonePaths["der"] = MakePath(lineDer);
            zonePaths["fondoIzq"] = MakePath(Concat(lineFondoIzq, lineIzq));
            zonePaths["fondoDer"] = MakePath(Concat(lineFondoDer, lineDer));
            lineSegs["izq"] = lineIzq; lineSegs["der"] = lineDer;
            lineSegs["fondoIzq"] = lineFondoIzq; lineSegs["fondoDer"] = lineFondoDer;
            BuildDock();
        }

        /// <summary>Monta la cinta de una línea (y la troncal a la que desemboca) si aún no está.</summary>
        void EnsureLine(string line)
        {
            if (line == null || !lineSegs.ContainsKey(line)) return;
            if (line == "fondoIzq") EnsureLine("izq");
            if (line == "fondoDer") EnsureLine("der");
            if (drawnLines.Add(line)) DrawBelt(lineSegs[line], line, line == "izq" || line == "der");
        }
        readonly HashSet<string> drawnLines = new HashSet<string>();

        static float NearestAlong(Path trunk, Vector3 w)
        {
            float bestD = float.MaxValue, along = 0;
            for (float d = 0; d < trunk.len; d += 0.5f)
            {
                var p = Eval(trunk, d, out _);
                float dd = (p - w).sqrMagnitude;
                if (dd < bestD) { bestD = dd; along = d; }
            }
            return along;
        }

        static Vector3[] Concat(Vector3[] a, Vector3[] b) { var l = new List<Vector3>(a); l.AddRange(b); return l.ToArray(); }

        static Path MakePath(Vector3[] pts)
        {
            var p = new Path { pts = pts, cum = new float[pts.Length] };
            for (int i = 1; i < pts.Length; i++) p.cum[i] = p.cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            p.len = p.cum[pts.Length - 1];
            return p;
        }

        static Vector3 Eval(Path p, float d, out Vector3 dir)
        {
            for (int i = 1; i < p.pts.Length; i++)
                if (d <= p.cum[i] || i == p.pts.Length - 1)
                {
                    float seg = p.cum[i] - p.cum[i - 1];
                    float k = seg > 0 ? Mathf.Clamp01((d - p.cum[i - 1]) / seg) : 1;
                    dir = (p.pts[i] - p.pts[i - 1]).normalized;
                    return Vector3.Lerp(p.pts[i - 1], p.pts[i], k);
                }
            dir = Vector3.forward;
            return p.pts[p.pts.Length - 1];
        }

        readonly HashSet<string> drawnBelts = new HashSet<string>();

        public const float BeltWidth = 1.0f;

        void DrawBelt(Vector3[] pts, string id, bool openEnd = false)
        {
            var belt = Build.Pbr("rubber_tiles", new Vector2(2, 6), Build.Hex("#1e1f21"), Build.Hex("#555555"));
            var frame = Build.Mat(Build.SteelDark, 0.45f, 0.8f);
            var chevron = Build.Mat(Build.Hex("#d9d9d4"), 0.3f);
            var roller = Build.Mat(Build.Steel, 0.6f, 0.9f);
            float hw = BeltWidth / 2f;
            for (int i = 1; i < pts.Length; i++)
            {
                Vector3 a = pts[i - 1] - Vector3.up * 0.04f, b = pts[i] - Vector3.up * 0.04f;
                string key = Vector3.Min(a, b) + "-" + Vector3.Max(a, b);
                if (!drawnBelts.Add(key)) continue;
                float len = Vector3.Distance(a, b);
                if (len < 0.05f) continue;
                var mid = (a + b) / 2;
                var rot = Quaternion.LookRotation(b - a);
                var seg = Build.Empty("Cinta_" + id, root, mid).transform;
                seg.rotation = rot;
                // banda con un poco de solape para que no se vean huecos en los codos
                Build.Box("Banda", seg, Vector3.zero, new Vector3(BeltWidth, 0.05f, len + 0.3f), belt);
                Build.Box("Bastidor", seg, new Vector3(0, -0.07f, 0), new Vector3(BeltWidth + 0.04f, 0.1f, len + 0.3f), frame, false);
                Build.Box("Lateral", seg, new Vector3(hw + 0.03f, 0.05f, 0), new Vector3(0.05f, 0.16f, len + 0.3f), frame);
                Build.Box("Lateral", seg, new Vector3(-hw - 0.03f, 0.05f, 0), new Vector3(0.05f, 0.16f, len + 0.3f), frame);
                // flechas blancas pintadas en la banda, como en las naves de verdad
                for (float s = 0.8f; s < len - 0.4f; s += 1.6f)
                {
                    float z = -len / 2 + s;
                    Build.Box("Flecha", seg, new Vector3(-0.17f, 0.026f, z), new Vector3(0.07f, 0.003f, 0.42f), chevron, false, new Vector3(0, 40, 0));
                    Build.Box("Flecha", seg, new Vector3(0.17f, 0.026f, z), new Vector3(0.07f, 0.003f, 0.42f), chevron, false, new Vector3(0, -40, 0));
                }
                // patas dobles cada 2,4 m hasta el suelo (las aéreas cuelgan del techo)
                for (float s = 0.5f; s < len; s += 2.4f)
                {
                    var p = Vector3.Lerp(a, b, s / len);
                    var side = seg.right * (hw - 0.05f);
                    if (p.y > 2f)
                    {
                        float h = WorldBuilder.Height - p.y;
                        Build.Box("Tirante", root, new Vector3(p.x, p.y + h / 2, p.z) + side, new Vector3(0.025f, h, 0.025f), frame, false);
                        Build.Box("Tirante", root, new Vector3(p.x, p.y + h / 2, p.z) - side, new Vector3(0.025f, h, 0.025f), frame, false);
                    }
                    else
                    {
                        Build.Box("Pata", root, new Vector3(p.x, (p.y - 0.1f) / 2, p.z) + side, new Vector3(0.06f, p.y - 0.1f, 0.06f), frame);
                        Build.Box("Pata", root, new Vector3(p.x, (p.y - 0.1f) / 2, p.z) - side, new Vector3(0.06f, p.y - 0.1f, 0.06f), frame);
                    }
                }
            }
            if (openEnd)
            {
                // rodillo de cabeza: aquí acaba la cinta y el papel cae al suelo
                var end = pts[pts.Length - 1] - Vector3.up * 0.04f;
                var dir = (pts[pts.Length - 1] - pts[pts.Length - 2]); dir.y = 0; dir.Normalize();
                var r = Build.Cyl("RodilloFinal", root, end, 0.12f, BeltWidth + 0.06f, roller, false);
                r.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, 90);
            }
        }

        /// <summary>Tramo corto de cinta de la salida de una máquina a su troncal.</summary>
        void DrawFeeder(Vector3 from, Vector3 to)
        {
            var a = new Vector3(from.x, to.y, from.z);
            if ((a - to).sqrMagnitude < 0.25f) return;
            DrawBelt(new[] { a, to }, "ramal");
        }

        void BuildDock()
        {
            // Muelle de carga: plataforma, palé donde se apilan las páginas del pedido y la furgoneta (Workday)
            var dock = Build.Empty("Muelle", root, new Vector3(24f, 0, -6.5f)).transform;
            Build.Box("Plataforma", dock, new Vector3(0, 0.05f, 0), new Vector3(6f, 0.1f, 4f), Build.Pbr("metal_plate", new Vector2(3, 2), Build.Hex("#6d6d6d")), false);
            for (int i = 0; i < 6; i++)
                Build.Box("Raya", dock, new Vector3(-2.5f + i, 0.105f, 1.9f), new Vector3(0.5f, 0.005f, 0.1f), Build.Mat(Build.Safety, 0.35f), false, new Vector3(0, 30, 0));
            var pallet = Build.Pbr("plywood", new Vector2(1, 1), Build.Hex("#9c7a52"));
            Build.Box("Palet", dock, new Vector3(0, 0.17f, 1.0f), new Vector3(1.2f, 0.14f, 0.8f), pallet);
            dockStack = Build.Box("Pila", dock, new Vector3(0, 0.25f, 1.0f), new Vector3(1.0f, 0.01f, 0.7f), Build.Mat(Build.Paper, 0.15f), false).transform;
            Build.Text("Rotulo", dock, new Vector3(0, 3.2f, 1.9f), "MUELLE DE CARGA", 0.22f, Build.Hex("#e8b923"), TextAnchor.MiddleCenter, new Vector3(0, 180, 0));
        }

        public void SetDockFill(float frac)
        {
            if (!dockStack) return;
            float h = Mathf.Lerp(0.01f, 1.4f, Mathf.Clamp01(frac));
            dockStack.localScale = new Vector3(1.0f, h, 0.7f);
            dockStack.localPosition = new Vector3(0, 0.25f + h / 2, 1.0f);
        }

        // ------------------------------------------------------------ producción
        public void Recalc()
        {
            income = Progress.IncomePerSec(g.S, out pagesPerSec, out sheetsPerSec);
        }

        public bool Running => g.Mode != GameMode.Paused && g.Mode != GameMode.Night && g.Mode != GameMode.ActComplete && !Events.Blackout &&
            (g.S.act < 2 || (Workday.I != null && Workday.I.MachinesOn));

        void Update()
        {
            if (!built) return;
            var S = g.S;
            float dt = Time.deltaTime;
            recalc -= dt;
            if (recalc <= 0) { recalc = 0.5f; Recalc(); }
            if (S.boostLeft > 0) S.boostLeft -= dt;

            bool run = Running;
            if (run)
            {
                double pages = pagesPerSec * dt;
                S.lifetimePages += pages; S.editionPages += pages; S.dayPages += pages;
                earnBuffer += income * dt;
                if (sheetsPerSec > 0)
                {
                    S.sheetsAuto += sheetsPerSec * dt;
                    g.OnAreaChanged();
                }
                earnTick -= dt;
                if (earnTick <= 0)
                {
                    earnTick = 0.5f;
                    g.EarnSilent(earnBuffer);
                    earnBuffer = 0;
                }
                scrollCheck -= dt;
                if (scrollCheck <= 0) { scrollCheck = 1f; g.CheckScrolls(); }
            }
            Animate(dt, run);
            UpdateSheets(dt, run);
        }

        public void OnAreaChangedVisual()
        {
            foreach (var p in Progress.Producers) if (p.IsPicker) RepositionPickers(p);
        }

        // ------------------------------------------------------------ animación
        void Animate(float dt, bool run)
        {
            float t = Time.time;
            foreach (var kv in units)
            {
                foreach (var u in kv.Value)
                {
                    if (!u.root) continue;
                    float ph = t + u.phase;
                    float act = run ? 1 : 0;
                    switch (u.def.key)
                    {
                        case "mecanografo":
                        case "pc":
                        case "equipo":
                            for (int i = 0; i + 1 < u.parts.Count; i += 3)
                            {
                                if (u.parts[i] == null) continue;
                                float k = ph * 11f + i;
                                u.parts[i].localRotation = Quaternion.Euler(act * Mathf.Abs(Mathf.Sin(k)) * 9f, 0, 0);
                                u.parts[i + 1].localRotation = Quaternion.Euler(act * Mathf.Abs(Mathf.Sin(k * 1.13f + 1.7f)) * 9f, 0, 0);
                                if (i + 2 < u.parts.Count && u.parts[i + 2] != null)
                                    u.parts[i + 2].localRotation = Quaternion.Euler(4 + Mathf.Sin(ph * 0.7f) * 3f, Mathf.Sin(ph * 0.31f) * 12f, 0);
                            }
                            if (u.def.key == "pc" && u.parts.Count > 3 && u.parts[3] is Transform st && st.TryGetComponent<TextMesh>(out var tm) && run)
                            {
                                int n = (int)(ph * 6) % 160;
                                if (n % 4 == 0) tm.text = ScreenText(n, u.phase);
                            }
                            if (u.def.key == "equipo" && u.parts.Count > 0)
                            {
                                var sup = u.parts[u.parts.Count - 1];
                                float a = ph * 0.25f;
                                sup.localPosition = new Vector3(Mathf.Sin(a) * 1.6f, 0, Mathf.Sin(a * 2) * 0.3f);
                                sup.localRotation = Quaternion.Euler(0, Mathf.Cos(a) > 0 ? 90 : -90, 0);
                            }
                            break;
                        case "matricial":
                            u.parts[0].localPosition = new Vector3(Mathf.PingPong(ph * 0.6f * act, 0.4f) - 0.2f, 0.9f, 0.02f);
                            u.parts[1].localPosition = new Vector3(0, 1.05f + Mathf.Repeat(ph * 0.05f * act, 0.04f), 0.12f);
                            break;
                        case "inyeccion":
                            u.parts[0].localPosition = new Vector3(Mathf.PingPong(ph * 1.6f * act, 0.3f) - 0.15f, 0.89f, 0f);
                            break;
                        case "fotocopiadora":
                            u.parts[0].localPosition = new Vector3(0, 1.0f, Mathf.PingPong(ph * 0.9f, 0.46f) - 0.23f);
                            u.parts[0].gameObject.SetActive(run);
                            break;
                        case "plotter":
                            u.parts[0].localPosition = new Vector3(Mathf.PingPong(ph * 1.2f * act, 1.8f) - 0.9f, 1.05f, -0.21f);
                            float grow = Mathf.Repeat(ph * 0.05f, 1f);
                            u.parts[1].localScale = new Vector3(1.8f, 0.05f + grow * 0.6f, 0.002f);
                            u.parts[1].localPosition = new Vector3(0, 0.95f - grow * 0.3f, -0.25f - grow * 0.05f);
                            break;
                        case "fotolab":
                        case "offset":
                        case "rotativa":
                            foreach (var p in u.parts) if (p) p.Rotate(Vector3.up, act * dt * (u.def.key == "rotativa" ? 400f : 220f), Space.Self);
                            break;
                        case "ia":
                            u.parts[0].localRotation = Quaternion.Euler(0, ph * 40, 0);
                            u.parts[0].localPosition = new Vector3(0, 2.1f + Mathf.Sin(ph * 2) * 0.05f, 0);
                            break;
                        case "cuantica":
                            u.parts[0].localRotation = Quaternion.Euler(ph * 30, ph * 47, ph * 13);
                            break;
                        case "fractal":
                            for (int i = 0; i < u.parts.Count; i++) u.parts[i].localRotation = Quaternion.Euler(0, ph * (10 + i * 12) * (i % 2 == 0 ? 1 : -1), i * 8);
                            break;
                        case "babel":
                            u.parts[0].localRotation = Quaternion.Euler(0, ph * 2f, 0);
                            break;
                        case "brazo":
                        {
                            float cyc = Mathf.Repeat(ph * 0.5f, 1f);
                            float reach = Mathf.Sin(cyc * Mathf.PI * 2) * 0.5f + 0.5f;
                            u.parts[0].localRotation = Quaternion.Euler(Mathf.Lerp(-10, 35, reach * act), Mathf.Sin(cyc * Mathf.PI * 2) * 25f * act, 0);
                            u.parts[1].localRotation = Quaternion.Euler(Mathf.Lerp(-20, 25, reach * act), 0, 0);
                            break;
                        }
                        case "grua":
                        {
                            float cyc = Mathf.Repeat(ph * 0.25f, 1f);
                            u.parts[0].localPosition = new Vector3(Mathf.Sin(cyc * Mathf.PI * 2) * 2f * act, 7.7f, 0);
                            float drop = Mathf.Abs(Mathf.Sin(cyc * Mathf.PI * 2)) * 4f * act + 1.5f;
                            u.parts[1].localScale = new Vector3(0.03f, drop, 0.03f);
                            u.parts[1].localPosition = new Vector3(0, -drop / 2, 0);
                            u.parts[2].localPosition = new Vector3(0, -drop, 0);
                            break;
                        }
                        case "excavadora":
                        {
                            float cyc = Mathf.Repeat(ph * 0.3f, 1f);
                            u.parts[0].localRotation = Quaternion.Euler(Mathf.Sin(cyc * Mathf.PI * 2) * 18f * act, Mathf.Sin(cyc * Mathf.PI) * 30f * act, 0);
                            u.parts[1].localRotation = Quaternion.Euler(Mathf.Cos(cyc * Mathf.PI * 2) * 40f * act, 0, 0);
                            break;
                        }
                        case "aspirador":
                            u.parts[0].localRotation = Quaternion.Euler(Mathf.Sin(ph * 0.3f) * 6f, Mathf.Sin(ph * 0.21f) * 25f, 0);
                            break;
                        case "tuneladora":
                            u.parts[0].Rotate(Vector3.forward, act * dt * 60f, Space.Self);
                            break;
                    }
                    if (u.leds.Count > 0 && run && Mathf.Repeat(ph * 9f, 1f) < dt * 9f)
                        foreach (var l in u.leds) l.enabled = UnityEngine.Random.value > 0.4f;
                }
            }
        }

        static readonly string[] ScreenWords = { "La", "montaña", "imprime", "papel", "infinito", "frase", "página", "tinta", "cinta", "ducto", "letra", "noche", "hoja", "nave" };
        static string ScreenText(int n, float seed)
        {
            var sb = new System.Text.StringBuilder("ESCRIBA 1.0  A:\\DOC.TXT\n");
            int words = n / 4;
            var r = new System.Random((int)(seed * 1000));
            int col = 0;
            for (int i = 0; i < words % 28; i++)
            {
                string w = ScreenWords[r.Next(ScreenWords.Length)];
                if (col + w.Length > 22) { sb.Append('\n'); col = 0; }
                sb.Append(w).Append(' '); col += w.Length + 1;
            }
            sb.Append('_');
            return sb.ToString();
        }

        // ------------------------------------------------------------ hojas por las cintas
        void UpdateSheets(float dt, bool run)
        {
            if (run)
            {
                foreach (var kv in units)
                {
                    foreach (var u in kv.Value)
                    {
                        if (!u.root) continue;
                        var def = u.def;
                        if (def.IsPicker)
                        {
                            u.spawnT -= dt;
                            if (u.spawnT <= 0 && sheets.Count < 1500)
                            {
                                u.spawnT = 1.2f + UnityEngine.Random.value;
                                // hojas que vuelan del frente de la montaña al recogedor
                                var from = u.root.position + u.root.forward * 2.5f + Vector3.up * 1.2f;
                                g.SpawnFlyingSheet(from, u.root.position + Vector3.up * 1.0f, 0.6f);
                            }
                            continue;
                        }
                        var zone = ZoneFor(def.key);
                        if (zone.line == null || !zonePaths.TryGetValue(zone.line, out var trunk)) continue;
                        u.spawnT -= dt;
                        if (u.spawnT > 0 || sheets.Count > 1400) continue;
                        u.spawnT = Mathf.Lerp(4f, 0.6f, Mathf.Clamp01(def.act / 8f)) * (0.7f + UnityEngine.Random.value * 0.6f);
                        var outW = u.root.TransformPoint(u.outLocal);
                        // tramo propio: de la máquina a la cinta troncal más cercana
                        float along = NearestAlong(trunk, outW);
                        var join = Eval(trunk, along, out _);
                        var pts = new List<Vector3> { outW, new Vector3(outW.x, join.y, outW.z) };
                        // primero en línea recta hacia la troncal
                        pts.Add(join);
                        for (int i = 0; i < trunk.pts.Length; i++) if (trunk.cum[i] > along) pts.Add(trunk.pts[i]);
                        var path = MakePath(pts.ToArray());
                        sheets.Add(new Sheet { path = path, d = 0, speed = 1.6f + 0.25f * g.S.Node("cintaVel"), rot = Quaternion.Euler(0, UnityEngine.Random.Range(-25f, 25f), 0), lateral = UnityEngine.Random.Range(-0.28f, 0.28f) });
                    }
                }
            }
            dropCooldown -= dt;
            sheetMatrices.Clear();
            for (int i = sheets.Count - 1; i >= 0; i--)
            {
                var s = sheets[i];
                if (run) s.d += s.speed * dt;
                if (s.d >= s.path.len)
                {
                    // fin de la cinta: la hoja sale despedida y cae al suelo junto al muelle
                    var endP = Eval(s.path, s.path.len, out var endDir);
                    endDir.y = 0; endDir.Normalize();
                    if (dropCooldown <= 0 || UnityEngine.Random.value < 0.15f)
                    {
                        dropCooldown = 0.25f;
                        var pg = new PageData { text = Phrases.Get(UnityEngine.Random.Range(0, 2000000), 77u, 0, out _), quality = 1, value = Econ.RecycleValue * 4 };
                        Paper.Spawn(endP + Vector3.up * 0.03f, Quaternion.LookRotation(endDir) * s.rot, endDir * s.speed + Vector3.up * 0.2f, pg);
                    }
                    sheets.RemoveAt(i); continue;
                }
                var p = Eval(s.path, s.d, out var dir);
                var flat = new Vector3(dir.x, 0, dir.z);
                if (flat.sqrMagnitude > 0.01f) p += Vector3.Cross(Vector3.up, flat.normalized) * s.lateral;
                var rot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).sqrMagnitude > 0.01f ? new Vector3(dir.x, dir.y, dir.z) : Vector3.forward) * s.rot;
                sheetMatrices.Add(Matrix4x4.TRS(p + Vector3.up * 0.002f, rot, Vector3.one));
            }
            var rp = new RenderParams(Paper.WrittenMat) { shadowCastingMode = ShadowCastingMode.On, receiveShadows = true };
            for (int i = 0; i < sheetMatrices.Count; i += 1023)
            {
                int n = Mathf.Min(1023, sheetMatrices.Count - i);
                sheetMatrices.CopyTo(i, batch, 0, n);
                Graphics.RenderMeshInstanced(rp, Paper.CurledMesh, 0, batch, n);
            }
        }

        public int VisibleUnits(string key) => units.TryGetValue(key, out var l) ? l.Count : 0;
    }
}
