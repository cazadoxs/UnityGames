using UnityEngine;

namespace Mecanicas.Torretas.Demo
{
    /// <summary>
    /// Monta una escena de prueba con las tres torretas (fija, un eje y todos los ángulos), un
    /// muro para probar la línea de visión y varias dianas en movimiento. Pon este componente en
    /// un objeto vacío de una escena vacía y dale a Play, o usa el menú
    /// Herramientas > Mecánicas > Torreta > Crear escena de demostración.
    /// </summary>
    [AddComponentMenu("Mecanicas/Torretas/Demo/Demo Torretas")]
    public class DemoTorretas : MonoBehaviour
    {
        [Tooltip("Construye la demo al darle a Play (desmárcalo si ya la construiste desde el editor).")]
        public bool construirAlIniciar = true;
        [Tooltip("Crea cámara y luz si la escena no tiene.")]
        public bool crearCamaraYLuz = true;

        void Start()
        {
            if (construirAlIniciar) Construir();
        }

        /// <summary>Crea todos los objetos de la demo como hijos de este objeto.</summary>
        public void Construir()
        {
            Transform raiz = transform;

            // Suelo
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.SetParent(raiz, false);
            suelo.transform.localScale = new Vector3(6f, 1f, 6f);
            suelo.GetComponent<Renderer>().sharedMaterial = FabricaTorretas.CrearMaterial(new Color(0.32f, 0.36f, 0.3f));

            // Torretas
            var fija = FabricaTorretas.Crear(ModoApuntado.Fija, new Vector3(-14f, 0f, 0f), Quaternion.identity, raiz);
            var unEje = FabricaTorretas.Crear(ModoApuntado.UnEje, new Vector3(0f, 0f, 0f), Quaternion.identity, raiz);
            var todos = FabricaTorretas.Crear(ModoApuntado.TodosLosAngulos, new Vector3(14f, 0f, 0f), Quaternion.identity, raiz);
            Etiqueta("FIJA\n(dispara recto si algo cruza su cono)", fija.transform, raiz);
            Etiqueta("UN EJE\n(gira a izquierda y derecha)", unEje.transform, raiz);
            Etiqueta("TODOS LOS ÁNGULOS\n(gira y eleva, compensa gravedad)", todos.transform, raiz);

            // Muro para comprobar la línea de visión
            var muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            muro.name = "Muro (tapa la vista)";
            muro.transform.SetParent(raiz, false);
            muro.transform.localPosition = new Vector3(5f, 1.5f, 7f);
            muro.transform.localScale = new Vector3(4f, 3f, 0.4f);
            muro.GetComponent<Renderer>().sharedMaterial = FabricaTorretas.CrearMaterial(new Color(0.55f, 0.45f, 0.35f));

            // Dianas
            Diana("Diana carril", PrimitiveType.Capsule, new Color(0.85f, 0.2f, 0.2f), raiz, d =>
            {
                d.recorrido = ObjetivoMovil.Recorrido.IdaYVuelta;
                d.puntos = new[] { new Vector3(-22f, 1f, 12f), new Vector3(22f, 1f, 12f) };
                d.velocidad = 5f;
            });
            Diana("Diana en círculo", PrimitiveType.Capsule, new Color(0.9f, 0.5f, 0.1f), raiz, d =>
            {
                d.recorrido = ObjetivoMovil.Recorrido.Circulo;
                d.centroCirculo = new Vector3(0f, 1f, 2f);
                d.radioCirculo = 9f;
                d.velocidad = 4f;
            });
            Diana("Diana voladora", PrimitiveType.Sphere, new Color(0.6f, 0.2f, 0.9f), raiz, d =>
            {
                d.recorrido = ObjetivoMovil.Recorrido.Circulo;
                d.centroCirculo = new Vector3(14f, 6f, 2f);
                d.radioCirculo = 8f;
                d.velocidad = 6f;
                d.amplitudVertical = 2f;
            }, 1.2f);

            if (!crearCamaraYLuz) return;
            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera");
                cam.tag = "MainCamera";
                cam.transform.SetParent(raiz, false);
                cam.AddComponent<Camera>();
                cam.AddComponent<AudioListener>();
                cam.transform.position = new Vector3(0f, 20f, -24f);
                cam.transform.LookAt(new Vector3(0f, 0f, 5f));
            }
            if (SoloLucesDeTorreta())
            {
                var sol = new GameObject("Luz direccional");
                sol.transform.SetParent(raiz, false);
                var l = sol.AddComponent<Light>();
                l.type = LightType.Directional;
                l.shadows = LightShadows.Soft;
                sol.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        static bool SoloLucesDeTorreta()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) return false;
            return true;
        }

        static void Diana(string nombre, PrimitiveType forma, Color color, Transform padre,
            System.Action<ObjetivoMovil> configurar, float escala = 1f)
        {
            var go = GameObject.CreatePrimitive(forma);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localScale = Vector3.one * escala;
            go.GetComponent<Renderer>().sharedMaterial = FabricaTorretas.CrearMaterial(color);
            var salud = go.AddComponent<Salud>();
            salud.vidaMaxima = 120f;
            var mov = go.AddComponent<ObjetivoMovil>();
            configurar(mov);
            if (mov.recorrido != ObjetivoMovil.Recorrido.Circulo && mov.puntos.Length > 0) go.transform.position = mov.puntos[0];
            else go.transform.position = mov.centroCirculo + Vector3.right * mov.radioCirculo;
        }

        static void Etiqueta(string texto, Transform torreta, Transform padre)
        {
            var go = new GameObject("Etiqueta");
            go.transform.SetParent(padre, false);
            go.transform.position = torreta.position + new Vector3(0f, 3f, -1.5f);
            go.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = texto;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.12f;
            tm.fontSize = 48;
            tm.color = Color.white;
            var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fuente != null)
            {
                tm.font = fuente;
                go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            }
        }
    }
}
