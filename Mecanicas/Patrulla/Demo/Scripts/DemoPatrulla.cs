using UnityEngine;
using UnityEngine.Rendering;

namespace Mecanicas.Patrulla.Demo
{
    /// <summary>
    /// Construye la escena de demostración al darle a Play (en una escena vacía basta con este componente):
    ///  1. Circuito de tráfico con esquinas suaves, 6 coches de un generador, cola automática y un semáforo.
    ///  2. Paso de peatones de ida y vuelta controlado por el mismo semáforo.
    ///  3. Guardia que patrulla con esquinas en pico, se para en los extremos y mira hacia fuera.
    ///  4. Comparación: la misma ruta en pico y en suave, una al lado de la otra.
    ///  5. Dron en una ruta 3D que mezcla esquinas suaves y en pico.
    /// Un panel en pantalla permite cambiar las esquinas y el radio en directo.
    /// </summary>
    [AddComponentMenu("Mecanicas/Patrulla/Demo/Demo Patrulla")]
    public class DemoPatrulla : MonoBehaviour
    {
        RutaDePuntos circuito, comparaPico, comparaSuave;
        SemaforoDeRuta semaforo;
        GeneradorDeTrafico generador;

        void Awake()
        {
            CrearEntorno();
            CrearCircuitoYSemaforo();
            CrearGuardia();
            CrearComparacion();
            CrearDron();
        }

        // ---------------------------------------------------------------- Entorno

        void CrearEntorno()
        {
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.localScale = new Vector3(10f, 1f, 10f);
            Pintar(suelo, new Color(0.35f, 0.42f, 0.33f));

            if (FindAnyObjectByType<Light>() == null)
            {
                var luz = new GameObject("Sol").AddComponent<Light>();
                luz.type = LightType.Directional;
                luz.intensity = 1.1f;
                luz.shadows = LightShadows.Soft;
                luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 48f, -58f);
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 500f;
        }

        // ---------------------------------------------------------------- 1 + 2: tráfico

