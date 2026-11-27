using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Eventos de la nave (doc 6.4): la hoja dorada que vuela, el apagón, la inspección de Hacienda y la paloma.
    /// </summary>
    public static class Events
    {
        public static bool Blackout;

        public static void Random(Game g)
        {
            float r = UnityEngine.Random.value;
            if (r < 0.4f) FlyingSheet.Spawn(g);
            else if (r < 0.6f && g.S.act >= 3 && !Blackout) StartBlackout(g);
            else if (r < 0.75f && g.S.act >= 4) Inspection(g);
            else Pigeon.Spawn(g);
        }

        static void StartBlackout(Game g)
        {
            Blackout = true;
            Sfx.Play("jam", 1f, 0.5f);
            g.Toast("¡Apagón! Las máquinas se paran. Sube los plomos en el cuadro eléctrico, junto al vestuario");
            Breaker.I?.Arm();
        }

        public static void EndBlackout(Game g)
        {
            Blackout = false;
            g.S.blackoutsFixed++;
            Sfx.Play("success", 0.7f);
            g.Toast("Se hizo la luz. Las máquinas vuelven a arrancar");
        }

        static void Inspection(Game g)
        {
            g.S.inspections++;
            if (g.S.punctualDays >= 3)
                g.Toast("Inspección de Hacienda: tus fichajes están en regla. Se van sin multa");
            else
            {
                double fine = g.S.money * 0.05;
                g.S.money -= fine;
                g.Toast("Inspección de Hacienda: faltan fichajes. Multa de " + Fmt.Money(fine) + " (ficha 3 días puntual para evitarla)");
            }
            Sfx.Play("stamp", 0.8f);
        }
    }

    /// <summary>Cuadro eléctrico para el apagón.</summary>
    public class Breaker : MonoBehaviour, IInteractable
    {
        public static Breaker I;
        Transform lever;
        Renderer lamp;

        public static void Create(Transform world)
        {
            var go = Build.Empty("CuadroElectrico", world, new Vector3(-6.5f, 0, -9.7f), new Vector3(0, 180, 0));
            var b = go.AddComponent<Breaker>();
            I = b;
            var t = go.transform;
            Build.Box("Caja", t, new Vector3(0, 1.5f, 0), new Vector3(0.6f, 0.8f, 0.2f), Build.Mat(Build.Hex("#8d9094"), 0.5f, 0.6f), false);
            Build.Text("Rayo", t, new Vector3(0, 1.75f, -0.11f), "⚡ PELIGRO", 0.05f, Build.Hex("#e8b923"));
            b.lever = Build.Empty("Palanca", t, new Vector3(0, 1.45f, -0.12f)).transform;
            Build.Box("Mango", b.lever, new Vector3(0, 0.08f, 0), new Vector3(0.05f, 0.16f, 0.05f), Build.Mat(Build.Hex("#a3302a"), 0.4f), false);
            b.lamp = Build.Box("Piloto", t, new Vector3(0.2f, 1.3f, -0.11f), new Vector3(0.04f, 0.04f, 0.02f), Build.Mat(Build.Hex("#ff3b30"), 0.6f, 0, new Color(3, 0.3f, 0.2f)), false).GetComponent<Renderer>();
            b.lamp.enabled = false;
            var c = go.AddComponent<BoxCollider>(); c.center = new Vector3(0, 1.5f, 0); c.size = new Vector3(0.7f, 0.9f, 0.4f);
        }

        public void Arm() { lever.localRotation = Quaternion.Euler(0, 0, 180); }

        void Update() { if (lamp) lamp.enabled = Events.Blackout && Time.time % 0.8f < 0.4f; }

        public string Prompt(Game g) => Events.Blackout ? "[Clic] Subir los plomos" : "Cuadro eléctrico";
        public void Interact(Game g)
        {
            if (!Events.Blackout) return;
            lever.localRotation = Quaternion.identity;
            Sfx.Play("stamp", 1f, 0.7f);
            Events.EndBlackout(g);
        }
    }

    /// <summary>La hoja dorada que revolotea por la nave: atrápala para ×7 de producción durante 30 s.</summary>
    public class FlyingSheet : MonoBehaviour, IInteractable
    {
        Vector3 center;
        float life = 25f, t0;

        public static void Spawn(Game g)
        {
            var p = g.player.transform.position + g.player.transform.forward * 6f + Vector3.up * 1.4f;
            var go = Build.Box("HojaDorada", null, p, new Vector3(0.3f, 0.01f, 0.4f), Build.Mat(Build.Hex("#ffd34d"), 0.8f, 0.6f, new Color(1.6f, 1.2f, 0.3f)), true);
            go.GetComponent<BoxCollider>().size = new Vector3(3f, 60f, 3f);
            var f = go.AddComponent<FlyingSheet>();
            f.center = p; f.t0 = Random.value * 10;
            Sfx.Play("chime", 0.8f, 1.3f);
            g.Toast("¡Una hoja dorada vuela por la nave! Atrápala");
        }

        void Update()
        {
            life -= Time.deltaTime;
            float t = Time.time + t0;
            transform.position = center + new Vector3(Mathf.Sin(t * 0.7f) * 3f, Mathf.Sin(t * 1.3f) * 0.5f, Mathf.Cos(t * 0.5f) * 3f);
            transform.rotation = Quaternion.Euler(Mathf.Sin(t * 3) * 40, t * 90, Mathf.Cos(t * 2) * 30);
            if (life <= 0) Destroy(gameObject);
        }

        public string Prompt(Game g) => "[Clic] ¡Atrapar la hoja dorada!";
        public void Interact(Game g)
        {
            g.S.flyingCaught++;
            g.S.boostLeft = 30f;
            Sfx.Play("success", 1f);
            g.Toast("¡Hoja dorada! ×7 de producción durante 30 segundos");
            Destroy(gameObject);
        }
    }

    /// <summary>Una paloma se cuela en la nave y se posa cerca. Espantarla es un logro oculto.</summary>
    public class Pigeon : MonoBehaviour, IInteractable
    {
        float life = 40f, hop;
        bool flying;

        public static void Spawn(Game g)
        {
            var p = g.player.transform.position + g.player.transform.forward * 3f;
            p.y = 0;
            var go = Build.Empty("Paloma", null, p, new Vector3(0, Random.Range(0, 360), 0));
            var t = go.transform;
            var grey = Build.Mat(Build.Hex("#7d7f86"), 0.3f);
            Build.Box("Cuerpo", t, new Vector3(0, 0.12f, 0), new Vector3(0.12f, 0.12f, 0.24f), grey, false);
            Build.Box("Cabeza", t, new Vector3(0, 0.22f, 0.1f), new Vector3(0.07f, 0.07f, 0.08f), Build.Mat(Build.Hex("#4f6d6a"), 0.5f), false);
            Build.Box("Pico", t, new Vector3(0, 0.21f, 0.15f), new Vector3(0.02f, 0.02f, 0.03f), Build.Mat(Build.Hex("#e0b090"), 0.3f), false);
            Build.Box("Cola", t, new Vector3(0, 0.12f, -0.15f), new Vector3(0.1f, 0.02f, 0.1f), grey, false);
            var c = go.AddComponent<BoxCollider>(); c.center = new Vector3(0, 0.15f, 0); c.size = new Vector3(0.6f, 0.6f, 0.6f);
            go.AddComponent<Pigeon>();
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (flying)
            {
                transform.position += (transform.forward * 6f + Vector3.up * 4f) * Time.deltaTime;
                if (transform.position.y > 25) Destroy(gameObject);
                return;
            }
            hop -= Time.deltaTime;
            if (hop <= 0) { hop = Random.Range(0.6f, 2f); transform.Rotate(0, Random.Range(-60, 60), 0); transform.position += transform.forward * 0.15f; }
            if (life <= 0) flying = true;
        }

        public string Prompt(Game g) => flying ? null : "[Clic] Espantar a la paloma";
        public void Interact(Game g)
        {
            if (flying) return;
            flying = true;
            g.S.pigeons++;
            Sfx.Play("paper", 1f, 1.6f);
        }
    }

    public static partial class Halls
    {
        public static float Gravity(int hall) => hall == 3 ? 0.17f : 1f;

        static GameObject water;

        /// <summary>Ambiente de cada nave de la Biblioteca infinita.</summary>
        public static void Apply(Game g, Transform world)
        {
            Breaker.Create(world);
            int h = g.S.hall;
            if (h == 0) return;
            switch (h)
            {
                case 1: // barco: luz de mar y oleaje en la cámara
                    RenderSettings.fogColor = Build.Hex("#56636b");
                    break;
                case 2: // catedral: vidrieras de colores
                    string[] cols = { "#c0392b", "#2d5b8a", "#e8b923", "#4f7a4a" };
                    for (int i = 0; i < 12; i++)
                    {
                        float z = 10 + i * 18;
                        foreach (var x in new[] { -59.6f, 59.6f })
                        {
                            var c = Build.Hex(cols[i % 4]);
                            Build.Box("Vidriera", world, new Vector3(x, 14, z), new Vector3(0.1f, 9f, 3.5f), Build.Mat(c, 0.9f, 0, c * 2.5f), false);
                            Build.PointLight(world, new Vector3(x * 0.9f, 12, z), c, 6f, 18f);
                        }
                    }
                    RenderSettings.fogColor = Build.Hex("#4a4038");
                    break;
                case 3: // estación lunar: cielo negro y luz blanca dura
                    RenderSettings.fogDensity = 0.002f;
                    RenderSettings.fogColor = Build.Hex("#101218");
                    break;
                case 4: // sótano inundado: agua hasta los tobillos
                    water = Build.Box("Agua", world, new Vector3(0, 0.08f, 110), new Vector3(120, 0.02f, 240), Build.Mat(new Color(0.08f, 0.12f, 0.12f), 0.97f, 0.2f), false);
                    RenderSettings.fogColor = Build.Hex("#55656a");
                    RenderSettings.fogDensity = 0.006f;
                    for (int i = 0; i < 8; i++)
                        Build.PointLight(world, new Vector3(-30 + (i % 4) * 20, 7, 10 + (i / 4) * 30), Build.Hex("#bfe3ff"), 4f, 26f);
                    break;
            }
        }

        public static void CameraEffect(Game g, Transform cam)
        {
            if (g.S.hall == 1 && g.Mode == GameMode.Walking)
                cam.localRotation *= Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 0.6f) * 2.2f);
        }
    }
}
