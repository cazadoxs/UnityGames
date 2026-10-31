using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Interfaz provisional sin configuración (IMGUI): punto de mira, ayuda de interacción, pedidos,
    /// dinero y tiempo, barras flotantes de las estaciones, avisos y resumen del día.
    /// Sustitúyela por tu propia UI cuando quieras: todo lo que muestra está en los sistemas y en <see cref="KitchenEvents"/>.
    /// </summary>
    public class KitchenHUD : MonoBehaviour
    {
        public PlayerInteractor interactor;
        public Camera worldCamera;
        [Tooltip("Escala de toda la interfaz (1 = pensado para 1080p).")]
        public float uiScale = 1f;
        public bool showWorldStatus = true;
        public float worldStatusMaxDistance = 12f;

        readonly List<WorldStatus> statuses = new List<WorldStatus>();
        readonly List<(string text, MessageKind kind, float time)> toasts = new List<(string, MessageKind, float)>();
        bool showHelp;
        GUIStyle box, label, small, big, center, title;
        float scale;

        static readonly Color Good = new Color(0.45f, 0.9f, 0.45f);
        static readonly Color Warn = new Color(1f, 0.8f, 0.3f);
        static readonly Color Bad = new Color(1f, 0.4f, 0.35f);
        static readonly Color Neutral = new Color(0.55f, 0.8f, 1f);

        void Awake()
        {
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        }

        void OnEnable() { KitchenEvents.Message += OnMessage; KitchenEvents.DayEnded += OnDayEnded; }
        void OnDisable() { KitchenEvents.Message -= OnMessage; KitchenEvents.DayEnded -= OnDayEnded; }

        void OnMessage(string text, MessageKind kind)
        {
            toasts.Add((text, kind, Time.time));
            if (toasts.Count > 5) toasts.RemoveAt(0);
        }

        void OnDayEnded(int day, DayStats stats)
        {
            if (interactor != null && interactor.input != null) interactor.input.SetCursorLocked(false);
            PlayerInputReader.GameplayBlocked = true;
        }

        void Update()
        {
            if (interactor != null && interactor.input != null && interactor.input.HelpPressed) showHelp = !showHelp;
            var gm = KitchenGameManager.Instance;
            if (gm != null && gm.State != KitchenGameManager.ShiftState.DayOver && PlayerInputReader.GameplayBlocked)
                PlayerInputReader.GameplayBlocked = false;
        }

        void Styles()
        {
            scale = Mathf.Max(0.5f, uiScale * Screen.height / 1080f);
            if (box != null && Mathf.Approximately(box.fontSize, Mathf.RoundToInt(18 * scale))) return;
            box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(18 * scale), alignment = TextAnchor.UpperLeft, richText = true, wordWrap = true };
            box.padding = new RectOffset(10, 10, 8, 8);
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(18 * scale), richText = true, wordWrap = true };
            small = new GUIStyle(label) { fontSize = Mathf.RoundToInt(15 * scale) };
            big = new GUIStyle(label) { fontSize = Mathf.RoundToInt(30 * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            title = new GUIStyle(label) { fontSize = Mathf.RoundToInt(20 * scale), fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            Styles();
            if (showWorldStatus) DrawWorldStatus();
            DrawTopBar();
            DrawOrders();
            DrawCrosshairAndPrompt();
            DrawToasts();
            if (showHelp) DrawHelp();
            DrawDaySummary();
        }

        // ---------- Barra superior ----------

        void DrawTopBar()
        {
            var gm = KitchenGameManager.Instance;
            if (gm == null) return;
            var r = new Rect(12 * scale, 12 * scale, 420 * scale, 70 * scale);
            string time = gm.State == KitchenGameManager.ShiftState.Closing ? "Cerrando"
                : $"{Mathf.FloorToInt(gm.TimeRemaining / 60f)}:{Mathf.FloorToInt(gm.TimeRemaining % 60f):00}";
            string stars = new string('★', Mathf.RoundToInt(gm.Rating)) + new string('☆', 5 - Mathf.RoundToInt(gm.Rating));
            string text = $"<b>Día {gm.Day}</b>   ⏱ {time}   <color=#7CFC7C>${gm.Money}</color>   {stars}";
            if (gm.State == KitchenGameManager.ShiftState.BeforeOpen) text += "\n<color=#FFD24D>Pulsa ENTER para abrir el local</color>";
            else text += "\n<size=" + Mathf.RoundToInt(14 * scale) + ">F1: controles</size>";
            GUI.Box(r, text, box);
        }

        // ---------- Pedidos ----------

        void DrawOrders()
        {
            var om = OrderManager.Instance;
            if (om == null) return;
            float w = 300 * scale, x = Screen.width - w - 12 * scale, y = 12 * scale;
            var sb = new StringBuilder();
            foreach (var o in om.ActiveOrders)
            {
                sb.Clear();
                sb.Append("<b>Pedido #").Append(o.Id).Append("</b>  $").Append(o.TotalPrice).Append('\n');
                foreach (var l in o.Lines)
                    sb.Append(l.Delivered ? "<color=#7CFC7C>✓ " : "• ").Append(l.Product.DisplayName).Append(l.Delivered ? "</color>" : "").Append('\n');
                float h = (34 + o.Lines.Count * 22) * scale;
                var r = new Rect(x, y, w, h + 12 * scale);
                GUI.Box(r, sb.ToString(), box);
                if (o.Customer != null)
                {
                    float f = o.Customer.PatienceFraction;
                    Bar(new Rect(x + 8 * scale, y + h, w - 16 * scale, 6 * scale), f, f > 0.5f ? Good : f > 0.25f ? Warn : Bad);
                }
                y += h + 18 * scale;
                if (y > Screen.height * 0.8f) break;
            }
        }

        // ---------- Punto de mira y ayuda ----------

        void DrawCrosshairAndPrompt()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            var it = interactor;
            bool hasTarget = it != null && it.HasPrompt;
            float s = (hasTarget ? 8 : 5) * scale;
            GUI.color = hasTarget ? Warn : new Color(1, 1, 1, 0.8f);
            GUI.DrawTexture(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (it == null) return;

            var lines = new List<string>();
            if (hasTarget)
            {
                var p = it.CurrentPrompt;
                if (!string.IsNullOrEmpty(p.Title)) lines.Add("<b>" + p.Title + "</b>");
                if (!string.IsNullOrEmpty(p.Info)) lines.Add("<color=#BBBBBB>" + p.Info + "</color>");
                if (!string.IsNullOrEmpty(p.Primary)) lines.Add("<color=#FFD24D>[E]</color> " + p.Primary);
                if (!string.IsNullOrEmpty(p.Secondary)) lines.Add("<color=#FFD24D>[F]</color> " + p.Secondary);
                if (!string.IsNullOrEmpty(p.Cycle)) lines.Add("<color=#FFD24D>[Q]</color> " + p.Cycle);
                if (lines.Count > 0)
                {
                    float w = 420 * scale, h = (lines.Count * 24 + 16) * scale;
                    var r = new Rect(cx + 24 * scale, cy - h * 0.5f, w, h);
                    GUI.Box(r, string.Join("\n", lines), box);
                    if (p.Progress >= 0f) Bar(new Rect(r.x + 8 * scale, r.yMax - 8 * scale, w - 16 * scale, 6 * scale), p.Progress, Neutral);
                }
            }

            if (it.HeldItem != null)
            {
                var r = new Rect(cx - 300 * scale, Screen.height - 60 * scale, 600 * scale, 40 * scale);
                GUI.Box(r, "En la mano: <b>" + it.HeldItem.Describe() + "</b>   <color=#BBBBBB>[G] soltar</color>", box);
            }
        }

        // ---------- Estado en el mundo ----------

        void DrawWorldStatus()
        {
            var cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return;
            WorldStatusRegistry.Collect(statuses);
            foreach (var s in statuses)
            {
                var sp = cam.WorldToScreenPoint(s.Position);
                if (sp.z <= 0f || sp.z > worldStatusMaxDistance) continue;
                float k = Mathf.Lerp(1f, 0.6f, sp.z / worldStatusMaxDistance) * scale;
                float w = 110 * k, x = sp.x - w * 0.5f, y = Screen.height - sp.y;
                var c = s.Kind == StatusKind.Good ? Good : s.Kind == StatusKind.Warning ? Warn : s.Kind == StatusKind.Danger ? Bad : Neutral;
                if (!string.IsNullOrEmpty(s.Label))
                {
                    GUI.color = c;
                    GUI.Label(new Rect(x - 40 * k, y - 26 * k, w + 80 * k, 24 * k), "<b>" + s.Label + "</b>", center);
                    GUI.color = Color.white;
                }
                if (s.Progress >= 0f) Bar(new Rect(x, y, w, 9 * k), s.Progress, c);
            }
        }

        // ---------- Avisos ----------

        void DrawToasts()
        {
            float y = Screen.height * 0.18f;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var t = toasts[i];
                float age = Time.time - t.time;
                if (age > 4f) { toasts.RemoveAt(i); continue; }
                var c = t.kind == MessageKind.Good ? Good : t.kind == MessageKind.Bad ? Bad : Color.white;
                c.a = Mathf.Clamp01(4f - age);
                GUI.color = c;
                GUI.Label(new Rect(0, y, Screen.width, 30 * scale), "<b>" + t.text + "</b>", center);
                y += 28 * scale;
            }
            GUI.color = Color.white;
        }

        void DrawHelp()
        {
            var r = new Rect(12 * scale, 95 * scale, 430 * scale, 360 * scale);
            GUI.Box(r,
                "<b>Controles</b>\n" +
                "WASD · mover      Ratón · mirar      Shift · correr\n" +
                "E / clic izq. · coger, dejar, montar, entregar\n" +
                "F / clic der. (mantener) · cortar, servir, batir, dar la vuelta, limpiar\n" +
                "Q / rueda · cambiar sabor u opción\n" +
                "G · soltar (¡la comida se ensucia!)\n" +
                "Enter · abrir el local / siguiente día\n" +
                "Esc · liberar el ratón\n\n" +
                "<b>Cómo se trabaja</b>\n" +
                "1. Toma nota en la caja.\n" +
                "2. Coge un papel de hamburguesa, déjalo en la encimera y apila pan, carne hecha, etc. (con el papel en la mano también puedes añadir desde las cajas).\n" +
                "3. Mete todo en una bandeja o entrégalo suelto en el mostrador.", box);
        }

        void DrawDaySummary()
        {
            var gm = KitchenGameManager.Instance;
            if (gm == null || gm.State != KitchenGameManager.ShiftState.DayOver) return;
            var t = gm.Today;
            float w = 460 * scale, h = 330 * scale;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(r, "", box);
            GUILayout.BeginArea(new Rect(r.x + 20 * scale, r.y + 16 * scale, w - 40 * scale, h - 32 * scale));
            GUILayout.Label($"Fin del día {gm.Day}", big);
            GUILayout.Space(10 * scale);
            GUILayout.Label($"Pedidos servidos: <b>{t.ordersCompleted}</b>", label);
            GUILayout.Label($"Clientes perdidos: <b>{t.customersLost}</b>", label);
            GUILayout.Label($"Ventas: <b>${t.revenue}</b>   Propinas: <b>${t.tips}</b>   Gastos: <b>${t.spent}</b>", label);
            GUILayout.Label($"Beneficio: <b>${t.Profit}</b>", title);
            GUILayout.Label($"Satisfacción media: <b>{Mathf.RoundToInt(t.AverageSatisfaction * 100f)}%</b>", label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Siguiente día (Enter)", GUILayout.Height(44 * scale)))
                gm.NextDay();
            GUILayout.EndArea();
        }

        static void Bar(Rect r, float value, Color c)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