        void CrearCircuitoYSemaforo()
        {
            // Carretera rectangular con una chicane arriba. Sentido: el orden de los puntos.
            circuito = CrearRuta("Circuito de tráfico", true, TipoEsquina.Suave, 6f, new Color(0.2f, 0.8f, 1f),
                new Vector3(-20, 0, -12), new Vector3(-3, 0, -12), new Vector3(20, 0, -12),
                new Vector3(20, 0, 12), new Vector3(6, 0, 12), new Vector3(0, 0, 6),
                new Vector3(-6, 0, 12), new Vector3(-20, 0, 12));

            // Asfalto bajo la ruta (solo decorado).
            var muestras = circuito.Muestras;
            for (int i = 0; i + 1 < muestras.Count; i++)
            {
                Vector3 a = muestras[i], b = muestras[i + 1];
                var tramo = Caja("Asfalto", (a + b) * 0.5f + Vector3.up * 0.01f, new Vector3(4f, 0.02f, Vector3.Distance(a, b) + 0.6f), new Color(0.15f, 0.15f, 0.17f));
                tramo.transform.rotation = Quaternion.LookRotation(b - a);
                Destroy(tramo.GetComponent<Collider>());
            }

            // El punto 1 es la línea de stop del semáforo.
            var stop = circuito.ObtenerPunto(1).gameObject.AddComponent<PuntoDeRuta>();

            // Coche plantilla (desactivado). El generador hace copias.
            var coche = new GameObject("Coche (plantilla)");
            coche.SetActive(false);
            var carroceria = Caja("Carrocería", Vector3.up * 0.55f, new Vector3(1.7f, 0.8f, 3.6f), Color.white);
            carroceria.transform.SetParent(coche.transform, false);
            var cabina = Caja("Cabina", new Vector3(0f, 1.2f, -0.3f), new Vector3(1.5f, 0.6f, 1.8f), new Color(0.2f, 0.25f, 0.3f));
            cabina.transform.SetParent(coche.transform, false);
            var s = coche.AddComponent<SeguidorDeRuta>();
            s.velocidad = 8f;
            s.aceleracion = 4f;
            s.frenada = 9f;
            s.separacionMinima = 6.5f;
            s.detectarObstaculos = true;   // frena si el peatón está en la calzada
            s.alturaDeteccion = 0.9f;
            s.distanciaDeteccion = 7f;
            s.inicioDeteccion = 1.9f;
            s.radioDeteccion = 0.7f;
            s.distanciaSeguridad = 1.2f;
            s.distanciaParada = 2.2f;
            s.velocidadGiro = 240f;
            s.adelantoMirada = 1.8f;
            s.factorVelocidadEnCurva = 0.45f;
            coche.AddComponent<ColorAlAzar>();

            generador = new GameObject("Generador de tráfico").AddComponent<GeneradorDeTrafico>();
            generador.ruta = circuito;
            generador.prefabs.Add(coche);
            generador.maximoAlaVez = 6;
            generador.repartirAlEmpezar = 6;
            generador.rangoVelocidad = new Vector2(6f, 9f);

            // Paso de peatones en x = 0 del tramo de abajo: acera sur → bordillo → bordillo → acera norte.
            var paso = CrearRuta("Paso de peatones", false, TipoEsquina.Pico, 0f, new Color(1f, 0.9f, 0.3f),
                new Vector3(1.5f, 0, -19), new Vector3(1.5f, 0, -15), new Vector3(1.5f, 0, -9), new Vector3(1.5f, 0, -5));
            for (int i = 0; i < 4; i++) Caja("Cebra", new Vector3(0.3f + i * 0.8f, 0.02f, -12f), new Vector3(0.5f, 0.02f, 4f), Color.white).GetComponent<Collider>().enabled = false;

            var extremoSur = paso.ObtenerPunto(0).gameObject.AddComponent<PuntoDeRuta>();
            extremoSur.espera = 1.5f; extremoSur.esperaAleatoria = 1.5f;
            var bordilloSur = paso.ObtenerPunto(1).gameObject.AddComponent<PuntoDeRuta>();
            bordilloSur.sentidoBloqueo = PuntoDeRuta.SentidoBloqueo.SoloIda;     // bloquea a quien va hacia el norte
            var bordilloNorte = paso.ObtenerPunto(2).gameObject.AddComponent<PuntoDeRuta>();
            bordilloNorte.sentidoBloqueo = PuntoDeRuta.SentidoBloqueo.SoloVuelta; // bloquea a quien vuelve al sur
            var extremoNorte = paso.ObtenerPunto(3).gameObject.AddComponent<PuntoDeRuta>();
            extremoNorte.espera = 1.5f; extremoNorte.esperaAleatoria = 1.5f;

            var peaton = Persona("Peatón", new Color(0.95f, 0.6f, 0.2f));
            var sp = peaton.AddComponent<SeguidorDeRuta>();
            sp.ruta = paso;
            sp.modo = ModoRecorrido.IdaYVuelta;
            sp.velocidad = 1.6f;
            sp.distanciaParada = 0.3f;
            sp.velocidadGiro = 300f;
            sp.respetarColaEnRuta = false;

            // Semáforo: A = coches, B = peatones.
            semaforo = new GameObject("Semáforo").AddComponent<SemaforoDeRuta>();
            semaforo.grupoA.Add(stop);
            semaforo.grupoB.Add(bordilloSur);
            semaforo.grupoB.Add(bordilloNorte);
            semaforo.tiempoVerdeA = 9f;
            semaforo.tiempoVerdeB = 6f;
            semaforo.tiempoAmbar = 2.5f;
            Caja("Poste coches", new Vector3(-3f, 1.5f, -14.6f), new Vector3(0.15f, 3f, 0.15f), Color.gray);
            semaforo.lucesA.Add(Esfera("Luz coches", new Vector3(-3f, 3.2f, -14.6f), 0.45f, Color.gray).GetComponent<Renderer>());
            Caja("Poste peatones", new Vector3(3.2f, 1.2f, -15f), new Vector3(0.12f, 2.4f, 0.12f), Color.gray);
            semaforo.lucesB.Add(Esfera("Luz peatones", new Vector3(3.2f, 2.5f, -15f), 0.35f, Color.gray).GetComponent<Renderer>());
        }

        // ---------------------------------------------------------------- 3: guardia

