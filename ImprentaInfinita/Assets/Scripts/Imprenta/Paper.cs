using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Hojas de papel reales: malla A4 subdividida que se dobla y se menea con los movimientos bruscos,
    /// cuerpo rígido con rozamiento del aire (planean al caer) y texto legible si la hoja está escrita.
    /// </summary>
    public static class Paper
    {
        public const float W = 0.21f, L = 0.297f;
        const int NX = 5, NZ = 7;
        static Mesh flatMesh, curledMesh, quadMesh, lowCurledMesh;
        static Material blankMat, writtenMat, kraftMat;
        static PhysicsMaterial physMat;
        static readonly List<LooseSheet> live = new List<LooseSheet>();
        static Transform poolRoot;
        public const int MaxLive = 900;

        public static Material BlankMat { get { Init(); return blankMat; } }
        public static Material WrittenMat { get { Init(); return writtenMat; } }
        public static Material KraftMat { get { Init(); return kraftMat; } }
        public static Mesh CurledMesh { get { Init(); return curledMesh; } }
        /// <summary>Hoja de 2 triángulos (solo la cara de arriba) para las hojas lejanas de la montaña.</summary>
        public static Mesh QuadMesh { get { Init(); return quadMesh; } }
        /// <summary>Hoja curvada ligera (3×4 vértices, una cara, 12 triángulos) para los folios cercanos de la montaña.</summary>
        public static Mesh LowCurledMesh { get { Init(); return lowCurledMesh; } }
        public static IReadOnlyList<LooseSheet> Live => live;

        static void Init()
        {
            if (flatMesh != null) return;
            flatMesh = MakeMesh(0, 0);
            curledMesh = MakeMesh(0.018f, 0.01f);
            lowCurledMesh = MakeGrid(3, 4, 0.016f, 0.008f);
            quadMesh = new Mesh { name = "HojaA4Quad" };
            quadMesh.vertices = new[] { new Vector3(-W / 2, 0.004f, -L / 2), new Vector3(W / 2, 0.004f, -L / 2), new Vector3(-W / 2, 0.004f, L / 2), new Vector3(W / 2, 0.004f, L / 2) };
            quadMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quadMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            quadMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            quadMesh.RecalculateBounds();
            blankMat = MakeMat(false, new Color(0.95f, 0.94f, 0.91f), 3);
            writtenMat = MakeMat(true, new Color(0.95f, 0.94f, 0.9f), 7);
            kraftMat = MakeMat(true, new Color(0.93f, 0.89f, 0.78f), 11);
            physMat = new PhysicsMaterial("Papel") { dynamicFriction = 0.55f, staticFriction = 0.7f, bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Minimum };
        }

        /// <summary>Malla de dos caras con NX×NZ vértices. curl y sag curvan los bordes (hoja que no está del todo plana).</summary>
        public static Mesh MakeMesh(float curl, float sag)
        {
            var m = new Mesh { name = "HojaA4" };
            int n = NX * NZ;
            var v = new Vector3[n * 2];
            var uv = new Vector2[n * 2];
            var nrm = new Vector3[n * 2];
            for (int z = 0; z < NZ; z++)
                for (int x = 0; x < NX; x++)
                {
                    float u = x / (float)(NX - 1), w = z / (float)(NZ - 1);
                    float px = (u - 0.5f) * W, pz = (w - 0.5f) * L;
                    float y = curl * Sq(px / (W / 2)) + sag * Sq(pz / (L / 2));
                    int i = z * NX + x;
                    v[i] = new Vector3(px, y + 0.0004f, pz); uv[i] = new Vector2(u, w); nrm[i] = Vector3.up;
                    v[i + n] = new Vector3(px, y - 0.0004f, pz); uv[i + n] = new Vector2(1 - u, w); nrm[i + n] = Vector3.down;
                }
            var tris = new List<int>();
            for (int z = 0; z < NZ - 1; z++)
                for (int x = 0; x < NX - 1; x++)
                {
                    int a = z * NX + x, b = a + 1, c = a + NX, d = c + 1;
                    tris.AddRange(new[] { a, c, b, b, c, d });
                    tris.AddRange(new[] { a + n, b + n, c + n, b + n, d + n, c + n });
                }
            m.vertices = v; m.uv = uv; m.normals = nrm;
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        static float Sq(float a) => a * a;

        static Mesh MakeGrid(int nx, int nz, float curl, float sag)
        {
            var m = new Mesh { name = "HojaA4Ligera" };
            var v = new Vector3[nx * nz]; var uv = new Vector2[nx * nz];
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    float u = x / (float)(nx - 1), w = z / (float)(nz - 1);
                    float px = (u - 0.5f) * W, pz = (w - 0.5f) * L;
                    v[z * nx + x] = new Vector3(px, curl * Sq(px / (W / 2)) + sag * Sq(pz / (L / 2)) + 0.002f, pz);
                    uv[z * nx + x] = new Vector2(u, w);
                }
            var t = new List<int>();
            for (int z = 0; z < nz - 1; z++)
                for (int x = 0; x < nx - 1; x++)
                {
                    int a = z * nx + x, b = a + 1, c = a + nx, d = c + 1;
                    t.AddRange(new[] { a, c, b, b, c, d });
                }
            m.vertices = v; m.uv = uv; m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Folio procedural: fibras, sombra de borde y, si está escrito, renglones de texto mecanografiado.</summary>
        static Material MakeMat(bool written, Color tint, int seed)
        {
            const int TW = 128, TH = 182;
            var rnd = new System.Random(seed);
            var px = new Color[TW * TH];
            for (int y = 0; y < TH; y++)
                for (int x = 0; x < TW; x++)
                {
                    float c = 0.97f + (float)rnd.NextDouble() * 0.03f;
                    float edge = Mathf.Min(Mathf.Min(x, TW - 1 - x), Mathf.Min(y, TH - 1 - y));
                    if (edge < 2) c *= 0.9f;
                    px[y * TW + x] = tint * c;
                }
            if (written)
            {
                // renglones con palabras de distinta longitud, margen de 2 cm, interlineado doble
                for (int line = 0; line < 22; line++)
                {
                    int y = TH - 20 - line * 7;
                    if (y < 16) break;
                    int x = 14;
                    int end = TW - 14 - (line % 5 == 4 ? rnd.Next(20, 60) : 0);
                    while (x < end)
                    {
                        int wl = rnd.Next(4, 18);
                        for (int k = x; k < Mathf.Min(end, x + wl); k++)
                            for (int t = 0; t < 3; t++)
                                if (rnd.NextDouble() < 0.75) px[(y + t) * TW + k] *= 0.45f + (float)rnd.NextDouble() * 0.15f;
                        x += wl + rnd.Next(3, 6);
                    }
                }
            }
            var tex = new Texture2D(TW, TH, TextureFormat.RGBA32, true, false) { name = written ? "FolioEscrito" : "Folio", wrapMode = TextureWrapMode.Clamp, anisoLevel = 4 };
            tex.SetPixels(px); tex.Apply(true, false);
            var m = new Material(Build.Mat(Color.white, 0.18f));
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.enableInstancing = true;
            return m;
        }

        /// <summary>Suelta una hoja física. Si hay demasiadas, recicla la más antigua.</summary>
        public static LooseSheet Spawn(Vector3 pos, Quaternion rot, Vector3 vel, PageData page = null, bool kraft = false)
        {
            Init();
            if (poolRoot == null) poolRoot = new GameObject("HojasSueltas").transform;
            LooseSheet s;
            live.RemoveAll(x => x == null);
            if (live.Count >= MaxLive) { s = live[0]; live.RemoveAt(0); }
            else
            {
                var go = new GameObject("Hoja");
                go.transform.SetParent(poolRoot, false);
                go.layer = 0;
                s = go.AddComponent<LooseSheet>();
                s.Setup(physMat);
            }
            s.Reset(pos, rot, vel, page, page != null ? (kraft ? kraftMat : writtenMat) : blankMat);
            live.Add(s);
            return s;
        }

        public static void Remove(LooseSheet s)
        {
            live.Remove(s);
            if (s) Object.Destroy(s.gameObject);
        }

        /// <summary>Hojas sueltas dentro de un radio (para que el camión se las lleve).</summary>
        public static List<LooseSheet> Near(Vector3 c, float r)
        {
            var res = new List<LooseSheet>();
            foreach (var s in live) if (s && (s.transform.position - c).sqrMagnitude < r * r && !s.Collected) res.Add(s);
            return res;
        }

        public static Mesh NewWorkingMesh() { Init(); return Object.Instantiate(flatMesh); }
        public static int Nx => NX;
        public static int Nz => NZ;
    }

    /// <summary>Una hoja suelta con física, que se dobla al moverse y que se puede recoger.</summary>
    public class LooseSheet : MonoBehaviour, IInteractable
    {
        Rigidbody rb;
        Mesh mesh;
        Vector3[] baseV, workV;
        MeshRenderer mr;
        TextMesh text;
        public PageData page;
        float bendX, bendZ, twist, vX, vZ, vT;
        float restX, restZ;
        Vector3 lastVel;
        float textCheck;
        public bool Collected;
        Vector3 flyFrom, flyTo; float flyT = -1;

        public void Setup(PhysicsMaterial pm)
        {
            mesh = Paper.NewWorkingMesh();
            baseV = mesh.vertices;
            workV = (Vector3[])baseV.Clone();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = gameObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(Paper.W, 0.008f, Paper.L);
            col.sharedMaterial = pm;
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.005f;
            rb.linearDamping = 0.6f;
            rb.angularDamping = 1.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.maxAngularVelocity = 20f;
        }

        /// <summary>Hoja recién sacada de la montaña: quieta y dormida hasta que algo la toque.</summary>
        public void Push(Vector3 v)
        {
            rb.WakeUp(); rb.linearVelocity += v; rb.angularVelocity = Random.insideUnitSphere * 6f;
        }

        public void Sleep()
        {
            rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            bendX = 0; vX = vZ = vT = 0;
            rb.Sleep();
        }

        public void Reset(Vector3 pos, Quaternion rot, Vector3 vel, PageData p, Material mat)
        {
            Collected = false; flyT = -1;
            gameObject.SetActive(true);
            rb.isKinematic = false;
            transform.SetPositionAndRotation(pos, rot);
            rb.position = pos; rb.rotation = rot;
            rb.linearVelocity = vel;
            rb.angularVelocity = Random.insideUnitSphere * 3f;
            lastVel = vel;
            page = p;
            mr.sharedMaterial = mat;
            restX = Random.Range(0.006f, 0.03f);
            restZ = Random.Range(-0.008f, 0.015f);
            bendX = Random.Range(-0.03f, 0.03f); bendZ = 0; twist = 0; vX = vZ = vT = 0;
            if (text) text.gameObject.SetActive(false);
            if (p != null && !string.IsNullOrEmpty(p.text))
            {
                if (!text)
                {
                    text = Build.Text("Texto", transform, new Vector3(0, 0.0012f, 0.11f), "", 0.0105f, new Color(0.12f, 0.12f, 0.14f), TextAnchor.UpperCenter, new Vector3(90, 0, 0));
                }
                text.text = Wrap(p.text, 30);
                text.gameObject.SetActive(false);
            }
        }

        public static string Wrap(string s, int width)
        {
            var sb = new System.Text.StringBuilder();
            int col = 0;
            foreach (var w in s.Split(' '))
            {
                if (col + w.Length > width && col > 0) { sb.Append('\n'); col = 0; }
                sb.Append(w).Append(' ');
                col += w.Length + 1;
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>Vuela hacia un punto (el camión, la mano) y desaparece.</summary>
        public void FlyTo(Vector3 to)
        {
            Collected = true;
            rb.isKinematic = true;
            flyFrom = transform.position; flyTo = to; flyT = 0;
        }

        void FixedUpdate()
        {
            if (rb.isKinematic || rb.IsSleeping()) return;
            // Placa en el aire: la resistencia va contra la componente normal de la velocidad, así la hoja planea y se mece
            var v = rb.linearVelocity;
            var n = transform.up;
            float vn = Vector3.Dot(v, n);
            rb.AddForce(-n * vn * Mathf.Abs(vn) * 0.012f, ForceMode.Force);
            if (v.sqrMagnitude > 0.2f)
            {
                // el centro de presión va por delante del de masas: la hoja cabecea y aletea
                rb.AddTorque(Vector3.Cross(n, v.normalized) * vn * 0.0002f + Random.insideUnitSphere * 0.00005f, ForceMode.Force);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (flyT >= 0)
            {
                flyT += dt / 0.6f;
                float k = Mathf.Clamp01(flyT);
                transform.position = Vector3.Lerp(flyFrom, flyTo, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                transform.Rotate(new Vector3(160, 240, 90) * dt, Space.Self);
                if (k >= 1) Paper.Remove(this);
                return;
            }
            // Hoja dormida y ya asentada: no hay nada que calcular (la mayoría de las sacadas de la montaña)
            if (!rb.isKinematic && rb.IsSleeping() && Mathf.Abs(vX) + Mathf.Abs(vZ) + Mathf.Abs(vT) < 0.0005f)
            {
                lastVel = Vector3.zero;
                TextCheck(dt);
                return;
            }
            // Flexión: la aceleración en el eje normal deja atrás los bordes, el giro los retuerce
            var vel = rb.isKinematic ? Vector3.zero : rb.linearVelocity;
            var acc = (vel - lastVel) / Mathf.Max(dt, 0.0001f);
            lastVel = vel;
            var la = transform.InverseTransformDirection(acc - Physics.gravity * (rb.isKinematic || rb.IsSleeping() ? 1 : 0));
            var lw = transform.InverseTransformDirection(rb.isKinematic ? Vector3.zero : rb.angularVelocity);
            bool still = rb.isKinematic || rb.IsSleeping() || vel.sqrMagnitude < 0.0004f;
            float tx = still ? restX : Mathf.Clamp(-la.y * 0.004f + lw.z * 0.006f, -0.07f, 0.07f);
            float tz = still ? restZ : Mathf.Clamp(-la.y * 0.003f + lw.x * 0.006f, -0.06f, 0.06f);
            float tt = still ? 0 : Mathf.Clamp(lw.y * 0.01f, -0.04f, 0.04f);
            vX += ((tx - bendX) * 160f - vX * 9f) * dt; bendX += vX * dt;
            vZ += ((tz - bendZ) * 120f - vZ * 8f) * dt; bendZ += vZ * dt;
            vT += ((tt - twist) * 140f - vT * 9f) * dt; twist += vT * dt;
            if (!still || Mathf.Abs(vX) + Mathf.Abs(vZ) + Mathf.Abs(vT) > 0.0005f) Deform();

            TextCheck(dt);
        }

        void TextCheck(float dt)
        {
            textCheck -= dt;
            if (textCheck <= 0 && text != null && page != null)
            {
                textCheck = 0.4f;
                var cam = Camera.main;
                bool near = cam != null && (cam.transform.position - transform.position).sqrMagnitude < 9f;
                if (text.gameObject.activeSelf != near) text.gameObject.SetActive(near);
            }
        }

        void Deform()
        {
            int nx = Paper.Nx, nz = Paper.Nz, n = nx * nz;
            for (int i = 0; i < n; i++)
            {
                var b = baseV[i];
                float u = b.x / (Paper.W / 2), w = b.z / (Paper.L / 2);
                float y = bendX * u * u + bendZ * w * w + twist * u * w;
                workV[i] = new Vector3(b.x, y + 0.0004f, b.z);
                workV[i + n] = new Vector3(b.x, y - 0.0004f, b.z);
            }
            mesh.vertices = workV;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public string Prompt(Game g)
        {
            if (Collected) return null;
            if (page != null) return "[Clic] Recoger la página: «" + Trunc(page.text, 48) + "»";
            return "[Clic] Recoger la hoja";
        }

        static string Trunc(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n) + "…";

        public void Interact(Game g)
        {
            if (Collected) return;
            if (g.CarryCount >= Econ.Capacity(g.S)) { g.Toast("Vas lleno"); Sfx.Play("deny", 0.5f); return; }
            if (page != null)
            {
                g.S.carriedPages.Add(page);
                g.Toast("«" + Trunc(page.text, 90) + "»");
            }
            else g.S.carriedBlank++;
            Sfx.PlayAt("paper", transform.position, 0.8f);
            FlyTo(g.player.HandPosition);
        }
    }
}
