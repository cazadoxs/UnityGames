using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Imprenta
{
    public enum GameMode { Walking, Typing, Paused, ActComplete, Menu, Night, Computer }

    /// <summary>
    /// Director del prototipo gris del acto 1 (doc 6.5 paso 1). Monta la nave por código al abrir la escena
    /// «Imprenta», guarda la partida en JSON y lleva el tutorial, la economía y los eventos.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public SaveData S;
        public GameMode Mode = GameMode.Walking;

        public PlayerController player;
        public Mountain mountain;
        public Typewriter typewriter;
        public Duct duct;
        public DeskTray deskTray;
        public Conveyor handBelt, machineBelt;
        public readonly List<Margarita> margaritas = new List<Margarita>();
        readonly List<UpgradeCard> cards = new List<UpgradeCard>();
        Transform world, desk, board;
        public bool noSave, autoTest;
        public Factory factory;
        public Workday workday;
        public PcStation pc;
        public Counter counter;
        public string shopFocus;               // productor cuya hoja de mejoras está abierta
        public int completedAct;               // acto que muestra la pantalla de acto completado
        public static SaveData pendingSave;    // partida nueva tras un prestigio
        float achTimer, eventTimer = 240f, areaVisualTimer;

        // Efectos
        class Flyer { public Transform t; public Vector3 a, b; public float time, dur; }
        readonly List<Flyer> flyers = new List<Flyer>();
        class Floater { public TextMesh tm; public float time; public Vector3 start; }
        readonly List<Floater> floaters = new List<Floater>();
        readonly Queue<GameObject> sheetPool = new Queue<GameObject>();

        // HUD
        public readonly List<(string text, float time)> toasts = new List<(string, float)>();
        readonly Queue<(float t, double v)> earnLog = new Queue<(float, double)>();
        public string lastPageReport = "";
        float autosave, boardRefresh, coinCooldown;
        public float typingCamBlend, pcCamBlend;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Imprenta" || I != null) return;
            new GameObject("Imprenta").AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            Events.Blackout = false;
            var args = Environment.GetCommandLineArgs();
            autoTest = Array.IndexOf(args, "-autotest") >= 0;
            noSave = autoTest;
            bool fresh = autoTest || Array.IndexOf(args, "-nuevo") >= 0;
            S = pendingSave ?? (fresh ? SaveData.New() : SaveData.LoadOrNew());
            pendingSave = null;
            while (S.margaritaTrays.Count < Econ.MaxMargaritas) S.margaritaTrays.Add(0);

            Application.targetFrameRate = 144;
            Sfx.Init(gameObject);
            gameObject.AddComponent<Hud>();
            Sfx.Loop(gameObject, "hum", 0.12f, false);

            world = Build.Empty("Mundo", transform, Vector3.zero).transform;
            WorldBuilder.BuildHall(world);
            WorldBuilder.BuildLighting(world);
            desk = WorldBuilder.BuildDesk(world, out var anchor, out _);
            typewriter = Typewriter.Create(anchor);
            duct = Duct.Create(world, new Vector3(3.6f, 0, 10f));

            var mGo = new GameObject("Montaña");
            mGo.transform.SetParent(world, false);
            mountain = mGo.AddComponent<Mountain>();
            mountain.SetArea(Econ.FreeArea(S));

            BuildBoard();
            if (S.Level("bandejaMaq") > 0) EnsureDeskTray();
            if (S.Level("cinta") > 0) EnsureHandBelt();
            for (int i = 0; i < S.Level("margarita"); i++) AddMargarita(i);

            factory = Factory.Create(this, world);
            workday = Workday.Create(this, world);
            pc = PcStation.Create(world);
            counter = Counter.Create(world);
            Catalog.Create(world, new Vector3(-4.7f, 0, 8.4f));
            Halls.Apply(this, world);
            factory.BuildAll();
            workday.CollectLights(world);
            Build.AddColliders(world);

            var pGo = new GameObject("Jugador");
            pGo.transform.SetParent(transform, false);
            player = pGo.AddComponent<PlayerController>();
            player.Teleport(new Vector3(0, 0.05f, -5f), 0);

            if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput;
            ApplyOffline();
            RefreshBoard();
            workday.StartFirstDay();
            CheckAct();
            SetCursor();
            if (autoTest) gameObject.AddComponent<AutoTest>();
            else { Hud.TitleScreen = true; Mode = GameMode.Paused; SetCursor(); }
        }

        void OnDestroy()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
            if (I == this) I = null;
        }

        void OnApplicationQuit()
        {
            // lo que va en las cintas se cobra al salir para no perderlo
            if (handBelt) S.money += handBelt.PendingValue;
            if (machineBelt) S.money += machineBelt.PendingValue;
            if (!noSave) S.Save();
        }

        void OnApplicationFocus(bool focus) { if (focus) SetCursor(); }

        // ------------------------------------------------------------------ montaje
        void BuildBoard()
        {
            board = Build.Empty("TablonDeMejoras", world, new Vector3(-2.9f, 0, 11.3f), new Vector3(0, -57, 0)).transform;
            var wood = Build.Pbr("dark_wood", new Vector2(1, 1), Build.Hex("#5b3d24"));
            Build.Box("Marco", board, new Vector3(0, 1.55f, 0.02f), new Vector3(2.75f, 1.85f, 0.06f), wood);
            Build.Box("Corcho", board, new Vector3(0, 1.55f, -0.012f), new Vector3(2.6f, 1.7f, 0.01f), Build.Mat(Build.Cork, 0.05f), false);
            Build.Box("Pata", board, new Vector3(-1.2f, 0.33f, 0.05f), new Vector3(0.08f, 0.66f, 0.08f), wood);
            Build.Box("Pata", board, new Vector3(1.2f, 0.33f, 0.05f), new Vector3(0.08f, 0.66f, 0.08f), wood);
            Build.Text("Rotulo", board, new Vector3(0, 2.55f, -0.02f), "MEJORAS", 0.14f, Build.Hex("#f0e6c8"), TextAnchor.MiddleCenter);
            Build.Box("Cartel", board, new Vector3(0, 2.55f, 0.0f), new Vector3(1.1f, 0.24f, 0.03f), Build.Mat(Build.Hex("#2f2a24"), 0.3f), false);
        }

        void RefreshBoard()
        {
            foreach (var def in Econ.All)
            {
                if (cards.Exists(c => c.def == def) || !def.visible(S)) continue;
                int i = cards.Count;
                int col = i % 4, row = i / 4;
                var card = UpgradeCard.Create(board, def, new Vector3(-0.93f + col * 0.62f, 2.18f - row * 0.41f, -0.02f));
                cards.Add(card);
                if (Time.timeSinceLevelLoad > 1f) Toast("Nueva mejora en el tablón: " + def.name);
            }
        }

        public void EnsureDeskTray()
        {
            if (deskTray) return;
            deskTray = DeskTray.Create(desk, new Vector3(-0.42f, 0.78f, 0.12f));
        }

        public void EnsureHandBelt()
        {
            if (handBelt) return;
            handBelt = Conveyor.Create(world, "CintaCorta", new Vector3(0.9f, 0, 10.05f), new Vector3(3.25f, 0, 10.05f), 0.74f);
            handBelt.onArrive = p => duct.Accept(this, p);
        }

        void EnsureMachineBelt()
        {
            if (machineBelt) return;
            machineBelt = Conveyor.Create(world, "CintaMargaritas", new Vector3(11.4f, 0, 10.05f), new Vector3(4.1f, 0, 10.05f), 0.8f);
            machineBelt.onArrive = p => duct.Accept(this, p);
        }

        public void AddMargarita(int slot)
        {
            if (margaritas.Exists(m => m.slot == slot)) return;
            EnsureMachineBelt();
            var m = Margarita.Create(world, slot, new Vector3(6.0f + slot * 1.45f, 0, 10.75f));
            m.output = machineBelt;
            margaritas.Add(m);
        }

        void ApplyOffline()
        {
            if (S.savedAtUnix > 0 && S.act >= 2)
            {
                double awayAll = Math.Min(8 * 3600, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - S.savedAtUnix);
                if (awayAll >= 60)
                {
                    factory.Recalc();
                    double money0 = factory.income * awayAll * 0.5;
                    double pages0 = factory.pagesPerSec * awayAll * 0.5;
                    S.sheetsAuto += factory.sheetsPerSec * awayAll * 0.5;
                    S.money += money0; S.totalEarned += money0; S.lifetimeEarned += money0;
                    S.lifetimePages += pages0; S.editionPages += pages0;
                    mountain.SetArea(Econ.FreeArea(S));
                    if (money0 > 0) Toast("Mientras no estabas (" + (int)(awayAll / 60) + " min, turno de noche a medio gas): " + Fmt.Num(pages0) + " páginas, " + Fmt.Money(money0));
                }
            }
            if (S.savedAtUnix <= 0 || margaritas.Count == 0) return;
            double away = Math.Min(8 * 3600, DateTimeOffset.UtcNow.ToUnixTimeSeconds() - S.savedAtUnix);
            if (away < 30) return;
            long pages = 0; double money = 0;
            foreach (var m in margaritas)
            {
                int n = (int)Math.Min(S.margaritaTrays[m.slot], Math.Floor(away / Econ.MargaritaPeriod(S)));
                S.margaritaTrays[m.slot] -= n;
                pages += n;
                money += n * Econ.MargaritaPageValue(S);
            }
            if (pages == 0) return;
            S.pagesMachine += pages; S.pagesSold += pages;
            S.money += money; S.totalEarned += money;
            Toast("Mientras no estabas: " + Fmt.Int(pages) + " páginas de margarita, " + Fmt.Money(money));
        }

        // ------------------------------------------------------------------ bucle
        void Update()
        {
            float dt = Time.deltaTime;
            if (Mode != GameMode.Paused) S.playTime += dt;
            if (S.inspirationLeft > 0) { S.inspirationLeft -= dt; if (S.inspirationLeft <= 0) Toast("Se acabó la racha de inspiración"); }
            coinCooldown -= dt;

            var kb = Keyboard.current; var mouse = Mouse.current;
            switch (Mode)
            {
                case GameMode.Walking:
                    if (kb != null && kb.escapeKey.wasPressedThisFrame) { Mode = GameMode.Paused; SetCursor(); }
                    else if (kb != null && (kb.tabKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame)) OpenShop(null);
                    else if (kb != null && kb.tKey.wasPressedThisFrame) OpenShop(null, true);
                    break;
                case GameMode.Menu:
                    if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) CloseShop();
                    else if (kb != null && kb.tKey.wasPressedThisFrame) { if (Hud.ShopView == "arbol") CloseShop(); else Hud.ShopView = "arbol"; }
                    else if (kb != null && kb.cKey.wasPressedThisFrame && S.act >= 2) { if (Hud.ShopView == "catalogo") CloseShop(); else { Hud.ShopView = "catalogo"; shopFocus = null; } }
                    break;
                case GameMode.Computer:
                    if (kb != null)
                    {
                        if (kb.escapeKey.wasPressedThisFrame) ExitComputer();
                        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) pc.Submit(this);
                        else if (kb.backspaceKey.wasPressedThisFrame) pc.Backspace();
                    }
                    break;
                case GameMode.Typing:
                    if (kb != null)
                    {
                        if (kb.escapeKey.wasPressedThisFrame) ExitTyping();
                        else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                        {
                            if (typewriter.Done) ExtractAndNext();
                            else if (typewriter.AtLineEnd) typewriter.OnChar(this, '\n');
                        }
                        else if (kb.backspaceKey.wasPressedThisFrame) typewriter.Backspace(this);
                    }
                    if (mouse != null && mouse.leftButton.wasPressedThisFrame && typewriter.Jammed) typewriter.Unjam();
                    break;
                case GameMode.Paused:
                    if (kb != null && kb.escapeKey.wasPressedThisFrame) Resume();
                    break;
                case GameMode.ActComplete:
                    if ((kb != null && kb.enterKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                    {
                        Resume();
                        if (S.act >= 2) workday.StartFirstDay();
                    }
                    break;
            }

            UpdateEffects(dt);
            toasts.RemoveAll(t => Time.time - t.time > 4.5f);
            while (earnLog.Count > 0 && Time.time - earnLog.Peek().t > 20f) earnLog.Dequeue();

            achTimer -= dt;
            if (achTimer <= 0) { achTimer = 2f; CheckAchievements(); }
            areaVisualTimer -= dt;
            if (areaVisualTimer <= 0) { areaVisualTimer = 3f; factory.OnAreaChangedVisual(); }
            if (S.act >= 2 && Mode == GameMode.Walking) { eventTimer -= dt; if (eventTimer <= 0) { eventTimer = UnityEngine.Random.Range(180f, 420f); Events.Random(this); } }

            boardRefresh -= dt;
            if (boardRefresh <= 0) { boardRefresh = 0.5f; RefreshBoard(); }
            autosave -= dt;
            if (autosave <= 0) { autosave = 15f; if (!noSave) S.Save(); }
        }

        void LateUpdate()
        {
            // Cámara: en modo escritura se sienta frente a la máquina
            var cam = player.cam.transform;
            if (Mode == GameMode.Computer || pcCamBlend > 0)
            {
                pcCamBlend = Mathf.MoveTowards(pcCamBlend, Mode == GameMode.Computer ? 1f : 0f, Time.deltaTime * 2.5f);
                var pt = pc.transform;
                Vector3 seat = pt.TransformPoint(new Vector3(0, 1.22f, -0.55f));
                Quaternion lookPc = Quaternion.LookRotation(pt.TransformPoint(new Vector3(0, 1.17f, 0.15f)) - seat);
                Vector3 headPc = player.transform.TransformPoint(new Vector3(0, 1.62f, 0));
                float kp = Mathf.SmoothStep(0, 1, pcCamBlend);
                cam.position = Vector3.Lerp(headPc, seat, kp);
                cam.rotation = Quaternion.Slerp(player.transform.rotation * Quaternion.Euler(player.Pitch, 0, 0), lookPc, kp);
                player.cam.fieldOfView = Mathf.Lerp(70f, 50f, kp);
                foreach (var f in floaters) f.tm.transform.rotation = Quaternion.LookRotation(f.tm.transform.position - cam.position);
                return;
            }
            Halls.CameraEffect(this, cam);
            float target = Mode == GameMode.Typing ? 1f : 0f;
            typingCamBlend = Mathf.MoveTowards(typingCamBlend, target, Time.deltaTime * 2.5f);
            if (typingCamBlend > 0)
            {
                var tw = typewriter.transform;
                Vector3 seat = tw.TransformPoint(new Vector3(0.0f, 0.5f, -0.4f));
                Quaternion look = Quaternion.LookRotation(tw.TransformPoint(new Vector3(0, 0.17f, 0.1f)) - seat);
                Vector3 head = player.transform.TransformPoint(new Vector3(0, 1.62f, 0));
                float k = Mathf.SmoothStep(0, 1, typingCamBlend);
                cam.position = Vector3.Lerp(head, seat, k);
                cam.rotation = Quaternion.Slerp(player.transform.rotation, look, k);
                player.cam.fieldOfView = Mathf.Lerp(70f, 55f, k);
            }
            else player.cam.fieldOfView = 70f;

            foreach (var f in floaters) f.tm.transform.rotation = Quaternion.LookRotation(f.tm.transform.position - cam.position);
        }

        void OnTextInput(char c)
        {
            if (c < 32 || c == 127) return;
            if (Mode == GameMode.Typing) typewriter.OnChar(this, c);
            else if (Mode == GameMode.Computer) pc.OnChar(this, c);
        }

        void SetCursor()
        {
            bool free = Mode == GameMode.Paused || Mode == GameMode.ActComplete || Mode == GameMode.Menu || Mode == GameMode.Night;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        public void Resume() { Hud.TitleScreen = false; Mode = GameMode.Walking; SetCursor(); }

        // ------------------------------------------------------------------ máquina de escribir
        public void EnterTyping()
        {
            Mode = GameMode.Typing;
            // el jugador se sienta en la silla; al levantarse queda delante del escritorio
            player.Teleport(new Vector3(0.05f, 0.05f, 9.0f), 0);
            SetCursor();
            if (S.tutorialStep == 1) S.tutorialStep = 2;
        }

        public void ExitTyping()
        {
            Mode = GameMode.Walking;
            player.SetLook(0, 15);
            SetCursor();
        }

        public void ExtractAndNext()
        {
            var page = typewriter.Extract(this);
            if (page == null) return;
            if (handBelt) handBelt.Add(page);
            else S.carriedPages.Add(page);
            if (S.deskTray > 0 || S.carriedBlank > 0) typewriter.LoadSheet(this);
            else
            {
                ExitTyping();
                Toast(handBelt ? "No quedan hojas en blanco: ve a la montaña" : "Lleva la hoja al ducto y trae más papel de la montaña");
            }
        }

        public double PageValueHand(int chars, float quality, bool impeccable, bool golden)
        {
            double v = Econ.HandPageBase(chars) * quality;
            // Páginas de autor: desde el acto 2 valen también un par de segundos de la producción de la nave
            if (S.act >= 2) v = v * Progress.GlobalMult(S) + factory.income * 2.0 * quality;
            if (S.Node("ductoDoble") > 0) v *= 2;
            if (S.Node("papelPergamino") > 0) v *= 2;
            if (S.hall == 2) v *= 10;
            if (S.inspirationLeft > 0) v *= 1.5;
            if (golden && impeccable) v *= 10;
            return v;
        }

        public void OnPageTypedComplete()
        {
            if (S.tutorialStep == 2) S.tutorialStep = 3;
        }

        public void RegisterHandPage(PageData page, bool impeccable, float seconds)
        {
            S.pagesTyped++;
            float wpm = seconds > 0.5f ? page.text.Length / 5f / (seconds / 60f) : 0;
            if (impeccable)
            {
                S.streak++;
                S.bestStreak = Mathf.Max(S.bestStreak, S.streak);
                if (S.streak % Econ.StreakForInspiration == 0)
                {
                    S.inspirationLeft = Econ.InspirationSeconds;
                    Toast("¡Racha de inspiración! +50 % de valor durante 60 s");
                    Sfx.Play("success", 0.7f);
                }
            }
            else S.streak = 0;
            if (page.golden)
            {
                S.goldenCount++;
                Toast(impeccable ? "¡Frase dorada impecable! ×10 de valor" : "Frase dorada… con fallos. La próxima, impecable.");
            }
            lastPageReport = (impeccable ? "<color=#ffd34d>IMPECABLE ×2</color>" : "Calidad ×" + page.quality.ToString("0.00")) +
                             "   " + Fmt.Money(page.value) + "   " + Mathf.RoundToInt(wpm) + " ppm";
        }

        // ------------------------------------------------------------------ economía
        public int CarryCount => S.carriedBlank + S.carriedPages.Count;

        public double CarriedPagesValue()
        {
            double v = 0;
            foreach (var p in S.carriedPages) v += p.value;
            return v;
        }

        public double EarnRate
        {
            get
            {
                double sum = 0;
                foreach (var e in earnLog) sum += e.v;
                return sum / Mathf.Min(20f, Mathf.Max(1f, Time.timeSinceLevelLoad));
            }
        }

        public void Earn(double v, Vector3 where)
        {
            if (v <= 0) return;
            S.money += v; S.totalEarned += v; S.lifetimeEarned += v; S.dayEarned += v;
            earnLog.Enqueue((Time.time, v));
            if (coinCooldown <= 0) { Sfx.PlayAt("coin", where, 0.6f); coinCooldown = 0.12f; }
            SpawnFloater("+" + Fmt.Money(v), where + Vector3.up * 0.4f);
        }

        /// <summary>Ingresos de las máquinas: sin moneda ni cifra flotante (se ven en el contador y el HUD).</summary>
        public void EarnSilent(double v)
        {
            if (v <= 0) return;
            S.money += v; S.totalEarned += v; S.lifetimeEarned += v; S.dayEarned += v;
            earnLog.Enqueue((Time.time, v));
        }

        public void OnSoldAtDuct(long pages, int blanks)
        {
            if (S.act < 2) S.dayPages += pages;
            if (S.tutorialStep == 3 && pages > 0) S.tutorialStep = 4;
        }

        public void OnMargaritaFilled() { }

        public void PickSheet(Vector3 at)
        {
            S.sheetsPicked++;
            S.carriedBlank++;
            mountain.SetArea(Econ.FreeArea(S));
            if (S.act >= 2 && UnityEngine.Random.value < 0.002f * (S.Node("lupa") > 0 ? 2 : 1)) FindScroll(true);
            if (Econ.HasGun(S)) Sfx.PlayAt("vacuum", at, 0.5f);
            else Sfx.PlayAt("paper", at, 0.9f);
            if (S.tutorialStep == 0) S.tutorialStep = 1;
            CheckAct();
        }

        public void OnAreaChanged()
        {
            mountain.SetArea(Econ.FreeArea(S));
            CheckAct();
        }

        /// <summary>Avanza de acto según el terreno ganado (doc 6.1). Puede saltar varios a la vez tras un prestigio.</summary>
        public void CheckAct()
        {
            double area = Econ.FreeArea(S);
            int target = S.act;
            for (int a = S.act + 1; a <= 6; a++) if (area >= Progress.ActArea[a]) target = a; else break;
            if (target >= 6 && area >= Progress.FullArea - 1) target = 7;
            if (target >= 7 && S.publishers >= 1) target = 8;
            if (target <= S.act) return;
            int from = S.act;
            S.act = target;
            if (S.act >= 2) S.act1Done = true;
            if (S.act > S.maxAct) S.maxAct = S.act;
            if (S.act >= 7 && S.hall > 0 && !S.hallsDone.Contains(S.hall))
            {
                S.hallsDone.Add(S.hall);
                Toast("¡Nave completada: " + Halls.Names[S.hall] + "! Multiplicador global ×3");
            }
            completedAct = from;
            if (Mode == GameMode.Typing) ExitTyping();
            if (Mode == GameMode.Computer) ExitComputer();
            if (Mode == GameMode.Menu) CloseShop();
            if (Mode == GameMode.Night) return; // se verá al empezar el día
            Mode = GameMode.ActComplete;
            SetCursor();
            Sfx.Play("success", 1f);
            RefreshBoard();
        }

        public static string ActIntro(int act)
        {
            switch (act)
            {
                case 2: return "Se abre El Taller: mecanógrafos, brazos de recogida y cintas hasta el muelle.\nDesde hoy hay jornada: pedido del día, sirena a las 19:30 y fichar antes de las 20:00.\nPulsa Tab o usa el atril del catálogo para comprar.";
                case 3: return "Se abre La Oficina: puestos PC con Escriba, impresoras matricial y de inyección.\nTu propio PC está junto al tablón: escribe más rápido que en la máquina y sin atascos.";
                case 4: return "Se abre El Mostrador: la ventanilla al público, fotocopias, disquetes y casetes.\nLa reputación multiplica todo. Ya puedes hacer tu primer prestigio: Nueva edición.";
                case 5: return "Se abre El Estudio: plóter de gran formato, laboratorio de fotos y bots de macros.";
                case 6: return "Se abre La Fábrica: offset, rotativa e IA redactora. La nave se automatiza entera.";
                case 7: return "Has llegado al fondo de la montaña. Se abre La Red: racks de IA y contratos de gobierno.\nYa puedes fundar tu Editorial (segundo prestigio).";
                case 8: return "Más allá: prensas cuánticas, imprentas fractales y la Biblioteca de Babel.\nLa Biblioteca infinita (tercer prestigio) abre naves nuevas con reglas propias.";
            }
            return "";
        }

        public void TryBuy(UpgradeDef def)
        {
            int lvl = S.Level(def.key);
            if (lvl >= def.maxLevel) { Sfx.Play("deny", 0.4f); return; }
            double cost = def.Cost(lvl);
            if (S.money < cost) { Sfx.Play("deny", 0.5f); Toast("Te faltan " + Fmt.Money(cost - S.money)); return; }
            S.money -= cost;
            S.SetLevel(def.key, lvl + 1);
            Sfx.Play("buy", 0.8f);
            Toast(def.name + (def.maxLevel > 1 ? " nivel " + (lvl + 1) : "") + ": " + def.desc);
            switch (def.key)
            {
                case "bandejaMaq": EnsureDeskTray(); break;
                case "cinta": EnsureHandBelt(); break;
                case "margarita": AddMargarita(lvl); break;
            }
            if (S.tutorialStep == 4) S.tutorialStep = 5;
            RefreshBoard();
        }

        public string Objective()
        {
            switch (S.tutorialStep)
            {
                case 0: return "Coge una hoja de la montaña: acércate al papel y haz clic";
                case 1: return "Llévala a la máquina de escribir del escritorio y haz clic en ella";
                case 2: return "Teclea la frase de la tarjeta. Al final de cada línea, Espacio o Enter";
                case 3: return typewriter.Done ? "Pulsa Enter para sacar la hoja" : "Échala al ducto de succión, junto al escritorio";
                case 4: return "Gana dinero y compra una mejora en el tablón de corcho";
            }
            if (S.Level("cinta") == 0) return "Escribe 4 páginas y compra la cinta corta";
            if (S.Level("margarita") == 0) return "Compra una máquina de margarita en el tablón";
            if (S.pagesMachine == 0) return "Rellena la bandeja de la margarita con hojas en blanco";
            if (!S.act1Done) return "Gana terreno a la montaña: " + Fmt.Area(Econ.FreeArea(S)) + " de " + Fmt.Area(Econ.GoalArea);
            return ObjectiveLate();
        }

        string ObjectiveLate()
        {
            if (S.act == 2 && S.Count("mecanografo") == 0) return "Pulsa Tab (o el atril del catálogo) y contrata un mecanógrafo";
            if (S.act == 2 && S.Count("brazo") == 0) return "Compra un brazo de recogida: gana terreno a la montaña solo";
            if (S.act >= 2 && workday.started && workday.CanClockIn) return "¡Sirena! Ficha la salida en el vestuario, junto al portón, antes de las " + (int)workday.End + ":00";
            if (S.act == 3 && S.pagesPC == 0) return "Siéntate en tu PC (junto al tablón) y escribe en Escriba";
            if (S.act == 4 && S.customersServed == 0) return "Atiende a un cliente en la ventanilla, al fondo a la derecha de la entrada";
            if (S.act >= 4 && Prestige.EditionGain(S) >= 10 && S.editions == 0) return "Puedes hacer una Nueva edición: " + Prestige.EditionGain(S) + " gotas de tinta dorada (menú de pausa)";
            if (S.act < 6) return "Acto " + S.act + ": " + Progress.ActNames[S.act] + " · terreno " + Fmt.Area(Econ.FreeArea(S)) + " de " + Fmt.Area(Progress.ActArea[S.act + 1]);
            if (S.act == 6) return "Vacía la montaña hasta el fondo: " + Fmt.Area(Econ.FreeArea(S)) + " de " + Fmt.Area(Progress.FullArea);
            if (S.act == 7) return S.publishers == 0 ? "Funda tu Editorial (menú de pausa) para pasar a Más allá" : "La Red: amplía la producción";
            return "Más allá · " + Halls.Names[S.hall] + (S.hallsDone.Count < 4 ? " · Biblioteca infinita en el menú de pausa" : " · todas las naves completadas");
        }

        // ------------------------------------------------------------------ tienda (actos 2+)
        public void OpenShop(string focus, bool tree = false)
        {
            if (S.act < 2 && !tree) { Toast("El catálogo se abre en el acto 2 (el árbol ya se puede mirar con T)"); return; }
            Hud.ShopView = tree ? "arbol" : "catalogo";
            shopFocus = focus;
            Mode = GameMode.Menu;
            SetCursor();
        }

        public void CloseShop()
        {
            Mode = S.nightPending ? GameMode.Night : GameMode.Walking;
            SetCursor();
        }

        public int AffordableCount(ProducerDef p)
        {
            int n = S.Count(p.key), k = 0;
            double money = S.money;
            while (k < 1000) { double c = p.Cost(n + k); if (money < c) break; money -= c; k++; }
            return k;
        }

        public void BuyProducer(ProducerDef p, int amount)
        {
            if (S.act < p.act) return;
            int bought = 0;
            for (int i = 0; i < amount; i++)
            {
                double c = p.Cost(S.Count(p.key));
                if (S.money < c) break;
                S.money -= c;
                S.SetCount(p.key, S.Count(p.key) + 1);
                bought++;
            }
            if (bought == 0) { Sfx.Play("deny", 0.5f); return; }
            Sfx.Play("buy", 0.8f);
            factory.Sync(p);
            factory.Recalc();
            if (p.key == "dependiente") counter.Sync(this);
        }

        public void BuyNode(NodeDef n)
        {
            int lvl = S.Node(n.key);
            if (lvl >= n.maxLevel || Progress.Missing(S, n) != null) { Sfx.Play("deny", 0.5f); return; }
            double c = n.Cost(lvl);
            if (S.money < c) { Sfx.Play("deny", 0.5f); return; }
            S.money -= c;
            S.SetNode(n.key, lvl + 1);
            Sfx.Play("buy", 0.8f);
            factory.Recalc();
        }

        public static readonly (string key, string name, string desc, int max, double costMul)[] ObjUpgrades =
        {
            ("vel", "Velocidad", "+15 % de producción por nivel", 10, 8),
            ("cal", "Calidad", "+10 % de valor por nivel", 10, 12),
            ("esp", "Revisión completa", "×2 a este objeto", 1, 200),
        };

        public double ObjCost(ProducerDef p, int idx) => p.baseCost * ObjUpgrades[idx].costMul * Math.Pow(1.6, S.Obj(p.key, ObjUpgrades[idx].key));

        public void BuyObj(ProducerDef p, int idx)
        {
            var u = ObjUpgrades[idx];
            int lvl = S.Obj(p.key, u.key);
            double c = ObjCost(p, idx);
            if (lvl >= u.max || S.money < c || S.Count(p.key) == 0) { Sfx.Play("deny", 0.5f); return; }
            S.money -= c;
            S.SetObj(p.key, u.key, lvl + 1);
            Sfx.Play("buy", 0.8f);
            factory.Recalc();
        }

        // ------------------------------------------------------------------ noche
        public void EnterNight()
        {
            if (Mode == GameMode.Typing) ExitTyping();
            if (Mode == GameMode.Computer) ExitComputer();
            Mode = GameMode.Night;
            SetCursor();
            if (!noSave) S.Save();
        }

        public void StartNextDay()
        {
            workday.StartNextDay();
            Mode = GameMode.Walking;
            SetCursor();
            if (completedAct > 0 && S.act > completedAct && S.act >= 3) { Mode = GameMode.ActComplete; SetCursor(); }
        }

        // ------------------------------------------------------------------ PC (acto 3)
        public void EnterComputer()
        {
            Mode = GameMode.Computer;
            player.Teleport(pc.transform.TransformPoint(new Vector3(0, 0.05f, -0.9f)), pc.transform.eulerAngles.y);
            SetCursor();
            pc.Begin(this);
        }

        public void ExitComputer()
        {
            Mode = GameMode.Walking;
            SetCursor();
        }

        // ------------------------------------------------------------------ pergaminos y logros
        public void CheckScrolls()
        {
            int k = S.scrolls.Count;
            if (k >= Progress.ScrollNames.Length) return;
            double need = Progress.SheetsForScroll(k) / (S.Node("lupa") > 0 ? 2 : 1);
            if (S.TotalSheets >= need) FindScroll(false);
        }

        void FindScroll(bool byHand)
        {
            for (int i = 0; i < Progress.ScrollNames.Length; i++)
                if (!S.scrolls.Contains(i))
                {
                    S.scrolls.Add(i);
                    Sfx.Play("chime", 0.9f);
                    Toast("Pergamino secreto" + (byHand ? " entre tus hojas" : " en la montaña") + ": «" + Progress.ScrollNames[i] + "» (+" + (S.Node("archivo") > 0 ? 5 : 3) + " %)");
                    return;
                }
        }

        void CheckAchievements()
        {
            foreach (var a in Progress.Achievements)
            {
                if (S.achievements.Contains(a.key)) continue;
                bool ok = false;
                try { ok = a.done(S); } catch { }
                if (!ok) continue;
                S.achievements.Add(a.key);
                Toast("Logro: " + a.name + " (+1 %)");
                Sfx.Play("chime", 0.5f);
            }
        }

        // ------------------------------------------------------------------ efectos
        public void Toast(string s)
        {
            toasts.RemoveAll(t => t.text == s);
            toasts.Add((s, Time.time));
            if (toasts.Count > 4) toasts.RemoveAt(0);
        }

        public void SpawnFlyingSheet(Vector3 from, Vector3 to, float dur)
        {
            if (flyers.Count > 60) return;
            var go = sheetPool.Count > 0 ? sheetPool.Dequeue() : Build.Box("HojaVolando", transform, Vector3.zero, new Vector3(0.21f, 0.002f, 0.297f), Build.Mat(Build.Paper, 0.2f), false);
            go.SetActive(true);
            go.transform.position = from;
            go.transform.rotation = UnityEngine.Random.rotation;
            flyers.Add(new Flyer { t = go.transform, a = from, b = to, time = 0, dur = dur });
        }

        void SpawnFloater(string text, Vector3 at)
        {
            var tm = Build.Text("Flotante", transform, at, text, 0.09f, Build.Hex("#ffd34d"));
            floaters.Add(new Floater { tm = tm, time = 0, start = at });
            if (floaters.Count > 12) { Destroy(floaters[0].tm.gameObject); floaters.RemoveAt(0); }
        }

        void UpdateEffects(float dt)
        {
            for (int i = flyers.Count - 1; i >= 0; i--)
            {
                var f = flyers[i];
                f.time += dt;
                float k = Mathf.Clamp01(f.time / f.dur);
                var p = Vector3.Lerp(f.a, f.b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
                f.t.position = p;
                f.t.Rotate(new Vector3(200, 340, 120) * dt, Space.Self);
                if (k >= 1)
                {
                    f.t.gameObject.SetActive(false);
                    sheetPool.Enqueue(f.t.gameObject);
                    flyers.RemoveAt(i);
                }
            }
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var f = floaters[i];
                f.time += dt;
                f.tm.transform.position = f.start + Vector3.up * f.time * 0.5f;
                var c = f.tm.color; c.a = 1 - f.time / 1.3f; f.tm.color = c;
                if (f.time > 1.3f) { Destroy(f.tm.gameObject); floaters.RemoveAt(i); }
            }
        }

        public void LoadNewRun(SaveData next)
        {
            pendingSave = next;
            if (!noSave) next.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ResetSave()
        {
            SaveData.Delete();
            noSave = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
