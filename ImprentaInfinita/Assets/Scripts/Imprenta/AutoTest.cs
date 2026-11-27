using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Partida automática para comprobar el prototipo sin tocarlo: se lanza con «-autotest -autotestOut carpeta».
    /// Recorre el bucle del acto 1, guarda capturas y un registro, y cierra el juego. No toca la partida guardada.
    /// </summary>
    public class AutoTest : MonoBehaviour
    {
        string outDir;
        readonly StringBuilder log = new StringBuilder();
        int errors;

        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-autotestOut");
            outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "autotest");
            Directory.CreateDirectory(outDir);
            if (phase == 0) Application.logMessageReceived += OnLogStatic;
            if (phase > 0) { log.Append(logStatic); errors = errorsStatic; StartCoroutine(Phase2()); }
            else StartCoroutine(Run());
        }

        static AutoTest current;
        void Awake() { current = this; }

        static void OnLogStatic(string msg, string stack, LogType type)
        {
            if (current != null) current.OnLog(msg, stack, type);
        }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                log.AppendLine("[" + type + "] " + msg + "\n" + stack);
                try { File.WriteAllText(Path.Combine(outDir, "autotest_log.txt"), log.ToString()); } catch { }
            }
        }

        void Note(string s)
        {
            log.AppendLine(Time.realtimeSinceStartup.ToString("0.0") + "s  " + s);
            Debug.Log("[AutoTest] " + s);
            try { File.WriteAllText(Path.Combine(outDir, "autotest_log.txt"), log.ToString()); } catch { }
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Note("captura " + name);
        }

        IEnumerator Run()
        {
            var g = Game.I;
            var S = g.S;
            yield return new WaitForSeconds(2f);
            Note("frases distintas disponibles: " + Phrases.TotalPhrases);
            // Comprobación de no repetición en las primeras 3.000 frases
            var seen = new System.Collections.Generic.HashSet<string>();
            int dup = 0;
            for (long k = 0; k < 3000; k++) { if (!seen.Add(Phrases.Get(k, S.seed, 0, out bool gold)) && !gold) dup++; }
            Note("repeticiones en 3.000 frases: " + dup);
            yield return Shot("01_entrada");

            g.player.Teleport(new Vector3(0, 0.05f, 22.0f), 0); g.player.SetLook(0, 25);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("02_montana");
            Note("hojas reales en la montaña: " + g.mountain.SheetTotal + ", dibujadas ahora: " + g.mountain.SheetsDrawn + ", convertidas en objetos: " + g.mountain.SheetsActivated + " (vivas " + Paper.Live.Count + "), FPS " + (1f / Time.smoothDeltaTime).ToString("0"));
            Note("mirando a la montaña: " + g.player.LookingAtMountain);
            for (int k = 0; k < 3; k++) g.PickSheet(g.player.MountainPoint);
            Note("hojas en mano: " + g.CarryCount + ", terreno " + Econ.FreeArea(S));

            // Escribir una página con un fallo a propósito
            g.player.Teleport(new Vector3(0.05f, 0.05f, 9.0f), 0);
            g.typewriter.Interact(g);
            yield return new WaitForSeconds(1.2f);
            Note("modo: " + g.Mode + ", frase: " + g.typewriter.Phrase);
            string phrase = g.typewriter.Phrase;
            for (int k = 0; k < phrase.Length && !g.typewriter.Done; k++)
            {
                if (g.typewriter.AtLineEnd) g.typewriter.OnChar(g, '\n');
                else g.typewriter.OnChar(g, k == 3 ? 'x' : phrase[k]);
                if (k == phrase.Length / 2) yield return Shot("03_escribiendo");
                yield return new WaitForSeconds(0.21f);
            }
            yield return new WaitForSeconds(0.5f);
            yield return Shot("04_hoja_terminada");
            Note("errores: " + g.typewriter.Errors + ", terminada: " + g.typewriter.Done);
            g.ExtractAndNext();
            Note("tras sacar: " + g.lastPageReport + " | escritas en mano: " + S.carriedPages.Count);
            // Segunda página impecable y prueba del atasco
            yield return new WaitForSeconds(0.8f);
            if (g.Mode == GameMode.Typing)
            {
                Note("segunda hoja cargada sola: " + g.typewriter.Phrase);
                for (int k = 0; k < 7; k++) { g.typewriter.OnChar(g, g.typewriter.Phrase[Mathf.Min(k, g.typewriter.Phrase.Length - 1)]); }
                Note("atasco al teclear muy rápido: " + g.typewriter.Jammed);
                g.typewriter.Unjam();
                g.ExitTyping();
            }
            g.player.Teleport(new Vector3(3.6f, 0.05f, 9.0f), 0); g.player.SetLook(0, 30);
            double before = S.money;
            g.duct.Interact(g);
            Note("vendido en el ducto: " + Fmt.Money(S.money - before));

            // Comprar todo lo que se pueda con dinero de prueba
            S.money += 20000; S.pagesTyped += 10; S.everError = true; S.everJam = true;
            for (int round = 0; round < 6; round++)
            {
                foreach (var def in Econ.All)
                    if (def.visible(S) && S.Level(def.key) < Mathf.Min(def.maxLevel, 4)) { Note("comprando " + def.key); g.TryBuy(def); }
                yield return new WaitForSeconds(0.6f);
            }
            Note("margaritas: " + g.margaritas.Count + ", capacidad: " + Econ.Capacity(S) + " (" + Econ.ContainerName(S) + ")");
            S.carriedBlank = 0;
            foreach (var m in g.margaritas) { S.carriedBlank = 30; m.Interact(g); }
            S.deskTray = 10;
            yield return new WaitForSeconds(9f);
            g.player.Teleport(new Vector3(7.5f, 0.05f, 7.6f), 0); g.player.SetLook(0, 22);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("05_margaritas");
            Note("páginas de margarita: " + S.pagesMachine);
            g.player.Teleport(new Vector3(-1.2f, 0.05f, 9.6f), -57); g.player.SetLook(-57, 8);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("06_tablon");
            g.player.Teleport(new Vector3(0.6f, 0.05f, 8.2f), -10); g.player.SetLook(-10, 28);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("07_puesto");
            g.player.Teleport(new Vector3(0.05f, 0.05f, 9.3f), 0); g.player.SetLook(0, 48);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("11_maquina");
            g.player.Teleport(new Vector3(4f, 0.05f, 16f), -20); g.player.SetLook(-20, -8);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("12_ladera");
            // Las hojas al alcance son objetos de verdad: una patada al montón las levanta
            yield return new WaitForSeconds(0.4f);
            Note("hojas sueltas con física cerca: " + Paper.Live.Count + ", sacadas de la montaña: " + g.mountain.SheetsActivated);
            g.player.SetLook(-20, 12);
            yield return new WaitForSeconds(0.2f);
            var camT = Camera.main.transform;
            if (!g.mountain.Raycast(camT.position, camT.forward, 14f, out var kickAt))
            {
                var fwd = camT.forward; fwd.y = 0; fwd.Normalize();
                var q = camT.position + fwd * 7f;
                kickAt = new Vector3(q.x, g.mountain.HeightAt(q.x, q.z), q.z);
            }
            g.mountain.ActivateNear(kickAt, 2.5f, 400);
            Note("patada en " + kickAt + ", hojas con física " + Paper.Live.Count);
            foreach (var ls in Paper.Live)
                if (ls && (ls.transform.position - kickAt).sqrMagnitude < 9f)
                    ls.Push((ls.transform.position - kickAt).normalized * 2.5f - camT.forward * 1.5f + Vector3.up * UnityEngine.Random.Range(2.5f, 5f));
            yield return new WaitForSeconds(0.45f);
            yield return Shot("12b_patada_hojas");
            yield return new WaitForSeconds(1.2f);
            yield return Shot("12c_hojas_cayendo");

            // Terreno: casi al objetivo y la última hoja abre el final del acto
            S.sheetsPicked = (long)((Econ.GoalArea - Econ.StartArea) / Econ.AreaPerSheet) - 1;
            S.carriedBlank = 0;
            g.PickSheet(Vector3.zero);
            yield return new WaitForSeconds(0.5f);
            Note("acto 1 completado: " + S.act1Done + ", modo " + g.Mode);
            yield return Shot("08_acto_completado");
            g.Resume();
            g.player.Teleport(new Vector3(0, 0.05f, 2f), 0); g.player.SetLook(0, -4);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("09_montana_ganada");
            g.player.Teleport(new Vector3(-6f, 0.05f, 22f), 30); g.player.SetLook(30, -12);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("10_nave");

            yield return Acts(g);
        }

        // ------------------------------------------------------------------ actos 2 a 8 y prestigio
        static int phase;     // sobrevive a la recarga de escena del prestigio
        static string outStatic;
        static StringBuilder logStatic;
        static int errorsStatic;

        IEnumerator Look(Game g, Vector3 pos, float yaw, float pitch, string shot)
        {
            g.player.Teleport(pos, yaw); g.player.SetLook(yaw, pitch);
            yield return new WaitForSeconds(0.7f);
            yield return Shot(shot);
        }

        void BuyAll(Game g, string key, int n)
        {
            var p = Progress.P(key);
            g.S.money += p.Cost(g.S.Count(key) + n) * n * 1.2 + 1;
            g.BuyProducer(p, n);
        }

        IEnumerator Acts(Game g)
        {
            var S = g.S;
            g.workday.StartFirstDay();
            yield return new WaitForSeconds(1f);
            Note("acto " + S.act + ", día " + S.day + ", pedido " + S.order + ", hora " + g.workday.ClockText);
            BuyAll(g, "mecanografo", 12); BuyAll(g, "brazo", 8); BuyAll(g, "equipo", 3);
            S.money += 1e6;
            foreach (var n in Progress.Nodes) if (n.act <= 2) g.BuyNode(n);
            yield return new WaitForSeconds(3f);
            Note("acto 2: ingresos " + Fmt.Money(g.factory.income) + "/s, páginas " + Fmt.Num(g.factory.pagesPerSec) + "/s, hojas " + Fmt.Num(g.factory.sheetsPerSec) + "/s, unidades visibles " + g.factory.VisibleUnits("mecanografo"));
            yield return Look(g, new Vector3(-4f, 0.05f, 3f), -45, 12, "13_taller");
            yield return Look(g, new Vector3(0f, 0.05f, 14f), 0, 8, "14_brazos");
            g.OpenShop(null);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("15_catalogo");
            g.CloseShop();
            g.OpenShop(null, true);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("15b_arbol");
            g.CloseShop();
            // Primer plano de los mecanógrafos (humanos MPFB)
            var rigs = FindObjectsByType<HumanRig>(FindObjectsSortMode.None);
            Note("humanos con esqueleto: " + rigs.Length);
            if (rigs.Length > 0)
            {
                var rt = rigs[0].transform;
                var cp = rt.TransformPoint(new Vector3(1.1f, 0, 1.3f)); cp.y = 0.05f;
                var dv = rt.position - cp;
                yield return Look(g, cp, Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg, 22, "13b_mecanografo");
                cp = rt.TransformPoint(new Vector3(-0.5f, 0, -1.6f)); cp.y = 0.05f; dv = rt.position - cp;
                yield return Look(g, cp, Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg, 18, "13c_mecanografo_espalda");
            }
            // El camión: llega marcha atrás, abre puertas y carga
            S.dayTime = (Progress.EndHour(S) - 2.19f - 8f) / 12f * Progress.DaySeconds;
            g.player.Teleport(new Vector3(23f, 0.05f, 3f), 160); g.player.SetLook(160, 4);
            yield return new WaitForSeconds(11f);
            yield return Shot("16a_camion_marcha_atras");
            yield return new WaitForSeconds(8f);
            yield return Shot("16b_camion_cargando");
            // Fin de la jornada: sirena, fichar y noche
            S.dayTime = (Progress.EndHour(S) - 0.45f - 8f) / 12f * Progress.DaySeconds;
            yield return new WaitForSeconds(0.5f);
            Note("hora " + g.workday.ClockText + ", se puede fichar: " + g.workday.CanClockIn);
            yield return Look(g, new Vector3(-9.6f, 0.05f, -7.5f), 180, 5, "16_fichar");
            g.workday.ClockIn();
            for (int k = 0; k < 40 && g.Mode != GameMode.Night; k++) yield return new WaitForSeconds(0.25f);
            Note("modo tras fichar: " + g.Mode + " · " + g.workday.lastReport.Replace("\n", " | "));
            yield return Shot("17_noche");
            g.StartNextDay();
            yield return new WaitForSeconds(0.5f);
            Note("día " + S.day + " pedido " + Fmt.Num(S.order));

            // Acto 3
            S.sheetsAuto = Progress.SheetsForArea(1250); g.OnAreaChanged();
            yield return new WaitForSeconds(0.3f);
            Note("acto tras 1.250 m²: " + S.act + " modo " + g.Mode);
            yield return Shot("18_acto3");
            g.Resume();
            BuyAll(g, "pc", 8); BuyAll(g, "matricial", 6); BuyAll(g, "inyeccion", 6); BuyAll(g, "grua", 3);
            S.money += 1e9; foreach (var n in Progress.Nodes) if (n.act <= 3) g.BuyNode(n);
            g.EnterComputer();
            yield return new WaitForSeconds(1.2f);
            string tgt = g.pc.Target;
            foreach (var ch in tgt) g.pc.OnChar(g, ch);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("19_escriba");
            double m0 = S.money;
            g.pc.Submit(g);
            Note("página de PC: " + Fmt.Money(S.money - m0) + " · " + g.lastPageReport);
            g.ExitComputer();
            yield return new WaitForSeconds(1f);
            yield return Look(g, new Vector3(20f, 0.05f, 1f), 10, 10, "20_oficina");

            // Acto 4
            S.sheetsAuto = Progress.SheetsForArea(5100); g.OnAreaChanged(); g.Resume();
            BuyAll(g, "fotocopiadora", 6); BuyAll(g, "duplicadora", 4); BuyAll(g, "dependiente", 2); BuyAll(g, "excavadora", 2);
            S.money += 1e12; foreach (var n in Progress.Nodes) if (n.act <= 4) g.BuyNode(n);
            yield return new WaitForSeconds(1f);
            Note("acto " + S.act + ": ingresos " + Fmt.Money(g.factory.income) + "/s");
            yield return Look(g, new Vector3(41f, 0.05f, -3.5f), 180, 8, "21_ventanilla");
            yield return Look(g, new Vector3(34f, 0.05f, 0f), 20, 8, "22_copias");

            // Actos 5 y 6
            S.sheetsAuto = Progress.SheetsForArea(14100); g.OnAreaChanged(); g.Resume();
            BuyAll(g, "plotter", 4); BuyAll(g, "fotolab", 3); BuyAll(g, "macrobot", 5); BuyAll(g, "aspirador", 2);
            S.sheetsAuto = Progress.SheetsForArea(22100); g.OnAreaChanged(); g.Resume();
            BuyAll(g, "offset", 4); BuyAll(g, "rotativa", 2); BuyAll(g, "ia", 3); BuyAll(g, "tuneladora", 1);
            S.money += 1e18; foreach (var n in Progress.Nodes) g.BuyNode(n);
            yield return new WaitForSeconds(1.5f);
            Note("acto " + S.act + ": ingresos " + Fmt.Money(g.factory.income) + "/s, terreno " + Fmt.Area(Econ.FreeArea(S)));
            yield return Look(g, new Vector3(0f, 0.05f, 24f), 0, 2, "23_estudio_fabrica");
            yield return Look(g, new Vector3(-6f, 0.05f, 52f), 210, 6, "24_rotativa");

            // Acto 7: el fondo
            S.sheetsAuto = Progress.SheetsForArea(Progress.FullArea + 10); g.OnAreaChanged(); g.Resume();
            BuyAll(g, "redia", 10); BuyAll(g, "contratos", 3);
            yield return new WaitForSeconds(1.5f);
            Note("acto " + S.act + ", montaña vacía: " + g.mountain.Empty + ", tinta posible " + Prestige.EditionGain(S) + ", logros " + S.achievements.Count);
            yield return Look(g, new Vector3(0f, 0.05f, 60f), 0, 4, "25_fondo");
            g.Mode = GameMode.Paused;
            yield return new WaitForSeconds(0.3f);
            yield return Shot("26_pausa_prestigio");
            g.Resume();

            Note("FPS aprox: " + (1f / Time.smoothDeltaTime).ToString("0") + " · hojas en la montaña " + g.mountain.SheetTotal + ", dibujadas " + g.mountain.SheetsDrawn);
            // Prestigio: nueva edición (recarga la escena; la prueba sigue en Phase2)
            phase = 1; outStatic = outDir; logStatic = log; errorsStatic = errors;
            S.editionPages = 1e12;
            Note("haciendo Nueva edición con " + Prestige.EditionGain(S) + " gotas");
            Prestige.DoEdition(g);
        }

        IEnumerator Phase2()
        {
            yield return new WaitForSeconds(1.5f);
            var g = Game.I; var S = g.S;
            if (phase == 1)
            {
                Note("tras la edición: acto " + S.act + ", tinta " + S.ink + ", ediciones " + S.editions + ", dinero " + Fmt.Money(S.money));
                foreach (var n in Progress.InkTree) if (n.layer == 1) Prestige.BuyInk(S, n);
                S.sheetsAuto = Progress.SheetsForArea(Progress.FullArea + 10); g.OnAreaChanged(); g.Resume();
                S.inkEver += 100000;
                Note("acto " + S.act + ", sellos posibles " + Prestige.PublisherGain(S));
                phase = 2; logStatic = log; errorsStatic = errors;
                Prestige.DoPublisher(g);
                yield break;
            }
            if (phase == 2)
            {
                Note("tras la editorial: sellos " + S.seals + ", editoriales " + S.publishers + ", tinta " + S.ink);
                foreach (var n in Progress.InkTree) if (n.layer == 2) Prestige.BuyInk(S, n);
                S.sheetsAuto = Progress.SheetsForArea(Progress.FullArea + 10); g.OnAreaChanged(); g.Resume();
                Note("acto " + S.act + " (Más allá), biblioteca posible: " + Prestige.CanLibrary(S));
                BuyAll(g, "cuantica", 2); BuyAll(g, "fractal", 1); BuyAll(g, "babel", 1);
                yield return new WaitForSeconds(1.5f);
                yield return Look(g, new Vector3(5f, 0.05f, 140f), 20, 6, "27_mas_alla");
                phase = 3; logStatic = log; errorsStatic = errors;
                Prestige.DoLibrary(g, 4);
                yield break;
            }
            if (phase == 3)
            {
                Note("nave: " + Halls.Names[S.hall] + ", gravedad " + Halls.Gravity(S.hall));
                yield return Look(g, new Vector3(0f, 0.05f, 2f), 0, 0, "28_sotano");
                S.sheetsAuto = Progress.SheetsForArea(Progress.FullArea + 10); g.OnAreaChanged(); g.Resume();
                Note("naves completadas: " + S.hallsDone.Count + ", multiplicador global " + Progress.GlobalMult(S).ToString("0.0"));
                Note("logros: " + S.achievements.Count + "/" + Progress.Achievements.Count);
                Note("FPS aprox: " + (1f / Time.smoothDeltaTime).ToString("0"));
                Note("errores en consola: " + errors);
                File.WriteAllText(Path.Combine(outDir, "autotest_log.txt"), log.ToString());
                yield return new WaitForSeconds(0.3f);
                Application.Quit();
            }
        }
    }
}
