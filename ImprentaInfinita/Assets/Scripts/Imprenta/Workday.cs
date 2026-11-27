using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// La jornada con hora de cierre (doc 4.17), desde el acto 2: reloj de 8:00 a 20:00 en 7 minutos reales,
    /// pedido del día, sirena, apagado por sectores, furgoneta, reloj de fichar y la noche con el parte.
    /// </summary>
    public class Workday : MonoBehaviour
    {
        public static Workday I;
        Game g;
        SaveData S => g.S;
        Transform root, hourHand, minuteHand, van, vanDoor, shutter, doorL, doorR;
        Light reverseLight;
        Transform liftL, liftR;
        float liftOpen = -1;
        static readonly Vector3 TruckOut = new Vector3(28.5f, 0, -68f), TruckIn = new Vector3(28.5f, 0, -9.9f);
        // 0 fuera, 1 sube el portón, 2 entra marcha atrás, 3 abre puertas, 4 cargando, 5 cierra puertas, 6 se va, 7 baja el portón, 8 terminado
        int truckPhase;
        float truckT, loadTick, beepT;
        readonly List<(Light l, float intensity, float z)> lights = new List<(Light, float, float)>();
        Light sun, flashlight;
        float sunIntensity;
        Color ambSky, ambEq;
        bool sirenDone, honkDone, lightsOutStarted, urgentAnnounced;
        float fastForward = 1f, darkTimer, whisperT;
        public string lastReport = "";
        public double lastBonus;
        public bool started;
        public double urgentTarget;

        public float Hour => 8f + S.dayTime / Progress.DaySeconds * 12f;
        public float End => Progress.EndHour(S);
        public bool MachinesOn => started && !S.nightPending && Hour < End - 0.25f;
        public bool Dark => started && Hour >= End - 0.2f;
        public string ClockText { get { float h = Mathf.Min(Hour, End); int hh = (int)h, mm = (int)((h - hh) * 60); return hh.ToString("00") + ":" + mm.ToString("00"); } }

        public static Workday Create(Game g, Transform world)
        {
            var go = new GameObject("Jornada");
            go.transform.SetParent(world, false);
            var w = go.AddComponent<Workday>();
            w.g = g; w.root = go.transform;
            I = w;
            w.BuildProps();
            return w;
        }

        void BuildProps()
        {
            // Reloj de pared sobre el portón
            var clock = Build.Empty("RelojPared", root, new Vector3(0, 9.5f, -9.6f), new Vector3(0, 180, 0)).transform;
            if (Build.Model("RelojPared", clock, Vector3.zero, null, Props.Map) == null)
            {
                Build.Cyl("Esfera", clock, Vector3.zero, 2.0f, 0.12f, Build.Mat(Build.Hex("#efe9da"), 0.5f), false, new Vector3(90, 0, 0));
                Build.Cyl("Aro", clock, new Vector3(0, 0, 0.02f), 2.15f, 0.1f, Build.Mat(Build.SteelDark, 0.6f, 0.8f), false, new Vector3(90, 0, 0));
                for (int i = 0; i < 12; i++)
                    Build.Box("Marca", clock, Quaternion.Euler(0, 0, i * 30) * new Vector3(0, 0.85f, -0.07f), new Vector3(0.04f, 0.14f, 0.01f), Build.Mat(Build.Hex("#1a1a1a"), 0.3f), false, new Vector3(0, 0, i * 30));
            }
            hourHand = Build.Empty("Horaria", clock, new Vector3(0, 0, -0.09f)).transform;
            Build.Box("Aguja", hourHand, new Vector3(0, 0.25f, 0), new Vector3(0.06f, 0.5f, 0.01f), Build.Mat(Build.Hex("#1a1a1a"), 0.3f), false);
            minuteHand = Build.Empty("Minutero", clock, new Vector3(0, 0, -0.1f)).transform;
            Build.Box("Aguja", minuteHand, new Vector3(0, 0.36f, 0), new Vector3(0.035f, 0.72f, 0.01f), Build.Mat(Build.Hex("#1a1a1a"), 0.3f), false);

            // Vestuario: taquillas, reloj de fichar y el ascensor de personal
            var locker = Build.Empty("Vestuario", root, new Vector3(-13f, 0, -9.4f), new Vector3(0, 180, 0)).transform;
            if (Build.Model("Taquillas", locker, Vector3.zero, null, Props.Map) == null)
                for (int i = 0; i < 6; i++)
                {
                    Build.Box("Taquilla", locker, new Vector3(-1.5f + i * 0.6f, 0.95f, 0), new Vector3(0.58f, 1.9f, 0.5f), Build.Mat(Build.Hex("#5e7a8a"), 0.45f, 0.5f), true);
                    Build.Box("Rejilla", locker, new Vector3(-1.5f + i * 0.6f, 1.65f, -0.255f), new Vector3(0.3f, 0.12f, 0.01f), Build.Mat(Build.Hex("#2a2f33"), 0.3f), false);
                }
            var tc = Build.Empty("RelojFichar", root, new Vector3(-9.6f, 0, -9.65f), new Vector3(0, 180, 0));
            tc.AddComponent<TimeClock>();
            if (Build.Model("RelojFichar", tc.transform, Vector3.zero, null, Props.Map) == null)
            {
                Build.Box("Caja", tc.transform, new Vector3(0, 1.45f, 0), new Vector3(0.36f, 0.48f, 0.22f), Build.Mat(Build.Hex("#9b9a8f"), 0.5f, 0.3f), true);
                Build.Cyl("Esfera", tc.transform, new Vector3(0, 1.55f, -0.115f), 0.22f, 0.02f, Build.Mat(Build.Hex("#efe9da"), 0.5f), false, new Vector3(90, 0, 0));
                Build.Box("Ranura", tc.transform, new Vector3(0, 1.3f, -0.115f), new Vector3(0.12f, 0.03f, 0.01f), Build.Mat(Build.Hex("#1a1a1a"), 0.3f), false);
            }
            var bc = tc.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.45f, 0); bc.size = new Vector3(0.5f, 0.6f, 0.4f);
            var rack = Build.Empty("Tarjetero", tc.transform, new Vector3(0.45f, 1.45f, 0));
            for (int i = 0; i < 8; i++)
                Build.Box("Tarjeta", rack.transform, new Vector3((i % 2) * 0.09f, 0.2f - (i / 2) * 0.12f, -0.03f), new Vector3(0.07f, 0.11f, 0.005f), Build.Mat(Build.Hex("#e8dcc0"), 0.2f), false);
            Build.Text("Cartel", tc.transform, new Vector3(0, 2.05f, -0.02f), "FICHAR", 0.1f, Build.Hex("#e8b923"));
            // Ascensor de personal
            var lift = Build.Empty("Ascensor", root, new Vector3(-17.5f, 0, -9.7f)).transform;
            Build.Box("Marco", lift, new Vector3(0, 1.25f, 0), new Vector3(1.6f, 2.5f, 0.2f), Build.Mat(Build.SteelDark, 0.6f, 0.8f), false);
            Build.Box("Hueco", lift, new Vector3(0, 1.1f, 0.06f), new Vector3(1.4f, 2.1f, 0.04f), Build.Mat(Build.Hex("#e8e4d8"), 0.4f, 0, new Color(0.6f, 0.58f, 0.5f)), false);
            liftL = Build.Box("Puerta", lift, new Vector3(-0.35f, 1.1f, 0.11f), new Vector3(0.68f, 2.1f, 0.03f), Build.Mat(Build.Steel, 0.8f, 1f), false).transform;
            liftR = Build.Box("Puerta", lift, new Vector3(0.35f, 1.1f, 0.11f), new Vector3(0.68f, 2.1f, 0.03f), Build.Mat(Build.Steel, 0.8f, 1f), false).transform;
            Build.Box("Indicador", lift, new Vector3(0, 2.4f, 0.11f), new Vector3(0.3f, 0.12f, 0.02f), Build.Mat(Build.Hex("#1a1a1a"), 0.4f, 0, new Color(0.2f, 1.2f, 0.3f)), false);
            Build.Box("Boton", lift, new Vector3(1.0f, 1.2f, 0.11f), new Vector3(0.08f, 0.12f, 0.02f), Build.Mat(Build.Hex("#e8b923"), 0.5f, 0, new Color(1.5f, 1.1f, 0.2f)), false);

            // Portón del muelle (persiana que sube) y camión de reparto
            shutter = Build.Box("PortonMuelle", root, new Vector3(28f, 2.5f, -9.93f), new Vector3(6f, 5f, 0.1f), Build.Pbr("painted_metal_shutter", new Vector2(2, 2), Build.Hex("#7c5d4a"))).transform;
            Build.Box("GuiaPorton", root, new Vector3(24.95f, 2.6f, -9.9f), new Vector3(0.12f, 5.2f, 0.16f), Build.Mat(Build.SteelDark, 0.5f, 0.8f));
            Build.Box("GuiaPorton", root, new Vector3(31.05f, 2.6f, -9.9f), new Vector3(0.12f, 5.2f, 0.16f), Build.Mat(Build.SteelDark, 0.5f, 0.8f));
            Build.Box("CajonPorton", root, new Vector3(28f, 5.35f, -9.85f), new Vector3(6.3f, 0.6f, 0.5f), Build.Mat(Build.SteelDark, 0.5f, 0.8f), false);
            for (int i = 0; i < 6; i++)
                Build.Box("RayaMuelle", root, new Vector3(25.6f + i, 0.006f, -9.3f), new Vector3(0.45f, 0.01f, 0.9f), Build.Mat(Build.Safety, 0.35f), false, new Vector3(0, 35, 0));
            vanDoor = shutter;
            van = Build.Empty("Camion", root, TruckOut, new Vector3(0, 180, 0)).transform;
            var paint = Build.Mat(Build.Hex("#2d5b8a"), 0.6f, 0.3f);
            if (Build.Model("Camion", van, Vector3.zero, null, n => n == "Pintura" ? paint : n == "PinturaBlanca" ? Build.Mat(Build.Hex("#e9e7e0"), 0.55f, 0.1f) : Props.Map(n)) == null)
            {
                Build.Box("Caja", van, new Vector3(0, 2.28f, -1.7f), new Vector3(2.44f, 2.56f, 4.45f), Build.Mat(Build.Hex("#e9e7e0"), 0.55f, 0.1f), false);
                Build.Box("Cabina", van, new Vector3(0, 1.9f, 1.95f), new Vector3(2.38f, 2.4f, 2.4f), paint, false);
                foreach (var x in new[] { -1f, 1f })
                    foreach (var z in new[] { 2.1f, -2.6f, -1.5f })
                        Build.Cyl("Rueda", van, new Vector3(x, 0.52f, z), 1.04f, 0.3f, Build.Rubber, false, new Vector3(0, 0, 90));
            }
            var vc = van.gameObject.AddComponent<BoxCollider>();
            vc.center = new Vector3(0, 2.0f, -0.4f); vc.size = new Vector3(2.5f, 3.6f, 7.2f);
            Build.Text("Rotulo", van, new Vector3(1.25f, 2.6f, -1.7f), "IMPRENTA INFINITA", 0.3f, Build.Hex("#2d5b8a"), TextAnchor.MiddleCenter, new Vector3(0, -90, 0));
            Build.Text("Rotulo", van, new Vector3(-1.25f, 2.6f, -1.7f), "IMPRENTA INFINITA", 0.3f, Build.Hex("#2d5b8a"), TextAnchor.MiddleCenter, new Vector3(0, 90, 0));
            Build.Text("Lema", van, new Vector3(1.25f, 2.15f, -1.7f), "papel sin fin · reparto diario", 0.12f, Build.Hex("#555555"), TextAnchor.MiddleCenter, new Vector3(0, -90, 0));
            doorL = MakeTruckDoor(new Vector3(-1.2f, 1.05f, -3.94f), false);
            doorR = MakeTruckDoor(new Vector3(1.2f, 1.05f, -3.94f), true);
            reverseLight = Build.PointLight(van, new Vector3(0, 0.9f, -4.3f), Build.Hex("#fff4e0"), 0f, 6f);
            van.gameObject.SetActive(false);

            // Linterna (doc 4.17: si no fichas, te quedas a oscuras con ella)
            var fl = new GameObject("Linterna");
            flashlight = fl.AddComponent<Light>();
            flashlight.type = LightType.Spot; flashlight.spotAngle = 45; flashlight.intensity = 6f; flashlight.range = 14f;
            flashlight.color = Build.Hex("#fff0d0"); flashlight.enabled = false; flashlight.shadows = LightShadows.Soft;
        }

        Transform MakeTruckDoor(Vector3 hinge, bool right)
        {
            var h = Build.Empty(right ? "PuertaDer" : "PuertaIzq", van, hinge).transform;
            var m = Build.Model("CamionPuerta", h, Vector3.zero, null, n => n == "Pintura" ? Build.Mat(Build.Hex("#2d5b8a"), 0.6f, 0.3f) : n == "PinturaBlanca" ? Build.Mat(Build.Hex("#e9e7e0"), 0.55f, 0.1f) : Props.Map(n));
            if (m == null) m = Build.Box("Hoja", h, new Vector3(0.6f, 1.25f, 0), new Vector3(1.18f, 2.46f, 0.05f), Build.Mat(Build.Hex("#e9e7e0"), 0.55f, 0.1f), false);
            // la hoja debe crecer hacia el centro del camión: +x la izquierda, -x la derecha
            var rs = m.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float cx = h.InverseTransformPoint(b.center).x;
                bool growsPlus = cx > 0;
                if (growsPlus == right) { var sc = m.transform.localScale; sc.x = -sc.x; m.transform.localScale = sc; }
            }
            return h;
        }

        void SetTruckDoors(float open)
        {
            float a = Mathf.SmoothStep(0, 1, open) * 105f;
            if (doorL) doorL.localRotation = Quaternion.Euler(0, a, 0);
            if (doorR) doorR.localRotation = Quaternion.Euler(0, -a, 0);
        }

        void SetShutter(float open)
        {
            float k = Mathf.SmoothStep(0, 1, open);
            shutter.localPosition = new Vector3(28f, 2.5f + 4.7f * k, -9.93f);
        }

        public void Announce(string text)
        {
            g.Toast("<color=#dba92b>MEGAFONÍA</color> · " + text);
            Sfx.Play("chime", 0.45f);
        }

        public Vector3 TruckLoadPoint => van.TransformPoint(new Vector3(0, 1.6f, -2.6f));

        /// <summary>El camión llega por la tarde marcha atrás, abre, carga lo que hay en el muelle y se va antes del cierre.</summary>
        void UpdateTruck(float hour, float dt)
        {
            float arrive = End - 2.2f, leave = End - 0.35f;
            if (truckPhase == 0 && hour >= arrive && hour < leave) { truckPhase = 1; truckT = 0; Sfx.PlayAt("bell", shutter.position, 0.5f); Announce("El camión de reparto entra en el muelle. Despejen la zona de carga."); }
            if (truckPhase >= 1 && truckPhase <= 4 && hour >= leave) { if (truckPhase < 4) { truckPhase = 4; } }
            truckT += dt;
            switch (truckPhase)
            {
                case 1:
                    SetShutter(truckT / 4f);
                    if (truckT >= 4f) { truckPhase = 2; truckT = 0; van.gameObject.SetActive(true); van.localPosition = TruckOut; SetTruckDoors(0); }
                    break;
                case 2:
                {
                    float k = Mathf.Clamp01(truckT / 14f);
                    float e = 1 - (1 - k) * (1 - k);
                    van.localPosition = Vector3.Lerp(TruckOut, TruckIn, e);
                    reverseLight.intensity = 2.5f;
                    beepT -= dt;
                    if (beepT <= 0) { beepT = 0.8f; Sfx.PlayAt("deny", van.position, 0.35f); }
                    if (k >= 1) { truckPhase = 3; truckT = 0; reverseLight.intensity = 0; Sfx.PlayAt("stamp", van.position, 0.6f); }
                    break;
                }
                case 3:
                    SetTruckDoors(truckT / 2.2f);
                    if (truckT >= 2.2f) { truckPhase = 4; truckT = 0; }
                    break;
                case 4:
                    // carga: las hojas del muelle vuelan a la caja
                    loadTick -= dt;
                    if (loadTick <= 0)
                    {
                        loadTick = 0.12f;
                        var near = Paper.Near(new Vector3(25f, 0.5f, -5f), 7.5f);
                        for (int i = 0; i < Mathf.Min(4, near.Count); i++) near[i].FlyTo(TruckLoadPoint + Random.insideUnitSphere * 0.6f);
                    }
                    if (hour >= leave) { truckPhase = 5; truckT = 0; Sfx.PlayAt("fump", van.position, 0.8f); }
                    break;
                case 5:
                    SetTruckDoors(1 - truckT / 2f);
                    if (truckT >= 2f) { truckPhase = 6; truckT = 0; Sfx.PlayAt("horn", van.position, 1f); g.Toast("El camión se va con el pedido del día"); }
                    break;
                case 6:
                {
                    float k = Mathf.Clamp01(truckT / 11f);
                    van.localPosition = Vector3.Lerp(TruckIn, TruckOut, k * k);
                    if (k >= 1) { truckPhase = 7; truckT = 0; van.gameObject.SetActive(false); }
                    break;
                }
                case 7:
                    SetShutter(1 - truckT / 4f);
                    if (truckT >= 4f) { truckPhase = 8; Sfx.PlayAt("fump", shutter.position, 0.7f); }
                    break;
            }
        }

        /// <summary>Se llama cuando el mundo ya está montado: recoge las luces para apagarlas por sectores.</summary>
        public void CollectLights(Transform world)
        {
            lights.Clear();
            foreach (var l in world.GetComponentsInChildren<Light>(true))
            {
                if (l == flashlight) continue;
                if (l.type == LightType.Directional) { sun = l; sunIntensity = l.intensity; continue; }
                lights.Add((l, l.intensity, l.transform.position.z));
            }
            ambSky = RenderSettings.ambientSkyColor; ambEq = RenderSettings.ambientEquatorColor;
        }

        public void StartFirstDay()
        {
            if (started) return;
            started = true;
            if (S.day == 0) NewDay(true);
            else if (S.nightPending) EnterNight(false, true);
            ApplyLighting();
        }

        void NewDay(bool first)
        {
            S.day++;
            S.dayTime = 0; S.dayPages = 0; S.dayEarned = 0; S.orderMet = false; S.clockedIn = false; S.nightPending = false; S.urgentDone = false;
            Factory.I.Recalc();
            S.order = S.act < 2 ? 6 + 2 * S.day : Progress.OrderFor(S, Factory.I.pagesPerSec);
            sirenDone = honkDone = lightsOutStarted = urgentAnnounced = false;
            fastForward = 1f; darkTimer = 0;
            urgentTarget = 0;
            truckPhase = 0; truckT = 0;
            van.gameObject.SetActive(false);
            SetShutter(0);
            Sfx.Play("bell", 0.7f);
            if (!first) liftOpen = 0;
            Announce("Atención. Empieza el turno del día " + S.day + ". Pedido: " + Fmt.Num(S.order) + " páginas.");
            g.Toast("Día " + S.day + " · 8:00 · Pedido de hoy: " + Fmt.Num(S.order) + " páginas antes de las " + (int)End + ":00");
            if (first) g.Toast("Desde hoy hay hora de cierre: ficha la salida antes de las " + (int)End + ":00");
        }

        void Update()
        {
            if (!started || g == null) return;
            if (liftOpen >= 0 && liftL)
            {
                liftOpen += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((liftOpen - 0.4f) / 1.2f)) * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((liftOpen - 5f) / 1.2f)));
                liftL.localPosition = new Vector3(-0.35f - 0.66f * k, 1.1f, 0.11f);
                liftR.localPosition = new Vector3(0.35f + 0.66f * k, 1.1f, 0.11f);
                if (liftOpen > 6.5f) liftOpen = -1;
            }
            // agujas del reloj de pared
            float h = Mathf.Min(Hour, End);
            if (hourHand) hourHand.localRotation = Quaternion.Euler(0, 0, -(h % 12f) * 30f);
            if (minuteHand) minuteHand.localRotation = Quaternion.Euler(0, 0, -(h % 1f) * 360f);

            if (S.nightPending || g.Mode == GameMode.Night) { ApplyLighting(); return; }
            if (g.Mode == GameMode.Paused || g.Mode == GameMode.ActComplete) return;

            float dt = Time.deltaTime * fastForward;
            S.dayTime += dt;
            float hour = Hour;

            if (Factory.I) Factory.I.SetDockFill((float)(S.dayPages / System.Math.Max(1, S.order)));
            if (!S.orderMet && S.dayPages >= S.order)
            {
                S.orderMet = true;
                Sfx.Play("success", 0.7f);
                Announce("Pedido del día completo. Lo que salga a partir de ahora es propina.");
            }
            // Pedido urgente (mejora del árbol)
            if (S.Node("urgentes") > 0 && !urgentAnnounced && hour >= 13f)
            {
                urgentAnnounced = true;
                urgentTarget = S.dayPages + System.Math.Max(20, Factory.I.pagesPerSec * Progress.DaySeconds / 12f * 2.2f);
                g.Toast("Pedido urgente: " + Fmt.Num(urgentTarget - S.dayPages) + " páginas más antes de las 16:00");
            }
            if (urgentAnnounced && !S.urgentDone && urgentTarget > 0)
            {
                if (S.dayPages >= urgentTarget && hour <= 16f)
                {
                    S.urgentDone = true;
                    double b = Factory.I.income * 90;
                    g.EarnSilent(b);
                    g.Toast("Pedido urgente entregado: +" + Fmt.Money(b));
                }
                else if (hour > 16f) { urgentTarget = 0; g.Toast("El pedido urgente se fue sin entregar"); }
            }

            if (!sirenDone && hour >= End - 0.5f)
            {
                sirenDone = true;
                Sfx.Play("siren", 1f);
                Announce("Atención. La nave cierra en media hora. Fichen la salida en el vestuario.");
            }
            if (!lightsOutStarted && hour >= End - 0.25f)
            {
                lightsOutStarted = true;
                g.Toast("Se apagan las luces por sectores y las máquinas se paran");
            }
            UpdateTruck(hour, Time.deltaTime);
            ApplyLighting();

            // linterna si te quedas a oscuras sin fichar
            bool dark = Dark && !S.clockedIn;
            flashlight.enabled = dark;
            if (dark)
            {
                var cam = g.player.cam.transform;
                flashlight.transform.SetPositionAndRotation(cam.position + cam.right * 0.2f - cam.up * 0.15f, cam.rotation);
                flashlight.range = 14f + 10f * S.Node("linterna");
            }

            if (hour >= End)
            {
                if (S.clockedIn) EnterNight(true, false);
                else
                {
                    // a oscuras: un rato con la linterna y luego el vigilante te acompaña a la salida
                    darkTimer += Time.deltaTime;
                    var pp = g.player.transform.position;
                    if (g.mountain != null && pp.z > Mountain.FrontZ + g.mountain.Band - 6f)
                    {
                        whisperT -= Time.deltaTime;
                        if (whisperT <= 0) { whisperT = 2.5f; Sfx.PlayAt("paper", pp + new Vector3(Random.Range(-6, 6), 2, 8), 0.5f); }
                        if (!S.heardMountain && darkTimer > 4f) { S.heardMountain = true; g.Toast("…hojas que se mueven solas, en la oscuridad de la montaña"); }
                    }
                    if (darkTimer > (S.Node("linterna") > 0 ? 25f : 12f)) EnterNight(false, false);
                }
            }
        }

        void ApplyLighting()
        {
            float hour = S.nightPending ? End + 1 : Hour;
            // sol: atardece desde las 18:30 y entra naranja por los lucernarios
            float sunset = Mathf.InverseLerp(End - 1.5f, End, hour);
            if (sun)
            {
                // el sol recorre el cielo de la mañana a la tarde: las manchas de luz de los lucernarios se mueven por la nave
                float k = Mathf.InverseLerp(8f, End, Mathf.Min(hour, End));
                float elev = Mathf.Lerp(18f, 62f, Mathf.Sin(k * Mathf.PI * 0.85f + 0.05f)) * (1f - sunset * 0.7f);
                float az = Mathf.Lerp(-80f, 70f, k);
                sun.transform.rotation = Quaternion.Euler(elev, az, 0);
                float morning = Mathf.InverseLerp(8f, 9.5f, hour);
                sun.intensity = sunIntensity * Mathf.Lerp(1f, 0.05f, sunset) * Mathf.Lerp(0.85f, 1f, morning);
                sun.color = Color.Lerp(Color.Lerp(Build.Hex("#ffd2a0"), Build.Hex("#ffe7c4"), morning), Build.Hex("#ff8a4a"), sunset);
            }
            float off = Mathf.InverseLerp(End - 0.25f, End - 0.05f, hour); // 0 → 1 mientras se apaga la nave
            if (Events.Blackout) off = Mathf.Max(off, 0.9f);
            float cutZ = Mathf.Lerp(250f, -20f, off);
            foreach (var (l, inten, z) in lights)
                if (l) l.intensity = z > cutZ ? 0f : inten;
            RenderSettings.ambientSkyColor = Color.Lerp(ambSky, ambSky * 0.12f, off);
            RenderSettings.ambientEquatorColor = Color.Lerp(ambEq, ambEq * 0.12f, off);
        }

        public bool CanClockIn => started && !S.nightPending && Hour >= End - 0.5f && !S.clockedIn;

        public void ClockIn()
        {
            if (!CanClockIn) return;
            S.clockedIn = true;
            Sfx.Play("stamp", 1f);
            g.Toast("Fichado a las " + ClockText + ". Vas al ascensor de personal");
            fastForward = 25f;
        }

        void EnterNight(bool punctual, bool resuming)
        {
            fastForward = 1f;
            flashlight.enabled = false;
            if (!resuming)
            {
                double bonus = 0;
                var sb = new System.Text.StringBuilder();
                sb.Append("Páginas hoy: ").Append(Fmt.Num(S.dayPages)).Append(" de ").Append(Fmt.Num(S.order)).Append('\n');
                sb.Append("Ganado durante el día: ").Append(Fmt.Money(S.dayEarned)).Append('\n');
                double ratio = S.order > 0 ? S.dayPages / S.order : 0;
                if (ratio >= 1)
                {
                    double k = ratio >= 1.5 ? 2 : 1;
                    if (S.Node("furgoneta2") > 0) k *= 1.5;
                    double b = S.dayEarned * k;
                    if (S.Node("tren") > 0 && ratio > 1) b += S.dayEarned * (ratio - 1) * 3;
                    bonus += b;
                    S.ordersMet++;
                    sb.Append("Pedido cumplido").Append(ratio >= 1.5 ? " de sobra (×3)" : " (×2)").Append(": +").Append(Fmt.Money(b)).Append('\n');
                }
                else sb.Append("Pedido incompleto: la furgoneta se lleva lo que hay, sin bonus\n");
                if (punctual)
                {
                    double p = S.dayEarned * 0.1 * (S.InkLevel("jornadaLarga") > 0 ? 2 : 1);
                    bonus += p;
                    S.punctualDays++;
                    sb.Append("Puntualidad: +").Append(Fmt.Money(p)).Append('\n');
                }
                else sb.Append("No fichaste: el vigilante te acompañó a la salida. Sin bonus de puntualidad.\n");
                if (bonus > 0) g.EarnSilent(bonus);
                lastBonus = bonus;
                lastReport = sb.ToString();
                S.nightPending = true;
                van.gameObject.SetActive(false);
                SetShutter(0);
            }
            ApplyLighting();
            g.EnterNight();
        }

        public void StartNextDay()
        {
            NewDay(false);
            ApplyLighting();
            g.player.Teleport(new Vector3(-17.5f, 0.05f, -8.9f), 0f);
        }

    }

    /// <summary>El reloj de fichar del vestuario.</summary>
    public class TimeClock : MonoBehaviour, IInteractable
    {
        public string Prompt(Game g)
        {
            var w = Workday.I;
            if (w == null || !w.started) return "Reloj de fichar (la jornada empieza en el acto 2)";
            if (g.S.clockedIn) return "Ya has fichado";
            if (w.CanClockIn) return "[Clic] Fichar la salida (" + w.ClockText + ")";
            return "Reloj de fichar: se ficha a partir de las " + ((int)(w.End - 0.5f)) + ":30";
        }
        public void Interact(Game g) => Workday.I?.ClockIn();
    }
}
