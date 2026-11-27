using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// HUD mínimo (doc 3, regla 2): dinero, ritmo y objetivo. El resto vive en el mundo: contador de la pared,
    /// tablón de corcho, tarjeta guía de la máquina. IMGUI para el prototipo; se rehace en UI Toolkit después.
    /// </summary>
    public partial class Hud : MonoBehaviour
    {
        GUIStyle big, mid, small, center, line, panel, button, title;
        Texture2D white;
        float scale = 1f;

        void Init()
        {
            if (big != null) return;
            white = Texture2D.whiteTexture;
            Font f = Build.MonoFont;
            big = new GUIStyle { fontSize = 30, fontStyle = FontStyle.Bold, richText = true, font = f };
            big.normal.textColor = Color.white;
            mid = new GUIStyle(big) { fontSize = 18, fontStyle = FontStyle.Normal };
            small = new GUIStyle(mid) { fontSize = 15 };
            center = new GUIStyle(mid) { alignment = TextAnchor.MiddleCenter };
            line = new GUIStyle(big) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Normal };
            title = new GUIStyle(big) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
            panel = new GUIStyle { normal = { background = white } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, font = f };
        }

        void Shadow(Rect r, string text, GUIStyle s)
        {
            var c = s.normal.textColor;
            s.normal.textColor = new Color(0, 0, 0, 0.75f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), StripColor(text), s);
            s.normal.textColor = c;
            GUI.Label(r, text, s);
        }

        static string StripColor(string s) => System.Text.RegularExpressions.Regex.Replace(s, "<color=[^>]*>|</color>", "");

        void Box(Rect r, float alpha)
        {
            var c = GUI.color; GUI.color = new Color(0.05f, 0.05f, 0.06f, alpha);
            GUI.DrawTexture(r, white); GUI.color = c;
        }

        void OnGUI()
        {
            var g = Game.I;
            if (g == null) return;
            Init();
            scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float W = Screen.width / scale, H = 1080f;
            var S = g.S;
            if (g.Mode == GameMode.Night) { DrawNight(g, W, H); return; }

            // Arriba a la izquierda: dinero y ritmo
            Box(new Rect(16, 16, 360, 96), 0.45f);
            Shadow(new Rect(30, 22, 340, 40), Fmt.Money(S.money), big);
            double rate = S.act >= 2 && g.factory != null ? g.factory.income + g.EarnRate * 0 : g.EarnRate;
            Shadow(new Rect(30, 62, 340, 24), Fmt.Money(System.Math.Max(rate, g.EarnRate)) + "/s   ·   " + (S.act >= 2 ? "Acto " + S.act + ": " + Progress.ActNames[S.act] : Fmt.Int(S.pagesSold) + " páginas vendidas"), small);
            if (S.inspirationLeft > 0)
                Shadow(new Rect(30, 84, 340, 24), "<color=#ffd34d>Inspiración +50 %  " + Mathf.CeilToInt(S.inspirationLeft) + " s</color>", small);
            else if (S.streak > 0)
                Shadow(new Rect(30, 84, 340, 24), "Racha impecable: " + S.streak + "/" + Econ.StreakForInspiration * (S.streak / Econ.StreakForInspiration + 1), small);

            // Arriba a la derecha: objetivo y terreno
            float ow = 560;
            Box(new Rect(W - ow - 16, 16, ow, 96), 0.45f);
            Shadow(new Rect(W - ow - 2, 22, ow - 24, 50), "<color=#e8b923>OBJETIVO</color>  " + g.Objective(), new GUIStyle(small) { wordWrap = true });
            double area = Econ.FreeArea(S);
            double a0 = S.act <= 1 ? Econ.StartArea : Progress.ActArea[Mathf.Min(S.act, 6)];
            double a1 = S.act <= 1 ? Econ.GoalArea : S.act < 6 ? Progress.ActArea[S.act + 1] : Progress.FullArea;
            if (S.act >= 7) { a0 = 0; a1 = Progress.FullArea; }
            float frac = Mathf.Clamp01((float)((area - a0) / System.Math.Max(1, a1 - a0)));
            var bar = new Rect(W - ow - 2, 80, ow - 30, 14);
            Box(bar, 0.6f);
            var c0 = GUI.color; GUI.color = new Color(0.91f, 0.73f, 0.14f, 0.9f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * frac, bar.height), white); GUI.color = c0;
            Shadow(new Rect(bar.x, bar.y - 4, bar.width, 20), "Terreno libre " + Fmt.Area(area) + " / " + Fmt.Area(a1), new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 13 });

            // Arriba en el centro: reloj de la jornada y pedido del día (doc 4.17)
            var wd = Workday.I;
            if (wd != null && wd.started)
            {
                Box(new Rect(W / 2 - 200, 16, 400, 70), 0.45f);
                bool late = wd.Hour >= wd.End - 0.5f;
                Shadow(new Rect(W / 2 - 190, 18, 380, 34), "Día " + S.day + "   " + (late ? "<color=#ff6a4a>" + wd.ClockText + "</color>" : wd.ClockText), new GUIStyle(mid) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold });
                float of = Mathf.Clamp01((float)(S.dayPages / System.Math.Max(1, S.order)));
                var ob = new Rect(W / 2 - 180, 56, 360, 12);
                Box(ob, 0.6f);
                var c1 = GUI.color; GUI.color = S.orderMet ? new Color(0.35f, 0.8f, 0.4f, 0.9f) : new Color(0.55f, 0.7f, 0.95f, 0.9f);
                GUI.DrawTexture(new Rect(ob.x, ob.y, ob.width * of, ob.height), white); GUI.color = c1;
                Shadow(new Rect(ob.x, ob.y - 5, ob.width, 20), "Pedido " + Fmt.Num(S.dayPages) + " / " + Fmt.Num(S.order) + " pág.", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
                if (Events.Blackout) Shadow(new Rect(W / 2 - 300, 90, 600, 26), "<color=#ff6a4a>APAGÓN · sube los plomos junto al vestuario</color>", center);
                else if (S.boostLeft > 0) Shadow(new Rect(W / 2 - 300, 90, 600, 26), "<color=#ffd34d>Hoja dorada ×7 · " + Mathf.CeilToInt(S.boostLeft) + " s</color>", center);
            }

            // Avisos
            float ty = 130;
            foreach (var t in g.toasts)
            {
                float age = Time.time - t.time;
                var cc = GUI.color; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(4.5f - age));
                Box(new Rect(W / 2 - 330, ty, 660, 32), 0.5f);
                Shadow(new Rect(W / 2 - 330, ty, 660, 32), t.text, center);
                GUI.color = cc;
                ty += 38;
            }

            if (g.Mode == GameMode.Walking) DrawWalking(g, W, H);
            else if (g.Mode == GameMode.Typing) DrawTyping(g, W, H);
            else if (g.Mode == GameMode.Paused) DrawPause(g, W, H);
            else if (g.Mode == GameMode.ActComplete) DrawActComplete(g, W, H);
            else if (g.Mode == GameMode.Menu) DrawShop(g, W, H);
            else if (g.Mode == GameMode.Night) DrawNight(g, W, H);
            else if (g.Mode == GameMode.Computer) DrawComputer(g, W, H);
        }

        void DrawWalking(Game g, float W, float H)
        {
            var p = g.player;
            // Punto de mira
            var cc = GUI.color; GUI.color = new Color(1, 1, 1, 0.85f);
            GUI.DrawTexture(new Rect(W / 2 - 3, H / 2 - 3, 6, 6), white); GUI.color = cc;

            string prompt = null;
            if (p.Focus != null) prompt = p.Focus.Prompt(g);
            else if (p.LookingAtMountain)
            {
                int cap = Econ.Capacity(g.S);
                prompt = g.CarryCount >= cap ? "Vas lleno (" + cap + "). Lleva las hojas a la máquina, a una margarita o al ducto"
                       : Econ.HasGun(g.S) ? "[Mantén clic] Aspirar hojas" : "[Clic] Coger una hoja   (mantén para seguir)";
                if (p.PickProgress > 0 && p.PickProgress < 1 && g.CarryCount < cap)
                {
                    var r = new Rect(W / 2 - 40, H / 2 + 18, 80, 6);
                    Box(r, 0.6f);
                    GUI.DrawTexture(new Rect(r.x, r.y, r.width * p.PickProgress, r.height), white);
                }
            }
            if (!string.IsNullOrEmpty(prompt)) Shadow(new Rect(W / 2 - 500, H / 2 + 30, 1000, 30), prompt, center);

            // Abajo a la izquierda: lo que llevas
            var S = g.S;
            Box(new Rect(16, H - 92, 470, 76), 0.45f);
            Shadow(new Rect(30, H - 86, 450, 26), Econ.ContainerName(S) + "  " + g.CarryCount + "/" + Econ.Capacity(S), mid);
            Shadow(new Rect(30, H - 58, 450, 26), S.carriedBlank + " en blanco · " + S.carriedPages.Count + " escritas (" + Fmt.Money(g.CarriedPagesValue()) + ")", small);
            // barra de aliento (solo si se ha gastado)
            if (p.Stamina < 0.999f)
            {
                var sb = new Rect(16, H - 104, 470, 6);
                Box(sb, 0.5f);
                var c2 = GUI.color; GUI.color = p.Stamina < 0.25f ? new Color(0.9f, 0.4f, 0.3f, 0.9f) : new Color(0.86f, 0.67f, 0.17f, 0.9f);
                GUI.DrawTexture(new Rect(sb.x, sb.y, sb.width * p.Stamina, sb.height), white); GUI.color = c2;
            }
            Shadow(new Rect(W - 620, H - 40, 604, 24), "<color=#9a9a9a>WASD andar · Shift correr · Clic/E usar" + (S.act >= 2 ? " · C catálogo" : "") + " · T árbol · Esc pausa</color>", new GUIStyle(small) { alignment = TextAnchor.MiddleRight, fontSize = 13 });
        }

        void DrawTyping(Game g, float W, float H)
        {
            var tw = g.typewriter;
            Box(new Rect(W / 2 - 720, H - 200, 1440, 170), 0.6f);
            Shadow(new Rect(W / 2 - 700, H - 196, 1400, 30), tw.LineCounter() + (tw.Golden ? "   <color=#ffd34d>★ FRASE DORADA</color>" : ""), new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(W / 2 - 720, H - 165, 1440, 60), tw.HudLine(), line);
            string status;
            if (tw.Jammed) status = "<color=#ff6a4a>¡Varillas atascadas! Haz clic para soltarlas (vas más rápido que la máquina)</color>";
            else if (tw.Done) status = "<color=#ffd34d>Página terminada · Enter para sacarla</color>  " + (tw.Errors == 0 && tw.Fixes == 0 ? "impecable" : tw.Errors + " fallos");
            else if (tw.AtLineEnd) status = "<color=#ffd34d>Fin de línea: Espacio o Enter (retorno de carro)</color>";
            else status = "Fallos: " + tw.Errors + (g.S.Level("corrector") > 0 ? "   ·   Retroceso: corrector" : "") + "   ·   Esc: levantarse";
            Shadow(new Rect(W / 2 - 700, H - 98, 1400, 30), status, center);
            if (!string.IsNullOrEmpty(g.lastPageReport))
                Shadow(new Rect(W / 2 - 700, H - 66, 1400, 26), "Última hoja: " + g.lastPageReport, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
            Shadow(new Rect(W / 2 - 400, H - 236, 800, 30), "Hojas: " + (g.S.deskTray + g.S.carriedBlank) + " en blanco a mano" + (g.handBelt ? "" : " · " + g.S.carriedPages.Count + " escritas contigo"), new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }

        void DrawPause(Game g, float W, float H) => DrawTitleOrPause(g, W, H);

        void DrawActComplete(Game g, float W, float H)
        {
            Box(new Rect(0, 0, W, H), 0.7f);
            var S = g.S;
            int done = Mathf.Max(1, g.completedAct > 0 ? g.completedAct : S.act - 1);
            Shadow(new Rect(0, 260, W, 60), "ACTO " + done + " COMPLETADO", title);
            Shadow(new Rect(0, 330, W, 40), "Acto " + S.act + ": " + Progress.ActNames[S.act], new GUIStyle(center) { fontSize = 28 });
            Shadow(new Rect(W / 2 - 600, 390, 1200, 160), Game.ActIntro(S.act), new GUIStyle(center) { wordWrap = true });
            Shadow(new Rect(0, 560, W, 60),
                "Terreno: " + Fmt.Area(Econ.FreeArea(S)) + " · tiempo " + Mathf.FloorToInt(S.playTime / 60) + " min · ganado " + Fmt.Money(S.lifetimeEarned), center);
            Shadow(new Rect(0, 640, W, 30), "<color=#9a9a9a>Clic o Enter para seguir</color>", center);
        }

        Vector2 metaScroll;

        void DrawObjectSheet(Game g, Rect view)
        {
            var S = g.S;
            var p = Progress.P(g.shopFocus);
            if (p == null) { g.shopFocus = null; return; }
            int n = S.Count(p.key);
            Shadow(new Rect(view.x, view.y, view.width, 40), "Hoja de mejoras: " + p.name + "  ×" + n, new GUIStyle(big) { fontSize = 26 });
            string prod = p.IsPicker ? Fmt.Num(n * p.sheets * Progress.ProducerMult(S, p)) + " hojas/s en total"
                                     : Fmt.Money(n * p.income * Progress.ProducerMult(S, p) * Progress.GlobalMult(S)) + "/s en total";
            Shadow(new Rect(view.x, view.y + 44, view.width, 30), p.desc + "  · " + prod + " · siguiente hito: " + NextMilestone(n), small);
            float y = view.y + 90;
            for (int i = 0; i < Game.ObjUpgrades.Length; i++)
            {
                var u = Game.ObjUpgrades[i];
                int lvl = S.Obj(p.key, u.key);
                var row = new Rect(view.x, y, view.width, 70);
                Box(row, 0.35f);
                Shadow(new Rect(row.x + 14, row.y + 6, 700, 26), u.name + "  <color=#e8b923>" + lvl + "/" + u.max + "</color>", mid);
                Shadow(new Rect(row.x + 14, row.y + 34, 900, 26), u.desc, small);
                if (lvl < u.max)
                {
                    double c = g.ObjCost(p, i);
                    GUI.enabled = S.money >= c && n > 0;
                    if (GUI.Button(new Rect(row.xMax - 300, row.y + 12, 290, 44), "Mejorar  " + Fmt.Money(c), button)) g.BuyObj(p, i);
                    GUI.enabled = true;
                }
                y += 78;
            }
            if (GUI.Button(new Rect(view.x, y + 10, 240, 44), "← Volver", button)) g.shopFocus = null;
        }

        static string NextMilestone(int n)
        {
            foreach (var m in Progress.Milestones) if (n < m) return m + " unidades (×2)";
            return "todos los hitos";
        }

        // ------------------------------------------------------------------ noche
        void DrawNight(Game g, float W, float H) => DrawNightNew(g, W, H);

        // ------------------------------------------------------------------ PC
        void DrawComputer(Game g, float W, float H)
        {
            var pc = g.pc;
            Box(new Rect(W / 2 - 520, H - 110, 1040, 80), 0.6f);
            Shadow(new Rect(W / 2 - 500, H - 104, 1000, 30), pc.Done ? "<color=#ffd34d>Página lista · Enter para imprimir</color>" : "Escriba · " + pc.Typed + "/" + pc.Target.Length + " · fallos " + pc.Errors + " · Retroceso borra · Esc levantarse", center);
            if (!string.IsNullOrEmpty(g.lastPageReport)) Shadow(new Rect(W / 2 - 500, H - 72, 1000, 26), "Última página: " + g.lastPageReport, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
        }

        // ------------------------------------------------------------------ prestigio, árbol de tinta y logros (en pausa)
        string metaTab = "Prestigio";

        void DrawMeta(Game g, float W, float H)
        {
            var S = g.S;
            var r = new Rect(W / 2 + 240, 320, Mathf.Min(760, W / 2 - 260), H - 380);
            if (r.width < 400) return;
            Box(r, 0.6f);
            float tx = r.x + 12;
            foreach (var t in new[] { "Prestigio", "Tinta", "Logros", "Pergaminos" })
            {
                if (GUI.Button(new Rect(tx, r.y + 10, 170, 36), (metaTab == t ? "▸ " : "") + t, button)) { metaTab = t; metaScroll = Vector2.zero; }
                tx += 178;
            }
            var view = new Rect(r.x + 12, r.y + 56, r.width - 24, r.height - 66);
            var st = new GUIStyle(small) { wordWrap = true };
            switch (metaTab)
            {
                case "Prestigio":
                {
                    float y = view.y;
                    Shadow(new Rect(view.x, y, view.width, 60), "Nueva edición: reinicia la nave y la montaña. Ganas " + Prestige.EditionGain(S) + " gotas de tinta dorada (+2 % cada una sin gastar). Tienes " + S.ink + ".", st);
                    GUI.enabled = Prestige.CanEdition(S);
                    if (GUI.Button(new Rect(view.x, y + 62, 360, 42), S.act < 4 ? "Desde el acto 4" : "Nueva edición (+" + Prestige.EditionGain(S) + ")", button)) Prestige.DoEdition(g);
                    GUI.enabled = true;
                    y += 120;
                    Shadow(new Rect(view.x, y, view.width, 60), "Editorial: reinicia ediciones y tinta. Ganas " + Prestige.PublisherGain(S) + " sellos editoriales (+25 % cada uno). Tienes " + S.seals + ".", st);
                    GUI.enabled = Prestige.CanPublisher(S);
                    if (GUI.Button(new Rect(view.x, y + 62, 360, 42), S.act < 7 ? "Desde el acto 7" : "Fundar la editorial (+" + Prestige.PublisherGain(S) + ")", button)) Prestige.DoPublisher(g);
                    GUI.enabled = true;
                    y += 120;
                    Shadow(new Rect(view.x, y, view.width, 40), "Biblioteca infinita: empieza en una nave nueva con sus reglas. Completarla (vaciar su montaña) da ×3 para siempre.", st);
                    y += 46;
                    for (int h = 1; h <= 4; h++)
                    {
                        bool done = S.hallsDone.Contains(h);
                        GUI.enabled = Prestige.CanLibrary(S) && !done && S.hall != h;
                        if (GUI.Button(new Rect(view.x, y, 300, 36), Halls.Names[h] + (done ? " ✔" : ""), button)) Prestige.DoLibrary(g, h);
                        GUI.enabled = true;
                        Shadow(new Rect(view.x + 310, y, view.width - 310, 40), "<size=12>" + Halls.Rules[h] + "</size>", st);
                        y += 42;
                    }
                    break;
                }
                case "Tinta":
                {
                    var content = new Rect(0, 0, view.width - 24, Progress.InkTree.Count * 64);
                    metaScroll = GUI.BeginScrollView(view, metaScroll, content);
                    float y = 0;
                    foreach (var n in Progress.InkTree)
                    {
                        int lvl = S.InkLevel(n.key);
                        string cur = n.layer == 1 ? "gotas" : "sellos";
                        Shadow(new Rect(0, y, content.width - 200, 24), n.name + (n.maxLevel > 1 ? "  " + lvl + "/" + n.maxLevel : lvl > 0 ? "  ✔" : "") + "  <color=#e8b923>" + n.cost + " " + cur + "</color>", mid);
                        Shadow(new Rect(0, y + 26, content.width - 200, 30), n.desc, st);
                        if (lvl < n.maxLevel)
                        {
                            GUI.enabled = n.layer == 1 ? S.ink >= n.cost : S.seals >= n.cost;
                            if (GUI.Button(new Rect(content.width - 180, y + 8, 170, 40), "Comprar", button)) { if (Prestige.BuyInk(S, n)) Sfx.Play("buy", 0.8f); }
                            GUI.enabled = true;
                        }
                        y += 64;
                    }
                    GUI.EndScrollView();
                    break;
                }
                case "Logros":
                {
                    var list = Progress.Achievements;
                    var content = new Rect(0, 0, view.width - 24, list.Count * 26);
                    metaScroll = GUI.BeginScrollView(view, metaScroll, content);
                    float y = 0;
                    foreach (var a in list)
                    {
                        bool got = S.achievements.Contains(a.key);
                        string txt = got ? "<color=#7dff9a>✔</color> " + a.name + " <color=#9a9a9a>· " + a.desc + "</color>" : a.hidden ? "<color=#6a6a6a>? Logro oculto</color>" : "<color=#9a9a9a>· " + a.name + " · " + a.desc + "</color>";
                        GUI.Label(new Rect(0, y, content.width, 24), txt, new GUIStyle(small) { fontSize = 14, richText = true });
                        y += 26;
                    }
                    GUI.EndScrollView();
                    break;
                }
                case "Pergaminos":
                {
                    var content = new Rect(0, 0, view.width - 24, Progress.ScrollNames.Length * 26);
                    metaScroll = GUI.BeginScrollView(view, metaScroll, content);
                    for (int i = 0; i < Progress.ScrollNames.Length; i++)
                        GUI.Label(new Rect(0, i * 26, content.width, 24), S.scrolls.Contains(i) ? "<color=#ffd34d>✦</color> " + Progress.ScrollNames[i] : "<color=#6a6a6a>· · ·</color>", new GUIStyle(small) { fontSize = 14, richText = true });
                    GUI.EndScrollView();
                    break;
                }
            }
        }
    }
}
