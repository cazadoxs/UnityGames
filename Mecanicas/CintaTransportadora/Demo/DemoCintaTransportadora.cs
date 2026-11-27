using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Cintas.Demo
{
    /// <summary>
    /// Monta la escena de demostración al darle a Play: suelo, luz, jugador en primera
    /// persona, dos cintas encadenadas con un generador y un contenedor receptor, y una
    /// cinta ancha (pasillo rodante) para que el jugador se suba.
    ///
    /// Controles: WASD moverse · Ratón mirar · Espacio saltar · Shift correr
    /// F encender/apagar · R invertir sentido · Z / X bajar / subir velocidad
    /// G soltar una caja delante · Esc liberar el ratón (clic para volver).
    /// </summary>
    public class DemoCintaTransportadora : MonoBehaviour
    {
        readonly List<CintaTransportadora> cintas = new List<CintaTransportadora>();
        CintaReceptor receptor;
        CintaGenerador generador;
        Transform camaraJugador;

        void Start()
        {
            CrearEntorno();

            // Cinta 1: recibe los objetos del generador y avanza hacia +Z.
            var c1 = CintaTransportadoraConstructor.Crear(new CintaTransportadoraConstructor.Opciones
            {
                nombre = "Cinta 1 (entrada)", ancho = 0.8f, largo = 5f, alto = 0.6f, velocidad = 1.5f
            }, new Vector3(0f, 0f, -4.2f), Quaternion.identity, transform);
            cintas.Add(c1);

            // Cinta 2: más baja y perpendicular; recoge lo que cae de la 1 y lo lleva a +X.
            var c2 = CintaTransportadoraConstructor.Crear(new CintaTransportadoraConstructor.Opciones
            {
                nombre = "Cinta 2 (salida)", ancho = 1f, largo = 6f, alto = 0.4f, velocidad = 1.5f
            }, new Vector3(2.4f, 0f, -1.1f), Quaternion.Euler(0f, 90f, 0f), transform);
            cintas.Add(c2);

            // Pasillo rodante: ancho, a ras de suelo, para subirse con el jugador.
            var pasillo = CintaTransportadoraConstructor.Crear(new CintaTransportadoraConstructor.Opciones
            {
                nombre = "Pasillo rodante", ancho = 2f, largo = 10f, alto = 0.12f, velocidad = 2f,
                barandillas = false, metrosPorRepeticion = 1f
            }, new Vector3(-4f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f), transform);
            cintas.Add(pasillo);

            // Generador encima del principio de la cinta 1.
            var gen = new GameObject("Generador");
            gen.transform.SetParent(transform, false);
            gen.transform.position = new Vector3(0f, 1.3f, -6.1f);
            generador = gen.AddComponent<CintaGenerador>();

            receptor = CrearContenedor(new Vector3(6.2f, 0f, -1.1f));
            CrearJugador(new Vector3(-1.5f, 0.1f, 3f), 200f);
        }

        void Update()
        {
            if (DemoEntrada.Pulsada(KeyCode.F)) foreach (var c in cintas) c.Alternar();
            if (DemoEntrada.Pulsada(KeyCode.R)) foreach (var c in cintas) c.InvertirSentido();
            if (DemoEntrada.Pulsada(KeyCode.X)) foreach (var c in cintas) c.SumarVelocidad(0.5f);
            if (DemoEntrada.Pulsada(KeyCode.Z)) foreach (var c in cintas) c.SumarVelocidad(-0.5f);
            if (DemoEntrada.Pulsada(KeyCode.G)) SoltarCaja();
        }

        void OnGUI()
        {
            var c = cintas.Count > 0 ? cintas[0] : null;
            string estado = c == null ? "" :
                $"Cintas: {(c.EstaEncendida ? "ENCENDIDAS" : "APAGADAS")}   Velocidad: {c.Velocidad:0.0} m/s (actual {c.VelocidadActual:0.00})\n" +
                $"Objetos recibidos en el contenedor: {(receptor != null ? receptor.Contador : 0)}\n\n";
            GUI.Box(new Rect(10, 10, 430, 150),
                estado +
                "WASD moverse · Ratón mirar · Espacio saltar · Shift correr\n" +
                "F encender/apagar · R invertir sentido · Z/X velocidad -/+\n" +
                "G soltar una caja delante · Esc soltar el ratón (clic para volver)\n" +
                "Súbete al pasillo rodante (izquierda) para notar el arrastre.");
        }

        void SoltarCaja()
        {
            if (camaraJugador == null || generador == null) return;
            var caja = generador.Generar();
            if (caja == null) return;
            caja.transform.position = camaraJugador.position + camaraJugador.forward * 1.5f;
        }

        // ------------------------------------------------------------------ Montaje

        void CrearEntorno()
        {
#if UNITY_2023_1_OR_NEWER
            bool hayLuz = FindFirstObjectByType<Light>() != null;
#else
            bool hayLuz = FindObjectOfType<Light>() != null;
#endif
            if (!hayLuz)
            {
                var luz = new GameObject("Luz").AddComponent<Light>();
                luz.type = LightType.Directional;
                luz.intensity = 1.1f;
                luz.shadows = LightShadows.Soft;
                luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.SetParent(transform, false);
            suelo.transform.localScale = new Vector3(3f, 1f, 3f);
            suelo.GetComponent<Renderer>().sharedMaterial =
                CintaMateriales.Crear("Demo - Suelo", new Color(0.55f, 0.57f, 0.6f), 0f, 0.2f);
        }

        CintaReceptor CrearContenedor(Vector3 pos)
        {
            var raiz = new GameObject("Contenedor (receptor)");
            raiz.transform.SetParent(transform, false);
            raiz.transform.position = pos;
            var mat = CintaMateriales.Crear("Demo - Contenedor", new Color(0.15f, 0.45f, 0.25f), 0.2f, 0.4f);

            void Pared(Vector3 centro, Vector3 tam)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "Pared";
                p.transform.SetParent(raiz.transform, false);
                p.transform.localPosition = centro;
                p.transform.localScale = tam;
                p.GetComponent<Renderer>().sharedMaterial = mat;
            }
            Pared(new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.04f, 1.6f));
            Pared(new Vector3(0.7f, 0.3f, 0f), new Vector3(0.05f, 0.6f, 1.6f));
            Pared(new Vector3(0f, 0.2f, 0.8f), new Vector3(1.4f, 0.4f, 0.05f));
            Pared(new Vector3(0f, 0.2f, -0.8f), new Vector3(1.4f, 0.4f, 0.05f));

            var zona = raiz.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            zona.center = new Vector3(0f, 0.3f, 0f);
            zona.size = new Vector3(1.3f, 0.5f, 1.5f);
            var r = raiz.AddComponent<CintaReceptor>();
            return r;
        }

        void CrearJugador(Vector3 pos, float yaw)
        {
#if UNITY_2023_1_OR_NEWER
            var camaras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
            var camaras = FindObjectsOfType<Camera>();
#endif
            foreach (var cam in camaras) cam.gameObject.SetActive(false);

            var jugador = new GameObject("Jugador");
            jugador.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var cc = jugador.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(jugador.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var camara = camGo.AddComponent<Camera>();
            camara.nearClipPlane = 0.05f;
            if (RenderSettings.skybox == null)
            {
                camara.clearFlags = CameraClearFlags.SolidColor;
                camara.backgroundColor = new Color(0.62f, 0.74f, 0.86f);
            }
            camGo.AddComponent<AudioListener>();
            camaraJugador = camGo.transform;

            var control = jugador.AddComponent<DemoJugadorPrimeraPersona>();
            control.camara = camGo.transform;
        }
    }
}
