using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>Ducto de succión (doc 4.5): el único «vendedor» del acto 1. Todo lo que entra se paga al instante.</summary>
    public class Duct : MonoBehaviour, IInteractable
    {
        public Transform mouth;
        Transform flap;
        float flapT;
        TextMesh counter;
        double shownMoney = -1;
        float fumpCooldown;

        public static Duct Create(Transform root, Vector3 pos)
        {
            var go = new GameObject("Ducto");
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var d = go.AddComponent<Duct>();
            d.BuildModel();
            return d;
        }

        void BuildModel()
        {
            var t = transform;
            var galv = Build.Pbr("metal_plate_02", new Vector2(1, 3), Build.Hex("#a9adb1"));
            var dark = Build.Mat(Build.SteelDark, 0.4f, 0.8f);
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.0f, 0); col.size = new Vector3(1.0f, 0.5f, 0.7f);
            // Repisa de acero con patas
            Build.Box("Repisa", t, new Vector3(0, 0.92f, 0), new Vector3(0.9f, 0.04f, 0.55f), galv, false);
            foreach (var x in new[] { -0.4f, 0.4f })
                foreach (var z in new[] { -0.22f, 0.22f })
                    Build.Box("Pata", t, new Vector3(x, 0.45f, z), new Vector3(0.04f, 0.9f, 0.04f), dark);
            // Boca con trampilla
            Build.Box("Campana", t, new Vector3(0, 1.18f, 0.2f), new Vector3(0.6f, 0.5f, 0.2f), galv, false);
            mouth = Build.Empty("Boca", t, new Vector3(0, 1.1f, 0.12f)).transform;
            var flapPivot = Build.Empty("TrampillaPivote", t, new Vector3(0, 1.32f, 0.1f)).transform;
            Build.Box("Trampilla", flapPivot, new Vector3(0, -0.15f, 0), new Vector3(0.5f, 0.3f, 0.015f), Build.Mat(Build.Hex("#7f8488"), 0.5f, 0.9f), false);
            flap = flapPivot;
            // Tubo de 40 cm que sube al techo, con abrazaderas cada 2 m
            Build.Cyl("Codo", t, new Vector3(0, 1.55f, 0.25f), 0.4f, 0.5f, galv);
            Build.Cyl("Tubo", t, new Vector3(0, 15f, 0.25f), 0.4f, 26.5f, galv);
            for (float y = 2.2f; y < 28; y += 2f)
                Build.Cyl("Abrazadera", t, new Vector3(0, y, 0.25f), 0.43f, 0.05f, dark);
            Build.Cyl("Tramo_Techo", t, new Vector3(0, 28.3f, 6f), 0.4f, 11.5f, galv, false, new Vector3(90, 0, 0));
            // Rayas de seguridad en la repisa
            var safety = Build.Mat(Build.Safety, 0.35f);
            for (int i = 0; i < 5; i++)
                Build.Box("Raya", t, new Vector3(-0.36f + i * 0.18f, 0.942f, -0.25f), new Vector3(0.08f, 0.003f, 0.04f), safety, false, new Vector3(0, 35, 0));

            // Contador mecánico de dinero en la pared del ducto (UI diegética, doc 3)
            var panel = Build.Empty("Contador", t, new Vector3(1.05f, 2.05f, 0.1f), new Vector3(0, -12, 0)).transform;
            Build.Box("Caja", panel, Vector3.zero, new Vector3(0.95f, 0.32f, 0.12f), Build.Mat(Build.Hex("#20252a"), 0.4f, 0.4f), true);
            Build.Box("Ventana", panel, new Vector3(0, 0, -0.061f), new Vector3(0.85f, 0.2f, 0.005f), Build.Mat(Build.Hex("#e9e4d6"), 0.6f), false);
            counter = Build.Text("Cifras", panel, new Vector3(0, 0, -0.066f), "0", 0.13f, Build.Hex("#1a1a1a"));
            Build.Text("Rotulo", panel, new Vector3(0, 0.22f, -0.061f), "CAJA", 0.07f, Build.Hex("#e8b923"));
            Build.Cyl("Poste", t, new Vector3(1.05f, 0.95f, 0.18f), 0.08f, 1.9f, dark, true);
        }

        public string Prompt(Game g)
        {
            int n = g.CarryCount;
            if (n == 0) return "Ducto de succión: trae hojas para venderlas";
            string s = "[Clic] Echar al ducto";
            if (g.S.carriedPages.Count > 0) s += "  " + g.S.carriedPages.Count + " escritas (" + Fmt.Money(g.CarriedPagesValue()) + ")";
            if (g.S.carriedBlank > 0) s += "  " + g.S.carriedBlank + " en blanco (" + Fmt.Money(g.S.carriedBlank * Econ.RecycleValue) + ")";
            return s;
        }

        public void Interact(Game g)
        {
            if (g.CarryCount == 0) { Sfx.Play("deny", 0.4f); return; }
            int visuals = 0;
            double total = 0;
            foreach (var p in g.S.carriedPages)
            {
                total += p.value;
                if (visuals++ < 16) g.SpawnFlyingSheet(g.player.HandPosition, mouth.position, 0.25f + visuals * 0.05f);
            }
            int blanks = g.S.carriedBlank;
            total += blanks * Econ.RecycleValue;
            for (int i = 0; i < Mathf.Min(blanks, 16); i++)
                if (visuals++ < 24) g.SpawnFlyingSheet(g.player.HandPosition, mouth.position, 0.25f + visuals * 0.05f);
            long sold = g.S.carriedPages.Count;
            g.S.pagesSold += sold;
            g.S.carriedPages.Clear();
            g.S.carriedBlank = 0;
            g.Earn(total, mouth.position);
            Swallow(true);
            g.OnSoldAtDuct(sold, blanks);
        }

        /// <summary>Entrada desde una cinta.</summary>
        public void Accept(Game g, PageData p)
        {
            g.S.pagesSold++;
            if (g.S.act < 2) g.S.dayPages++;
            g.Earn(p.value, mouth.position);
            Swallow(false);
        }

        void Swallow(bool loud)
        {
            flapT = 1f;
            if (fumpCooldown <= 0 || loud)
            {
                Sfx.PlayAt("fump", mouth.position, loud ? 1f : 0.55f);
                fumpCooldown = 0.35f;
            }
        }

        void Update()
        {
            fumpCooldown -= Time.deltaTime;
            flapT = Mathf.Max(0, flapT - Time.deltaTime * 3f);
            flap.localRotation = Quaternion.Euler(-70f * Mathf.Sin(flapT * Mathf.PI * 0.5f), 0, 0);
            var g = Game.I;
            if (g != null && shownMoney != g.S.money)
            {
                // Rodillos de cifras: el número visible persigue al real
                if (shownMoney < 0) shownMoney = g.S.money;
                shownMoney = g.S.money - (g.S.money - shownMoney) * Mathf.Exp(-Time.deltaTime * 8f);
                if (System.Math.Abs(g.S.money - shownMoney) < 0.005) shownMoney = g.S.money;
                counter.text = Fmt.Money(shownMoney);
            }
        }
    }

    /// <summary>Cinta transportadora recta: lleva páginas visibles de un punto a otro (doc 4.7).</summary>
    public class Conveyor : MonoBehaviour
    {
        class Item { public PageData page; public Transform vis; public float t; }
        readonly List<Item> items = new List<Item>();
        Vector3 a, b;
        float length, speed = 0.6f;
        readonly List<Transform> slats = new List<Transform>();
        float slatOffset;
        public System.Action<PageData> onArrive;

        public static Conveyor Create(Transform root, string name, Vector3 from, Vector3 to, float height = 0.8f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var c = go.AddComponent<Conveyor>();
            c.a = new Vector3(from.x, height + 0.03f, from.z);
            c.b = new Vector3(to.x, height + 0.03f, to.z);
            c.length = Vector3.Distance(c.a, c.b);
            var dir = (c.b - c.a).normalized;
            var rot = Quaternion.LookRotation(dir);
            var frame = Build.Empty("Bastidor", go.transform, (c.a + c.b) / 2f - Vector3.up * 0.03f).transform;
            frame.rotation = rot;
            var dark = Build.Mat(Build.Hex("#1e1f21"), 0.35f);
            var steel = Build.Mat(Build.SteelDark, 0.45f, 0.8f);
            Build.Box("Banda", frame, Vector3.zero, new Vector3(0.6f, 0.04f, c.length), Build.Pbr("rubber_tiles", new Vector2(1, c.length * 2.5f), Build.Hex("#1e1f21"), Build.Hex("#555555")));
            Build.Box("Lateral", frame, new Vector3(0.32f, 0.02f, 0), new Vector3(0.03f, 0.1f, c.length), steel, false);
            Build.Box("Lateral", frame, new Vector3(-0.32f, 0.02f, 0), new Vector3(0.03f, 0.1f, c.length), steel, false);
            for (float z = -c.length / 2 + 0.2f; z < c.length / 2; z += 1.2f)
            {
                Build.Box("Pata", frame, new Vector3(0.28f, -height / 2, z), new Vector3(0.04f, height, 0.04f), steel);
                Build.Box("Pata", frame, new Vector3(-0.28f, -height / 2, z), new Vector3(0.04f, height, 0.04f), steel);
            }
            var slatMat = Build.Mat(Build.Hex("#34363a"), 0.3f);
            int n = Mathf.Max(2, Mathf.CeilToInt(c.length / 0.25f));
            for (int i = 0; i < n; i++)
                c.slats.Add(Build.Box("Listón", frame, Vector3.zero, new Vector3(0.58f, 0.006f, 0.03f), slatMat, false).transform);
            return c;
        }

        /// <summary>Pone una página en la cinta, a <paramref name="atDistance"/> metros del principio.</summary>
        public void Add(PageData p, float atDistance = 0f)
        {
            var vis = Build.Box("HojaEnCinta", transform, a, new Vector3(0.21f, 0.003f, 0.297f), Build.Mat(Build.Hex("#ebe6d6"), 0.2f), false).transform;
            vis.rotation = Quaternion.LookRotation(b - a) * Quaternion.Euler(0, Random.Range(-8f, 8f), 0);
            var it = new Item { page = p, vis = vis, t = Mathf.Clamp(atDistance, 0, length) };
            // la lista va ordenada de la más avanzada a la más atrasada
            int idx = items.FindIndex(x => x.t < it.t);
            if (idx < 0) items.Add(it); else items.Insert(idx, it);
        }

        /// <summary>Distancia sobre la cinta del punto más cercano a <paramref name="world"/>.</summary>
        public float DistanceAlong(Vector3 world) => Mathf.Clamp(Vector3.Dot(world - a, (b - a).normalized), 0, length);

        public double PendingValue { get { double v = 0; foreach (var i in items) v += i.page.value; return v; } }

        public int Count => items.Count;

        void Update()
        {
            float dt = Time.deltaTime;
            slatOffset = (slatOffset + dt * speed) % 0.25f;
            for (int i = 0; i < slats.Count; i++)
            {
                float z = -length / 2 + ((i * 0.25f + slatOffset) % length);
                slats[i].localPosition = new Vector3(0, 0.021f, z);
            }
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var it = items[i];
                // separación mínima: la hoja de delante frena a la de detrás (se ve el cuello de botella)
                float ahead = i > 0 ? items[i - 1].t : 999f;
                float nt = it.t + dt * speed;
                if (nt > ahead - 0.32f) nt = Mathf.Max(it.t, ahead - 0.32f);
                it.t = nt;
                it.vis.position = Vector3.Lerp(a, b, Mathf.Clamp01(it.t / length));
                if (it.t >= length)
                {
                    onArrive?.Invoke(it.page);
                    Destroy(it.vis.gameObject);
                    items.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>Bandeja junto a la máquina de escribir (doc 4.2): las hojas se cargan solas al terminar una página.</summary>
    public class DeskTray : MonoBehaviour, IInteractable
    {
        readonly List<GameObject> stack = new List<GameObject>();

        public static DeskTray Create(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("BandejaMaquina");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<DeskTray>();
            var dark = Build.Mat(Build.Hex("#3a3d40"), 0.4f, 0.5f);
            Build.Box("Fondo", go.transform, Vector3.zero, new Vector3(0.26f, 0.008f, 0.33f), dark, false);
            Build.Box("Borde", go.transform, new Vector3(0, 0.02f, 0.165f), new Vector3(0.26f, 0.04f, 0.008f), dark, false);
            Build.Box("Borde", go.transform, new Vector3(0.13f, 0.02f, 0), new Vector3(0.008f, 0.04f, 0.33f), dark, false);
            Build.Box("Borde", go.transform, new Vector3(-0.13f, 0.02f, 0), new Vector3(0.008f, 0.04f, 0.33f), dark, false);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.04f, 0); col.size = new Vector3(0.3f, 0.12f, 0.36f);
            return t;
        }

        public string Prompt(Game g)
        {
            int cap = Econ.DeskTrayCap(g.S);
            string s = "Bandeja de la máquina " + g.S.deskTray + "/" + cap;
            if (g.S.carriedBlank > 0 && g.S.deskTray < cap) s = "[Clic] Dejar hojas en blanco — " + s;
            return s;
        }

        public void Interact(Game g)
        {
            int cap = Econ.DeskTrayCap(g.S);
            int n = Mathf.Min(g.S.carriedBlank, cap - g.S.deskTray);
            if (n <= 0) { Sfx.Play("deny", 0.4f); return; }
            g.S.carriedBlank -= n; g.S.deskTray += n;
            Sfx.PlayAt("paper", transform.position, 0.9f);
        }

        void Update()
        {
            var g = Game.I; if (g == null) return;
            int show = Mathf.Min(g.S.deskTray, 30);
            while (stack.Count < show)
                stack.Add(Build.Box("Hoja", transform, new Vector3(0, 0.006f + stack.Count * 0.0015f, 0), new Vector3(0.21f, 0.0012f, 0.297f), Build.Mat(Build.Paper, 0.2f), false, new Vector3(0, Random.Range(-3f, 3f), 0)));
            for (int i = 0; i < stack.Count; i++) stack[i].SetActive(i < show);
        }
    }

    /// <summary>Máquina eléctrica de margarita (doc 4.10 #2): la primera automatización del acto 1.</summary>
    public class Margarita : MonoBehaviour, IInteractable
    {
        public int slot;
        Transform wheel, hammer, outSheet;
        Renderer lamp;
        float progress, hammerT;
        AudioSource motor;
        readonly List<GameObject> trayStack = new List<GameObject>();
        public Conveyor output;

        public static Margarita Create(Transform root, int slot, Vector3 pos)
        {
            var go = new GameObject("Margarita_" + (slot + 1));
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            var m = go.AddComponent<Margarita>();
            m.slot = slot;
            m.BuildModel();
            return m;
        }

        void BuildModel()
        {
            var t = transform;
            var shell = Build.Mat(Build.Beige, 0.45f);
            var dark = Build.Mat(Build.Hex("#2f3234"), 0.4f);
            var steel = Build.Mat(Build.SteelDark, 0.45f, 0.8f);
            // Mesa
            Build.Box("Mesa", t, new Vector3(0, 0.7f, 0), new Vector3(0.9f, 0.04f, 0.6f), steel);
            foreach (var x in new[] { -0.4f, 0.4f })
                foreach (var z in new[] { -0.25f, 0.25f })
                    Build.Box("Pata", t, new Vector3(x, 0.35f, z), new Vector3(0.04f, 0.7f, 0.04f), steel);
            // Carcasa: modelo de Blender (máquina electrónica de los 80) o cajas si falta
            var keysLight = Build.Mat(Build.Hex("#d9d0bd"), 0.35f);
            var keysDark = Build.Mat(Build.Hex("#4a3f36"), 0.35f);
            bool model = Build.Model("Margarita", t, Vector3.zero, null, n =>
                n == "Plastico" ? shell : n == "Teclas" ? keysDark : n == "TeclasClaras" ? keysLight : n == "Goma" ? Build.Rubber :
                n == "Cromo" ? Build.Chrome : n == "Cristal" ? Build.Glass : null) != null;
            if (!model)
            {
                Build.Box("Carcasa", t, new Vector3(0.12f, 0.8f, 0.02f), new Vector3(0.5f, 0.16f, 0.42f), shell, false);
                Build.Box("Teclado", t, new Vector3(0.12f, 0.75f, -0.21f), new Vector3(0.46f, 0.05f, 0.14f), dark, false, new Vector3(12, 0, 0));
                Build.Cyl("Rodillo", t, new Vector3(0.12f, 0.9f, 0.14f), 0.06f, 0.46f, dark, false, new Vector3(0, 0, 90));
            }
            // Rueda margarita con pétalos, visible bajo una tapa transparente
            wheel = Build.Empty("Rueda", t, new Vector3(0.05f, 0.91f, 0.06f), new Vector3(-75, 0, 0)).transform;
            Build.Cyl("Cubo", wheel, Vector3.zero, 0.025f, 0.008f, steel);
            var petal = Build.Mat(Build.Hex("#e0e0e0"), 0.4f);
            for (int i = 0; i < 24; i++)
                Build.Box("Petalo", wheel, Quaternion.Euler(0, i * 15, 0) * new Vector3(0, 0, 0.035f), new Vector3(0.004f, 0.002f, 0.04f), petal, false, new Vector3(0, i * 15, 0));
            hammer = Build.Box("Martillo", t, new Vector3(0.05f, 0.93f, 0.02f), new Vector3(0.01f, 0.01f, 0.04f), steel, false).transform;
            lamp = Build.Box("Piloto", t, new Vector3(0.3f, 0.862f, 0.0f), new Vector3(0.025f, 0.012f, 0.025f), Build.Mat(Build.ButtonGreen, 0.6f, 0, new Color(0.2f, 1.2f, 0.3f)), false).GetComponent<Renderer>();
            outSheet = Build.Box("HojaSaliendo", t, new Vector3(0.12f, 1.0f, 0.165f), new Vector3(0.21f, 0.15f, 0.0012f), Build.Mat(Build.Hex("#ebe6d6"), 0.2f), false, new Vector3(-15, 0, 0)).transform;
            // Bandeja de entrada lateral
            var tray = Build.Empty("Bandeja", t, new Vector3(-0.29f, 0.725f, -0.05f), new Vector3(0, 0, 0)).transform;
            Build.Box("Fondo", tray, Vector3.zero, new Vector3(0.24f, 0.01f, 0.32f), dark, false);
            trayRoot = tray;
            Build.Text("Numero", t, new Vector3(0.26f, 0.862f, 0.17f), "M" + (slot + 1), 0.03f, Build.Hex("#2a2a2a"), TextAnchor.MiddleCenter, new Vector3(90, 0, 0));
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.85f, 0); col.size = new Vector3(0.95f, 0.35f, 0.65f);
            motor = Sfx.Loop(gameObject, "motor", 0.0f, true);
        }
        Transform trayRoot;

        int Tray { get => Game.I.S.margaritaTrays[slot]; set => Game.I.S.margaritaTrays[slot] = value; }

        public string Prompt(Game g)
        {
            int cap = Econ.MargaritaTrayCap(g.S);
            string s = "Margarita " + (slot + 1) + ": bandeja " + Tray + "/" + cap;
            if (g.S.carriedBlank > 0 && Tray < cap) return "[Clic] Rellenar — " + s;
            if (Tray == 0) return s + " (vacía: tráele hojas en blanco)";
            return s;
        }

        public void Interact(Game g)
        {
            int cap = Econ.MargaritaTrayCap(g.S);
            int n = Mathf.Min(g.S.carriedBlank, cap - Tray);
            if (n <= 0) { Sfx.Play("deny", 0.4f); return; }
            g.S.carriedBlank -= n; Tray += n;
            Sfx.PlayAt("paper", transform.position, 1f);
            g.OnMargaritaFilled();
        }

        void Update()
        {
            var g = Game.I; if (g == null) return;
            float dt = Time.deltaTime;
            bool working = Tray > 0 || progress > 0;
            if (working)
            {
                progress += dt / Econ.MargaritaPeriod(g.S);
                wheel.Rotate(0, dt * 900f, 0, Space.Self);
                hammerT += dt * 14f;
                hammer.localPosition = new Vector3(0.05f, 0.93f, 0.02f + Mathf.Abs(Mathf.Sin(hammerT)) * 0.012f);
                outSheet.localScale = new Vector3(0.21f, 0.02f + 0.15f * progress, 0.0012f);
                outSheet.localPosition = new Vector3(0.12f, 0.93f + 0.075f * progress, 0.165f);
                if (progress >= 1f && Tray > 0)
                {
                    progress = 0;
                    Tray--;
                    g.S.pagesMachine++;
                    var page = new PageData { text = Phrases.Get(g.S.phraseSeq++, g.S.seed, g.S.goldenCount, out _), quality = 1, value = Econ.MargaritaPageValue(g.S) };
                    output.Add(page, output.DistanceAlong(transform.position));
                    Sfx.PlayAt("paper", transform.position, 0.6f, 1.2f);
                }
                else if (progress >= 1f) progress = 0;
            }
            outSheet.gameObject.SetActive(working);
            motor.volume = Mathf.Lerp(motor.volume, working ? 0.22f * Sfx.Volume : 0, dt * 5);
            lamp.enabled = working;

            int show = Mathf.Min(Tray, 30);
            while (trayStack.Count < show)
                trayStack.Add(Build.Box("Hoja", trayRoot, new Vector3(0, 0.007f + trayStack.Count * 0.0015f, 0), new Vector3(0.21f, 0.0012f, 0.297f), Build.Mat(Build.Paper, 0.2f), false));
            for (int i = 0; i < trayStack.Count; i++) trayStack[i].SetActive(i < show);
        }
    }

    /// <summary>Tablón de corcho con tarjetas de mejora (doc 4.13). Las tarjetas aparecen poco a poco.</summary>
    public class UpgradeCard : MonoBehaviour, IInteractable
    {
        public UpgradeDef def;
        TextMesh title, cost;
        Renderer paperR;
        Transform pin;
        float pop;

        public static UpgradeCard Create(Transform board, UpgradeDef def, Vector3 localPos)
        {
            var go = new GameObject("Tarjeta_" + def.key);
            go.transform.SetParent(board, false);
            go.transform.localPosition = localPos;
            go.transform.localEulerAngles = new Vector3(0, 0, Random.Range(-3f, 3f));
            var c = go.AddComponent<UpgradeCard>();
            c.def = def;
            c.paperR = Build.Box("Papel", go.transform, Vector3.zero, new Vector3(0.56f, 0.36f, 0.004f), Build.Mat(Build.Hex("#f1ead2"), 0.15f), false).GetComponent<Renderer>();
            c.pin = Build.Cyl("Chincheta", go.transform, new Vector3(0, 0.15f, -0.01f), 0.025f, 0.012f, Build.Mat(Build.Hex("#c0392b"), 0.6f), false, new Vector3(90, 0, 0)).transform;
            c.title = Build.Text("Titulo", go.transform, new Vector3(-0.25f, 0.11f, -0.004f), def.name, 0.045f, Build.Hex("#222222"), TextAnchor.UpperLeft);
            c.cost = Build.Text("Precio", go.transform, new Vector3(-0.25f, -0.1f, -0.004f), "", 0.045f, Build.Hex("#1d6b2c"), TextAnchor.UpperLeft);
            Build.Text("Desc", go.transform, new Vector3(-0.25f, 0.045f, -0.004f), WrapDesc(def.desc, 28), 0.026f, Build.Hex("#555555"), TextAnchor.UpperLeft);
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.56f, 0.36f, 0.04f);
            c.pop = 1f;
            return c;
        }

        static string WrapDesc(string s, int w)
        {
            var sb = new System.Text.StringBuilder(); int col = 0;
            foreach (var word in s.Split(' '))
            {
                if (col + word.Length > w && col > 0) { sb.Append('\n'); col = 0; }
                else if (col > 0) { sb.Append(' '); col++; }
                sb.Append(word); col += word.Length;
            }
            return sb.ToString();
        }

        public string Prompt(Game g)
        {
            int lvl = g.S.Level(def.key);
            if (lvl >= def.maxLevel) return def.name + " — comprada";
            double c = def.Cost(lvl);
            return (g.S.money >= c ? "[Clic] Comprar " : "Te falta dinero: ") + def.name + " por " + Fmt.Money(c);
        }

        public void Interact(Game g) => g.TryBuy(def);

        void Update()
        {
            var g = Game.I; if (g == null) return;
            int lvl = g.S.Level(def.key);
            bool maxed = lvl >= def.maxLevel;
            string lv = def.maxLevel > 1 ? "  Nv " + lvl + "/" + def.maxLevel : "";
            cost.text = maxed ? "<color=#555555>COMPRADA</color>" + lv : (g.S.money >= def.Cost(lvl) ? "" : "<color=#9a2b2b>") + Fmt.Money(def.Cost(lvl)) + (g.S.money >= def.Cost(lvl) ? "" : "</color>") + lv;
            paperR.sharedMaterial = maxed ? Build.Mat(Build.Hex("#cfc8b0"), 0.1f) : Build.Mat(Build.Hex("#f1ead2"), 0.15f);
            if (pop > 0)
            {
                pop = Mathf.Max(0, pop - Time.deltaTime * 2.5f);
                transform.localScale = Vector3.one * (1f + Mathf.Sin(pop * Mathf.PI) * 0.15f) * (1 - pop * 0.5f + 0.5f * (pop > 0 ? 1 : 0));
            }
            else transform.localScale = Vector3.one;
        }
    }
}
