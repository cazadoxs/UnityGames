using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>Una unidad visible de un productor en la nave, con sus piezas animadas.</summary>
    public class Unit
    {
        public ProducerDef def;
        public Transform root;
        public readonly List<Transform> parts = new List<Transform>();   // piezas que se mueven (según el tipo)
        public readonly List<Renderer> leds = new List<Renderer>();
        public Vector3 outLocal = new Vector3(0, 0.8f, 0.5f);           // por dónde sale el papel
        public float phase, spawnT;
        public Vector3 basePos;
    }

    /// <summary>Etiqueta interactiva de una unidad: mirar y pulsar E abre su hoja de mejoras (doc 4.13).</summary>
    public class UnitTag : MonoBehaviour, IInteractable
    {
        public ProducerDef def;
        public string Prompt(Game g)
        {
            int n = g.S.Count(def.key);
            return def.name + " ×" + n + "   [Clic] Hoja de mejoras de este objeto";
        }
        public void Interact(Game g) => g.OpenShop(def.key);
    }

    /// <summary>
    /// Constructores del atrezo de los actos 2 a 8. Si el modelo de Blender está en Resources/Models se usa;
    /// si no, se monta con primitivas para que el juego funcione igual.
    /// </summary>
    public static class Props
    {
        static Material M(string hex, float smooth = 0.4f, float metal = 0f) => Build.Mat(Build.Hex(hex), smooth, metal);
        static Material Emit(string hex, float k) { var c = Build.Hex(hex); return Build.Mat(c, 0.6f, 0, c * k); }

        /// <summary>Asignación de materiales por nombre para todo el atrezo exportado desde Blender.</summary>
        public static Material Map(string n)
        {
            switch (n)
            {
                case "Metal": return M("#3a3b3e", 0.55f, 0.85f);
                case "MetalClaro": return M("#9aa0a6", 0.6f, 0.9f);
                case "Cromo": return Build.Chrome;
                case "Goma": return Build.Rubber;
                case "Plastico": return M("#d8cfb8", 0.45f);
                case "PlasticoGris": return M("#8d9094", 0.4f);
                case "PlasticoOscuro": return M("#2b2c2e", 0.4f);
                case "Teclas": return M("#4a3f36", 0.35f);
                case "TeclasClaras": return M("#d9d0bd", 0.35f);
                case "Cristal": return Build.Glass;
                case "Pantalla": return Emit("#3dff7a", 0.6f);
                case "PantallaAzul": return Emit("#7fb2ff", 0.8f);
                case "Madera": return Build.Pbr("fine_grained_wood", new Vector2(1, 1), Build.Oak, null, 0.8f);
                case "MaderaOscura": return Build.Pbr("dark_wood", new Vector2(1, 1), Build.Hex("#5b3d24"));
                case "Tela": return M("#3e4a3d", 0.1f);
                case "Pintura": return M("#2f5d50", 0.5f, 0.3f);
                case "PinturaAmarilla": return M("#e8b923", 0.45f, 0.2f);
                case "PinturaAzul": return M("#2d5b8a", 0.45f, 0.3f);
                case "PinturaRoja": return M("#a3302a", 0.45f, 0.2f);
                case "PinturaVerde": return M("#4f7a4a", 0.45f, 0.2f);
                case "PinturaBlanca": return M("#e4e2dc", 0.45f, 0.1f);
                case "Papel": return Build.Mat(Build.Paper, 0.15f);
                case "Carton": return M("#a7834f", 0.15f);
                case "Piel": return M("#c99a7a", 0.35f);
                case "Pelo": return M("#2b2018", 0.3f);
                case "Ropa": return M("#5a6b8c", 0.15f);
                case "Pantalon": return M("#2e3340", 0.15f);
                case "Zapatos": return M("#1d1a17", 0.5f);
                case "Led": return Emit("#ff3b30", 3f);
                case "LedVerde": return Emit("#39ff5a", 3f);
                case "LedAzul": return Emit("#4aa8ff", 4f);
                case "Luz": return Build.Mat(Color.white, 0.5f, 0, new Color(4, 3.6f, 3f));
                case "Tinta": return M("#151515", 0.7f);
                case "Cian": return M("#00a0d8", 0.6f);
                case "Magenta": return M("#d0207a", 0.6f);
                case "Oro": return M("#b8963e", 0.62f, 1f);
                case "Libros1": return M("#7a2e2a", 0.3f);
                case "Libros2": return M("#2f4a6b", 0.3f);
                case "Libros3": return M("#4a6b2f", 0.3f);
                case "Libros4": return M("#8a6a2f", 0.3f);
                case "Piedra": return Build.Pbr("hangar_concrete_floor", new Vector2(2, 2), Build.Hex("#a39b8c"));
                case "Ojos": return M("#1b1512", 0.85f);
                case "Labios": return M("#a8635a", 0.4f);
                case "Cuero": return M("#3b2616", 0.5f);
                case "Etiqueta": return M("#c0392b", 0.4f);
                case "PantallaOscura": return Build.Mat(Build.Hex("#0a140d"), 0.5f, 0, new Color(0.01f, 0.04f, 0.02f));
                case "Esfera": return M("#efe9da", 0.5f);
                case "Negro": return M("#1a1a1a", 0.3f);
                case "PinturaTaquilla": return M("#5e7a8a", 0.45f, 0.5f);
                case "Nucleo": return Build.Mat(Build.Hex("#9b7bff"), 0.9f, 0, Build.Hex("#9b7bff") * 5f);
            }
            return null;
        }

        static GameObject Model(string name, Transform parent, Vector3 pos, float yaw = 0, bool shadows = true) =>
            Build.Model(name, parent, pos, new Vector3(0, yaw, 0), Map, shadows);

        // ------------------------------------------------------------ personas
        static readonly string[] Shirts = { "#5a6b8c", "#8c5a5a", "#6b8c5a", "#c8c2b0", "#3f3f46", "#8c7a5a", "#5a8c88", "#7a5a8c" };

        /// <summary>Persona sentada mirando a +Z. Devuelve los antebrazos (para teclear) y la cabeza.</summary>
        public static Transform[] SeatedPerson(Transform parent, Vector3 pos, int seed)
        {
            var root = Build.Empty("Persona", parent, pos).transform;
            var shirt = M(Shirts[Mathf.Abs(seed) % Shirts.Length], 0.15f);
            var skin = M(new[] { "#e0b394", "#c99a7a", "#9c6b4e", "#6e4a35" }[Mathf.Abs(seed / 3) % 4], 0.35f);
            var hair = M(new[] { "#2b2018", "#5a3b22", "#b08a55", "#8a8a8a", "#1a1a1a" }[Mathf.Abs(seed / 7) % 5], 0.3f);
            System.Func<string, Material> map = n => n == "Ropa" ? shirt : n == "Piel" ? skin : n == "Pelo" ? hair : Map(n);
            Transform head, armL, armR;
            var human = Humans.Spawn("HSent" + (Mathf.Abs(seed) % Humans.SeatedCount), root, 1.204f);
            if (human != null)
            {
                head = Build.Empty("Cabeza", root, new Vector3(0, 1.2f, 0.09f)).transform;
                armL = Build.Empty("BrazoI", root, new Vector3(-0.19f, 1.0f, 0.04f)).transform;
                armR = Build.Empty("BrazoD", root, new Vector3(0.19f, 1.0f, 0.04f)).transform;
                root.gameObject.AddComponent<HumanRig>().Bind(human.transform, new[] { "lowerarm_l", "lowerarm_r", "head" }, new[] { armL, armR, head });
            }
            else if (Build.Model("Persona", root, Vector3.zero, null, map) != null)
            {
                head = Build.Empty("Cabeza", root, new Vector3(0, 1.22f, -0.02f)).transform;
                armL = Build.Empty("BrazoI", root, new Vector3(-0.2f, 1.02f, 0.02f)).transform;
                armR = Build.Empty("BrazoD", root, new Vector3(0.2f, 1.02f, 0.02f)).transform;
                Build.Model("PersonaBrazo", armL, Vector3.zero, null, map);
                var r = Build.Model("PersonaBrazo", armR, Vector3.zero, null, map);
                if (r != null) r.transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                Build.Box("Piernas", root, new Vector3(0, 0.5f, 0.18f), new Vector3(0.34f, 0.14f, 0.42f), M("#2e3340", 0.15f), false);
                Build.Box("Espinillas", root, new Vector3(0, 0.25f, 0.38f), new Vector3(0.3f, 0.5f, 0.12f), M("#2e3340", 0.15f), false);
                Build.Box("Torso", root, new Vector3(0, 0.85f, -0.02f), new Vector3(0.38f, 0.56f, 0.22f), shirt, false);
                head = Build.Empty("Cabeza", root, new Vector3(0, 1.22f, -0.02f)).transform;
                Build.Box("Cara", head, Vector3.zero, new Vector3(0.18f, 0.23f, 0.2f), skin, false);
                Build.Box("Pelo", head, new Vector3(0, 0.08f, -0.02f), new Vector3(0.19f, 0.09f, 0.21f), hair, false);
                armL = Build.Empty("BrazoI", root, new Vector3(-0.2f, 1.02f, 0.02f)).transform;
                armR = Build.Empty("BrazoD", root, new Vector3(0.2f, 1.02f, 0.02f)).transform;
                foreach (var a in new[] { armL, armR })
                {
                    Build.Box("Brazo", a, new Vector3(0, -0.14f, 0.02f), new Vector3(0.09f, 0.3f, 0.09f), shirt, false);
                    Build.Box("Antebrazo", a, new Vector3(0, -0.27f, 0.17f), new Vector3(0.08f, 0.07f, 0.3f), shirt, false);
                    Build.Box("Mano", a, new Vector3(0, -0.28f, 0.34f), new Vector3(0.08f, 0.04f, 0.1f), skin, false);
                }
            }
            return new[] { armL, armR, head };
        }

        static void Chair(Transform parent, Vector3 pos, float yaw)
        {
            if (Build.Model("Silla", parent, pos, new Vector3(0, yaw, 0), n => n == "Plastico" ? M("#2b2b2a", 0.35f) : Map(n)) != null) return;
            var c = Build.Empty("Silla", parent, pos, new Vector3(0, yaw, 0)).transform;
            Build.Box("Asiento", c, new Vector3(0, 0.45f, 0), new Vector3(0.46f, 0.08f, 0.44f), Map("Tela"), false);
            Build.Box("Respaldo", c, new Vector3(0, 0.8f, -0.23f), new Vector3(0.44f, 0.5f, 0.06f), Map("Tela"), false);
            Build.Cyl("Pie", c, new Vector3(0, 0.22f, 0), 0.06f, 0.44f, Map("Metal"));
        }

        static void Desk(Transform parent, Vector3 pos, float w = 1.3f, float d = 0.7f)
        {
            if (Model("Mesa", parent, pos) != null) return;
            var wood = Map("Madera");
            Build.Box("Tablero", parent, pos + new Vector3(0, 0.74f, 0), new Vector3(w, 0.04f, d), wood, false);
            foreach (var x in new[] { -w / 2 + 0.05f, w / 2 - 0.05f })
                foreach (var z in new[] { -d / 2 + 0.05f, d / 2 - 0.05f })
                    Build.Box("Pata", parent, pos + new Vector3(x, 0.36f, z), new Vector3(0.04f, 0.72f, 0.04f), Map("Metal"), false);
        }

        static void Typewriter(Transform parent, Vector3 pos)
        {
            if (Build.Model("MaquinaCuerpo", parent, pos, null, n => n.StartsWith("Cromo") ? Build.Chrome : n.StartsWith("Goma") ? Build.Rubber : n.StartsWith("Oro") ? Map("Oro") : M("#121214", 0.8f, 0.15f)) != null)
            {
                for (int r = 0; r < 4; r++)
                    Build.Box("Teclas", parent, pos + new Vector3(0.0f, 0.05f + r * 0.012f, -0.17f + r * 0.028f), new Vector3(0.3f, 0.012f, 0.02f), M("#1b1b1b", 0.6f), false);
                Build.Cyl("Rodillo", parent, pos + new Vector3(0, 0.17f, 0.08f), 0.06f, 0.4f, Build.Rubber, false, new Vector3(0, 0, 90));
                Build.Box("Hoja", parent, pos + new Vector3(0, 0.25f, 0.1f), new Vector3(0.21f, 0.16f, 0.002f), Map("Papel"), false, new Vector3(-12, 0, 0));
                return;
            }
            Build.Box("Maquina", parent, pos + new Vector3(0, 0.06f, 0), new Vector3(0.4f, 0.12f, 0.34f), M("#121214", 0.8f), false);
        }

        static void Pc(Transform parent, Vector3 pos, Unit u)
        {
            if (Model("PC286", parent, pos) == null)
            {
                Build.Box("Caja", parent, pos + new Vector3(0, 0.07f, 0.12f), new Vector3(0.42f, 0.14f, 0.38f), Map("Plastico"), false);
                Build.Box("Monitor", parent, pos + new Vector3(0, 0.36f, 0.14f), new Vector3(0.36f, 0.32f, 0.34f), Map("Plastico"), false);
                Build.Box("Teclado", parent, pos + new Vector3(0, 0.015f, -0.2f), new Vector3(0.44f, 0.03f, 0.16f), Map("Plastico"), false);
            }
            // Pantalla con texto verde que avanza
            var scr = Build.Text("Pantalla", parent, pos + new Vector3(-0.12f, 0.44f, -0.031f), "", 0.012f, Build.Hex("#56ff8a"), TextAnchor.UpperLeft);
            u.parts.Add(scr.transform);
        }

        // ------------------------------------------------------------ productores
        public static Unit Create(ProducerDef def, Transform parent, Vector3 pos, float yaw, int index)
        {
            var u = new Unit { def = def, phase = Random.value * 10f, spawnT = Random.value * 3f };
            u.root = Build.Empty(def.name, parent, pos, new Vector3(0, yaw, 0)).transform;
            u.basePos = pos;
            var t = u.root;
            switch (def.key)
            {
                case "mecanografo":
                {
                    Desk(t, Vector3.zero);
                    Typewriter(t, new Vector3(0, 0.76f, 0.02f));
                    Build.Box("Montón", t, new Vector3(-0.45f, 0.8f, 0.05f), new Vector3(0.21f, 0.08f, 0.3f), Map("Papel"), false);
                    Chair(t, new Vector3(0, 0, -0.65f), 0);
                    var p = SeatedPerson(t, new Vector3(0, 0, -0.6f), index * 7 + 3);
                    u.parts.AddRange(p);
                    u.outLocal = new Vector3(0.5f, 0.8f, 0.1f);
                    Collider(t, new Vector3(0, 0.7f, -0.2f), new Vector3(1.4f, 1.4f, 1.4f), def);
                    break;
                }
                case "equipo":
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var p0 = new Vector3((i % 2 == 0 ? -1 : 1) * 1.4f, 0, (i < 2 ? 1 : -1) * 1.1f);
                        var d = Build.Empty("Puesto", t, p0, new Vector3(0, i < 2 ? 180 : 0, 0)).transform;
                        Desk(d, Vector3.zero);
                        Typewriter(d, new Vector3(0, 0.76f, 0.02f));
                        Chair(d, new Vector3(0, 0, -0.65f), 0);
                        u.parts.AddRange(SeatedPerson(d, new Vector3(0, 0, -0.6f), index * 13 + i));
                    }
                    // Supervisor de pie con carpeta
                    var sup = Build.Empty("Supervisor", t, new Vector3(0, 0, 0)).transform;
                    StandingPerson(sup, index);
                    u.parts.Add(sup);
                    u.outLocal = new Vector3(2.4f, 0.8f, 0);
                    Collider(t, new Vector3(0, 0.7f, 0), new Vector3(4.6f, 1.4f, 3.6f), def);
                    break;
                }
                case "pc":
                {
                    Desk(t, Vector3.zero);
                    Chair(t, new Vector3(0, 0, -0.65f), 0);
                    u.parts.AddRange(SeatedPerson(t, new Vector3(0, 0, -0.6f), index * 5 + 11));
                    Pc(t, new Vector3(0, 0.76f, 0.05f), u);
                    u.outLocal = new Vector3(0.5f, 0.8f, 0.1f);
                    Collider(t, new Vector3(0, 0.7f, -0.2f), new Vector3(1.4f, 1.4f, 1.4f), def);
                    break;
                }
                case "matricial":
                {
                    Desk(t, Vector3.zero, 1.0f, 0.6f);
                    if (Model("Matricial", t, new Vector3(0, 0.76f, 0)) == null)
                        Build.Box("Cuerpo", t, new Vector3(0, 0.84f, 0), new Vector3(0.6f, 0.16f, 0.36f), Map("Plastico"), false);
                    var head = Build.Box("Cabezal", t, new Vector3(0, 0.9f, 0.02f), new Vector3(0.06f, 0.05f, 0.06f), Map("PlasticoOscuro"), false).transform;
                    u.parts.Add(head);
                    var paper = Build.Box("PapelContinuo", t, new Vector3(0, 1.05f, 0.12f), new Vector3(0.38f, 0.3f, 0.002f), Map("Papel"), false, new Vector3(-10, 0, 0)).transform;
                    u.parts.Add(paper);
                    Build.Box("Caja", t, new Vector3(0, 0.15f, 0.05f), new Vector3(0.42f, 0.3f, 0.3f), Map("Carton"), false);
                    u.outLocal = new Vector3(0, 0.9f, 0.4f);
                    Collider(t, new Vector3(0, 0.6f, 0), new Vector3(1.0f, 1.2f, 0.7f), def);
                    break;
                }
                case "inyeccion":
                {
                    Desk(t, Vector3.zero, 0.9f, 0.6f);
                    if (Model("Inyeccion", t, new Vector3(0, 0.76f, 0)) == null)
                        Build.Box("Cuerpo", t, new Vector3(0, 0.84f, 0), new Vector3(0.46f, 0.16f, 0.32f), Map("PlasticoGris"), false);
                    var head = Build.Box("Cabezal", t, new Vector3(0, 0.89f, 0.0f), new Vector3(0.05f, 0.04f, 0.05f), Map("PlasticoOscuro"), false).transform;
                    u.parts.Add(head);
                    u.outLocal = new Vector3(0, 0.82f, -0.3f);
                    Collider(t, new Vector3(0, 0.6f, 0), new Vector3(0.9f, 1.2f, 0.7f), def);
                    break;
                }
                case "fotocopiadora":
                {
                    if (Model("Fotocopiadora", t, Vector3.zero) == null)
                    {
                        Build.Box("Cuerpo", t, new Vector3(0, 0.5f, 0), new Vector3(1.0f, 1.0f, 0.7f), Map("PinturaBlanca"), false);
                        Build.Box("Cristal", t, new Vector3(0, 1.005f, 0), new Vector3(0.8f, 0.01f, 0.5f), Build.Glass, false);
                    }
                    var bar = Build.Box("Luz", t, new Vector3(0, 1.0f, 0), new Vector3(0.78f, 0.01f, 0.03f), Map("Luz"), false).transform;
                    u.parts.Add(bar);
                    u.outLocal = new Vector3(0.6f, 0.7f, 0);
                    Collider(t, new Vector3(0, 0.6f, 0), new Vector3(1.1f, 1.2f, 0.8f), def);
                    break;
                }
                case "duplicadora":
                {
                    if (Model("Duplicadora", t, Vector3.zero) == null)
                        Build.Box("Torre", t, new Vector3(0, 0.9f, 0), new Vector3(0.6f, 1.8f, 0.6f), Map("PlasticoGris"), false);
                    for (int i = 0; i < 8; i++)
                    {
                        var led = Build.Box("Led", t, new Vector3(-0.2f + (i % 4) * 0.13f, 0.5f + (i / 4) * 0.5f, -0.305f), new Vector3(0.02f, 0.01f, 0.005f), Map("LedVerde"), false);
                        u.leds.Add(led.GetComponent<Renderer>());
                    }
                    u.outLocal = new Vector3(0.4f, 0.8f, -0.2f);
                    Collider(t, new Vector3(0, 0.9f, 0), new Vector3(0.7f, 1.8f, 0.7f), def);
                    break;
                }
                case "dependiente":
                {
                    var p = StandingPerson(t, index * 3 + 1);
                    u.parts.Add(p);
                    Collider(t, new Vector3(0, 0.9f, 0), new Vector3(0.6f, 1.8f, 0.6f), def);
                    break;
                }
                case "plotter":
                {
                    if (Model("Plotter", t, Vector3.zero) == null)
                    {
                        Build.Box("Viga", t, new Vector3(0, 1.0f, 0), new Vector3(2.2f, 0.3f, 0.4f), Map("PlasticoOscuro"), false);
                        foreach (var x in new[] { -1f, 1f }) Build.Box("Pata", t, new Vector3(x, 0.45f, 0), new Vector3(0.08f, 0.9f, 0.5f), Map("Metal"), false);
                    }
                    u.parts.Add(Build.Box("Cabezal", t, new Vector3(0, 1.05f, -0.21f), new Vector3(0.12f, 0.1f, 0.06f), Map("PlasticoGris"), false).transform);
                    u.parts.Add(Build.Box("Poster", t, new Vector3(0, 0.75f, -0.25f), new Vector3(1.8f, 0.5f, 0.002f), M("#c9d7e8", 0.3f), false, new Vector3(-10, 0, 0)).transform);
                    u.outLocal = new Vector3(1.3f, 0.8f, 0);
                    Collider(t, new Vector3(0, 0.7f, 0), new Vector3(2.4f, 1.4f, 0.8f), def);
                    break;
                }
                case "fotolab":
                {
                    if (Model("Fotolab", t, Vector3.zero) == null)
                        Build.Box("Cuerpo", t, new Vector3(0, 0.6f, 0), new Vector3(2.4f, 1.2f, 0.9f), Map("PlasticoGris"), false);
                    for (int i = 0; i < 4; i++)
                        u.parts.Add(Build.Cyl("Rodillo", t, new Vector3(-0.9f + i * 0.25f, 1.05f, -0.46f), 0.06f, 0.7f, Map("Goma"), false, new Vector3(0, 0, 90)).transform);
                    u.outLocal = new Vector3(1.4f, 0.8f, 0);
                    Collider(t, new Vector3(0, 0.7f, 0), new Vector3(2.5f, 1.4f, 1.0f), def);
                    break;
                }
                case "offset":
                {
                    if (Model("Offset", t, Vector3.zero) == null)
                    {
                        Build.Box("Bastidor", t, new Vector3(0, 1.0f, 0), new Vector3(1.4f, 2.0f, 3.2f), Map("PinturaVerde"), false);
                    }
                    for (int i = 0; i < 4; i++)
                        u.parts.Add(Build.Cyl("Cilindro", t, new Vector3(0, 1.5f, -1.2f + i * 0.8f), 0.4f, 1.2f, Map(new[] { "Cian", "Magenta", "PinturaAmarilla", "Tinta" }[i]), false, new Vector3(0, 0, 90)).transform);
                    u.outLocal = new Vector3(0, 0.9f, 1.8f);
                    Collider(t, new Vector3(0, 1.1f, 0), new Vector3(1.6f, 2.2f, 3.4f), def);
                    break;
                }
                case "rotativa":
                {
                    if (Model("Rotativa", t, Vector3.zero) == null)
                        Build.Box("Torre", t, new Vector3(0, 2.5f, 0), new Vector3(2.4f, 5f, 8f), Map("PinturaAzul"), false);
                    for (int i = 0; i < 6; i++)
                        u.parts.Add(Build.Cyl("Cilindro", t, new Vector3(0, 1.4f + (i % 2) * 1.6f, -3f + (i / 2) * 3f), 0.7f, 2.0f, Map("MetalClaro"), false, new Vector3(0, 0, 90)).transform);
                    var bobina = Build.Cyl("Bobina", t, new Vector3(0, 0.75f, -5f), 1.5f, 1.8f, Map("Papel"), false, new Vector3(0, 0, 90)).transform;
                    u.parts.Add(bobina);
                    u.outLocal = new Vector3(0, 1.0f, 4.5f);
                    Collider(t, new Vector3(0, 2.5f, 0), new Vector3(2.6f, 5f, 11f), def);
                    break;
                }
                case "ia":
                {
                    if (Model("IA", t, Vector3.zero) == null)
                    {
                        Build.Box("Consola", t, new Vector3(0, 0.55f, 0), new Vector3(1.2f, 1.1f, 0.6f), Map("PlasticoOscuro"), false);
                        Build.Box("Pantalla", t, new Vector3(0, 1.4f, 0), new Vector3(1.0f, 0.6f, 0.05f), Map("PantallaAzul"), false);
                    }
                    var ring = Build.Cyl("Anillo", t, new Vector3(0, 2.1f, 0), 0.5f, 0.03f, Map("LedAzul"), false).transform;
                    u.parts.Add(ring);
                    u.outLocal = new Vector3(0.7f, 0.8f, 0);
                    Collider(t, new Vector3(0, 1f, 0), new Vector3(1.3f, 2.2f, 0.7f), def);
                    break;
                }
                case "redia":
                {
                    if (Model("Rack", t, Vector3.zero) == null)
                        Build.Box("Rack", t, new Vector3(0, 1.0f, 0), new Vector3(0.6f, 2.0f, 1.0f), Map("PlasticoOscuro"), false);
                    for (int i = 0; i < 24; i++)
                    {
                        var led = Build.Box("Led", t, new Vector3(-0.22f + (i % 4) * 0.08f, 0.2f + (i / 4) * 0.3f, -0.505f), new Vector3(0.015f, 0.015f, 0.004f), Map(i % 3 == 0 ? "LedAzul" : "LedVerde"), false);
                        u.leds.Add(led.GetComponent<Renderer>());
                    }
                    u.outLocal = new Vector3(0, 0.5f, 0.6f);
                    Collider(t, new Vector3(0, 1f, 0), new Vector3(0.6f, 2f, 1f), def);
                    break;
                }
                case "cuantica":
                {
                    if (Model("Cuantica", t, Vector3.zero) == null)
                        Build.Cyl("Base", t, new Vector3(0, 0.4f, 0), 4f, 0.8f, Map("MetalClaro"));
                    var core = Build.Empty("Nucleo", t, new Vector3(0, 3.2f, 0)).transform;
                    Build.Box("Cubo", core, Vector3.zero, Vector3.one * 0.8f, Map("Nucleo"), false, new Vector3(45, 45, 0));
                    for (int i = 0; i < 3; i++)
                        Build.Cyl("Anillo", core, Vector3.zero, 2.2f + i * 0.5f, 0.05f, Map("Cromo"), false, new Vector3(i * 60, 0, i * 30));
                    u.parts.Add(core);
                    Build.PointLight(t, new Vector3(0, 3.2f, 0), Build.Hex("#9b7bff"), 8f, 12f);
                    u.outLocal = new Vector3(2.2f, 0.8f, 0);
                    Collider(t, new Vector3(0, 2.5f, 0), new Vector3(4f, 5f, 4f), def);
                    break;
                }
                case "fractal":
                {
                    var core = Build.Empty("Fractal", t, new Vector3(0, 3f, 0)).transform;
                    float s = 3f;
                    Transform prev = core;
                    for (int i = 0; i < 6; i++)
                    {
                        var c = Build.Empty("Nivel", prev, Vector3.zero).transform;
                        Build.Box("Marco", c, Vector3.zero, Vector3.one * s, Map(i % 2 == 0 ? "Oro" : "Cromo"), false);
                        Build.Box("Hueco", c, Vector3.zero, new Vector3(s * 1.01f, s * 0.8f, s * 0.8f), Map("Tinta"), false);
                        u.parts.Add(c);
                        prev = c; s *= 0.55f;
                    }
                    Build.Cyl("Pedestal", t, new Vector3(0, 0.6f, 0), 3f, 1.2f, Map("MaderaOscura"), true);
                    u.outLocal = new Vector3(1.8f, 0.8f, 0);
                    Collider(t, new Vector3(0, 2.5f, 0), new Vector3(3.5f, 5f, 3.5f), def);
                    break;
                }
                case "babel":
                {
                    var tower = Build.Empty("Torre", t, Vector3.zero).transform;
                    if (Model("Babel", tower, Vector3.zero) == null)
                    for (int lvl = 0; lvl < 8; lvl++)
                        for (int k = 0; k < 6; k++)
                        {
                            float a = k * 60 + lvl * 15;
                            var sh = Build.Empty("Estante", tower, Quaternion.Euler(0, a, 0) * new Vector3(0, lvl * 2.6f, 3.6f), new Vector3(0, a, 0)).transform;
                            Build.Box("Mueble", sh, new Vector3(0, 1.2f, 0), new Vector3(3.6f, 2.4f, 0.5f), Map("MaderaOscura"), false);
                            Build.Box("Libros", sh, new Vector3(0, 1.2f, -0.1f), new Vector3(3.4f, 2.2f, 0.32f), M(new[] { "#7a2e2a", "#2f4a6b", "#4a6b2f", "#6b5a2f" }[(lvl + k) % 4], 0.3f), false);
                        }
                    u.parts.Add(tower);
                    Build.PointLight(t, new Vector3(0, 8, 0), Build.Hex("#ffd9a0"), 20f, 25f);
                    u.outLocal = new Vector3(4.5f, 0.8f, 0);
                    Collider(t, new Vector3(0, 10.5f, 0), new Vector3(9f, 21f, 9f), def);
                    break;
                }
                // ---- recogedores
                case "brazo":
                {
                    if (Model("BrazoBase", t, Vector3.zero) == null)
                        Build.Cyl("Base", t, new Vector3(0, 0.2f, 0), 0.6f, 0.4f, Map("PinturaAmarilla"), true);
                    var j1 = Build.Empty("Hombro", t, new Vector3(0, 0.45f, 0)).transform;
                    if (Model("BrazoSegmento", j1, Vector3.zero) == null)
                        Build.Box("Segmento1", j1, new Vector3(0, 0.6f, 0), new Vector3(0.18f, 1.2f, 0.18f), Map("PinturaAmarilla"), false);
                    var j2 = Build.Empty("Codo", j1, new Vector3(0, 1.2f, 0)).transform;
                    if (Model("BrazoSegmento2", j2, Vector3.zero) == null)
                        Build.Box("Segmento2", j2, new Vector3(0, 0, 0.6f), new Vector3(0.14f, 0.14f, 1.2f), Map("PinturaAmarilla"), false);
                    var head = Build.Empty("Ventosa", j2, new Vector3(0, 0, 1.2f)).transform;
                    Build.Cyl("Ventosa", head, new Vector3(0, -0.08f, 0), 0.26f, 0.06f, Map("Goma"));
                    u.parts.Add(j1); u.parts.Add(j2); u.parts.Add(head);
                    Collider(t, new Vector3(0, 0.6f, 0), new Vector3(0.8f, 1.2f, 0.8f), def);
                    break;
                }
                case "grua":
                {
                    var steel = Map("PinturaAmarilla");
                    Build.Box("Pilar", t, new Vector3(-2.5f, 4f, 0), new Vector3(0.3f, 8f, 0.3f), steel, true);
                    Build.Box("Pilar", t, new Vector3(2.5f, 4f, 0), new Vector3(0.3f, 8f, 0.3f), steel, true);
                    Build.Box("Viga", t, new Vector3(0, 8f, 0), new Vector3(5.3f, 0.4f, 0.4f), steel, false);
                    var trolley = Build.Empty("Carro", t, new Vector3(0, 7.7f, 0)).transform;
                    Build.Box("Carro", trolley, Vector3.zero, new Vector3(0.6f, 0.3f, 0.6f), Map("Metal"), false);
                    var cable = Build.Box("Cable", trolley, new Vector3(0, -1.5f, 0), new Vector3(0.03f, 3f, 0.03f), Map("Metal"), false).transform;
                    var claw = Build.Empty("Garra", trolley, new Vector3(0, -3f, 0)).transform;
                    if (Model("Garra", claw, Vector3.zero) == null)
                        for (int i = 0; i < 4; i++)
                            Build.Box("Uña", claw, Quaternion.Euler(0, i * 90, 0) * new Vector3(0, -0.3f, 0.25f), new Vector3(0.08f, 0.6f, 0.08f), Map("Metal"), false, new Vector3(i % 2 == 0 ? 20 : 0, i * 90, i % 2 == 1 ? 20 : 0));
                    u.parts.Add(trolley); u.parts.Add(cable); u.parts.Add(claw);
                    break;
                }
                case "excavadora":
                {
                    if (Model("Excavadora", t, Vector3.zero) == null)
                    {
                        Build.Box("Orugas", t, new Vector3(0, 0.35f, 0), new Vector3(2.2f, 0.7f, 2.8f), Map("Goma"), true);
                        Build.Box("Cabina", t, new Vector3(0, 1.4f, -0.3f), new Vector3(1.9f, 1.4f, 1.9f), Map("PinturaAmarilla"), false);
                    }
                    var boom = Build.Empty("Pluma", t, new Vector3(0.4f, 1.6f, 0.6f)).transform;
                    Build.Box("Pluma", boom, new Vector3(0, 0.6f, 1.2f), new Vector3(0.3f, 0.3f, 2.6f), Map("PinturaAmarilla"), false, new Vector3(-25, 0, 0));
                    var bucket = Build.Empty("Cazo", boom, new Vector3(0, 1.2f, 2.4f)).transform;
                    Build.Box("Cazo", bucket, new Vector3(0, -0.4f, 0.2f), new Vector3(0.9f, 0.6f, 0.6f), Map("Metal"), false);
                    u.parts.Add(boom); u.parts.Add(bucket);
                    break;
                }
                case "aspirador":
                {
                    var pipeMat = Build.Pbr("metal_plate_02", new Vector2(1, 4), Build.Hex("#a9adb1"));
                    Build.Cyl("Motor", t, new Vector3(0, 1.2f, -2f), 2.2f, 2.4f, Map("PinturaAzul"), true);
                    var pipe = Build.Empty("Tubo", t, new Vector3(0, 2.0f, -1f)).transform;
                    Build.Cyl("Tubo", pipe, new Vector3(0, 0, 2.5f), 1.0f, 5f, pipeMat, false, new Vector3(90, 0, 0));
                    Build.Cyl("Boca", pipe, new Vector3(0, 0, 5.1f), 1.5f, 0.4f, Map("Goma"), false, new Vector3(90, 0, 0));
                    u.parts.Add(pipe);
                    break;
                }
                case "tuneladora":
                {
                    Build.Cyl("Cuerpo", t, new Vector3(0, 2.2f, -3f), 4.2f, 7f, Map("PinturaAmarilla"), true, new Vector3(90, 0, 0));
                    var head = Build.Empty("Cabeza", t, new Vector3(0, 2.2f, 0.6f)).transform;
                    Build.Cyl("Disco", head, Vector3.zero, 4.6f, 0.4f, Map("MetalClaro"), false, new Vector3(90, 0, 0));
                    for (int i = 0; i < 8; i++)
                        Build.Box("Diente", head, Quaternion.Euler(0, 0, i * 45) * new Vector3(0, 1.6f, 0.25f), new Vector3(0.25f, 0.9f, 0.25f), Map("Metal"), false, new Vector3(0, 0, i * 45));
                    u.parts.Add(head);
                    break;
                }
                case "macrobot":
                case "contratos":
                    break;
                default:
                    Build.Box("Caja", t, new Vector3(0, 0.5f, 0), Vector3.one, Map("PlasticoGris"), true);
                    break;
            }
            return u;
        }

        public static Transform StandingPerson(Transform parent, int seed)
        {
            var root = Build.Empty("PersonaDePie", parent, Vector3.zero).transform;
            var shirt = M(Shirts[Mathf.Abs(seed) % Shirts.Length], 0.15f);
            System.Func<string, Material> map = n => n == "Ropa" ? shirt : Map(n);
            var human = Humans.Spawn("HPie" + (Mathf.Abs(seed) % Humans.StandingCount), root, 1.58f);
            if (human != null)
            {
                var hand = Humans.Find(human.transform, "hand_r");
                var hp = hand != null ? root.InverseTransformPoint(hand.position) : new Vector3(0.18f, 1.05f, 0.18f);
                Build.Box("Carpeta", root, hp + new Vector3(-0.04f, 0.1f, 0.06f), new Vector3(0.24f, 0.32f, 0.02f), M("#7b4a2b", 0.3f), false, new Vector3(-35, 0, 0));
                return root;
            }
            if (Build.Model("PersonaDePie", root, Vector3.zero, null, map) == null)
            {
                Build.Box("Piernas", root, new Vector3(0, 0.45f, 0), new Vector3(0.32f, 0.9f, 0.2f), Map("Pantalon"), false);
                Build.Box("Torso", root, new Vector3(0, 1.2f, 0), new Vector3(0.4f, 0.6f, 0.22f), shirt, false);
                Build.Box("Cabeza", root, new Vector3(0, 1.62f, 0), new Vector3(0.19f, 0.24f, 0.21f), Map("Piel"), false);
                Build.Box("Pelo", root, new Vector3(0, 1.72f, -0.02f), new Vector3(0.2f, 0.08f, 0.22f), Map("Pelo"), false);
                Build.Box("Brazos", root, new Vector3(0, 1.15f, 0), new Vector3(0.56f, 0.5f, 0.12f), shirt, false);
            }
            Build.Box("Carpeta", root, new Vector3(0.18f, 1.05f, 0.18f), new Vector3(0.24f, 0.32f, 0.02f), M("#7b4a2b", 0.3f), false, new Vector3(-40, 0, 0));
            return root;
        }

        static void Collider(Transform t, Vector3 center, Vector3 size, ProducerDef def)
        {
            var c = t.gameObject.AddComponent<BoxCollider>();
            c.center = center; c.size = size;
            t.gameObject.AddComponent<UnitTag>().def = def;
        }
    }
}
