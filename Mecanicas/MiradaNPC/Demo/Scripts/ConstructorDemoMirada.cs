using System;
using UnityEngine;

namespace Mecanicas.MiradaNPC.Demo
{
    /// <summary>
    /// Construye la escena de demostración con primitivas (no necesita modelos externos).
    /// Uso A: menú Herramientas → Mecánicas → Mirada NPC → Crear escena demo (crea y guarda la escena).
    /// Uso B: pon este componente en un objeto vacío de una escena vacía y dale a Play.
    /// </summary>
    [AddComponentMenu("Mecánicas/Mirada NPC/Demo/Constructor de Demo")]
    public class ConstructorDemoMirada : MonoBehaviour
    {
        [Tooltip("Construye la demo al darle a Play si todavía no existe.")]
        public bool construirAlEmpezar = true;

        void Start()
        {
            if (construirAlEmpezar && GameObject.Find(NombreRaiz) == null) Construir();
        }

        public const string NombreRaiz = "Demo Mirada NPC";

        static Material materialBase;
        static Func<Material, Material> guardarMaterial;

        /// <summary>Crea toda la demo. 'guardar' permite al editor guardar los materiales como assets (puede ser null).</summary>
        public static GameObject Construir(Func<Material, Material> guardar = null)
        {
            guardarMaterial = guardar;
            materialBase = null;
            var raiz = new GameObject(NombreRaiz);

#if UNITY_2023_1_OR_NEWER
            bool hayLuz = UnityEngine.Object.FindFirstObjectByType<Light>() != null;
#else
            bool hayLuz = UnityEngine.Object.FindObjectOfType<Light>() != null;
#endif
            if (!hayLuz)
            {
                var luz = new GameObject("Luz").AddComponent<Light>();
                luz.type = LightType.Directional;
                luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                luz.transform.SetParent(raiz.transform);
            }

            // Escenario
            var suelo = Primitiva(PrimitiveType.Plane, "Suelo", raiz.transform, Vector3.zero, Vector3.one * 4f, new Color(0.55f, 0.58f, 0.6f));
            suelo.transform.localScale = new Vector3(4f, 1f, 4f);
            Primitiva(PrimitiveType.Cube, "Muro (tapa la vista)", raiz.transform, new Vector3(9f, 1.25f, 1.5f), new Vector3(3f, 2.5f, 0.3f), new Color(0.45f, 0.35f, 0.3f));
            Primitiva(PrimitiveType.Cube, "Plataforma alta", raiz.transform, new Vector3(0f, 1f, 8f), new Vector3(4f, 2f, 3f), new Color(0.35f, 0.45f, 0.55f));
            var rampa = Primitiva(PrimitiveType.Cube, "Rampa", raiz.transform, new Vector3(0f, 1f, 4.6f), new Vector3(2f, 0.2f, 4.4f), new Color(0.35f, 0.45f, 0.55f));
            rampa.transform.rotation = Quaternion.Euler(-27f, 0f, 0f);

            // Jugador
            var jugador = new GameObject("Jugador");
            jugador.transform.SetParent(raiz.transform);
            jugador.transform.position = new Vector3(0f, 0.05f, -7f);
            var cc = jugador.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.radius = 0.3f;
            var camGO = new GameObject("Camara Jugador");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(jugador.transform, false);
            camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGO.AddComponent<Camera>().nearClipPlane = 0.05f;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<ObjetivoMirada>(); // los NPC miran a los ojos del jugador
            jugador.AddComponent<JugadorDemoMirada>().camara = camGO.transform;

            // NPC con distintas configuraciones
            CrearNPC(raiz.transform, "Normal", "NORMAL\nCuello humano ±70°\nse queda en el tope", new Vector3(-6f, 0f, 0f), 0f, new Color(0.9f, 0.6f, 0.4f), npc => { });

            CrearNPC(raiz.transform, "Buho", "BÚHO\nGira ±170°", new Vector3(-3f, 0f, 2f), 0f, new Color(0.6f, 0.45f, 0.3f), npc =>
            {
                npc.limitesCuello = new LimitesMirada(170f, 170f, 60f, 60f);
                npc.campoVision = 360f;
                npc.suavizadoCabeza = 0.12f;
                npc.velocidadMaximaCabeza = 600f;
            });

            CrearNPC(raiz.transform, "Timido", "TÍMIDO\n±45° y si te sales\nvuelve al frente", new Vector3(3f, 0f, 2f), 0f, new Color(0.5f, 0.75f, 0.5f), npc =>
            {
                npc.limitesCuello = new LimitesMirada(45f, 45f, 25f, 30f);
                npc.fueraDeLimite = ModoFueraDeLimite.VolverAlFrente;
                npc.margenLimite = 10f;
                npc.ojosSiguenFueraDeLimite = false;
            });

            CrearNPC(raiz.transform, "Vigilante", "VIGILANTE\nSolo si te ve\n(muro, 120°, 12 m)", new Vector3(9f, 0f, 4f), 180f, new Color(0.4f, 0.5f, 0.85f), npc =>
            {
                npc.requiereLineaDeVision = true;
                npc.campoVision = 120f;
                npc.distanciaMaxima = 12f;
                npc.tiempoReaccion = 0.4f;
                npc.tiempoMemoria = 1.5f;
            });

            CrearNPC(raiz.transform, "GiraCuerpo", "GIRA EL CUERPO\nsi la cabeza\npasa de 50°", new Vector3(6f, 0f, -2f), -90f, new Color(0.85f, 0.8f, 0.4f), npc =>
            {
                npc.girarCuerpo = true;
                npc.umbralGiroCuerpo = 50f;
                npc.velocidadGiroCuerpo = 120f;
                npc.campoVision = 360f;
            });

            CrearNPC(raiz.transform, "SoloOjos", "SOLO OJOS\ncabeza quieta", new Vector3(-9f, 0f, 3f), 30f, new Color(0.8f, 0.5f, 0.8f), npc =>
            {
                npc.cadena.Clear();
                npc.limitesOjos = new LimitesMirada(35f, 35f, 25f, 25f);
            });

            CrearNPC(raiz.transform, "RobotLento", "ROBOT LENTO\n45°/s, sin ojos", new Vector3(-6f, 0f, -4f), 90f, new Color(0.6f, 0.6f, 0.65f), npc =>
            {
                npc.ojos.Clear();
                npc.suavizadoCabeza = 0.8f;
                npc.velocidadMaximaCabeza = 45f;
                npc.limitesCuello = new LimitesMirada(90f, 90f, 30f, 30f);
            });

            return raiz;
        }

