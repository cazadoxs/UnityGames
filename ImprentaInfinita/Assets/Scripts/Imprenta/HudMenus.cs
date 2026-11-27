using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Menús nuevos, inspirados en los juegos de referencia: pantalla de título y pausa oscuras con barra amarilla,
    /// catálogo de construcción por categorías con tarjetas, árbol de mejoras en un lienzo con líneas doradas
    /// en ángulo recto y panel de detalle, y la noche con el parte y el correo del día.
    /// </summary>
    public partial class Hud
    {
        static readonly Color Gold = new Color(0.86f, 0.67f, 0.17f), Ink = new Color(0.055f, 0.065f, 0.06f), Panel2 = new Color(0.09f, 0.105f, 0.1f);
        static readonly Color Grey = new Color(0.45f, 0.47f, 0.46f), Green = new Color(0.45f, 0.78f, 0.42f);
        GUIStyle cardTitle, cardSmall, menuItem, goldButton, darkButton, labelGold, tiny;
        public static bool TitleScreen;
        bool settingsOpen;
        public static string ShopView = "catalogo"; // o "arbol"

        void InitMenus()
        {
            if (cardTitle != null) return;
            cardTitle = new GUIStyle(mid) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = true };
            cardSmall = new GUIStyle(small) { fontSize = 13, wordWrap = true };
            tiny = new GUIStyle(small) { fontSize = 12 };
            labelGold = new GUIStyle(mid) { fontSize = 14, fontStyle = FontStyle.Bold };
            labelGold.normal.textColor = Gold;
            menuItem = new GUIStyle(mid) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            menuItem.normal.textColor = new Color(0.85f, 0.85f, 0.82f);
            menuItem.hover.textColor = Gold;
            goldButton = new GUIStyle(GUI.skin.button) { font = Build.MonoFont, fontSize = 18, fontStyle = FontStyle.Bold };
            goldButton.normal.background = Tex(Gold); goldButton.hover.background = Tex(Gold * 1.15f); goldButton.active.background = Tex(Gold * 0.8f);
            goldButton.normal.textColor = goldButton.hover.textColor = goldButton.active.textColor = new Color(0.08f, 0.07f, 0.04f);
            darkButton = new GUIStyle(goldButton);
            darkButton.normal.background = Tex(new Color(0.16f, 0.18f, 0.17f)); darkButton.hover.background = Tex(new Color(0.22f, 0.25f, 0.23f)); darkButton.active.background = Tex(new Color(0.1f, 0.1f, 0.1f));
            darkButton.normal.textColor = darkButton.hover.textColor = new Color(0.9f, 0.9f, 0.86f);
        }

        static readonly Dictionary<Color, Texture2D> texCache = new Dictionary<Color, Texture2D>();
        static Texture2D Tex(Color c)
        {
            c.a = 1;
            if (texCache.TryGetValue(c, out var t) && t) return t;
            t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply();
            texCache[c] = t;
            return t;
        }

        void Fill(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = o; }
        void Frame(Rect r, Color c, float w = 2)
        {
            Fill(new Rect(r.x, r.y, r.width, w), c); Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
            Fill(new Rect(r.x, r.y, w, r.height), c); Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        // ------------------------------------------------------------------ título y pausa
        static readonly string[] Tips =
        {
            "Las hojas que caen al final de una cinta se quedan en el suelo hasta que llega el camión.",
            "Puedes recoger una hoja suelta del suelo para leer lo que lleva escrito.",
            "El camión llega por la tarde marcha atrás: deja el muelle libre de hojas para que cargue rápido.",
            "Ficha la salida antes del cierre o te quedarás a oscuras con la linterna.",
            "Con la tecla T abres el árbol de mejoras; con C, el catálogo.",
            "Mantén Shift para correr; la barra de aliento se recupera al andar.",
            "Los hitos de 10, 25, 50 y 100 unidades duplican la producción de un tipo de máquina.",
            "Por la noche no se produce: aprovecha para comprar y leer el correo.",
        };

        void DrawTitleOrPause(Game g, float W, float H)
        {
            InitMenus();
            bool title = TitleScreen;
            // velo oscuro con degradado hacia la izquierda
            Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, title ? 0.35f : 0.55f));
            for (int i = 0; i < 12; i++) Fill(new Rect(0, 0, 640 - i * 30, H), new Color(0.03f, 0.035f, 0.035f, 0.13f));
            Fill(new Rect(70, 120, 8, 260), Gold);
            Shadow(new Rect(100, 110, 600, 80), "IMPRENTA", new GUIStyle(big) { fontSize = 72 });
            Shadow(new Rect(100, 185, 600, 80), "<color=#dba92b>INFINITA</color>", new GUIStyle(big) { fontSize = 72 });
            Shadow(new Rect(104, 270, 560, 30), title ? "Una montaña de papel. Una máquina de escribir. Un día detrás de otro." : "Pausa · Día " + g.S.day + " · Acto " + g.S.act + ": " + Progress.ActNames[g.S.act], new GUIStyle(small) { fontSize = 16 });

            float y = 420;
            if (settingsOpen) { DrawSettings(g, 100, y); }
            else
            {
                if (MenuItem(new Rect(100, y, 500, 52), title ? (g.S.sheetsPicked > 0 || g.S.day > 1 ? "Continuar" : "Empezar") : "Continuar")) { TitleScreen = false; g.Resume(); }
                y += 64;
                if (MenuItem(new Rect(100, y, 500, 52), "Ajustes")) settingsOpen = true;
                y += 64;
                if (!title)
                {
                    if (MenuItem(new Rect(100, y, 500, 52), "Árbol de mejoras")) { TitleScreen = false; g.OpenShop(null, true); }
                    y += 64;
                }
                if (MenuItem(new Rect(100, y, 500, 52), "Borrar partida")) confirmReset = true;
                y += 64;
                if (MenuItem(new Rect(100, y, 500, 52), "Salir")) Application.Quit();
                y += 64;
                if (confirmReset)
                {
                    Fill(new Rect(100, y + 10, 500, 90), new Color(0.25f, 0.06f, 0.05f, 0.9f));
                    GUI.Label(new Rect(116, y + 16, 470, 30), "¿Seguro? Se pierde todo el progreso.", small);
                    if (GUI.Button(new Rect(116, y + 52, 200, 38), "Sí, borrar", goldButton)) { confirmReset = false; TitleScreen = false; g.ResetSave(); }
                    if (GUI.Button(new Rect(330, y + 52, 200, 38), "No", darkButton)) confirmReset = false;
                }
            }

            // consejo rotativo, como en las pantallas de carga
            int tip = (int)(Time.unscaledTime / 7f) % Tips.Length;
            var tr = new Rect(100, H - 150, 640, 90);
            Fill(tr, new Color(0.05f, 0.06f, 0.055f, 0.85f));
            Fill(new Rect(tr.x, tr.y, 4, tr.height), Gold);
            GUI.Label(new Rect(tr.x + 18, tr.y + 10, 600, 20), "CONSEJO", labelGold);
            GUI.Label(new Rect(tr.x + 18, tr.y + 34, 610, 50), Tips[tip], new GUIStyle(small) { wordWrap = true });
            if (!title) DrawMeta(g, W, H);
            if (!title)
            {
                var S = g.S;
                GUI.Label(new Rect(W - 520, H - 60, 500, 40), "<color=#8a8a8a>" + Mathf.FloorToInt(S.playTime / 60) + " min jugados · " + Fmt.Int(S.sheetsPicked) + " hojas sacadas · " + Fmt.Money(S.lifetimeEarned) + " ganados</color>", new GUIStyle(tiny) { alignment = TextAnchor.MiddleRight, richText = true });
            }
        }
        bool confirmReset;

        bool MenuItem(Rect r, string text)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            if (hover) { Fill(new Rect(r.x - 30, r.y + 10, 6, r.height - 20), Gold); Fill(r, new Color(1, 1, 1, 0.04f)); }
            return GUI.Button(r, (hover ? "<color=#dba92b>" : "") + text.ToUpperInvariant() + (hover ? "</color>" : ""), new GUIStyle(menuItem) { richText = true, normal = { background = null }, hover = { background = null }, active = { background = null } });
        }

        void DrawSettings(Game g, float x, float y)
        {
            var r = new Rect(x, y, 560, 380);
            Fill(r, new Color(0.05f, 0.06f, 0.055f, 0.9f));
            GUI.Label(new Rect(x + 20, y + 14, 500, 24), "AJUSTES", labelGold);
            float yy = y + 54;
            GUI.Label(new Rect(x + 20, yy, 500, 24), "Sensibilidad del ratón  " + g.player.sensitivity.ToString("0.00"), small);
            g.player.sensitivity = GUI.HorizontalSlider(new Rect(x + 20, yy + 28, 520, 20), g.player.sensitivity, 0.03f, 0.4f);
            yy += 64;
            GUI.Label(new Rect(x + 20, yy, 500, 24), "Volumen general  " + Mathf.RoundToInt(Sfx.Volume * 100) + " %", small);
            Sfx.Volume = GUI.HorizontalSlider(new Rect(x + 20, yy + 28, 520, 20), Sfx.Volume, 0f, 1f);
            AudioListener.volume = Mathf.Clamp01(Sfx.Volume / 0.8f);
            yy += 64;
            var names = QualitySettings.names;
            int q = QualitySettings.GetQualityLevel();
            GUI.Label(new Rect(x + 20, yy, 300, 30), "Calidad gráfica", small);
            if (GUI.Button(new Rect(x + 300, yy - 4, 240, 36), names.Length > 0 ? names[q] : "—", darkButton)) QualitySettings.SetQualityLevel((q + 1) % Mathf.Max(1, names.Length), true);
            yy += 50;
            GUI.Label(new Rect(x + 20, yy, 300, 30), "Pantalla completa", small);
            if (GUI.Button(new Rect(x + 300, yy - 4, 240, 36), Screen.fullScreen ? "Sí" : "No", darkButton)) Screen.fullScreen = !Screen.fullScreen;
            yy += 50;
            GUI.Label(new Rect(x + 20, yy, 300, 30), "Campo de visión " + Mathf.RoundToInt(g.player.cam.fieldOfView), small);
            g.player.SetFov(GUI.HorizontalSlider(new Rect(x + 300, yy + 8, 240, 20), g.player.cam.fieldOfView, 55f, 95f));
            if (GUI.Button(new Rect(x + 20, y + r.height - 54, 200, 40), "← Volver", goldButton)) settingsOpen = false;
        }

        // ------------------------------------------------------------------ catálogo + árbol
        void DrawShop(Game g, float W, float H)
        {
            InitMenus();
            var S = g.S;
            Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.6f));
            var r = new Rect(60, 60, W - 120, H - 120);
            Fill(r, Ink);
            Frame(r, new Color(0.2f, 0.22f, 0.21f), 1);
            // barra superior con pestañas, dinero y cerrar
            Fill(new Rect(r.x, r.y, r.width, 64), Panel2);
            Fill(new Rect(r.x, r.y + 63, r.width, 1), Gold * 0.6f);
            if (Tab(new Rect(r.x + 20, r.y + 12, 260, 40), "CATÁLOGO  [C]", ShopView == "catalogo")) { ShopView = "catalogo"; g.shopFocus = null; }
            if (Tab(new Rect(r.x + 290, r.y + 12, 320, 40), "ÁRBOL DE MEJORAS  [T]", ShopView == "arbol")) ShopView = "arbol";
            Shadow(new Rect(r.xMax - 640, r.y + 12, 460, 40), "<color=#dba92b>" + Fmt.Money(S.money) + "</color>  <size=15>" + Fmt.Money(g.factory.income) + "/s</size>", new GUIStyle(big) { fontSize = 26, alignment = TextAnchor.MiddleRight });
            if (GUI.Button(new Rect(r.xMax - 160, r.y + 14, 140, 36), "CERRAR  Esc", darkButton)) { g.CloseShop(); return; }
            var body = new Rect(r.x, r.y + 64, r.width, r.height - 64);
            if (ShopView == "arbol") DrawTree(g, body);
            else if (S.act < 2) GUI.Label(new Rect(body.x + 40, body.y + 40, 900, 40), "El catálogo de máquinas se abre en el acto 2. El árbol ya puedes mirarlo.", mid);
            else if (g.shopFocus != null) DrawObjectSheet(g, new Rect(body.x + 30, body.y + 20, body.width - 60, body.height - 40));
            else DrawCatalog(g, body);
            GUI.Label(new Rect(r.x + 20, r.yMax - 28, 900, 24), "<color=#6f6f6f>Clic para elegir · arrastra el lienzo del árbol para moverte · rueda para subir y bajar</color>", new GUIStyle(tiny) { richText = true });
        }

        bool Tab(Rect r, string text, bool sel)
        {
            if (sel) { Fill(r, new Color(1, 1, 1, 0.05f)); Fill(new Rect(r.x, r.yMax - 3, r.width, 3), Gold); }
            var st = new GUIStyle(labelGold) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = sel ? Gold : new Color(0.75f, 0.75f, 0.72f);
            return GUI.Button(r, text, st);
        }

        static readonly string[] Cats = { "Todo", "Disponible", "Recogida", "Escritura", "Impresión", "Ventanilla", "Lo imposible" };
        string cat = "Disponible";
        string selProducer;
        Vector2 catScroll;

        static string CatOf(ProducerDef p)
        {
            if (p.IsPicker) return "Recogida";
            switch (p.key)
            {
                case "mecanografo": case "equipo": case "pc": case "macrobot": case "ia": case "redia": return "Escritura";
                case "dependiente": case "contratos": return "Ventanilla";
                case "cuantica": case "fractal": case "babel": return "Lo imposible";
            }
            return "Impresión";
        }

        static Color CatColor(string c)
        {
            switch (c)
            {
                case "Recogida": return new Color(0.85f, 0.45f, 0.15f);
                case "Escritura": return new Color(0.3f, 0.55f, 0.8f);
                case "Impresión": return new Color(0.35f, 0.65f, 0.45f);
                case "Ventanilla": return new Color(0.75f, 0.35f, 0.4f);
            }
            return new Color(0.6f, 0.45f, 0.85f);
        }

        void DrawCatalog(Game g, Rect body)
        {
            var S = g.S;
            // barra lateral de categorías
            var side = new Rect(body.x, body.y, 230, body.height);
            Fill(side, Panel2);
            float y = side.y + 20;
            foreach (var c in Cats)
            {
                int n = 0;
                foreach (var p in Progress.Producers) if (InCat(S, p, c)) n++;
                bool sel = cat == c;
                var rr = new Rect(side.x + 10, y, side.width - 20, 38);
                if (sel) { Fill(rr, new Color(1, 1, 1, 0.06f)); Fill(new Rect(rr.x, rr.y, 4, rr.height), Gold); }
                if (GUI.Button(rr, "  " + c + "  <color=#6f6f6f>" + n + "</color>", new GUIStyle(mid) { fontSize = 16, alignment = TextAnchor.MiddleLeft, richText = true, normal = { textColor = sel ? Gold : new Color(0.8f, 0.8f, 0.78f) } })) { cat = c; catScroll = Vector2.zero; }
                y += 44;
            }
            // detalle a la derecha
            var det = new Rect(body.xMax - 420, body.y, 420, body.height);
            Fill(det, Panel2);
            var grid = new Rect(side.xMax + 20, body.y + 20, det.x - side.xMax - 40, body.height - 60);
            var list = new List<ProducerDef>();
            foreach (var p in Progress.Producers) if (InCat(S, p, cat)) list.Add(p);
            float cw = 210, ch = 190, gap = 14;
            int cols = Mathf.Max(1, (int)((grid.width - 20) / (cw + gap)));
            int rows = Mathf.CeilToInt(list.Count / (float)cols);
            catScroll = GUI.BeginScrollView(grid, catScroll, new Rect(0, 0, grid.width - 20, rows * (ch + gap)));
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                var cr = new Rect((i % cols) * (cw + gap), (i / cols) * (ch + gap), cw, ch);
                DrawCard(g, p, cr);
            }
            GUI.EndScrollView();
            if (selProducer == null && list.Count > 0) selProducer = list[0].key;
            var sp = selProducer != null ? Progress.P(selProducer) : null;
            if (sp != null) DrawProducerDetail(g, sp, det);
        }

        static bool InCat(SaveData S, ProducerDef p, string c)
        {
            if (p.act > S.act + 1) return false;
            if (c == "Todo") return true;
            if (c == "Disponible") return p.act <= S.act;
            return CatOf(p) == c;
        }

        void DrawCard(Game g, ProducerDef p, Rect cr)
        {
            var S = g.S;
            bool locked = S.act < p.act;
            int n = S.Count(p.key);
            double c1 = p.Cost(n);
            bool afford = !locked && S.money >= c1;
            bool sel = selProducer == p.key;
            Fill(cr, sel ? new Color(0.14f, 0.16f, 0.15f) : new Color(0.1f, 0.115f, 0.11f));
            Frame(cr, sel ? Gold : new Color(0.2f, 0.22f, 0.21f), sel ? 2 : 1);
            // «foto» del objeto: bloque de color de su categoría con sus iniciales
            var pic = new Rect(cr.x + 10, cr.y + 10, cr.width - 20, 90);
            var cc = CatColor(CatOf(p));
            Fill(pic, locked ? new Color(0.15f, 0.15f, 0.15f) : cc * 0.45f);
            Fill(new Rect(pic.x, pic.yMax - 4, pic.width, 4), locked ? Grey : cc);
            string ini = Initials(p.name);
            GUI.Label(pic, locked ? "BLOQUEADO" : ini, new GUIStyle(big) { fontSize = locked ? 18 : 40, alignment = TextAnchor.MiddleCenter, normal = { textColor = locked ? Grey : new Color(1, 1, 1, 0.85f) } });
            if (n > 0) GUI.Label(new Rect(pic.xMax - 70, pic.y + 4, 64, 22), "×" + n, new GUIStyle(labelGold) { alignment = TextAnchor.MiddleRight });
            GUI.Label(new Rect(cr.x + 10, cr.y + 106, cr.width - 20, 44), p.name, cardTitle);
            GUI.Label(new Rect(cr.x + 10, cr.y + 156, cr.width - 20, 24), locked ? "<color=#7a7a7a>Acto " + p.act + "</color>" : (afford ? "<color=#dba92b>" : "<color=#b36a5a>") + Fmt.Money(c1) + "</color>", new GUIStyle(cardSmall) { richText = true, fontSize = 16, fontStyle = FontStyle.Bold });
            if (GUI.Button(cr, GUIContent.none, GUIStyle.none)) selProducer = p.key;
        }

        static string Initials(string s)
        {
            var parts = s.Split(' ');
            string r = "";
            foreach (var w in parts) if (w.Length > 2 && r.Length < 3) r += char.ToUpperInvariant(w[0]);
            return r.Length > 0 ? r : s.Substring(0, 1);
        }

        void DrawProducerDetail(Game g, ProducerDef p, Rect det)
        {
            var S = g.S;
            bool locked = S.act < p.act;
            int n = S.Count(p.key);
            float x = det.x + 24, w = det.width - 48, y = det.y + 24;
            GUI.Label(new Rect(x, y, w, 20), CatOf(p).ToUpperInvariant(), labelGold); y += 26;
            GUI.Label(new Rect(x, y, w, 60), p.name, new GUIStyle(cardTitle) { fontSize = 24 }); y += 66;
            GUI.Label(new Rect(x, y, w, 110), p.desc, new GUIStyle(small) { wordWrap = true }); y += 116;
            string what = p.IsPicker ? Fmt.Num(p.sheets * Progress.ProducerMult(S, p)) + " hojas/s de la montaña"
                                     : Fmt.Money(p.income * Progress.ProducerMult(S, p) * Progress.GlobalMult(S)) + "/s cada uno";
            Fill(new Rect(x, y, w, 1), new Color(1, 1, 1, 0.08f)); y += 10;
            GUI.Label(new Rect(x, y, w, 24), "Produce  <color=#9dd>" + what + "</color>", new GUIStyle(small) { richText = true }); y += 28;
            GUI.Label(new Rect(x, y, w, 24), "Tienes  <color=#dba92b>" + n + "</color>" + (Progress.MilestoneMult(n) > 1 ? "  (hito ×" + Progress.MilestoneMult(n) + ")" : "") + " · siguiente hito " + NextMilestone(n), new GUIStyle(small) { richText = true, wordWrap = true }); y += 50;
            if (locked) { GUI.Label(new Rect(x, y, w, 50), "<color=#b36a5a>Se abre en el acto " + p.act + ": " + Progress.ActNames[p.act] + "</color>", new GUIStyle(small) { richText = true, wordWrap = true }); return; }
            double c1 = p.Cost(n);
            int afford = g.AffordableCount(p);
            GUI.enabled = S.money >= c1;
            if (GUI.Button(new Rect(x, y, w, 52), "COMPRAR  " + Fmt.Money(c1), goldButton)) g.BuyProducer(p, 1);
            y += 62;
            GUI.enabled = afford >= 10;
            if (GUI.Button(new Rect(x, y, w / 2 - 6, 42), "×10", darkButton)) g.BuyProducer(p, 10);
            GUI.enabled = afford >= 1;
            if (GUI.Button(new Rect(x + w / 2 + 6, y, w / 2 - 6, 42), "MÁX (" + afford + ")", darkButton)) g.BuyProducer(p, afford);
            y += 52;
            GUI.enabled = n > 0;
            if (GUI.Button(new Rect(x, y, w, 42), "HOJA DE MEJORAS", darkButton)) g.shopFocus = p.key;
            GUI.enabled = true;
        }

        // ------------------------------------------------------------------ árbol
        Vector2 treePan = new Vector2(40, 40);
        string selNode;
        static readonly string[] Branches = { "Papel", "Escritura", "Impresión", "Logística", "Negocio", "Investigación" };
        Dictionary<string, Rect> layout;
        const float NW = 230, NH = 74;

        void BuildLayout()
        {
            layout = new Dictionary<string, Rect>();
            var depth = new Dictionary<string, int>();
            int D(NodeDef n)
            {
                if (depth.TryGetValue(n.key, out var d)) return d;
                depth[n.key] = 0;
                int best = 0;
                foreach (var r in n.requires)
                {
                    if (r.StartsWith("p:")) continue;
                    string k = r.Contains(":") ? r.Substring(0, r.IndexOf(':')) : r;
                    var pn = Progress.Nodes.Find(x => x.key == k);
                    if (pn != null) best = Mathf.Max(best, D(pn) + 1);
                }
                return depth[n.key] = best;
            }
            float top = 30;
            foreach (var b in Branches)
            {
                var slots = new Dictionary<int, int>();
                int maxSlots = 1;
                foreach (var n in Progress.Nodes)
                {
                    if (n.branch != b) continue;
                    int d = D(n);
                    slots.TryGetValue(d, out int s);
                    layout[n.key] = new Rect(170 + d * (NW + 70), top + s * (NH + 18), NW, NH);
                    slots[d] = s + 1;
                    maxSlots = Mathf.Max(maxSlots, s + 1);
                }
                layout["#" + b] = new Rect(0, top, 150, maxSlots * (NH + 18) - 18);
                top += maxSlots * (NH + 18) + 40;
            }
        }

        void DrawTree(Game g, Rect body)
        {
            var S = g.S;
            if (layout == null) BuildLayout();
            var det = new Rect(body.xMax - 400, body.y, 400, body.height);
            var canvas = new Rect(body.x, body.y, det.x - body.x, body.height);
            Fill(canvas, new Color(0.04f, 0.05f, 0.045f));
            // arrastrar y rueda para moverse por el lienzo
            var e = Event.current;
            if (canvas.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag) { treePan += e.delta; e.Use(); }
                if (e.type == EventType.ScrollWheel) { treePan.y -= e.delta.y * 30f; e.Use(); }
            }
            GUI.BeginGroup(canvas);
            // rejilla tenue
            for (float gx = Mathf.Repeat(treePan.x, 40); gx < canvas.width; gx += 40) Fill(new Rect(gx, 0, 1, canvas.height), new Color(1, 1, 1, 0.025f));
            for (float gy = Mathf.Repeat(treePan.y, 40); gy < canvas.height; gy += 40) Fill(new Rect(0, gy, canvas.width, 1), new Color(1, 1, 1, 0.025f));
            // ramas
            foreach (var b in Branches)
            {
                var br = layout["#" + b]; br.position += treePan;
                Fill(new Rect(br.x + 14, br.y, 3, br.height), Gold * 0.7f);
                GUI.Label(new Rect(br.x + 26, br.y, 140, 24), b.ToUpperInvariant(), labelGold);
                int own = 0, tot = 0;
                foreach (var n in Progress.Nodes) if (n.branch == b) { tot++; if (S.Node(n.key) > 0) own++; }
                GUI.Label(new Rect(br.x + 26, br.y + 22, 140, 20), own + " / " + tot, tiny);
            }
            // líneas en ángulo recto (doradas si el requisito está comprado)
            foreach (var n in Progress.Nodes)
            {
                var cr = layout[n.key]; cr.position += treePan;
                foreach (var r in n.requires)
                {
                    if (r.StartsWith("p:")) continue;
                    string k = r.Contains(":") ? r.Substring(0, r.IndexOf(':')) : r;
                    if (!layout.TryGetValue(k, out var pr)) continue;
                    pr.position += treePan;
                    var col = S.Node(k) > 0 ? Gold : new Color(0.3f, 0.32f, 0.31f);
                    float x0 = pr.xMax, y0 = pr.center.y, x1 = cr.x, y1 = cr.center.y, mx = x0 + 30;
                    if (x1 <= x0) { mx = Mathf.Max(x0, x1) + 20; }
                    Fill(new Rect(x0, y0 - 1, mx - x0, 2), col);
                    Fill(new Rect(mx - 1, Mathf.Min(y0, y1) - 1, 2, Mathf.Abs(y1 - y0) + 2), col);
                    Fill(new Rect(Mathf.Min(mx, x1), y1 - 1, Mathf.Abs(x1 - mx), 2), col);
                }
            }
            // nodos
            foreach (var n in Progress.Nodes)
            {
                var cr = layout[n.key]; cr.position += treePan;
                if (cr.xMax < 0 || cr.yMax < 0 || cr.x > canvas.width || cr.y > canvas.height) continue;
                int lvl = S.Node(n.key);
                string miss = Progress.Missing(S, n);
                bool full = lvl >= n.maxLevel, owned = lvl > 0, avail = miss == null;
                bool hidden = n.act > S.act + 1;
                Color bg = full ? new Color(0.22f, 0.18f, 0.07f) : owned ? new Color(0.15f, 0.14f, 0.09f) : avail ? new Color(0.12f, 0.14f, 0.13f) : new Color(0.08f, 0.085f, 0.085f);
                Color fr = full || owned ? Gold : avail ? new Color(0.8f, 0.8f, 0.76f) : new Color(0.22f, 0.24f, 0.23f);
                Fill(cr, bg);
                Frame(cr, selNode == n.key ? Color.white : fr, selNode == n.key ? 3 : 2);
                // icono cuadrado a la izquierda
                var ic = new Rect(cr.x + 8, cr.y + 10, 54, 54);
                Fill(ic, full || owned ? Gold * 0.85f : avail ? new Color(0.25f, 0.28f, 0.27f) : new Color(0.13f, 0.14f, 0.14f));
                GUI.Label(ic, hidden || !avail && !owned ? "—" : Initials(n.name), new GUIStyle(big) { fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = full || owned ? Ink : new Color(0.85f, 0.85f, 0.8f) } });
                GUI.Label(new Rect(cr.x + 70, cr.y + 6, cr.width - 76, 40), hidden ? "???" : n.name, new GUIStyle(cardTitle) { fontSize = 14, normal = { textColor = avail || owned ? new Color(0.92f, 0.92f, 0.88f) : Grey } });
                string state = full ? "<color=#dba92b>COMPRADO</color>" : !avail && !owned ? "<color=#7a7a7a>BLOQUEADO</color>" : "<color=#dba92b>" + Fmt.Money(n.Cost(lvl)) + "</color>";
                GUI.Label(new Rect(cr.x + 70, cr.y + 48, cr.width - 76, 20), state + (n.maxLevel > 1 ? "  <color=#9a9a9a>" + lvl + "/" + n.maxLevel + "</color>" : ""), new GUIStyle(tiny) { richText = true });
                if (GUI.Button(cr, GUIContent.none, GUIStyle.none)) selNode = n.key;
            }
            GUI.EndGroup();
            DrawNodeDetail(g, det);
        }

        void DrawNodeDetail(Game g, Rect det)
        {
            var S = g.S;
            Fill(det, Panel2);
            Fill(new Rect(det.x, det.y, 1, det.height), Gold * 0.5f);
            var n = selNode != null ? Progress.Nodes.Find(x => x.key == selNode) : null;
            float x = det.x + 24, w = det.width - 48, y = det.y + 24;
            if (n == null)
            {
                GUI.Label(new Rect(x, y, w, 120), "Elige una mejora del árbol para ver qué hace y comprarla.\n\nLas líneas doradas salen de lo que ya tienes.", new GUIStyle(small) { wordWrap = true });
                return;
            }
            int lvl = S.Node(n.key);
            string miss = Progress.Missing(S, n);
            GUI.Label(new Rect(x, y, w, 20), n.branch.ToUpperInvariant(), labelGold); y += 26;
            GUI.Label(new Rect(x, y, w, 60), n.name, new GUIStyle(cardTitle) { fontSize = 24 }); y += 64;
            GUI.Label(new Rect(x, y, w, 100), n.desc, new GUIStyle(small) { wordWrap = true }); y += 104;
            Fill(new Rect(x, y, w, 1), new Color(1, 1, 1, 0.08f)); y += 12;
            GUI.Label(new Rect(x, y, w, 24), "Nivel  <color=#dba92b>" + lvl + " / " + n.maxLevel + "</color>", new GUIStyle(small) { richText = true }); y += 30;
            if (n.requires.Length > 0 || n.act > 2)
            {
                GUI.Label(new Rect(x, y, w, 20), "REQUISITOS", labelGold); y += 24;
                if (n.act > 2) { GUI.Label(new Rect(x, y, w, 22), (S.act >= n.act ? "<color=#7dc96b>✓ " : "<color=#b36a5a>· ") + "Acto " + n.act + "</color>", new GUIStyle(small) { richText = true }); y += 24; }
                foreach (var r in n.requires)
                {
                    bool ok; string label;
                    if (r.StartsWith("p:")) { var pd = Progress.P(r.Substring(2)); ok = S.Count(pd.key) > 0; label = "Tener " + pd.name; }
                    else
                    {
                        int need = 1; string k = r; int c = r.IndexOf(':');
                        if (c > 0) { k = r.Substring(0, c); need = int.Parse(r.Substring(c + 1)); }
                        var nn = Progress.Nodes.Find(z => z.key == k);
                        ok = S.Node(k) >= need; label = (nn != null ? nn.name : k) + (need > 1 ? " nivel " + need : "");
                    }
                    GUI.Label(new Rect(x, y, w, 22), (ok ? "<color=#7dc96b>✓ " : "<color=#b36a5a>· ") + label + "</color>", new GUIStyle(small) { richText = true }); y += 24;
                }
                y += 8;
            }
            if (lvl >= n.maxLevel) { GUI.Label(new Rect(x, y, w, 30), "COMPRADO DEL TODO", labelGold); return; }
            double cost = n.Cost(lvl);
            GUI.Label(new Rect(x, det.yMax - 130, w, 30), Fmt.Money(cost), new GUIStyle(big) { fontSize = 28, normal = { textColor = S.money >= cost ? Gold : new Color(0.7f, 0.42f, 0.35f) } });
            GUI.enabled = miss == null && S.money >= cost;
            if (GUI.Button(new Rect(x, det.yMax - 90, w, 56), lvl > 0 ? "COMPRAR NIVEL " + (lvl + 1) : "COMPRAR", goldButton)) g.BuyNode(n);
            GUI.enabled = true;
            if (miss != null) GUI.Label(new Rect(x, det.yMax - 30, w, 24), "<color=#b36a5a>" + miss + "</color>", new GUIStyle(tiny) { richText = true });
        }

        // ------------------------------------------------------------------ noche: parte y correo
        static readonly string[][] Mail =
        {
            new[] { "Dirección", "Bienvenido a la Imprenta", "Le damos la bienvenida. La montaña no se acaba nunca, pero el sueldo tampoco. Recoja hojas, escriba, venda. Mañana más." },
            new[] { "Anónimo", "Una breve presentación", "Trabajé aquí antes que usted. Si de noche oye hojas que se mueven solas en la montaña, no es el viento. No hay viento en la nave." },
            new[] { "El Diario del Papel", "El precio del folio, por los suelos", "Una imprenta sin nombre inunda el mercado de páginas a precio de saldo. «No sabemos de dónde sacan tanto papel», dicen los competidores." },
            new[] { "Reparto Hermanos Gil", "Sobre el camión", "Nuestro conductor llega marcha atrás a media tarde. Le rogamos que deje las hojas cerca del muelle; él no se baja a buscarlas." },
            new[] { "Dirección", "Puntualidad", "Le recordamos que el reloj de fichar está en el vestuario. Quien no ficha, sale con el vigilante. Y el vigilante no da conversación." },
            new[] { "Anónimo", "Esa cesta no es tuya", "He visto que lees lo que va escrito en las hojas. Hazlo. Alguna no la escribió nadie de aquí." },
            new[] { "El Diario del Papel", "Récord de páginas", "La imprenta del polígono supera en un mes lo que la Biblioteca Nacional cataloga en un año. Los bibliotecarios piden calma." },
            new[] { "Proveedor de tinta", "Oferta", "Cinta de máquina de escribir de primera, a mitad de precio si compra mil cajas. Solo esta semana. Solo para usted." },
            new[] { "Anónimo", "Detrás de la montaña", "Hay una pared al fondo. Nadie ha llegado a verla. Tú vas a ser el primero." },
        };

        void DrawNightNew(Game g, float W, float H)
        {
            InitMenus();
            var S = g.S;
            Fill(new Rect(0, 0, W, H), new Color(0.03f, 0.035f, 0.045f, 1f));
            Fill(new Rect(70, 80, 8, 90), Gold);
            Shadow(new Rect(100, 70, 900, 60), "FIN DEL DÍA " + S.day, new GUIStyle(big) { fontSize = 52 });
            Shadow(new Rect(102, 130, 900, 30), "La nave se queda a oscuras. Mañana a las 8:00, otra vez.", small);
            // parte del día
            var rep = new Rect(100, 200, 620, 420);
            Fill(rep, Panel2); Fill(new Rect(rep.x, rep.y, rep.width, 3), Gold);
            GUI.Label(new Rect(rep.x + 24, rep.y + 18, 500, 22), "PARTE DEL DÍA", labelGold);
            var wd = Workday.I;
            GUI.Label(new Rect(rep.x + 24, rep.y + 54, rep.width - 48, 260), wd != null ? wd.lastReport : "", new GUIStyle(mid) { wordWrap = true, fontSize = 17 });
            GUI.Label(new Rect(rep.x + 24, rep.yMax - 70, rep.width - 48, 50), "Dinero  <color=#dba92b>" + Fmt.Money(S.money) + "</color>\nProducción " + Fmt.Money(g.factory.income) + "/s · reputación " + S.reputation.ToString("0.0"), new GUIStyle(small) { richText = true });
            // correo
            var mail = new Rect(760, 200, Mathf.Min(760, W - 860), 420);
            Fill(mail, new Color(0.93f, 0.91f, 0.85f));
            var m = Mail[(S.day - 1 + Mail.Length) % Mail.Length];
            var ink = new GUIStyle(mid) { wordWrap = true, fontSize = 17, normal = { textColor = new Color(0.15f, 0.14f, 0.12f) } };
            GUI.Label(new Rect(mail.x + 28, mail.y + 20, mail.width - 56, 22), "CORREO · DÍA " + S.day, new GUIStyle(labelGold) { normal = { textColor = new Color(0.55f, 0.4f, 0.1f) } });
            GUI.Label(new Rect(mail.x + 28, mail.y + 50, mail.width - 56, 24), "De: " + m[0], new GUIStyle(ink) { fontSize = 15 });
            GUI.Label(new Rect(mail.x + 28, mail.y + 78, mail.width - 56, 34), m[1], new GUIStyle(ink) { fontSize = 22, fontStyle = FontStyle.Bold });
            Fill(new Rect(mail.x + 28, mail.y + 118, mail.width - 56, 1), new Color(0, 0, 0, 0.2f));
            GUI.Label(new Rect(mail.x + 28, mail.y + 132, mail.width - 56, 260), m[2], ink);
            // acciones
            float by = 660;
            if (GUI.Button(new Rect(100, by, 290, 60), "CATÁLOGO  [C]", darkButton)) { ShopView = "catalogo"; g.OpenShop(null); }
            if (GUI.Button(new Rect(400, by, 320, 60), "ÁRBOL DE MEJORAS  [T]", darkButton)) g.OpenShop(null, true);
            if (GUI.Button(new Rect(760, by, mail.width, 60), "SIGUIENTE DÍA  ▸  Día " + (S.day + 1), goldButton)) g.StartNextDay();
            Shadow(new Rect(100, by + 76, 1200, 30), "<color=#8a8a8a>Por la noche no se produce: es el momento de comprar y planear.</color>", small);
        }
    }
}
