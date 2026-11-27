using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Patrulla
{
    /// <summary>
    /// Crea vehículos (o peatones) en una ruta cada cierto tiempo, hasta un máximo a la vez.
    /// Cada prefab debe tener un SeguidorDeRuta (si no lo tiene, se le añade uno con los ajustes de aquí).
    /// En rutas abiertas los vehículos se destruyen al llegar al final; en cerradas dan vueltas.
    /// </summary>
    [AddComponentMenu("Mecanicas/Patrulla/Generador De Trafico")]
    public class GeneradorDeTrafico : MonoBehaviour
    {
        [Header("Qué y dónde")]
        public RutaDePuntos ruta;
        [Tooltip("Prefabs entre los que elige al azar.")]
        public List<GameObject> prefabs = new List<GameObject>();
        [Tooltip("Punto de la ruta donde aparecen.")]
        [Min(0)] public int puntoDeAparicion = 0;
        public bool sentidoInverso = false;
        [Tooltip("Carril: desplazamiento lateral que se asigna a cada vehículo.")]
        public float desplazamientoLateral = 0f;

        [Header("Cuántos y cuándo")]
        [Min(0)] public int maximoAlaVez = 8;
        [Min(0.1f)] public float intervalo = 3f;
        [Tooltip("Segundos extra al azar entre apariciones.")]
        [Min(0f)] public float intervaloAleatorio = 2f;
        [Tooltip("Crea este número de vehículos repartidos por la ruta al empezar (solo rutas cerradas).")]
        [Min(0)] public int repartirAlEmpezar = 0;
        [Tooltip("No crea uno nuevo si hay otro seguidor de la ruta a menos de esta distancia del punto de aparición.")]
        [Min(0f)] public float espacioLibreNecesario = 8f;

        [Header("Variedad")]
        public Vector2 rangoVelocidad = new Vector2(8f, 12f);

        readonly List<SeguidorDeRuta> creados = new List<SeguidorDeRuta>();
        float temporizador;

        public IReadOnlyList<SeguidorDeRuta> Creados => creados;

        void Start()
        {
            temporizador = 0f;
            if (ruta == null || !ruta.EsValida || repartirAlEmpezar <= 0) return;
            if (!ruta.cerrada) return;
            float L = ruta.Longitud;
            float inicio = ruta.DistanciaDePunto(Mathf.Clamp(puntoDeAparicion, 0, ruta.NumeroDePuntos - 1));
            int cuantos = Mathf.Min(repartirAlEmpezar, maximoAlaVez);
            for (int i = 0; i < cuantos; i++)
            {
                float d = inicio + (sentidoInverso ? -1f : 1f) * L * i / cuantos;
                var s = Crear(ruta.PosicionEn(d));
                if (s != null) { s.empezarEnLugarMasCercano = true; s.Iniciar(); }
            }
        }

        void Update()
        {
            creados.RemoveAll(s => s == null);
            if (ruta == null || prefabs.Count == 0) return;
            temporizador -= Time.deltaTime;
            if (temporizador > 0f || creados.Count >= maximoAlaVez) return;
            if (!HayEspacio()) { temporizador = 0.25f; return; }

            var nuevo = Crear(ruta.PosicionEn(DistanciaAparicion()));
            if (nuevo != null) nuevo.Iniciar();
            temporizador = intervalo + Random.Range(0f, intervaloAleatorio);
        }

        float DistanciaAparicion()
        {
            return ruta.DistanciaDePunto(Mathf.Clamp(puntoDeAparicion, 0, ruta.NumeroDePuntos - 1));
        }

        bool HayEspacio()
        {
            float d0 = DistanciaAparicion();
            float L = ruta.Longitud;
            foreach (var s in SeguidorDeRuta.Activos)
            {
                if (s == null || s.ruta != ruta) continue;
                float delta = Mathf.Abs(s.Distancia - d0);
                if (ruta.cerrada) delta = Mathf.Min(delta, L - delta);
                if (delta < espacioLibreNecesario) return false;
            }
            return true;
        }

        SeguidorDeRuta Crear(Vector3 posicion)
        {
            var prefab = prefabs[Random.Range(0, prefabs.Count)];
            if (prefab == null) return null;
            var go = Instantiate(prefab, posicion, Quaternion.identity);
            var s = go.GetComponent<SeguidorDeRuta>();
            if (s == null) s = go.AddComponent<SeguidorDeRuta>();
            s.empezarAutomaticamente = false;
            s.ruta = ruta;
            s.puntoInicial = puntoDeAparicion;
            s.empezarEnLugarMasCercano = false;
            s.sentidoInverso = sentidoInverso;
            s.desplazamientoLateral = desplazamientoLateral;
            s.velocidad = Random.Range(rangoVelocidad.x, rangoVelocidad.y);
            if (!ruta.cerrada)
            {
                s.modo = ModoRecorrido.UnaVez;
                s.destruirAlTerminar = true;
            }
            else
            {
                s.modo = ModoRecorrido.Bucle;
            }
            if (!go.activeSelf) go.SetActive(true); // por si el prefab es una plantilla desactivada de la escena
            creados.Add(s);
            return s;
        }
    }
}
