using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// La ventanilla al público (doc 4.12), desde el acto 4: entran clientes con encargos, se aceptan en el mostrador,
    /// se fabrican con parte de la producción y pagan al salir. Cada cliente atendido sube la reputación (×1 a ×5).
    /// </summary>
    public class Counter : MonoBehaviour, IInteractable
    {
        class Customer
        {
            public Transform t;
            public TextMesh card;
            public string what;
            public double pages, done, reward;
            public bool accepted, leaving;
            public float patience, walk;
            public Vector3 target;
        }

        static readonly string[] Orders =
        {
            "Carteles para la verbena", "Una tesis doctoral", "Invitaciones de boda", "El menú del bar", "Folletos de la autoescuela",
            "Partituras del coro", "Esquelas", "El boletín del barrio", "Recetas de la abuela", "Un fanzine", "Etiquetas de vino",
            "El programa de fiestas", "Apuntes de oposición", "Un libro de poemas", "Pasquines electorales", "El catálogo de la ferretería",
        };

        GameObject visual;
        readonly List<Customer> queue = new List<Customer>();
        float spawnT = 8f;
        Game g;
        static readonly Vector3 Door = new Vector3(53f, 0, -9f);

        public static Counter Create(Transform world)
        {
            var go = new GameObject("Ventanilla");
            go.transform.SetParent(world, false);
            go.transform.localPosition = new Vector3(41f, 0, -7.4f);
            var c = go.AddComponent<Counter>();
            c.BuildModel();
            return c;
        }

        void BuildModel()
        {
            visual = Build.Empty("Visual", transform, Vector3.zero);
            var t = visual.transform;
            var wood = Props.Map("MaderaOscura");
            if (Build.Model("Mostrador", t, Vector3.zero, null, Props.Map) == null)
            {
                Build.Box("Mostrador", t, new Vector3(0, 0.55f, 0), new Vector3(9f, 1.1f, 0.7f), wood, true);
                Build.Box("Encimera", t, new Vector3(0, 1.12f, 0), new Vector3(9.1f, 0.05f, 0.8f), Build.Mat(Build.Hex("#d8d2c4"), 0.6f), false);
                Build.Box("Cristal", t, new Vector3(0, 1.75f, 0), new Vector3(9f, 1.2f, 0.02f), Build.Mat(new Color(0.6f, 0.7f, 0.72f), 0.95f, 0.1f), false);
            }
            Build.Text("Rotulo", t, new Vector3(0, 2.7f, 0.2f), "VENTANILLA · ATENCIÓN AL PÚBLICO", 0.16f, Build.Hex("#e8b923"), TextAnchor.MiddleCenter, new Vector3(0, 180, 0));
            Build.Cyl("Timbre", t, new Vector3(-3.6f, 1.17f, 0.1f), 0.08f, 0.04f, Build.Chrome, false);
            // puerta lateral de los clientes
            Build.Box("PuertaClientes", transform.parent, Door + new Vector3(0, 1.1f, -0.9f), new Vector3(1.4f, 2.2f, 0.1f), Build.Mat(Build.Hex("#6b4a32"), 0.4f), false);
            var col = gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.9f, 0); col.size = new Vector3(9f, 1.8f, 1f);
        }

        public void Sync(Game game) { g = game; }

        void Update()
        {
            g = Game.I;
            if (g == null) return;
            bool on = g.S.act >= 4;
            if (visual.activeSelf != on) visual.SetActive(on);
            GetComponent<Collider>().enabled = on;
            if (!on) return;
            bool open = Factory.I != null && Factory.I.Running;
            float dt = Time.deltaTime;
            if (open)
            {
                spawnT -= dt * (g.S.Node("rotulo") > 0 ? 2 : 1);
                if (spawnT <= 0 && queue.Count < 4) { spawnT = Random.Range(25f, 55f); Spawn(); }
            }
            int clerks = g.S.Count("dependiente");
            bool autoServe = clerks > 0 || g.S.InkLevel("selloVentanilla") > 0;
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                var c = queue[i];
                if (!c.t) { queue.RemoveAt(i); continue; }
                // caminar
                var target = c.leaving ? Door : SpotFor(i);
                var pos = c.t.position;
                var d = target - pos; d.y = 0;
                if (d.magnitude > 0.05f)
                {
                    var step = d.normalized * Mathf.Min(d.magnitude, 1.3f * dt);
                    c.t.position = pos + step;
                    c.t.rotation = Quaternion.Slerp(c.t.rotation, Quaternion.LookRotation(d.normalized), dt * 6);
                }
                else if (!c.leaving) c.t.rotation = Quaternion.Slerp(c.t.rotation, Quaternion.Euler(0, 0, 0), dt * 4);
                if (c.card) c.card.transform.rotation = Quaternion.LookRotation(c.card.transform.position - g.player.cam.transform.position);
                if (c.leaving)
                {
                    if (d.magnitude < 0.2f) { Destroy(c.t.gameObject); queue.RemoveAt(i); }
                    continue;
                }
                if (!c.accepted)
                {
                    if (autoServe && i == 0 && d.magnitude < 0.3f) Accept(c);
                    c.patience -= dt;
                    if (c.patience <= 0) { c.leaving = true; c.card.text = "…me voy"; }
                }
                else if (open)
                {
                    double rate = Factory.I.pagesPerSec * 0.15 * (1 + 0.5 * clerks) + 1;
                    c.done += rate * dt;
                    c.card.text = c.what + "\n" + Mathf.FloorToInt((float)(100 * c.done / c.pages)) + " %";
                    if (c.done >= c.pages) Finish(c);
                }
                if (!c.accepted) c.card.text = c.what + "\n" + Fmt.Num(c.pages) + " páginas · " + Fmt.Money(c.reward);
            }
        }

        Vector3 SpotFor(int i) => transform.position + new Vector3(-3f + i * 1.6f, 0, -1.25f);

        void Spawn()
        {
            var t = Build.Empty("Cliente", transform.parent, Door).transform;
            Props.StandingPerson(t, Random.Range(0, 999));
            var card = Build.Text("Encargo", t, new Vector3(0, 2.15f, 0), "", 0.07f, Build.Hex("#ffe9a8"));
            double inc = Factory.I != null ? Factory.I.income : 1;
            double pps = Factory.I != null ? Factory.I.pagesPerSec : 1;
            var c = new Customer
            {
                t = t, card = card, what = Orders[Random.Range(0, Orders.Length)],
                pages = System.Math.Max(50, System.Math.Round(pps * Random.Range(20f, 60f))),
                reward = System.Math.Max(100, inc * Random.Range(60f, 140f)) * (g.S.Node("laserCal") > 0 ? 1.3 : 1),
                patience = 90f,
            };
            queue.Add(c);
            Sfx.PlayAt("bell", Door, 0.5f, 1.4f);
            if (queue.Count == 1) g.Toast("Hay un cliente en la ventanilla: " + c.what);
        }

        void Accept(Customer c)
        {
            c.accepted = true;
            Sfx.PlayAt("bell", transform.position, 0.7f, 1.8f);
        }

        void Finish(Customer c)
        {
            c.leaving = true;
            c.card.text = "¡Gracias!";
            g.Earn(c.reward, c.t.position + Vector3.up * 1.5f);
            g.S.customersServed += 1 + (g.S.Node("laserCal") > 0 ? 0.5 : 0);
            g.S.reputation = Progress.ReputationFor(g.S.customersServed);
            g.Toast("Encargo entregado: " + c.what + " · reputación " + g.S.reputation.ToString("0.0") + " ★");
        }

        public string Prompt(Game game)
        {
            if (game.S.act < 4) return null;
            var c = queue.Find(q => !q.accepted && !q.leaving);
            if (c != null) return "[Clic] Aceptar el encargo: " + c.what + " (" + Fmt.Num(c.pages) + " páginas → " + Fmt.Money(c.reward) + ")";
            int busy = queue.FindAll(q => q.accepted && !q.leaving).Count;
            return busy > 0 ? "Ventanilla: " + busy + " encargo(s) en marcha · reputación " + game.S.reputation.ToString("0.0") + " ★" : "Ventanilla: sin clientes ahora · reputación " + game.S.reputation.ToString("0.0") + " ★";
        }

        public void Interact(Game game)
        {
            var c = queue.Find(q => !q.accepted && !q.leaving);
            if (c != null) Accept(c);
        }

        public int Waiting => queue.FindAll(q => !q.accepted && !q.leaving).Count;
    }
}
