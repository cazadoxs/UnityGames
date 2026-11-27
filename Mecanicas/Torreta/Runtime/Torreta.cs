using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Torretas
{
    /// <summary>Cómo se mueve la torreta para apuntar.</summary>
    public enum ModoApuntado
    {
        /// <summary>No gira nunca. Dispara recto cuando un objetivo entra en su cono de visión.</summary>
        Fija,
        /// <summary>Gira solo alrededor de un eje (por defecto el vertical: gira a izquierda/derecha).</summary>
        UnEje,
        /// <summary>Gira y además sube/baja el cañón: puede apuntar en cualquier dirección.</summary>
        TodosLosAngulos
    }

    /// <summary>A qué objetivo dispara cuando hay varios a la vez.</summary>
    public enum PrioridadObjetivo { MasCercano, MasLejano, MenosVida, PrimeroEnEntrar, MasAlineado }

    /// <summary>Qué hace con varios cañones.</summary>
    public enum ModoCanones { Alternar, Simultaneo }

    /// <summary>
    /// Cerebro de la torreta: busca objetivos, gira las piezas para apuntar y crea las balas.
    /// Los efectos (animación, sonido, fogonazo) están en <see cref="TorretaEfectos"/>, que escucha
    /// los eventos de esta clase.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecanicas/Torretas/Torreta")]
    public class Torreta : MonoBehaviour
    {
        // ------------------------------------------------------------------ Configuración
        [Header("Modo")]
        public ModoApuntado modo = ModoApuntado.TodosLosAngulos;

        [Header("Piezas")]
        [Tooltip("Pieza que gira en el eje principal (la base giratoria). Si se deja vacía se usa este mismo objeto.")]
        public Transform pivotGiro;
        [Tooltip("Pieza que sube y baja el cañón (hija de pivotGiro). Solo se usa en TodosLosAngulos. " +
                 "Si está vacía, pivotGiro mira directamente al objetivo en todos los ejes.")]
        public Transform pivotElevacion;
        [Tooltip("Bocas de los cañones. Las balas salen de aquí en la dirección de su eje Z (flecha azul).")]
        public Transform[] puntosDisparo = new Transform[0];

        [Header("Giro (eje principal)")]
        [Tooltip("Eje de giro, en coordenadas locales de pivotGiro en reposo. (0,1,0) = gira a izquierda y derecha.")]
        public Vector3 ejeGiro = Vector3.up;
        [Tooltip("Grados por segundo.")]
        [Min(0f)] public float velocidadGiro = 120f;
        public bool limitarGiro = false;
        [Range(-180f, 0f)] public float giroMinimo = -90f;
        [Range(0f, 180f)] public float giroMaximo = 90f;

        [Header("Elevación (solo TodosLosAngulos)")]
        [Min(0f)] public float velocidadElevacion = 80f;
        [Range(-90f, 0f)] public float elevacionMinima = -10f;
        [Range(0f, 90f)] public float elevacionMaxima = 60f;

        [Header("Reposo")]
        [Tooltip("Sin objetivo, vuelve poco a poco a su posición inicial.")]
        public bool volverAReposo = true;
        [Tooltip("Sin objetivo, gira de un lado a otro vigilando (solo UnEje / TodosLosAngulos).")]
        public bool vigilarSinObjetivo = false;
        public float anguloVigilancia = 45f;
        public float velocidadVigilancia = 30f;

        [Header("Detección")]
        [Min(0f)] public float alcance = 25f;
        [Min(0f)] public float alcanceMinimo = 0f;
        [Tooltip("Capas en las que se buscan objetivos.")]
        public LayerMask capasObjetivo = ~0;
        [Tooltip("Si tiene etiquetas, solo se atacan objetos con alguna de ellas. Si está vacía, se atacan los que tengan Salud (o IDaniable).")]
        public string[] etiquetasObjetivo = new string[0];
        public PrioridadObjetivo prioridad = PrioridadObjetivo.MasCercano;
        [Tooltip("Fija: medio ángulo del cono donde ve. UnEje: cuánto puede estar el objetivo por encima o debajo del plano de giro.")]
        [Range(1f, 90f)] public float anguloVision = 20f;
        public bool requiereLineaDeVision = true;
        [Tooltip("Capas que tapan la vista (paredes, suelo...).")]
        public LayerMask capasObstaculo = ~0;
        [Tooltip("Cada cuántos segundos busca objetivos (buscar cada frame es caro).")]
        [Min(0.02f)] public float intervaloBusqueda = 0.2f;
        [Tooltip("Cambia de objetivo si aparece uno con mejor prioridad, aunque el actual siga siendo válido.")]
        public bool cambiarObjetivoSiHayMejor = false;
        [Tooltip("Altura extra sobre el centro del objetivo a la que apunta.")]
        public float alturaApuntado = 0f;

        [Header("Disparo")]
        [Tooltip("Prefab (o plantilla) de la bala. Debe tener el componente Proyectil.")]
        public GameObject prefabProyectil;
        [Tooltip("Disparos por segundo.")]
        [Min(0.01f)] public float cadencia = 4f;
        [Tooltip("Disparos seguidos antes de hacer una pausa. 1 = sin ráfagas.")]
        [Min(1)] public int balasPorRafaga = 1;
        [Min(0f)] public float pausaEntreRafagas = 0.6f;
        [Tooltip("Balas que salen a la vez por cañón (más de 1 = escopeta).")]
        [Min(1)] public int proyectilesPorDisparo = 1;
        [Tooltip("Desvío aleatorio en grados.")]
        [Range(0f, 45f)] public float dispersion = 1f;
        public ModoCanones modoCanones = ModoCanones.Alternar;
        [Tooltip("Error máximo (grados) entre el cañón y el objetivo para empezar a disparar.")]
        [Range(0.1f, 45f)] public float toleranciaApuntado = 4f;
        [Tooltip("Dispara sin parar aunque no haya objetivo (trampas, torres de demostración...).")]
        public bool dispararSiempre = false;

        [Header("Bala")]
        [Min(0.1f)] public float velocidadProyectil = 45f;
        [Min(0f)] public float danio = 10f;
        [Tooltip("Multiplicador de la gravedad del proyecto (Physics.gravity). 0 = vuela recta.")]
        [Min(0f)] public float gravedadProyectil = 0f;
        [Tooltip("Capas contra las que choca la bala.")]
        public LayerMask capasImpacto = ~0;
        [Tooltip("Apunta adonde estará el objetivo cuando llegue la bala.")]
        public bool predecirMovimiento = true;
        [Tooltip("Con gravedad, sube el cañón para compensar la caída (solo TodosLosAngulos).")]
        public bool compensarGravedad = true;
        [Tooltip("Con gravedad, usa la parábola alta (estilo mortero).")]
        public bool arcoAlto = false;

        [Header("Munición")]
        public bool municionInfinita = true;
        [Min(1)] public int tamanoCargador = 30;
        [Min(0f)] public float tiempoRecarga = 2f;

        [Header("Eventos (para conectar cosas desde el Inspector)")]
        public UnityEvent<Transform> alAdquirirObjetivo = new UnityEvent<Transform>();
        public UnityEvent alPerderObjetivo = new UnityEvent();
        public UnityEvent<int> alDisparar = new UnityEvent<int>();
        public UnityEvent alEmpezarRecarga = new UnityEvent();
        public UnityEvent alTerminarRecarga = new UnityEvent();

        // ------------------------------------------------------------------ Eventos C#
        /// <summary>Se dispara cada vez que sale una bala (o un grupo de balas) de un cañón. Parámetro: índice del cañón.</summary>
        public event Action<int> Disparo;
        /// <summary>Cambio de objetivo. Parámetro: el nuevo (null si lo ha perdido).</summary>
        public event Action<Transform> ObjetivoCambiado;
        public event Action RecargaIniciada;
        public event Action RecargaTerminada;

        // ------------------------------------------------------------------ Estado público
        public Transform Objetivo => objetivo;
        public bool TieneObjetivo => objetivo != null;
        public bool Recargando => recargando;
        public int MunicionActual => municion;
        public bool Alineada { get; private set; }
        /// <summary>Velocidad de giro actual en grados/segundo (para el sonido del motor).</summary>
        public float VelocidadAngularActual { get; private set; }
        public Vector3 PuntoApuntado { get; private set; }

        // ------------------------------------------------------------------ Estado interno
        Transform objetivo;
        Collider colliderObjetivo;
        IDaniable vidaObjetivo;
        bool objetivoManual;
        Vector3 ultimaPosObjetivo;
        Vector3 velocidadObjetivo;

        Quaternion rotInicialGiro, rotInicialElev;
        float anguloGiro, anguloElev;
        Quaternion rotInicialMundoLibre;
        float tiempoVigilancia;

        float temporizadorBusqueda;
        float proximoDisparo;
        int disparosEnRafaga;
        int indiceCanon;
        int municion;
        bool recargando;
        float finRecarga;

        readonly Dictionary<Transform, float> tiempoEntrada = new Dictionary<Transform, float>();
        static Collider[] bufferDeteccion = new Collider[64];
        static readonly RaycastHit[] bufferVision = new RaycastHit[16];

        // ------------------------------------------------------------------ Ciclo de vida
        void Reset()
        {
            pivotGiro = transform;
        }

        void Awake()
        {
            if (pivotGiro == null) pivotGiro = transform;
            rotInicialGiro = pivotGiro.localRotation;
            rotInicialMundoLibre = pivotGiro.localRotation;
            if (pivotElevacion != null) rotInicialElev = pivotElevacion.localRotation;
            municion = tamanoCargador;
            temporizadorBusqueda = UnityEngine.Random.Range(0f, intervaloBusqueda); // reparte el trabajo entre torretas
        }

        void OnDisable()
        {
            if (objetivo != null) PerderObjetivo();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            ActualizarRecarga();

            temporizadorBusqueda -= dt;
            if (temporizadorBusqueda <= 0f)
            {
                temporizadorBusqueda = intervaloBusqueda;
                if (!objetivoManual && (objetivo == null || cambiarObjetivoSiHayMejor)) BuscarObjetivo();
            }
            if (objetivo != null && !ObjetivoSigueValido()) PerderObjetivo();

            Alineada = false;
            VelocidadAngularActual = 0f;
            if (objetivo != null)
            {
                Vector3 pos = PosicionMira(objetivo, colliderObjetivo);
                velocidadObjetivo = Vector3.Lerp(velocidadObjetivo, (pos - ultimaPosObjetivo) / dt, 0.5f);
                ultimaPosObjetivo = pos;
                Alineada = Apuntar(CalcularDireccionDeseada(pos), dt);
            }
            else
            {
                Reposar(dt);
            }

            bool quiere = dispararSiempre || (objetivo != null && Alineada);
            if (!quiere && objetivo == null) disparosEnRafaga = 0;
            if (quiere) IntentarDisparar();
        }

        // ------------------------------------------------------------------ API pública
        /// <summary>Fuerza un objetivo (por ejemplo, el jugador). Pasa null para volver a la búsqueda automática.</summary>
        public void AsignarObjetivo(Transform nuevo)
        {
            objetivoManual = nuevo != null;
            if (nuevo == null) { PerderObjetivo(); return; }
            CambiarObjetivo(nuevo, nuevo.GetComponentInChildren<Collider>());
        }

        /// <summary>Dispara una vez ahora mismo, sin esperar a la cadencia ni al apuntado.</summary>
        public void DispararAhora()
        {
            if (!recargando) EjecutarDisparo();
        }

        /// <summary>Empieza a recargar aunque el cargador no esté vacío.</summary>
        public void Recargar()
        {
            if (municionInfinita || recargando || municion == tamanoCargador) return;
            EmpezarRecarga();
        }

        /// <summary>Dirección hacia la que apunta el cañón principal ahora mismo.</summary>
        public Vector3 DireccionCanon
        {
            get
            {
                if (puntosDisparo != null && puntosDisparo.Length > 0 && puntosDisparo[0] != null) return puntosDisparo[0].forward;
                if (pivotElevacion != null) return pivotElevacion.forward;
                return (pivotGiro != null ? pivotGiro : transform).forward;
            }
        }

        /// <summary>Punto desde el que se calcula la puntería (centro de giro del cañón).</summary>
        public Vector3 OrigenApuntado
        {
            get
            {
                if (pivotElevacion != null) return pivotElevacion.position;
                if (modo == ModoApuntado.Fija && puntosDisparo != null && puntosDisparo.Length > 0 && puntosDisparo[0] != null)
                    return puntosDisparo[0].position;
                return (pivotGiro != null ? pivotGiro : transform).position;
            }
        }

        // ------------------------------------------------------------------ Búsqueda de objetivos
        void BuscarObjetivo()
        {
            Vector3 centro = OrigenApuntado;
            int n = Physics.OverlapSphereNonAlloc(centro, alcance, bufferDeteccion, capasObjetivo, QueryTriggerInteraction.Collide);
            if (n == bufferDeteccion.Length)
            {
                bufferDeteccion = new Collider[bufferDeteccion.Length * 2];
                n = Physics.OverlapSphereNonAlloc(centro, alcance, bufferDeteccion, capasObjetivo, QueryTriggerInteraction.Collide);
            }

            Transform mejor = null; Collider mejorCol = null; float mejorPuntuacion = float.MaxValue;
            float ahora = Time.time;
            var vistos = ListaTemporal;
            vistos.Clear();

            for (int i = 0; i < n; i++)
            {
                Collider col = bufferDeteccion[i];
                if (col == null || !col.enabled) continue;
                Transform raiz = RaizObjetivo(col);
                if (raiz == null || raiz.IsChildOf(transform)) continue;
                if (vistos.Contains(raiz)) continue;
                vistos.Add(raiz);
                if (!EsCandidato(raiz, col)) continue;

                if (!tiempoEntrada.ContainsKey(raiz)) tiempoEntrada[raiz] = ahora;
                float p = Puntuar(raiz, col);
                if (p < mejorPuntuacion) { mejorPuntuacion = p; mejor = raiz; mejorCol = col; }
            }

            // Olvida a los que ya no están en rango (para la prioridad "PrimeroEnEntrar").
            if (tiempoEntrada.Count > vistos.Count)
            {
                var borrar = ListaTemporal2; borrar.Clear();
                foreach (var kv in tiempoEntrada) if (kv.Key == null || !vistos.Contains(kv.Key)) borrar.Add(kv.Key);
                foreach (var t in borrar) tiempoEntrada.Remove(t);
            }

            if (mejor != null && mejor != objetivo) CambiarObjetivo(mejor, mejorCol);
        }

        static readonly List<Transform> ListaTemporal = new List<Transform>();
        static readonly List<Transform> ListaTemporal2 = new List<Transform>();

        /// <summary>El objeto "dueño" del collider: el que tiene la vida, o el Rigidbody, o el propio collider.</summary>
        static Transform RaizObjetivo(Collider col)
        {
            var d = col.GetComponentInParent<IDaniable>();
            if (d is Component c) return c.transform;
            if (col.attachedRigidbody != null) return col.attachedRigidbody.transform;
            return col.transform;
        }

        bool EsCandidato(Transform raiz, Collider col)
        {
            if (etiquetasObjetivo != null && etiquetasObjetivo.Length > 0)
            {
                bool ok = false;
                foreach (var e in etiquetasObjetivo)
                    if (!string.IsNullOrEmpty(e) && (raiz.CompareTag(e) || col.CompareTag(e))) { ok = true; break; }
                if (!ok) return false;
            }
            else if (raiz.GetComponent<IDaniable>() == null) return false;

            var vida = raiz.GetComponent<IDaniable>();
            if (vida != null && !vida.EstaVivo) return false;

            Vector3 punto = PosicionMira(raiz, col);
            float dist = Vector3.Distance(OrigenApuntado, punto);
            if (dist > alcance || dist < alcanceMinimo) return false;
            if (!PuedeApuntarA(punto)) return false;
            if (requiereLineaDeVision && !HayLineaDeVision(punto, raiz)) return false;
            return true;
        }

        float Puntuar(Transform raiz, Collider col)
        {
            Vector3 punto = PosicionMira(raiz, col);
            switch (prioridad)
            {
                case PrioridadObjetivo.MasLejano: return -Vector3.Distance(OrigenApuntado, punto);
                case PrioridadObjetivo.MenosVida:
                    var v = raiz.GetComponent<IDaniable>();
                    return v != null ? v.VidaActual : float.MaxValue;
                case PrioridadObjetivo.PrimeroEnEntrar:
                    return tiempoEntrada.TryGetValue(raiz, out float t) ? t : Time.time;
                case PrioridadObjetivo.MasAlineado:
                    return Vector3.Angle(DireccionCanon, punto - OrigenApuntado);
                default: return Vector3.Distance(OrigenApuntado, punto);
            }
        }

        /// <summary>¿Puede físicamente apuntar a ese punto con su modo y sus límites?</summary>
        public bool PuedeApuntarA(Vector3 punto)
        {
            Vector3 dir = punto - OrigenApuntado;
            if (dir.sqrMagnitude < 1e-6f) return true;
            switch (modo)
            {
                case ModoApuntado.Fija:
                    return Vector3.Angle(DireccionCanon, dir) <= anguloVision;

                case ModoApuntado.UnEje:
                {
                    Vector3 ejeMundo = EjeGiroMundo();
                    float fueraDelPlano = 90f - Vector3.Angle(ejeMundo, dir);
                    if (Mathf.Abs(fueraDelPlano) > anguloVision) return false;
                    return !limitarGiro || DentroDeLimitesGiro(dir);
                }

                default:
                {
                    if (limitarGiro && !DentroDeLimitesGiro(dir)) return false;
                    if (pivotElevacion != null)
                    {
                        Vector3 ejeMundo = EjeGiroMundo();
                        float elev = 90f - Vector3.Angle(ejeMundo, dir);
                        if (elev < elevacionMinima - toleranciaApuntado || elev > elevacionMaxima + toleranciaApuntado) return false;
                    }
                    return true;
                }
            }
        }

        bool DentroDeLimitesGiro(Vector3 dirMundo)
        {
            float a = AnguloDeseadoGiro(dirMundo);
            return a >= giroMinimo - toleranciaApuntado && a <= giroMaximo + toleranciaApuntado;
        }

        bool HayLineaDeVision(Vector3 punto, Transform raizObjetivo)
        {
            Vector3 origen = OrigenApuntado;
            Vector3 dir = punto - origen;
            float dist = dir.magnitude;
            if (dist < 1e-4f) return true;
            int n = Physics.RaycastNonAlloc(origen, dir / dist, bufferVision, dist, capasObstaculo, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = bufferVision[i].collider.transform;
                if (t.IsChildOf(transform)) continue;       // la propia torreta
                if (t.IsChildOf(raizObjetivo)) continue;    // el propio objetivo
                return false;
            }
            return true;
        }

        bool ObjetivoSigueValido()
        {
            if (objetivo == null || !objetivo.gameObject.activeInHierarchy) return false;
            if (colliderObjetivo != null && !colliderObjetivo.enabled) return false;
            if (vidaObjetivo != null && !vidaObjetivo.EstaVivo) return false;
            if (objetivoManual) return true;
            Vector3 punto = PosicionMira(objetivo, colliderObjetivo);
            float dist = Vector3.Distance(OrigenApuntado, punto);
            if (dist > alcance * 1.05f || dist < alcanceMinimo) return false;
            if (!PuedeApuntarA(punto)) return false;
            // La línea de visión se comprueba con menos frecuencia para no lanzar rayos cada frame.
            if (requiereLineaDeVision && temporizadorBusqueda == intervaloBusqueda && !HayLineaDeVision(punto, objetivo)) return false;
            return true;
        }

        void CambiarObjetivo(Transform nuevo, Collider col)
        {
            objetivo = nuevo;
            colliderObjetivo = col;
            vidaObjetivo = nuevo.GetComponent<IDaniable>();
            ultimaPosObjetivo = PosicionMira(nuevo, col);
            velocidadObjetivo = Vector3.zero;
            alAdquirirObjetivo.Invoke(nuevo);
            ObjetivoCambiado?.Invoke(nuevo);
        }

        void PerderObjetivo()
        {
            bool habia = objetivo != null;
            objetivo = null; colliderObjetivo = null; vidaObjetivo = null; objetivoManual = false;
            tiempoVigilancia = 0f;
            if (habia)
            {
                alPerderObjetivo.Invoke();
                ObjetivoCambiado?.Invoke(null);
            }
        }

        Vector3 PosicionMira(Transform t, Collider col)
        {
            Vector3 p = col != null && col.enabled ? col.bounds.center : t.position;
            return p + Vector3.up * alturaApuntado;
        }

        // ------------------------------------------------------------------ Apuntado
        Vector3 CalcularDireccionDeseada(Vector3 posObjetivo)
        {
            Vector3 origen = OrigenApuntado;
            Vector3 punto = posObjetivo;
            float g = Physics.gravity.magnitude * gravedadProyectil;
            bool usarBalistica = g > 0f && compensarGravedad && modo == ModoApuntado.TodosLosAngulos;

            if (predecirMovimiento)
            {
                if (usarBalistica)
                {
                    // Iteramos: tiempo de vuelo -> punto futuro -> nuevo tiempo de vuelo.
                    for (int i = 0; i < 3; i++)
                    {
                        MatematicaTorreta.DireccionBalistica(origen, punto, velocidadProyectil, g, arcoAlto, out _, out float tv);
                        punto = posObjetivo + velocidadObjetivo * tv;
                    }
                }
                else
                {
                    MatematicaTorreta.PuntoInterceptacion(origen, posObjetivo, velocidadObjetivo, velocidadProyectil, out punto, out _);
                }
            }
            PuntoApuntado = punto;

            if (usarBalistica)
            {
                MatematicaTorreta.DireccionBalistica(origen, punto, velocidadProyectil, g, arcoAlto, out Vector3 dir, out _);
                return dir;
            }
            return (punto - origen).normalized;
        }

        /// <summary>Gira las piezas hacia la dirección. Devuelve true si el cañón ya apunta (dentro de la tolerancia).</summary>
        bool Apuntar(Vector3 dirMundo, float dt)
        {
            switch (modo)
            {
                case ModoApuntado.Fija:
                    return Vector3.Angle(DireccionCanon, dirMundo) <= anguloVision;

                case ModoApuntado.UnEje:
                {
                    float deseado = AnguloDeseadoGiro(dirMundo);
                    if (limitarGiro) deseado = Mathf.Clamp(deseado, giroMinimo, giroMaximo);
                    GirarHacia(deseado, dt);
                    // En un solo eje solo podemos alinearnos dentro del plano de giro.
                    float error = Mathf.Abs(Mathf.DeltaAngle(anguloGiro, AnguloDeseadoGiro(dirMundo)));
                    return error <= toleranciaApuntado;
                }

                default:
                {
                    if (pivotElevacion == null)
                    {
                        // Una sola pieza que mira libremente al objetivo.
                        Transform padre = pivotGiro.parent;
                        Vector3 dirLocal = padre != null ? padre.InverseTransformDirection(dirMundo) : dirMundo;
                        Quaternion deseada = Quaternion.LookRotation(dirLocal, rotInicialMundoLibre * Vector3.up);
                        Quaternion antes = pivotGiro.localRotation;
                        pivotGiro.localRotation = Quaternion.RotateTowards(antes, deseada, velocidadGiro * dt);
                        VelocidadAngularActual = Quaternion.Angle(antes, pivotGiro.localRotation) / dt;
                    }
                    else
                    {
                        float deseado = AnguloDeseadoGiro(dirMundo);
                        if (limitarGiro) deseado = Mathf.Clamp(deseado, giroMinimo, giroMaximo);
                        GirarHacia(deseado, dt);
                        float elev = Mathf.Clamp(ElevacionDeseada(dirMundo), elevacionMinima, elevacionMaxima);
                        float antes = anguloElev;
                        anguloElev = Mathf.MoveTowards(anguloElev, elev, velocidadElevacion * dt);
                        AplicarElevacion();
                        VelocidadAngularActual = Mathf.Max(VelocidadAngularActual, Mathf.Abs(anguloElev - antes) / dt);
                    }
                    return Vector3.Angle(DireccionCanon, dirMundo) <= toleranciaApuntado;
                }
            }
        }

        void Reposar(float dt)
        {
            if (modo == ModoApuntado.Fija) return;

            if (vigilarSinObjetivo)
            {
                tiempoVigilancia += dt;
                float objetivoGiro = Mathf.Sin(tiempoVigilancia * velocidadVigilancia * Mathf.Deg2Rad * 2f) * anguloVigilancia;
                if (limitarGiro) objetivoGiro = Mathf.Clamp(objetivoGiro, giroMinimo, giroMaximo);
                if (modo == ModoApuntado.TodosLosAngulos && pivotElevacion == null)
                {
                    Quaternion q = Quaternion.AngleAxis(objetivoGiro, rotInicialGiro * ejeGiro.normalized) * rotInicialMundoLibre;
                    pivotGiro.localRotation = Quaternion.RotateTowards(pivotGiro.localRotation, q, velocidadGiro * dt);
                }
                else GirarHacia(objetivoGiro, dt, velocidadVigilancia * 2f);
            }
            else if (volverAReposo)
            {
                if (modo == ModoApuntado.TodosLosAngulos && pivotElevacion == null)
                {
                    Quaternion antes = pivotGiro.localRotation;
                    pivotGiro.localRotation = Quaternion.RotateTowards(antes, rotInicialMundoLibre, velocidadGiro * 0.5f * dt);
                    VelocidadAngularActual = Quaternion.Angle(antes, pivotGiro.localRotation) / dt;
                }
                else GirarHacia(0f, dt, velocidadGiro * 0.5f);
            }

            if (pivotElevacion != null && volverAReposo && modo == ModoApuntado.TodosLosAngulos)
            {
                anguloElev = Mathf.MoveTowards(anguloElev, 0f, velocidadElevacion * 0.5f * dt);
                AplicarElevacion();
            }
        }

        /// <summary>Eje de giro en el espacio del padre de pivotGiro.</summary>
        Vector3 EjeGiroEnPadre() => rotInicialGiro * (ejeGiro.sqrMagnitude > 0f ? ejeGiro.normalized : Vector3.up);

        Vector3 EjeGiroMundo()
        {
            Transform padre = pivotGiro != null ? pivotGiro.parent : null;
            Vector3 e = EjeGiroEnPadre();
            return padre != null ? padre.TransformDirection(e) : e;
        }

        float AnguloDeseadoGiro(Vector3 dirMundo)
        {
            Transform padre = pivotGiro.parent;
            Vector3 dirLocal = padre != null ? padre.InverseTransformDirection(dirMundo) : dirMundo;
            Vector3 frente = rotInicialGiro * Vector3.forward;
            return MatematicaTorreta.AnguloEnEje(frente, dirLocal, EjeGiroEnPadre());
        }

        void GirarHacia(float angulo, float dt, float velocidad = -1f)
        {
            if (velocidad < 0f) velocidad = velocidadGiro;
            float antes = anguloGiro;
            if (limitarGiro) anguloGiro = Mathf.MoveTowards(anguloGiro, angulo, velocidad * dt);
            else anguloGiro = MatematicaTorreta.NormalizarAngulo(Mathf.MoveTowardsAngle(anguloGiro, angulo, velocidad * dt));
            pivotGiro.localRotation = Quaternion.AngleAxis(anguloGiro, EjeGiroEnPadre()) * rotInicialGiro;
            VelocidadAngularActual = Mathf.Abs(Mathf.DeltaAngle(antes, anguloGiro)) / dt;
        }

        float ElevacionDeseada(Vector3 dirMundo)
        {
            Transform padre = pivotElevacion.parent;
            Vector3 dirLocal = padre != null ? padre.InverseTransformDirection(dirMundo) : dirMundo;
            Vector3 frente = rotInicialElev * Vector3.forward;
            Vector3 eje = rotInicialElev * Vector3.right;
            // Girar en +X baja el morro en Unity, por eso la elevación es el ángulo con signo cambiado.
            return -MatematicaTorreta.AnguloEnEje(frente, dirLocal, eje);
        }

        void AplicarElevacion()
        {
            pivotElevacion.localRotation = Quaternion.AngleAxis(-anguloElev, rotInicialElev * Vector3.right) * rotInicialElev;
        }

        // ------------------------------------------------------------------ Disparo
        void IntentarDisparar()
        {
            if (recargando || prefabProyectil == null) return;
            if (Time.time < proximoDisparo) return;

            EjecutarDisparo();

            disparosEnRafaga++;
            if (balasPorRafaga > 1 && disparosEnRafaga >= balasPorRafaga)
            {
                disparosEnRafaga = 0;
                proximoDisparo = Time.time + Mathf.Max(pausaEntreRafagas, 1f / cadencia);
            }
            else proximoDisparo = Time.time + 1f / cadencia;
        }

        void EjecutarDisparo()
        {
            if (prefabProyectil == null) return;
            int nCanones = puntosDisparo != null ? puntosDisparo.Length : 0;

            if (nCanones == 0)
            {
                Transform p = pivotElevacion != null ? pivotElevacion : pivotGiro;
                CrearBalas(p.position, p.forward);
                NotificarDisparo(0);
            }
            else if (modoCanones == ModoCanones.Simultaneo)
            {
                for (int i = 0; i < nCanones; i++)
                {
                    if (puntosDisparo[i] == null) continue;
                    CrearBalas(puntosDisparo[i].position, puntosDisparo[i].forward);
                    NotificarDisparo(i);
                }
            }
            else
            {
                int i = indiceCanon % nCanones;
                indiceCanon = (indiceCanon + 1) % nCanones;
                if (puntosDisparo[i] != null)
                {
                    CrearBalas(puntosDisparo[i].position, puntosDisparo[i].forward);
                    NotificarDisparo(i);
                }
            }

            if (!municionInfinita)
            {
                municion--;
                if (municion <= 0) EmpezarRecarga();
            }
        }

        void CrearBalas(Vector3 posicion, Vector3 direccion)
        {
            Vector3 g = Physics.gravity * gravedadProyectil;
            for (int k = 0; k < proyectilesPorDisparo; k++)
            {
                Vector3 dir = MatematicaTorreta.AplicarDispersion(direccion, dispersion);
                Proyectil bala = PoolProyectiles.Obtener(prefabProyectil, posicion, Quaternion.LookRotation(dir));
                if (bala != null) bala.Lanzar(dir * velocidadProyectil, danio, g, capasImpacto, transform);
            }
        }

        void NotificarDisparo(int indice)
        {
            alDisparar.Invoke(indice);
            Disparo?.Invoke(indice);
        }

        void EmpezarRecarga()
        {
            recargando = true;
            finRecarga = Time.time + tiempoRecarga;
            alEmpezarRecarga.Invoke();
            RecargaIniciada?.Invoke();
        }

        void ActualizarRecarga()
        {
            if (!recargando || Time.time < finRecarga) return;
            recargando = false;
            municion = tamanoCargador;
            disparosEnRafaga = 0;
            alTerminarRecarga.Invoke();
            RecargaTerminada?.Invoke();
        }

        // ------------------------------------------------------------------ Gizmos
        void OnDrawGizmosSelected()
        {
            Vector3 origen = OrigenApuntado;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireSphere(origen, alcance);
            if (alcanceMinimo > 0f) { Gizmos.color = new Color(1f, 0f, 0f, 0.4f); Gizmos.DrawWireSphere(origen, alcanceMinimo); }

            Vector3 frente = DireccionCanon;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origen, frente * alcance);

            if (modo == ModoApuntado.Fija)
            {
                // Cono de visión.
                Quaternion q = Quaternion.LookRotation(frente);
                for (int i = 0; i < 8; i++)
                {
                    Quaternion r = q * Quaternion.Euler(0f, 0f, i * 45f) * Quaternion.Euler(anguloVision, 0f, 0f);
                    Gizmos.DrawRay(origen, r * Vector3.forward * alcance);
                }
            }

            if (objetivo != null)
            {
                Gizmos.color = Alineada ? Color.red : Color.yellow;
                Gizmos.DrawLine(origen, PuntoApuntado);
                Gizmos.DrawWireSphere(PuntoApuntado, 0.3f);
            }
        }
    }
}