        void CrearGuardia()
        {
            var ruta = CrearRuta("Patrulla del guardia (pico)", false, TipoEsquina.Pico, 0f, new Color(1f, 0.5f, 0.1f),
                new Vector3(26, 0, -30), new Vector3(36, 0, -30), new Vector3(36, 0, -22),
                new Vector3(28, 0, -18), new Vector3(36, 0, -12), new Vector3(30, 0, -4));
            // Torre de vigilancia en cada extremo: se para y mira hacia fuera.
            var a = ruta.ObtenerPunto(0).gameObject.AddComponent<PuntoDeRuta>();
            a.espera = 2.5f; a.mirarEnDireccionDelPunto = true;
            ruta.ObtenerPunto(0).rotation = Quaternion.LookRotation(Vector3.left);
            var b = ruta.ObtenerPunto(ruta.NumeroDePuntos - 1).gameObject.AddComponent<PuntoDeRuta>();
            b.espera = 2.5f; b.mirarEnDireccionDelPunto = true;
            ruta.ObtenerPunto(ruta.NumeroDePuntos - 1).rotation = Quaternion.LookRotation(Vector3.forward);
            // Parada corta en una esquina para "echar un vistazo".
            var c = ruta.ObtenerPunto(2).gameObject.AddComponent<PuntoDeRuta>();
            c.espera = 1f;

            var guardia = Persona("Guardia", new Color(0.2f, 0.3f, 0.8f));
            var s = guardia.AddComponent<SeguidorDeRuta>();
            s.ruta = ruta;
            s.modo = ModoRecorrido.IdaYVuelta;
            s.velocidad = 2.2f;
            s.reducirEnCurvas = false;
            s.velocidadGiro = 220f;
            s.adelantoMirada = 0f;
        }

        // ---------------------------------------------------------------- 4: comparación

        void CrearComparacion()
        {
            Vector3[] forma =
            {
                new Vector3(0, 0, 0), new Vector3(6, 0, 6), new Vector3(0, 0, 12), new Vector3(-6, 0, 6)
            };
            comparaPico = CrearRuta("Comparación: pico", true, TipoEsquina.Pico, 0f, new Color(1f, 0.5f, 0.1f), Desplazar(forma, new Vector3(-30, 0, 20)));
            comparaSuave = CrearRuta("Comparación: suave", true, TipoEsquina.Suave, 4f, new Color(0.3f, 1f, 0.4f), Desplazar(forma, new Vector3(-14, 0, 20)));
            foreach (var r in new[] { comparaPico, comparaSuave })
            {
                var seguidor = new GameObject("Seguidor " + r.name);
                var cubo = Caja("Cubo", new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1.6f), new Color(0.9f, 0.9f, 0.9f));
                cubo.transform.SetParent(seguidor.transform, false);
                cubo.GetComponent<Collider>().enabled = false;
                var s = seguidor.AddComponent<SeguidorDeRuta>();
                s.ruta = r;
                s.velocidad = 5f;
                s.adelantoMirada = 0f;
                s.velocidadGiro = 0f; // gira al instante: se nota la diferencia entre pico y suave
                s.reducirEnCurvas = false;
            }
        }

        // ---------------------------------------------------------------- 5: dron

        void CrearDron()
        {
            var ruta = CrearRuta("Vuelo del dron (mixta)", true, TipoEsquina.Suave, 5f, new Color(0.8f, 0.4f, 1f),
                new Vector3(8, 6, 22), new Vector3(20, 9, 26), new Vector3(30, 5, 22),
                new Vector3(34, 8, 34), new Vector3(20, 4, 38), new Vector3(10, 7, 32));
            // Dos esquinas en pico para mostrar que se pueden mezclar.
            foreach (int i in new[] { 2, 4 })
            {
                var p = ruta.ObtenerPunto(i).gameObject.AddComponent<PuntoDeRuta>();
                p.usarEsquinaPropia = true;
                p.esquina = TipoEsquina.Pico;
                p.velocidadMaxima = 2f;
            }
            ruta.Reconstruir();

            var dron = new GameObject("Dron");
            var cuerpo = Caja("Cuerpo", Vector3.zero, new Vector3(1.2f, 0.3f, 1.2f), new Color(0.15f, 0.15f, 0.15f));
            cuerpo.transform.SetParent(dron.transform, false);
            var morro = Esfera("Morro", new Vector3(0f, 0f, 0.7f), 0.3f, new Color(1f, 0.2f, 0.2f));
            morro.transform.SetParent(dron.transform, false);
            var s = dron.AddComponent<SeguidorDeRuta>();
            s.ruta = ruta;
            s.velocidad = 7f;
            s.soloEjeY = false;
            s.velocidadGiro = 180f;
            s.adelantoMirada = 2f;
        }

