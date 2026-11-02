using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Patrulla
{
    /// <summary>Qué hace el seguidor al llegar al final de la ruta.</summary>
    public enum ModoRecorrido
    {
        /// <summary>Repite sin fin. En rutas cerradas da vueltas; en abiertas vuelve al primer punto de golpe.</summary>
        Bucle,
        /// <summary>Al llegar al final da media vuelta y recorre la ruta al revés (patrulla clásica).</summary>
        IdaYVuelta,
        /// <summary>Recorre la ruta una sola vez y se para (o se destruye).</summary>
        UnaVez
    }

    /// <summary>Cómo se aplica el movimiento al objeto.</summary>
    public enum ModoMovimiento
    {
        /// <summary>Mueve el Transform directamente (lo más simple).</summary>
        Transform,
        /// <summary>Usa Rigidbody.MovePosition/MoveRotation en FixedUpdate (Rigidbody cinemático, empuja a otros cuerpos).</summary>
        RigidbodyCinematico
    }

    [System.Serializable] public class EventoPunto : UnityEvent<int> { }

    /// <summary>
    /// Hace que un objeto recorra una RutaDePuntos: patrullas (guardias, animales, drones) y tráfico
    /// (coches, peatones, trenes). Controla velocidad con aceleración y frenada, reduce en curvas,
    /// espera en puntos, respeta semáforos (puntos bloqueados) y guarda distancia con el de delante.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecanicas/Patrulla/Seguidor De Ruta")]
    public class SeguidorDeRuta : MonoBehaviour
    {
        [Header("Ruta")]
        public RutaDePuntos ruta;
        public ModoRecorrido modo = ModoRecorrido.Bucle;
        [Tooltip("Punto de la ruta donde empieza.")]
        [Min(0)] public int puntoInicial = 0;
        [Tooltip("Ignora 'Punto Inicial' y empieza en el lugar de la ruta más cercano a donde está el objeto.")]
        public bool empezarEnLugarMasCercano = false;
        [Tooltip("Recorre la ruta en sentido contrario (del último punto al primero).")]
        public bool sentidoInverso = false;
        public bool empezarAutomaticamente = true;
        [Tooltip("Al terminar en modo UnaVez, destruye el objeto (útil para coches generados).")]
        public bool destruirAlTerminar = false;

        [Header("Velocidad")]
        [Tooltip("Velocidad de crucero en m/s (1.5 = paseo, 4 = carrera, 14 = coche en ciudad).")]
        [Min(0f)] public float velocidad = 3f;
        [Tooltip("Metros por segundo que gana cada segundo al acelerar.")]
        [Min(0.01f)] public float aceleracion = 4f;
        [Tooltip("Metros por segundo que pierde cada segundo al frenar. También decide con cuánta antelación frena.")]
        [Min(0.01f)] public float frenada = 8f;
        [Tooltip("Multiplicador de velocidad que puedes cambiar desde código (alerta, cansancio...).")]
        [Min(0f)] public float multiplicadorVelocidad = 1f;

        [Header("Curvas")]
        [Tooltip("Frena antes de las curvas según lo cerradas que sean.")]
        public bool reducirEnCurvas = true;
        [Tooltip("Fracción de la velocidad que conserva en la curva más cerrada (0.3 = va al 30%).")]
        [Range(0.05f, 1f)] public float factorVelocidadEnCurva = 0.4f;
        [Tooltip("Ángulo (grados) a partir del cual se aplica la reducción completa.")]
        [Range(5f, 180f)] public float anguloCurvaCompleta = 90f;
        [Tooltip("Metros por delante en los que mira si viene una curva.")]
        [Min(0.1f)] public float distanciaAnticipacion = 4f;

        [Header("Rotación")]
        public bool rotar = true;
        [Tooltip("Solo gira sobre el eje vertical (Y). Desactívalo para que se incline en cuestas (drones, coches en rampas).")]
        public bool soloEjeY = true;
        [Tooltip("Grados por segundo. 0 = gira al instante (esquinas en pico se verán secas).")]
        [Min(0f)] public float velocidadGiro = 360f;
        [Tooltip("Mira a un punto de la ruta estos metros por delante. Suaviza el giro (como al conducir).")]
        [Min(0f)] public float adelantoMirada = 1f;

        [Header("Carril")]
        [Tooltip("Desplazamiento a la derecha de la ruta (metros). Negativo = izquierda. Al ir en sentido inverso cambia de lado solo.")]
        public float desplazamientoLateral = 0f;

        [Header("Suelo")]
        [Tooltip("Lanza un rayo hacia abajo y pega el objeto al suelo (terrenos con desnivel).")]
        public bool ajustarAlSuelo = false;
        public LayerMask capasSuelo = 1;
        [Tooltip("Altura sobre el suelo a la que queda el pivote.")]
        public float alturaSobreSuelo = 0f;
        [Min(0.1f)] public float alturaRayoSuelo = 5f;

        [Header("Tráfico: cola en la misma ruta")]
        [Tooltip("Guarda distancia con otros seguidores de la MISMA ruta, mismo sentido y mismo carril. Funciona también en curvas.")]
        public bool respetarColaEnRuta = true;
        [Tooltip("Distancia mínima (de pivote a pivote) con el de delante, en metros. Para coches: largo del coche + hueco.")]
        [Min(0f)] public float separacionMinima = 6f;

        [Header("Tráfico: obstáculos físicos")]
        [Tooltip("Lanza una esfera hacia delante y frena ante cualquier collider (jugador, coches de otras rutas, cajas...).")]
        public bool detectarObstaculos = false;
        public LayerMask capasObstaculos = ~0;
        [Min(0.1f)] public float distanciaDeteccion = 8f;
        [Min(0.01f)] public float radioDeteccion = 0.6f;
        [Tooltip("Altura del rayo sobre el pivote.")]
        public float alturaDeteccion = 0.75f;
        [Tooltip("Metros por delante del pivote donde empieza el rayo (por ejemplo, el morro del coche).")]
        public float inicioDeteccion = 1f;
        [Tooltip("Hueco que deja con el obstáculo al pararse.")]
        [Min(0f)] public float distanciaSeguridad = 1.5f;

        [Header("Puntos bloqueados (semáforos)")]
        [Tooltip("Metros antes de un punto bloqueado donde se detiene (línea de stop).")]
        [Min(0f)] public float distanciaParada = 2f;

        [Header("Movimiento")]
        public ModoMovimiento modoMovimiento = ModoMovimiento.Transform;
        [Tooltip("Rigidbody para el modo RigidbodyCinematico. Si está vacío lo busca en el objeto.")]
        public Rigidbody cuerpo;

        [Header("Animación (opcional)")]
        public Animator animator;
        [Tooltip("Nombre de un parámetro Float del Animator que recibe la velocidad actual (m/s). Vacío = no se usa.")]
        public string parametroVelocidad = "Velocidad";

        [Header("Eventos")]
        public EventoPunto alLlegarAPunto = new EventoPunto();
        public EventoPunto alEmpezarEspera = new EventoPunto();
        public EventoPunto alTerminarEspera = new EventoPunto();
        public UnityEvent alCompletarVuelta = new UnityEvent();
        public UnityEvent alTerminarRuta = new UnityEvent();

        // ---- Estado ----
        static readonly List<SeguidorDeRuta> activos = new List<SeguidorDeRuta>();
        static readonly RaycastHit[] impactos = new RaycastHit[16];

        float distancia;
        int sentido = 1;
        int siguiente;
        int puntoOrigen;
        float velocidadActual;
        float esperaRestante;
        int puntoEnEspera = -1;
        bool iniciado, terminado, pausado;
        int hashParametro;
        bool tieneParametro;

        /// <summary>Todos los seguidores activos (para consultas de tráfico).</summary>
        public static IReadOnlyList<SeguidorDeRuta> Activos => activos;

        /// <summary>Distancia recorrida a lo largo de la ruta (0..Longitud).</summary>
        public float Distancia => distancia;
        /// <summary>+1 si avanza en el orden de los puntos, -1 si va al revés.</summary>
        public int Sentido => sentido;
        /// <summary>Velocidad real en este momento (m/s).</summary>
        public float VelocidadActual => velocidadActual;
        /// <summary>Índice del próximo punto al que se dirige.</summary>
        public int SiguientePunto => siguiente;
        public bool Esperando => esperaRestante > 0f;
        public bool Terminado => terminado;
        public bool Pausado => pausado;

        /// <summary>Evento en C# para quien prefiera código: (seguidor, índice de punto).</summary>
        public event System.Action<SeguidorDeRuta, int> AlLlegarAPunto;

        // ---------------------------------------------------------------- API

        /// <summary>Coloca el seguidor en la ruta y empieza a moverse.</summary>
        public void Iniciar()
        {
            terminado = false;
            pausado = false;
            esperaRestante = 0f;
            velocidadActual = 0f;
            iniciado = false;
            if (ruta == null || !ruta.EsValida) return;

            int n = ruta.NumeroDePuntos;
            sentido = sentidoInverso ? -1 : 1;
            if (empezarEnLugarMasCercano)
            {
                distancia = ruta.DistanciaMasCercana(transform.position);
                siguiente = BuscarSiguientePunto(distancia);
                // En circuitos, el "origen" (donde se completa la vuelta) es el punto que queda justo detrás.
                puntoOrigen = EsCircuito() ? ((siguiente - sentido) % n + n) % n : ruta.PuntoMasCercano(transform.position);
            }
            else
            {
                puntoOrigen = Mathf.Clamp(puntoInicial, 0, n - 1);
                distancia = ruta.DistanciaDePunto(puntoOrigen);
                siguiente = IndiceSiguiente(puntoOrigen);
            }
            if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
            PrepararAnimator();
            iniciado = true;
            AplicarPose(0f, true);
        }

        /// <summary>Cambia de ruta en marcha y empieza en el punto indicado (o el más cercano si es -1).</summary>
        public void AsignarRuta(RutaDePuntos nuevaRuta, int punto = -1, bool inverso = false)
        {
            ruta = nuevaRuta;
            sentidoInverso = inverso;
            empezarEnLugarMasCercano = punto < 0;
            if (punto >= 0) puntoInicial = punto;
            Iniciar();
        }

        public void Pausar() { pausado = true; }
        public void Reanudar() { pausado = false; }

        /// <summary>Termina la espera actual y sigue andando.</summary>
        public void SaltarEspera() { if (esperaRestante > 0f) esperaRestante = 0.0001f; }

        /// <summary>Da media vuelta en el sitio.</summary>
        public void InvertirSentido()
        {
            if (!iniciado) return;
            sentido = -sentido;
            siguiente = BuscarSiguientePunto(distancia);
        }

        // ---------------------------------------------------------------- Ciclo de vida

        void OnEnable() { if (!activos.Contains(this)) activos.Add(this); }
        void OnDisable() { activos.Remove(this); }

        void Start()
        {
            if (empezarAutomaticamente && !iniciado) Iniciar();
        }

        void Update()
        {
            if (modoMovimiento == ModoMovimiento.Transform) Paso(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (modoMovimiento == ModoMovimiento.RigidbodyCinematico) Paso(Time.fixedDeltaTime);
        }

        // ---------------------------------------------------------------- Lógica

        void Paso(float dt)
        {
            if (!iniciado || terminado || ruta == null || !ruta.EsValida || dt <= 0f) return;

            if (pausado)
            {
                velocidadActual = Mathf.MoveTowards(velocidadActual, 0f, frenada * dt);
                AvanzarSinLlegar(velocidadActual * dt);
                AplicarPose(dt, false);
                return;
            }

            if (esperaRestante > 0f)
            {
                velocidadActual = 0f;
                esperaRestante -= dt;
                if (esperaRestante <= 0f)
                {
                    esperaRestante = 0f;
                    int p = puntoEnEspera;
                    puntoEnEspera = -1;
                    alTerminarEspera.Invoke(p);
                }
                AplicarPose(dt, false);
                return;
            }

            float restante = RestanteHasta(siguiente);
            float objetivo = VelocidadObjetivo(restante, out bool bloqueadoDelante);

            float ritmo = objetivo > velocidadActual ? aceleracion : frenada;
            velocidadActual = Mathf.MoveTowards(velocidadActual, objetivo, ritmo * dt);
            float paso = velocidadActual * dt;

            if (bloqueadoDelante)
            {
                paso = Mathf.Min(paso, Mathf.Max(0f, restante - distanciaParada));
                AvanzarSinLlegar(paso);
            }
            else if (paso >= restante)
            {
                distancia = ruta.DistanciaDePunto(siguiente);
                Llegar(siguiente);
            }
            else
            {
                AvanzarSinLlegar(paso);
            }

            if (!terminado) AplicarPose(dt, false);
        }

        float VelocidadObjetivo(float restante, out bool bloqueadoDelante)
        {
            bloqueadoDelante = false;
            float objetivo = velocidad * multiplicadorVelocidad;
            if (objetivo <= 0f) return 0f;

            // 1) Curvas: compara la dirección actual con la de unos metros más adelante.
            if (reducirEnCurvas)
            {
                Vector3 ahora = ruta.DireccionEn(distancia);
                Vector3 luego = ruta.DireccionEn(distancia + sentido * distanciaAnticipacion);
                float angulo = Vector3.Angle(ahora, luego);
                float f = Mathf.Lerp(1f, factorVelocidadEnCurva, Mathf.Clamp01(angulo / anguloCurvaCompleta));
                objetivo *= f;
            }

            // 2) Ajustes del próximo punto.
            var ajustes = ruta.ObtenerAjustes(siguiente);
            if (ajustes != null)
            {
                if (ajustes.velocidadMaxima > 0f)
                    objetivo = Mathf.Min(objetivo, VelocidadParaLlegarA(restante, ajustes.velocidadMaxima));

                // Si ya ha pasado la línea de stop cuando se pone en rojo, sigue (como en la vida real).
                if (ajustes.EstaBloqueadoPara(sentido) && restante > distanciaParada - 0.05f)
                {
                    bloqueadoDelante = true;
                    objetivo = Mathf.Min(objetivo, VelocidadParaLlegarA(Mathf.Max(0f, restante - distanciaParada), 0f));
                }
                else if (ajustes.espera > 0f)
                {
                    objetivo = Mathf.Min(objetivo, VelocidadParaLlegarA(restante, 0.5f));
                }
            }

            // 3) Final de la ruta en modo UnaVez (o media vuelta en IdaYVuelta): llega frenando.
            if (modo != ModoRecorrido.Bucle && EsExtremo(siguiente))
                objetivo = Mathf.Min(objetivo, VelocidadParaLlegarA(restante, 0.5f));

            // 4) Cola con otros seguidores de la misma ruta.
            if (respetarColaEnRuta)
            {
                float hueco = HuecoConElDeDelante();
                if (hueco < float.MaxValue)
                    objetivo = Mathf.Min(objetivo, VelocidadParaLlegarA(Mathf.Max(0f, hueco - separacionMinima), 0f));
            }

            // 5) Obstáculos físicos.
            if (detectarObstaculos)
            {
                float d = DistanciaAObstaculo();
                if (d < float.MaxValue)
                    objetivo = Mathf.Min(objetivo, VelocidadParaLlegarA(Mathf.Max(0f, d - distanciaSeguridad), 0f));
            }

            return Mathf.Max(0f, objetivo);
        }

        /// <summary>Velocidad máxima a la que puede ir para, frenando con 'frenada', llegar a 'metros' yendo a 'velocidadFinal'.</summary>
        float VelocidadParaLlegarA(float metros, float velocidadFinal)
        {
            return Mathf.Sqrt(velocidadFinal * velocidadFinal + 2f * frenada * Mathf.Max(0f, metros));
        }

        void AvanzarSinLlegar(float paso)
        {
            if (paso <= 0f) return;
            float restante = RestanteHasta(siguiente);
            paso = Mathf.Min(paso, Mathf.Max(0f, restante - 0.0001f));
            distancia = ruta.NormalizarDistancia(distancia + sentido * paso);
        }

        void Llegar(int punto)
        {
            alLlegarAPunto.Invoke(punto);
            AlLlegarAPunto?.Invoke(this, punto);

            if (EsExtremo(punto))
            {
                switch (modo)
                {
                    case ModoRecorrido.UnaVez:
                        terminado = true;
                        velocidadActual = 0f;
                        ActualizarAnimator();
                        alTerminarRuta.Invoke();
                        if (destruirAlTerminar) Destroy(gameObject);
                        return;

                    case ModoRecorrido.IdaYVuelta:
                        sentido = -sentido;
                        alCompletarVuelta.Invoke();
                        break;

                    case ModoRecorrido.Bucle:
                        alCompletarVuelta.Invoke();
                        if (!ruta.cerrada)
                        {
                            // Ruta abierta en bucle: vuelve de golpe al principio.
                            punto = sentido > 0 ? 0 : ruta.NumeroDePuntos - 1;
                            distancia = ruta.DistanciaDePunto(punto);
                            AplicarPose(0f, true);
                        }
                        break;
                }
            }

            var ajustes = ruta.ObtenerAjustes(punto);
            float espera = ajustes != null ? ajustes.CalcularEspera() : 0f;
            if (espera > 0f)
            {
                esperaRestante = espera;
                puntoEnEspera = punto;
                velocidadActual = 0f;
                alEmpezarEspera.Invoke(punto);
            }

            siguiente = IndiceSiguiente(punto);
        }

        bool EsExtremo(int punto)
        {
            int n = ruta.NumeroDePuntos;
            if (EsCircuito()) return punto == puntoOrigen;
            return (sentido > 0 && punto == n - 1) || (sentido < 0 && punto == 0);
        }

        bool EsCircuito() => ruta.cerrada && ruta.NumeroDePuntos >= 3;

        int IndiceSiguiente(int punto)
        {
            int n = ruta.NumeroDePuntos;
            if (EsCircuito()) return ((punto + sentido) % n + n) % n;
            return Mathf.Clamp(punto + sentido, 0, n - 1);
        }

        float RestanteHasta(int punto)
        {
            float objetivo = ruta.DistanciaDePunto(punto);
            if (EsCircuito()) return Mathf.Repeat(sentido * (objetivo - distancia), ruta.Longitud);
            return Mathf.Max(0f, sentido * (objetivo - distancia));
        }

        int BuscarSiguientePunto(float d)
        {
            int n = ruta.NumeroDePuntos;
            int mejor = sentido > 0 ? n - 1 : 0;
            float mejorRestante = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                float objetivo = ruta.DistanciaDePunto(i);
                float r = EsCircuito() ? Mathf.Repeat(sentido * (objetivo - d), ruta.Longitud) : sentido * (objetivo - d);
                if (r > 0.001f && r < mejorRestante) { mejorRestante = r; mejor = i; }
            }
            return mejor;
        }

        float HuecoConElDeDelante()
        {
            float mejor = float.MaxValue;
            float L = ruta.Longitud;
            bool circuito = EsCircuito();
            for (int i = 0; i < activos.Count; i++)
            {
                var otro = activos[i];
                if (otro == this || otro.ruta != ruta || !otro.iniciado || otro.terminado) continue;
                if (otro.sentido != sentido) continue;
                if (Mathf.Abs(otro.desplazamientoLateral - desplazamientoLateral) > 1f) continue;
                float delta = sentido * (otro.distancia - distancia);
                if (circuito) delta = Mathf.Repeat(delta, L);
                if (delta > 0.001f && delta < mejor) mejor = delta;
            }
            return mejor;
        }

        float DistanciaAObstaculo()
        {
            Vector3 adelante = transform.forward;
            Vector3 origen = transform.position + transform.up * alturaDeteccion + adelante * inicioDeteccion;
            int n = Physics.SphereCastNonAlloc(origen, radioDeteccion, adelante, impactos, distanciaDeteccion,
                                               capasObstaculos, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = impactos[i];
                if (h.distance <= 0f) continue; // ya solapado al empezar el rayo (p. ej. el suelo o uno mismo)
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (h.distance < mejor) mejor = h.distance;
            }
            return mejor;
        }

        // ---------------------------------------------------------------- Pose

        void AplicarPose(float dt, bool instantaneo)
        {
            ruta.Evaluar(distancia, out Vector3 pos, out Vector3 dir);
            Vector3 avance = dir * sentido;
            pos += Lateral(avance);
            Vector3 posRuta = pos; // antes de pegarlo al suelo

            if (ajustarAlSuelo &&
                Physics.Raycast(pos + Vector3.up * alturaRayoSuelo, Vector3.down, out RaycastHit suelo,
                                alturaRayoSuelo * 2f, capasSuelo, QueryTriggerInteraction.Ignore))
            {
                pos.y = suelo.point.y + alturaSobreSuelo;
            }

            Quaternion rot = transform.rotation;
            if (rotar)
            {
                Vector3 mirar = avance;
                if (puntoEnEspera >= 0 && esperaRestante > 0f)
                {
                    var aj = ruta.ObtenerAjustes(puntoEnEspera);
                    if (aj != null && aj.mirarEnDireccionDelPunto) mirar = aj.transform.forward;
                }
                else if (adelantoMirada > 0f)
                {
                    float dAdelante = distancia + sentido * adelantoMirada;
                    ruta.Evaluar(dAdelante, out Vector3 posAdelante, out Vector3 dirAdelante);
                    Vector3 v = posAdelante + Lateral(dirAdelante * sentido) - posRuta;
                    if (v.sqrMagnitude > 0.0004f) mirar = v;
                }
                if (soloEjeY) mirar.y = 0f;
                if (mirar.sqrMagnitude > 1e-6f)
                {
                    Quaternion objetivo = Quaternion.LookRotation(mirar.normalized, Vector3.up);
                    rot = (instantaneo || velocidadGiro <= 0f)
                        ? objetivo
                        : Quaternion.RotateTowards(transform.rotation, objetivo, velocidadGiro * dt);
                }
            }

            if (modoMovimiento == ModoMovimiento.RigidbodyCinematico && cuerpo != null && !instantaneo)
            {
                cuerpo.MovePosition(pos);
                cuerpo.MoveRotation(rot);
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }

            ActualizarAnimator();
        }

        Vector3 Lateral(Vector3 avance)
        {
            if (Mathf.Approximately(desplazamientoLateral, 0f)) return Vector3.zero;
            Vector3 derecha = Vector3.Cross(Vector3.up, avance);
            if (derecha.sqrMagnitude < 1e-6f) return Vector3.zero;
            return derecha.normalized * desplazamientoLateral;
        }

        void PrepararAnimator()
        {
            tieneParametro = false;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || string.IsNullOrEmpty(parametroVelocidad)) return;
            hashParametro = Animator.StringToHash(parametroVelocidad);
            foreach (var p in animator.parameters)
                if (p.nameHash == hashParametro && p.type == AnimatorControllerParameterType.Float) tieneParametro = true;
        }

        void ActualizarAnimator()
        {
            if (tieneParametro && animator != null) animator.SetFloat(hashParametro, velocidadActual);
        }

        void OnDrawGizmosSelected()
        {
            if (!detectarObstaculos) return;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Vector3 o = transform.position + transform.up * alturaDeteccion + transform.forward * inicioDeteccion;
            Gizmos.DrawWireSphere(o, radioDeteccion);
            Gizmos.DrawWireSphere(o + transform.forward * distanciaDeteccion, radioDeteccion);
            Gizmos.DrawLine(o, o + transform.forward * distanciaDeteccion);
        }
    }
}
