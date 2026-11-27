using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Máquina de escribir de varillas (doc 4.2, 4.3 y 4.10 #1). 44 varillas en abanico, carro con escape
    /// que avanza un paso por letra, campanilla a 5 letras del final de línea y retorno de carro.
    /// Un fallo deja la letra equivocada impresa en rojo, como en una máquina real.
    /// </summary>
    public class Typewriter : MonoBehaviour, IInteractable
    {
        public const int LineChars = 32;
        const float CharStep = 0.0063f;
        const float LineStep = 0.012f;

        enum Mark { Ok, Wrong, Fixed }

        // Mecánica
        Transform carriage, paper, bell, leverPivot;
        readonly Transform[] bars = new Transform[44];
        readonly float[] barT = new float[44];
        readonly Transform[] keys = new Transform[44];
        readonly float[] keyT = new float[44];
        TextMesh paperText, cardText;
        float carriageTarget, carriageX, returnAnim;
        int lineIndex;
        float paperH, paperTargetH;
        Transform paperBox;

        // Estado de la página
        public bool HasPaper { get; private set; }
        public bool Done { get; private set; }
        public bool Jammed { get; private set; }
        public string Phrase { get; private set; } = "";
        public bool Golden { get; private set; }
        readonly List<char> typed = new List<char>();
        readonly List<Mark> marks = new List<Mark>();
        readonly List<int> lineBreaks = new List<int>(); // índices de la frase donde empieza cada línea
        readonly Queue<float> strokes = new Queue<float>();
        float startTime;

        public int Cursor => typed.Count;
        public bool AtLineEnd
        {
            get
            {
                if (!HasPaper || Done) return false;
                int idx = typed.Count, line = LineOf(idx);
                return line + 1 < lineBreaks.Count && idx == lineBreaks[line + 1] - 1;
            }
        }
        public int Errors { get { int e = 0; foreach (var m in marks) if (m == Mark.Wrong) e++; return e; } }
        public int Fixes { get { int e = 0; foreach (var m in marks) if (m == Mark.Fixed) e++; return e; } }

        public static Typewriter Create(Transform anchor)
        {
            var go = new GameObject("MaquinaDeEscribir");
            go.transform.SetParent(anchor, false);
            var tw = go.AddComponent<Typewriter>();
            tw.BuildModel();
            return tw;
        }

        void BuildModel()
        {
            var t = transform;
            var body = Build.Mat(Build.Hex("#2b2d2f"), 0.55f, 0.3f);     // negro esmaltado tipo Underwood
            var chrome = Build.Mat(Build.Hex("#c9ccd0"), 0.85f, 1f);
            var keyMat = Build.Mat(Build.Hex("#1b1b1b"), 0.6f);
            var keyTop = Build.Mat(Build.Hex("#e8e3d3"), 0.5f);
            var rubber = Build.Mat(Build.Hex("#151515"), 0.3f);

            // Colisión cómoda para interactuar
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.12f, 0.02f); col.size = new Vector3(0.52f, 0.26f, 0.42f);

            // Carcasa modelada en Blender (Tools/blender_maquina.py). Si falta, se usa la versión de cajas.
            var model = Resources.Load<GameObject>("Models/MaquinaCuerpo");
            if (model != null)
            {
                var inst = Instantiate(model, t, false);
                inst.name = "Carcasa";
                inst.transform.localPosition = Vector3.zero;
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string n = mats[i] != null ? mats[i].name : "";
                        mats[i] = n.StartsWith("Cromo") ? chrome : n.StartsWith("Goma") ? rubber : n.StartsWith("Oro") ? Build.Mat(Build.Hex("#b8963e"), 0.62f, 1f) : Build.Mat(Build.Hex("#121214"), 0.8f, 0.15f);
                    }
                    r.sharedMaterials = mats;
                }
            }
            else
            {
                Build.Box("Base", t, new Vector3(0, 0.03f, 0), new Vector3(0.44f, 0.06f, 0.38f), body, false);
                Build.Box("Cuerpo", t, new Vector3(0, 0.09f, 0.06f), new Vector3(0.40f, 0.1f, 0.24f), body, false, new Vector3(-8, 0, 0));
                Build.Box("Frontal", t, new Vector3(0, 0.055f, -0.13f), new Vector3(0.40f, 0.05f, 0.12f), body, false, new Vector3(18, 0, 0));
            }

            // Teclado: 4 filas de 11 teclas redondas, escalonadas
            int k = 0;
            for (int row = 0; row < 4; row++)
                for (int c = 0; c < 11; c++)
                {
                    float x = -0.15f + c * 0.03f + row * 0.006f;
                    float z = -0.175f + row * 0.028f;
                    float y = 0.07f + row * 0.012f;
                    var stem = Build.Empty("Tecla" + k, t, new Vector3(x, y, z)).transform;
                    Build.Cyl("Vastago", stem, new Vector3(0, -0.012f, 0), 0.004f, 0.024f, chrome);
                    Build.Cyl("Aro", stem, Vector3.zero, 0.022f, 0.006f, chrome);
                    Build.Cyl("Cara", stem, new Vector3(0, 0.0035f, 0), 0.018f, 0.002f, keyTop);
                    keys[k] = stem; k++;
                }
            Build.Box("Espaciadora", t, new Vector3(0, 0.062f, -0.2f), new Vector3(0.2f, 0.01f, 0.014f), keyMat, false);

            // Cesta de varillas en abanico, con pivote abajo; al golpear giran hacia el punto de impresión
            var basket = Build.Empty("Cesta", t, new Vector3(0, 0.12f, 0.03f)).transform;
            for (int i = 0; i < 44; i++)
            {
                float ang = Mathf.Lerp(-70f, 70f, i / 43f);
                var pivot = Build.Empty("Varilla" + i, basket, Quaternion.Euler(0, ang, 0) * new Vector3(0, 0, -0.075f), new Vector3(0, ang, 0)).transform;
                Build.Box("Brazo", pivot, new Vector3(0, 0.0f, 0.035f), new Vector3(0.003f, 0.004f, 0.07f), chrome, false);
                Build.Box("Tipo", pivot, new Vector3(0, 0.002f, 0.071f), new Vector3(0.004f, 0.008f, 0.004f), keyMat, false);
                bars[i] = pivot;
            }
            Build.Box("Guia", t, new Vector3(0, 0.17f, 0.075f), new Vector3(0.03f, 0.012f, 0.008f), chrome, false);

            // Carro con rodillo, papel y palanca de retorno
            carriage = Build.Empty("Carro", t, new Vector3(0, 0.19f, 0.11f)).transform;
            Build.Cyl("Rodillo", carriage, Vector3.zero, 0.05f, 0.46f, rubber, false, new Vector3(0, 0, 90));
            Build.Cyl("Pomo_I", carriage, new Vector3(-0.25f, 0, 0), 0.04f, 0.03f, keyMat, false, new Vector3(0, 0, 90));
            Build.Cyl("Pomo_D", carriage, new Vector3(0.25f, 0, 0), 0.04f, 0.03f, keyMat, false, new Vector3(0, 0, 90));
            Build.Box("Bastidor", carriage, new Vector3(0, -0.02f, 0.03f), new Vector3(0.5f, 0.02f, 0.05f), body, false);
            Build.Box("Guia_Papel", carriage, new Vector3(0, 0.03f, 0.03f), new Vector3(0.44f, 0.05f, 0.004f), chrome, false, new Vector3(-20, 0, 0));
            leverPivot = Build.Empty("Palanca", carriage, new Vector3(-0.24f, 0.02f, -0.01f)).transform;
            Build.Box("Palanca", leverPivot, new Vector3(-0.04f, 0.015f, -0.03f), new Vector3(0.09f, 0.008f, 0.012f), chrome, false, new Vector3(0, 30, 18));
            bell = Build.Cyl("Campanilla", t, new Vector3(0.17f, 0.15f, 0.12f), 0.03f, 0.012f, chrome).transform;

            // Hoja (A4 escalada a la vista del rodillo) con el texto que se va imprimiendo
            paper = Build.Empty("Hoja", carriage, new Vector3(0, 0.0f, 0.0f), new Vector3(12, 0, 0)).transform;
            paperBox = Build.Box("Papel", paper, new Vector3(0, 0.025f, 0), new Vector3(0.21f, 0.05f, 0.0012f), Build.Mat(Build.Paper, 0.15f), false).transform;
            Build.Box("PapelTrasero", paper, new Vector3(0, -0.06f, 0.04f), new Vector3(0.21f, 0.14f, 0.0012f), Build.Mat(Build.Paper, 0.15f), false, new Vector3(-50, 0, 0));
            paperText = Build.Text("Texto", paper, new Vector3(-0.098f, 0.04f, -0.0012f), "", 0.0105f, Color.black, TextAnchor.UpperLeft);
            paperText.lineSpacing = 1.05f;
            paper.gameObject.SetActive(false);

            // Tarjeta guía sujeta a la máquina (doc 4.3)
            var card = Build.Empty("TarjetaGuia", t, new Vector3(-0.32f, 0.2f, 0.0f), new Vector3(15, -25, 0)).transform;
            Build.Box("Carton", card, Vector3.zero, new Vector3(0.18f, 0.12f, 0.002f), Build.Mat(Build.Hex("#d8c49a"), 0.1f), false);
            Build.Box("Soporte", card, new Vector3(0, -0.09f, 0.01f), new Vector3(0.006f, 0.08f, 0.006f), chrome, false);
            cardText = Build.Text("Frase", card, new Vector3(-0.082f, 0.05f, -0.0015f), "", 0.0075f, Build.Hex("#3a2a14"), TextAnchor.UpperLeft);
        }

        // ------------------------------------------------------------------ IInteractable
        public string Prompt(Game g)
        {
            if (HasPaper) return "[Clic] Sentarse a escribir";
            if (g.S.carriedBlank > 0) return "[Clic] Meter una hoja y escribir";
            if (g.S.deskTray > 0) return "[Clic] Coger hoja de la bandeja y escribir";
            return "Necesitas una hoja en blanco de la montaña";
        }

        public void Interact(Game g)
        {
            if (!HasPaper && !LoadSheet(g)) { Sfx.Play("deny", 0.5f); g.Toast("Trae hojas en blanco de la montaña"); return; }
            g.EnterTyping();
        }

        /// <summary>Mete una hoja: primero de la bandeja de la máquina, si no, de las que lleves.</summary>
        public bool LoadSheet(Game g)
        {
            if (HasPaper) return true;
            if (g.S.deskTray > 0) g.S.deskTray--;
            else if (g.S.carriedBlank > 0) g.S.carriedBlank--;
            else return false;
            Phrase = Phrases.Get(g.S.phraseSeq++, g.S.seed, g.S.goldenCount, out bool golden);
            Golden = golden;
            typed.Clear(); marks.Clear(); strokes.Clear();
            ComputeLines();
            HasPaper = true; Done = false; Jammed = false;
            lineIndex = 0; carriageX = carriageTarget = 0;
            paperH = paperTargetH = 0.05f;
            paper.gameObject.SetActive(true);
            startTime = Time.time;
            Sfx.PlayAt("paper", transform.position, 0.8f);
            Sfx.PlayAt("return", transform.position, 0.35f, 1.3f);
            Refresh();
            return true;
        }

        void ComputeLines()
        {
            lineBreaks.Clear();
            lineBreaks.Add(0);
            int start = 0;
            while (Phrase.Length - start > LineChars)
            {
                int cut = Phrase.LastIndexOf(' ', start + LineChars, LineChars);
                if (cut <= start) cut = start + LineChars;
                lineBreaks.Add(cut + 1);
                start = cut + 1;
            }
        }

        int LineOf(int index)
        {
            int l = 0;
            for (int i = 1; i < lineBreaks.Count; i++) if (index >= lineBreaks[i]) l = i;
            return l;
        }

        int LineEnd(int line) => line + 1 < lineBreaks.Count ? lineBreaks[line + 1] - 1 : Phrase.Length;

        // ------------------------------------------------------------------ escritura
        public static bool Matches(char typedC, char expected)
        {
            if (typedC == expected) return true;
            return Fold(typedC) == Fold(expected) && char.IsUpper(typedC) == char.IsUpper(expected);
        }

        /// <summary>Las tildes son opcionales en el prototipo: «a» vale por «á», «n» por «ñ».</summary>
        static char Fold(char c)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'á': case 'à': case 'ä': return 'a';
                case 'é': case 'è': case 'ë': return 'e';
                case 'í': case 'ì': case 'ï': return 'i';
                case 'ó': case 'ò': case 'ö': return 'o';
                case 'ú': case 'ù': case 'ü': return 'u';
                case 'ñ': return 'n';
                case '—': case '–': return '-';
                case '¡': return '!';
                case '¿': return '?';
                case '«': case '»': case '“': case '”': return '"';
                default: return char.ToLowerInvariant(c);
            }
        }

        public void OnChar(Game g, char c)
        {
            if (!HasPaper || Done || Jammed) return;
            int idx = typed.Count;
            char expected = Phrase[idx];
            int line = LineOf(idx);
            bool atLineEnd = line + 1 < lineBreaks.Count && idx == lineBreaks[line + 1] - 1;

            // Atasco: si tecleas más rápido de lo que aguanta la mecánica
            float now = Time.time;
            strokes.Enqueue(now);
            while (strokes.Count > 0 && now - strokes.Peek() > 0.6f) strokes.Dequeue();
            if (!Econ.NoJams(g.S) && strokes.Count > Econ.KeyCap(g.S) * 0.6f + 1)
            {
                Jammed = true; g.S.everJam = true;
                int a = Random.Range(18, 26);
                barT[a] = 0.5f; barT[a + 1] = 0.5f;
                Sfx.PlayAt("jam", transform.position, 0.9f);
                g.Toast("¡Varillas atascadas! Haz clic para soltarlas");
                return;
            }

            bool ok = atLineEnd ? (c == ' ' || c == '\n') : Matches(c, expected);
            typed.Add(atLineEnd ? ' ' : c);
            marks.Add(ok ? Mark.Ok : Mark.Wrong);
            if (!ok) g.S.everError = true;

            if (!atLineEnd)
            {
                int bar = (int)(Phrases.Hash(char.ToLowerInvariant(c)) % 44);
                if (c != ' ') barT[bar] = 1f;
                keyT[bar] = 1f;
                Sfx.PlayAt(ok ? (c == ' ' ? "paper" : "key") : "keyBad", transform.position + Vector3.up * 0.1f, c == ' ' ? 0.25f : 0.9f, c == ' ' ? 2f : 1f);
                carriageTarget -= CharStep;
                if (LineEnd(line) - (idx + 1) == 5) Sfx.PlayAt("bell", bell.position, 0.6f);
            }
            else CarriageReturn(line + 1);

            if (typed.Count >= Phrase.Length)
            {
                Done = true;
                Sfx.PlayAt("bell", bell.position, 0.5f, 1.08f);
                g.OnPageTypedComplete();
            }
            Refresh();
        }

        void CarriageReturn(int newLine)
        {
            lineIndex = newLine;
            carriageTarget = 0;
            paperTargetH += LineStep;
            returnAnim = 0.45f;
            Sfx.PlayAt("return", transform.position, 0.8f);
        }

        public void Backspace(Game g)
        {
            if (!HasPaper || Done || Jammed || typed.Count == 0) return;
            if (g.S.Level("corrector") == 0) { g.Toast("Sin corrector líquido no se puede borrar (cómpralo en el tablón)"); Sfx.Play("deny", 0.4f); return; }
            int last = typed.Count - 1;
            if (marks[last] != Mark.Wrong) { Sfx.Play("deny", 0.3f); return; }
            // Corrector: se tapa la letra y se reescribe a mano la correcta
            marks[last] = Mark.Fixed;
            typed[last] = Phrase[last];
            Sfx.PlayAt("paper", transform.position, 0.5f, 1.5f);
            Refresh();
        }

        public void Unjam()
        {
            if (!Jammed) return;
            Jammed = false;
            strokes.Clear();
            for (int i = 0; i < 44; i++) barT[i] = 0;
            Sfx.PlayAt("key", transform.position, 0.8f, 0.6f);
        }

        /// <summary>Saca la hoja terminada. Devuelve la página con su calidad y valor.</summary>
        public PageData Extract(Game g)
        {
            if (!HasPaper || !Done) return null;
            int n = Phrase.Length;
            int errors = Errors, fixes = Fixes;
            bool impeccable = errors == 0 && fixes == 0;
            float acc = Mathf.Clamp01((n - errors - 0.3f * fixes) / (float)n);
            float quality = impeccable ? 2f : Mathf.Lerp(0.3f, 1f, acc * acc * acc);
            var page = new PageData { text = Phrase, quality = quality, golden = Golden, hand = true };
            page.value = g.PageValueHand(n, quality, impeccable, Golden);
            g.RegisterHandPage(page, impeccable, Time.time - startTime);
            HasPaper = false; Done = false;
            paper.gameObject.SetActive(false);
            carriageTarget = 0; returnAnim = 0.45f;
            Sfx.PlayAt("paper", transform.position, 1f, 0.8f);
            Sfx.PlayAt("return", transform.position, 0.6f, 1.2f);
            return page;
        }

        // ------------------------------------------------------------------ vista
        void Refresh()
        {
            // Texto en el papel: solo lo tecleado, con los fallos en rojo
            var sb = new StringBuilder();
            for (int i = 0; i < typed.Count; i++)
            {
                if (lineBreaks.Contains(i) && i > 0) sb.Append('\n');
                if (i > 0 && lineBreaks.Contains(i + 1) && i + 1 < Phrase.Length && typed[i] == ' ') continue;
                char ch = typed[i];
                string s = ch == '<' ? "‹" : ch.ToString();
                if (marks[i] == Mark.Wrong) sb.Append("<color=#b3121b>").Append(s).Append("</color>");
                else if (marks[i] == Mark.Fixed) sb.Append("<color=#555555>").Append(s).Append("</color>");
                else sb.Append(s);
            }
            paperText.text = sb.ToString();
            cardText.text = Wrap(Phrase, 26);
        }

        static string Wrap(string s, int width)
        {
            var sb = new StringBuilder(); int col = 0;
            foreach (var word in s.Split(' '))
            {
                if (col + word.Length > width && col > 0) { sb.Append('\n'); col = 0; }
                else if (col > 0) { sb.Append(' '); col++; }
                sb.Append(word); col += word.Length;
            }
            return sb.ToString();
        }

        /// <summary>Línea actual para el HUD: lo escrito (verde/rojo) y lo que falta (gris).</summary>
        public string HudLine()
        {
            if (!HasPaper) return "";
            int idx = Mathf.Min(typed.Count, Phrase.Length);
            int line = LineOf(Mathf.Min(idx, Phrase.Length - 1));
            if (Done) line = lineBreaks.Count - 1;
            int a = lineBreaks[line], b = LineEnd(line);
            var sb = new StringBuilder();
            for (int i = a; i < b; i++)
            {
                string ch = Phrase[i].ToString();
                if (i < typed.Count)
                {
                    string shown = marks[i] == Mark.Wrong ? (typed[i] == ' ' ? "_" : typed[i].ToString()) : ch;
                    string col = marks[i] == Mark.Ok ? "#7fd18b" : marks[i] == Mark.Fixed ? "#c8c8a0" : "#ff5a5a";
                    sb.Append("<color=").Append(col).Append(">").Append(shown).Append("</color>");
                }
                else if (i == typed.Count) sb.Append("<color=#ffffff><b>").Append(ch == " " ? "·" : ch).Append("</b></color>");
                else sb.Append("<color=#8a8a8a>").Append(ch).Append("</color>");
            }
            if (!Done && line + 1 < lineBreaks.Count)
                sb.Append(typed.Count == b ? "<color=#ffd34d><b> ↵</b></color>" : "<color=#6a6a6a> ↵</color>");
            return sb.ToString();
        }

        public string LineCounter()
        {
            if (!HasPaper) return "";
            return "Línea " + (Mathf.Min(LineOf(Mathf.Min(typed.Count, Phrase.Length - 1)), lineBreaks.Count - 1) + 1) + "/" + lineBreaks.Count;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < 44; i++)
            {
                if (Jammed && barT[i] > 0.4f) { bars[i].localRotation = Quaternion.Euler(-62f, bars[i].localEulerAngles.y, 0); continue; }
                if (barT[i] > 0) barT[i] = Mathf.Max(0, barT[i] - dt * 9f);
                float strike = barT[i] > 0.6f ? (1 - barT[i]) / 0.4f : barT[i] / 0.6f;
                float ang = Mathf.Lerp(0, -78f, Mathf.Clamp01(strike));
                bars[i].localRotation = Quaternion.Euler(ang, Mathf.Lerp(-70f, 70f, i / 43f), 0);
                if (keyT[i] > 0) keyT[i] = Mathf.Max(0, keyT[i] - dt * 12f);
                if (keys[i]) keys[i].localPosition = new Vector3(keys[i].localPosition.x, 0.07f + (i / 11) * 0.012f - keyT[i] * 0.008f, keys[i].localPosition.z);
            }
            if (returnAnim > 0) returnAnim -= dt;
            carriageX = Mathf.Lerp(carriageX, carriageTarget, dt * (returnAnim > 0 ? 9f : 30f));
            carriage.localPosition = new Vector3(0.1f + carriageX, 0.19f, 0.11f);
            paperH = Mathf.Lerp(paperH, paperTargetH, dt * 12f);
            paperBox.localScale = new Vector3(0.21f, paperH, 0.0012f);
            paperBox.localPosition = new Vector3(0, paperH / 2f, 0);
            paperText.transform.localPosition = new Vector3(-0.098f, paperH - 0.008f, -0.0012f);
            leverPivot.localRotation = Quaternion.Euler(0, returnAnim > 0 ? Mathf.Sin(returnAnim / 0.45f * Mathf.PI) * 35f : 0, 0);
        }
    }
}
