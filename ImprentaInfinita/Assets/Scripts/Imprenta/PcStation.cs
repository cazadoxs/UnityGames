using System.Text;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Tu PC retro con Escriba (doc 4.8), desde el acto 3: monitor de fósforo verde, teclado beige
    /// y una impresora de inyección al lado. Se escribe más rápido que en la máquina y no se atasca.
    /// </summary>
    public class PcStation : MonoBehaviour, IInteractable
    {
        GameObject visual;
        TextMesh screen;
        Transform printHead, printSheet;
        string target = "";
        readonly StringBuilder typed = new StringBuilder();
        int errors;
        bool golden;
        float startTime, printT;
        public string Target => target;
        public int Typed => typed.Length;
        public int Errors => errors;
        public bool Done => target.Length > 0 && typed.Length >= target.Length;

        public static PcStation Create(Transform world)
        {
            var go = new GameObject("PCPropio");
            go.transform.SetParent(world, false);
            go.transform.localPosition = new Vector3(-4.0f, 0, 4.0f);
            var p = go.AddComponent<PcStation>();
            p.BuildModel();
            return p;
        }

        void BuildModel()
        {
            visual = Build.Empty("Visual", transform, Vector3.zero);
            var t = visual.transform;
            if (Build.Model("Mesa", t, Vector3.zero, null, Props.Map) == null)
            {
                Build.Box("Tablero", t, new Vector3(0, 0.74f, 0), new Vector3(1.5f, 0.04f, 0.75f), Props.Map("Madera"), false);
                foreach (var x in new[] { -0.7f, 0.7f })
                    foreach (var z in new[] { -0.32f, 0.32f })
                        Build.Box("Pata", t, new Vector3(x, 0.36f, z), new Vector3(0.04f, 0.72f, 0.04f), Props.Map("Metal"), false);
            }
            if (Build.Model("PC286", t, new Vector3(0, 0.76f, 0.05f), null, Props.Map) == null)
            {
                Build.Box("Caja", t, new Vector3(0, 0.83f, 0.17f), new Vector3(0.42f, 0.14f, 0.38f), Props.Map("Plastico"), false);
                Build.Box("Monitor", t, new Vector3(0, 1.12f, 0.19f), new Vector3(0.36f, 0.32f, 0.34f), Props.Map("Plastico"), false);
                Build.Box("Teclado", t, new Vector3(0, 0.775f, -0.15f), new Vector3(0.44f, 0.03f, 0.16f), Props.Map("Plastico"), false);
                Build.Box("Cristal", t, new Vector3(0, 1.15f, 0.021f), new Vector3(0.3f, 0.23f, 0.002f), Props.Map("PantallaOscura"), false);
            }
            screen = Build.Text("Pantalla", t, new Vector3(-0.125f, 1.255f, 0.019f), "", 0.0105f, Build.Hex("#56ff8a"), TextAnchor.UpperLeft);
            if (Build.Model("Inyeccion", t, new Vector3(0.5f, 0.76f, 0.05f), new Vector3(0, -15, 0), Props.Map) == null)
                Build.Box("Impresora", t, new Vector3(0.5f, 0.84f, 0.05f), new Vector3(0.42f, 0.16f, 0.3f), Props.Map("PlasticoGris"), false, new Vector3(0, -15, 0));
            printHead = Build.Box("Cabezal", t, new Vector3(0.5f, 0.89f, 0.05f), new Vector3(0.05f, 0.04f, 0.05f), Props.Map("PlasticoOscuro"), false).transform;
            printSheet = Build.Box("Hoja", t, new Vector3(0.5f, 0.82f, -0.12f), new Vector3(0.21f, 0.002f, 0.2f), Props.Map("Papel"), false).transform;
            printSheet.gameObject.SetActive(false);
            Build.Model("Silla", t, new Vector3(0, 0, -0.7f), null, n => n == "Plastico" ? Build.Mat(Build.Hex("#2b2b2a"), 0.35f) : Props.Map(n));
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.0f, 0.05f); col.size = new Vector3(1.4f, 0.6f, 0.7f);
        }

        void Update()
        {
            var g = Game.I;
            if (g == null) return;
            bool on = g.S.act >= 3;
            if (visual.activeSelf != on) visual.SetActive(on);
            GetComponent<Collider>().enabled = on;
            if (!on) return;
            if (g.Mode != GameMode.Computer) screen.text = "ESCRIBA 1.0\n\nPulsa una tecla\npara empezar_";
            else screen.text = ScreenText(g);
            if (printT > 0)
            {
                printT -= Time.deltaTime;
                printHead.localPosition = new Vector3(0.5f + Mathf.Sin(printT * 18f) * 0.12f, 0.89f, 0.05f);
                printSheet.gameObject.SetActive(true);
                printSheet.localPosition = new Vector3(0.5f, 0.82f, -0.12f - (1.2f - printT) * 0.08f);
                if (printT <= 0) printSheet.gameObject.SetActive(false);
            }
        }

        public string Prompt(Game g) => g.S.act < 3 ? null : "[Clic] Sentarse al PC y escribir en Escriba";
        public void Interact(Game g) { if (g.S.act >= 3) g.EnterComputer(); }

        public void Begin(Game g)
        {
            if (target.Length == 0 || Done) NewPage(g);
        }

        void NewPage(Game g)
        {
            var s = g.S;
            string a = Phrases.Get(s.phraseSeq++, s.seed, s.goldenCount, out bool g1);
            string b = Phrases.Get(s.phraseSeq++, s.seed, s.goldenCount, out bool g2);
            target = a + " " + b;
            golden = g1 || g2;
            typed.Clear();
            errors = 0;
            startTime = Time.time;
        }

        public void OnChar(Game g, char c)
        {
            if (Done) return;
            if (typed.Length == 0) startTime = Time.time;
            char exp = target[typed.Length];
            bool ok = Typewriter.Matches(c, exp);
            if (!ok) errors++;
            typed.Append(ok ? exp : c);
            Sfx.Play("key", 0.35f, 1.35f);
        }

        public void Backspace()
        {
            if (typed.Length == 0) return;
            typed.Length--;
            Sfx.Play("key", 0.25f, 1.1f);
        }

        public void Submit(Game g)
        {
            if (!Done) { g.Toast("Termina la página antes de imprimir (" + typed.Length + "/" + target.Length + ")"); return; }
            int wrong = 0;
            for (int i = 0; i < target.Length; i++) if (!Typewriter.Matches(typed[i], target[i])) wrong++;
            float acc = 1f - wrong / (float)target.Length;
            bool impeccable = wrong == 0 && errors == 0;
            float quality = impeccable ? 2f : Mathf.Lerp(0.3f, 1f, acc * acc * acc);
            double v = g.PageValueHand(target.Length, quality, impeccable, golden) * 3 * (1 + 0.5 * g.S.Node("escriba2"));
            var page = new PageData { text = target, quality = quality, golden = golden, hand = true, value = v };
            g.RegisterHandPage(page, impeccable, Time.time - startTime);
            g.S.pagesPC++;
            g.S.pagesSold++;
            g.S.lifetimePages++; g.S.editionPages++; g.S.dayPages++;
            g.Earn(v, transform.TransformPoint(new Vector3(0.5f, 1.0f, 0)));
            printT = 1.2f;
            Sfx.PlayAt("printer", transform.position, 0.8f);
            NewPage(g);
        }

        string ScreenText(Game g)
        {
            var sb = new StringBuilder();
            sb.Append("<color=#9dffb8>ESCRIBA 1.0</color>  A:\\PAGINA.TXT  ").Append(golden ? "<color=#ffd34d>* DORADA *</color>" : "").Append('\n');
            sb.Append("----------------------------------\n");
            // objetivo en tenue, lo tecleado encima en brillante (fallos en rojo)
            const int W = 34;
            int col = 0;
            for (int i = 0; i < target.Length; i++)
            {
                if (col >= W && target[i] == ' ') { sb.Append('\n'); col = 0; continue; }
                char ch = target[i];
                if (i < typed.Length)
                {
                    bool ok = Typewriter.Matches(typed[i], ch);
                    sb.Append(ok ? "<color=#b6ffcc>" + ch + "</color>" : "<color=#ff5a4a>" + (typed[i] == ' ' ? '_' : typed[i]) + "</color>");
                }
                else if (i == typed.Length) sb.Append("<color=#ffffff>" + (Time.time % 0.8f < 0.4f ? '_' : ch) + "</color>");
                else sb.Append("<color=#2f7a46>" + ch + "</color>");
                col++;
            }
            sb.Append("\n\n");
            sb.Append(Done ? "<color=#ffd34d>ENTER: imprimir</color>" : "Fallos: " + errors + "   Esc: salir");
            return sb.ToString();
        }
    }

    /// <summary>Atril con el catálogo de compras (el árbol global y los productores).</summary>
    public class Catalog : MonoBehaviour, IInteractable
    {
        public static void Create(Transform world, Vector3 pos)
        {
            var go = Build.Empty("Catalogo", world, pos, new Vector3(0, 30, 0));
            var t = go.transform;
            var wood = Props.Map("MaderaOscura");
            if (Build.Model("Atril", t, Vector3.zero, null, Props.Map) == null)
            {
                Build.Box("Pie", t, new Vector3(0, 0.55f, 0), new Vector3(0.08f, 1.1f, 0.08f), Props.Map("Metal"), false);
                Build.Box("Base", t, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.04f, 0.5f), Props.Map("Metal"), false);
                Build.Box("Atril", t, new Vector3(0, 1.12f, 0), new Vector3(0.55f, 0.04f, 0.42f), wood, false, new Vector3(-25, 0, 0));
            }
            Build.Box("Catalogo", t, new Vector3(0, 1.15f, -0.01f), new Vector3(0.42f, 0.02f, 0.3f), Build.Mat(Build.Hex("#8a2f2a"), 0.35f), false, new Vector3(-25, 0, 0));
            Build.Text("Titulo", t, new Vector3(0, 1.17f, -0.02f), "CATÁLOGO", 0.035f, Build.Hex("#e8dcc0"), TextAnchor.MiddleCenter, new Vector3(65, 0, 0));
            var c = go.AddComponent<BoxCollider>(); c.center = new Vector3(0, 0.9f, 0); c.size = new Vector3(0.6f, 1.2f, 0.6f);
            go.AddComponent<Catalog>();
        }

        public string Prompt(Game g) => g.S.act < 2 ? "Catálogo de compras (se abre en el acto 2)" : "[Clic] Abrir el catálogo (también con Tab)";
        public void Interact(Game g) => g.OpenShop(null);
    }
}