        /// <summary>Crea un muñeco con jerarquía Columna → Cuello → Cabeza → Ojos, como tendría un modelo real.</summary>
        public static SeguimientoMiradaNPC CrearNPC(Transform padre, string nombre, string cartel, Vector3 posicion, float rotacionY, Color color, Action<SeguimientoMiradaNPC> configurar)
        {
            var raiz = new GameObject("NPC " + nombre);
            raiz.transform.SetParent(padre);
            raiz.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, rotacionY, 0f));

            Primitiva(PrimitiveType.Capsule, "Cuerpo", raiz.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.6f, 0.75f, 0.45f), color, true);

            var columna = Vacio("Columna", raiz.transform, new Vector3(0f, 1.0f, 0f));
            var cuello = Vacio("Cuello", columna, new Vector3(0f, 0.5f, 0f));
            Primitiva(PrimitiveType.Cylinder, "Visual", cuello, new Vector3(0f, 0.05f, 0f), new Vector3(0.14f, 0.08f, 0.14f), color * 0.9f);
            var cabeza = Vacio("Cabeza", cuello, new Vector3(0f, 0.22f, 0f));
            Primitiva(PrimitiveType.Sphere, "Visual", cabeza, Vector3.zero, new Vector3(0.36f, 0.4f, 0.36f), color);
            Primitiva(PrimitiveType.Cube, "Nariz", cabeza, new Vector3(0f, -0.02f, 0.19f), new Vector3(0.05f, 0.07f, 0.06f), color * 0.8f);

            foreach (float lado in new[] { -1f, 1f })
            {
                var ojo = Vacio(lado < 0 ? "OjoIzq" : "OjoDer", cabeza, new Vector3(0.075f * lado, 0.05f, 0.15f));
                Primitiva(PrimitiveType.Sphere, "Globo", ojo, Vector3.zero, Vector3.one * 0.085f, Color.white);
                Primitiva(PrimitiveType.Sphere, "Pupila", ojo, new Vector3(0f, 0f, 0.035f), new Vector3(0.04f, 0.04f, 0.025f), new Color(0.05f, 0.05f, 0.05f));
            }

            // Cartel
            var cartelGO = new GameObject("Cartel");
            cartelGO.transform.SetParent(raiz.transform, false);
            cartelGO.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            var tm = cartelGO.AddComponent<TextMesh>();
            tm.text = cartel;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.06f;
            tm.fontSize = 48;
            tm.color = Color.black;
            var fuente = FuentePorDefecto();
            if (fuente != null)
            {
                tm.font = fuente;
                cartelGO.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            }
            cartelGO.AddComponent<CartelDemoMirada>();

            var npc = raiz.AddComponent<SeguimientoMiradaNPC>();
            npc.referenciaCuerpo = raiz.transform;
            npc.AutoConfigurar();
            configurar?.Invoke(npc);
            return npc;
        }

        static Font FuentePorDefecto()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        static Transform Vacio(string nombre, Transform padre, Vector3 posLocal)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            t.localPosition = posLocal;
            return t;
        }

        static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 posLocal, Vector3 escala, Color color, bool conColision = true)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posLocal;
            go.transform.localScale = escala;

            // Solo el cuerpo y el escenario colisionan; las piezas pequeñas de la cabeza no.
            bool esPiezaDeCabeza = padre.name == "Cabeza" || padre.name == "Cuello" || padre.name.StartsWith("Ojo");
            if (esPiezaDeCabeza || !conColision)
            {
                var col = go.GetComponent<Collider>();
                if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
            }

            var r = go.GetComponent<Renderer>();
            if (materialBase == null) materialBase = r.sharedMaterial;
            var m = new Material(materialBase) { name = "MiradaDemo_" + ColorUtility.ToHtmlStringRGB(color), color = color };
            if (guardarMaterial != null) m = guardarMaterial(m);
            r.sharedMaterial = m;
            return go;
        }
    }

    /// <summary>Hace que el cartel mire siempre a la cámara.</summary>
    public class CartelDemoMirada : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
        }
    }
}