        // ---------------------------------------------------------------- Panel

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 330, 270), GUI.skin.box);
            GUILayout.Label("<b>Demo: tráfico y patrulla por puntos</b>", new GUIStyle(GUI.skin.label) { richText = true });
            if (circuito != null)
            {
                bool suave = circuito.esquinaPorDefecto == TipoEsquina.Suave;
                if (GUILayout.Button("Circuito: esquinas " + (suave ? "SUAVES (cambiar a pico)" : "EN PICO (cambiar a suave)")))
                {
                    circuito.esquinaPorDefecto = suave ? TipoEsquina.Pico : TipoEsquina.Suave;
                    circuito.Reconstruir();
                }
                GUILayout.Label($"Radio de curva del circuito: {circuito.radioPorDefecto:0.0} m");
                float r = GUILayout.HorizontalSlider(circuito.radioPorDefecto, 0.5f, 10f);
                if (!Mathf.Approximately(r, circuito.radioPorDefecto)) { circuito.radioPorDefecto = r; circuito.Reconstruir(); }
            }
            if (comparaSuave != null)
            {
                GUILayout.Label($"Radio de la comparación suave: {comparaSuave.radioPorDefecto:0.0} m");
                float r = GUILayout.HorizontalSlider(comparaSuave.radioPorDefecto, 0.5f, 8f);
                if (!Mathf.Approximately(r, comparaSuave.radioPorDefecto)) { comparaSuave.radioPorDefecto = r; comparaSuave.Reconstruir(); }
            }
            if (semaforo != null)
            {
                GUILayout.Label("Semáforo: " + semaforo.FaseActual);
                if (GUILayout.Button("Siguiente fase del semáforo")) semaforo.CambiarFase();
            }
            GUILayout.Label("Abajo-izq: pico vs suave · Abajo-dcha: guardia · Arriba-dcha: dron");
            GUILayout.EndArea();
        }

        // ---------------------------------------------------------------- Utilidades

        static Vector3[] Desplazar(Vector3[] puntos, Vector3 d)
        {
            var r = new Vector3[puntos.Length];
            for (int i = 0; i < puntos.Length; i++) r[i] = puntos[i] + d;
            return r;
        }

        static RutaDePuntos CrearRuta(string nombre, bool cerrada, TipoEsquina esquina, float radio, Color color, params Vector3[] puntos)
        {
            var go = new GameObject(nombre);
            var ruta = go.AddComponent<RutaDePuntos>();
            ruta.cerrada = cerrada;
            ruta.esquinaPorDefecto = esquina;
            ruta.radioPorDefecto = radio;
            ruta.colorRuta = color;
            for (int i = 0; i < puntos.Length; i++)
            {
                var p = new GameObject($"Punto {i}");
                p.transform.SetParent(go.transform, false);
                p.transform.position = puntos[i];
            }
            ruta.Reconstruir();
            // Línea visible también en el Game view (los gizmos solo se ven en la vista de escena).
            var linea = go.AddComponent<LineRenderer>();
            linea.widthMultiplier = 0.12f;
            linea.material = new Material(Shader.Find("Sprites/Default"));
            linea.startColor = linea.endColor = color;
            go.AddComponent<DibujarRutaEnJuego>();
            return ruta;
        }

        static GameObject Persona(string nombre, Color color)
        {
            var go = new GameObject(nombre);
            var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.name = "Cuerpo";
            cuerpo.transform.SetParent(go.transform, false);
            cuerpo.transform.localPosition = Vector3.up * 1f;
            Pintar(cuerpo, color);
            var nariz = Caja("Nariz (frente)", new Vector3(0f, 1.6f, 0.45f), new Vector3(0.2f, 0.2f, 0.3f), Color.black);
            nariz.transform.SetParent(go.transform, false);
            nariz.GetComponent<Collider>().enabled = false;
            return go;
        }

        static GameObject Caja(string nombre, Vector3 pos, Vector3 escala, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.localScale = escala;
            Pintar(go, color);
            return go;
        }

        static GameObject Esfera(string nombre, Vector3 pos, float radio, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * radio * 2f;
            Pintar(go, color);
            return go;
        }

        static Material materialBase;

        internal static void Pintar(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            if (materialBase == null) materialBase = new Material(ShaderLit()) { name = "Demo Patrulla" };
            var m = new Material(materialBase) { color = color };
            r.sharedMaterial = m;
        }

        static Shader ShaderLit()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            Shader s = null;
            if (rp != null)
            {
                if (rp.GetType().Name.Contains("HD")) s = Shader.Find("HDRP/Lit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Unlit/Color");
            return s;
        }
    }
}
