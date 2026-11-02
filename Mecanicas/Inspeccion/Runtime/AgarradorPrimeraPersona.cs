using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Inspeccion
{
    /// <summary>Qué hace el agarrador en este momento.</summary>
    public enum EstadoAgarre
    {
        /// <summary>Manos vacías: busca objetos con la mirada.</summary>
        Libre,
        /// <summary>Sujeta un objeto delante de la cámara (con física).</summary>
        Sujetando,
        /// <summary>Inspecciona un objeto: la cámara se bloquea y el ratón lo rota.</summary>
        Inspeccionando,
        /// <summary>Devuelve a su sitio un objeto que se inspeccionó sin cogerlo.</summary>
        Devolviendo
    }

    /// <summary>Qué pasa al terminar de inspeccionar un objeto que NO estaba en la mano.</summary>
    public enum FinInspeccionSinAgarrar
    {
        /// <summary>El objeto vuelve suavemente a donde estaba (estilo Resident Evil / juegos de misterio).</summary>
        VolverASuSitio,
        /// <summary>El objeto se queda en la mano, como si lo hubieras cogido.</summary>
        QuedarseEnLaMano,
        /// <summary>El objeto se suelta delante de la cámara y cae.</summary>
        SoltarDelante
    }

    /// <summary>Evento de Unity que recibe el objeto afectado.</summary>
    [Serializable] public class EventoObjetoAgarrable : UnityEvent<ObjetoAgarrable> { }

    /// <summary>
    /// Agarre, lanzamiento e inspección de objetos en primera persona.
    ///
    /// Ponlo en la cámara del jugador (o en un hijo de la cámara) junto con <see cref="EntradaInspeccion"/>.
    /// - Libre: mira un <see cref="ObjetoAgarrable"/> y pulsa Agarrar para cogerlo o Inspeccionar para mirarlo de cerca.
    /// - Sujetando: el objeto flota delante de la cámara con física (choca con paredes y empuja cosas).
    ///   Agarrar lo suelta; mantener Lanzar carga la fuerza y al soltar se lanza; Inspeccionar lo examina.
    /// - Inspeccionando: la cámara se queda quieta, el ratón rota el objeto y la rueda acerca o aleja.
    ///   Solo en este estado se puede rotar el objeto.
    /// </summary>
    [AddComponentMenu("Mecánicas/Inspección/Agarrador Primera Persona")]
    public class AgarradorPrimeraPersona : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Cámara del jugador. Si está vacía se busca en este objeto, en sus padres o se usa Camera.main.")]
        public Camera camara;
        [Tooltip("Lector de controles. Si está vacío se busca en este objeto o se añade uno.")]
        public EntradaInspeccion entrada;
        [Tooltip("Raíz del jugador. Sus colliders no chocarán con el objeto sujeto. Si está vacía se usa transform.root.")]
        public Transform raizJugador;

        [Header("Detección")]
        [Tooltip("Distancia máxima a la que se puede coger un objeto (metros).")]
        public float alcance = 2.5f;
        [Tooltip("Radio del rayo de detección. Un poco de radio hace más fácil apuntar a objetos pequeños.")]
        public float radioDeteccion = 0.05f;
        public LayerMask capasDetectables = ~0;
        public QueryTriggerInteraction detectarTriggers = QueryTriggerInteraction.Ignore;

        [Header("Sujetar")]
        [Tooltip("Distancia a la que se sujeta el objeto delante de la cámara.")]
        public float distanciaSujecion = 1.3f;
        [Tooltip("Desplazamiento del punto de sujeción respecto a la cámara (x derecha, y arriba).")]
        public Vector3 desplazamientoSujecion = new Vector3(0f, -0.15f, 0f);
        [Tooltip("Rapidez con la que el objeto sigue al punto de sujeción.")]
        public float rapidezSeguimiento = 14f;
        [Tooltip("Velocidad máxima del objeto mientras se sujeta (m/s).")]
        public float velocidadMaximaSujeto = 18f;
        [Tooltip("Rapidez con la que el objeto mantiene su orientación respecto al jugador.")]
        public float rapidezGiro = 12f;
        [Tooltip("Si el objeto queda a más de esta distancia del punto de sujeción (atascado tras una pared), se suelta.")]
        public float distanciaRotura = 1.2f;
        [Tooltip("Masa máxima (kg) que se puede coger.")]
        public float masaMaxima = 30f;
        [Tooltip("Velocidad máxima que conserva el objeto al soltarlo (evita que salga disparado).")]
        public float velocidadMaximaAlSoltar = 4f;
        [Tooltip("Si el objeto era cinemático (por ejemplo, quieto en una estantería), al soltarlo pasa a tener física normal y cae.")]
        public bool volverseFisicoAlSoltar = true;
        [Tooltip("Evita que el objeto sujeto choque con el jugador (así no te empuja ni puedes subirte encima).")]
        public bool ignorarColisionConJugador = true;

        [Header("Lanzar")]
        [Tooltip("Fuerza del lanzamiento sin cargar.")]
        public float fuerzaMinima = 3f;
        [Tooltip("Fuerza del lanzamiento con la carga al máximo.")]
        public float fuerzaMaxima = 14f;
        [Tooltip("Segundos manteniendo Lanzar para llegar a la fuerza máxima. 0 = lanza al máximo al pulsar.")]
        public float tiempoCargaMaxima = 0.8f;
        [Tooltip("Impulse: los objetos pesados vuelan menos. VelocityChange: todos salen igual de rápido.")]
        public ForceMode modoFuerza = ForceMode.Impulse;
        [Tooltip("Giro aleatorio (rad/s) que se le da al objeto al lanzarlo para que parezca natural.")]
        public float giroAleatorioAlLanzar = 2f;

        [Header("Inspeccionar")]
        [Tooltip("Distancia mínima de inspección. Los objetos grandes se colocan más lejos automáticamente.")]
        public float distanciaInspeccion = 0.55f;
        [Tooltip("Para objetos grandes: distancia = radio del objeto × este factor.")]
        public float factorDistanciaPorTamano = 2.2f;
        [Tooltip("Desplazamiento del centro de inspección respecto al centro de la pantalla.")]
        public Vector3 desplazamientoInspeccion = Vector3.zero;
        [Tooltip("Cuánto se puede acercar (multiplica la distancia).")]
        public float zoomMinimo = 0.6f;
        [Tooltip("Cuánto se puede alejar (multiplica la distancia).")]
        public float zoomMaximo = 1.6f;
        public float velocidadZoom = 1f;
        [Tooltip("Multiplicador de la rotación con el ratón o el stick.")]
        public float velocidadRotacion = 1f;
        public bool invertirX;
        public bool invertirY;
        [Tooltip("Si está activo, solo se rota manteniendo la tecla Rotar (clic derecho por defecto).")]
        public bool rotarSoloConBoton;
        [Tooltip("Suavizado de los movimientos de inspección (más alto = más rápido).")]
        public float suavizado = 14f;
        [Tooltip("Permite inspeccionar objetos sin cogerlos antes (mirándolos y pulsando Inspeccionar).")]
        public bool permitirInspeccionSinAgarrar = true;
        public FinInspeccionSinAgarrar alTerminarSinAgarrar = FinInspeccionSinAgarrar.VolverASuSitio;
        [Tooltip("Componentes que se desactivan mientras se inspecciona (tu controlador de jugador, el script de la cámara...).")]
        public Behaviour[] componentesABloquear;
        [Tooltip("Muestra el cursor del ratón mientras se inspecciona.")]
        public bool mostrarCursorAlInspeccionar;

        [Header("Eventos")]
        public EventoObjetoAgarrable alCambiarObjetivo;
        public EventoObjetoAgarrable alAgarrar;
        public EventoObjetoAgarrable alSoltar;
        public EventoObjetoAgarrable alLanzar;
        public EventoObjetoAgarrable alEmpezarInspeccion;
        public EventoObjetoAgarrable alTerminarInspeccion;

        /// <summary>Estado actual.</summary>
        public EstadoAgarre Estado { get; private set; } = EstadoAgarre.Libre;
        /// <summary>Objeto al que apunta la mirada (solo en estado Libre).</summary>
        public ObjetoAgarrable Objetivo { get; private set; }
        /// <summary>Objeto en la mano, inspeccionado o volviendo a su sitio.</summary>
        public ObjetoAgarrable Sujeto { get; private set; }
        /// <summary>Carga del lanzamiento entre 0 y 1.</summary>
        public float CargaLanzamiento { get; private set; }
        /// <summary>True mientras se mantiene Lanzar.</summary>
        public bool Cargando { get; private set; }
        /// <summary>True si la inspección actual empezó con el objeto en la mano.</summary>
        public bool InspeccionDesdeMano { get; private set; }

        /// <summary>True si algún agarrador está inspeccionando. Útil para pausar otros sistemas.</summary>
        public static bool HayInspeccionActiva => _inspeccionesActivas > 0;
        static int _inspeccionesActivas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstatico() => _inspeccionesActivas = 0;

        // Física guardada del objeto para restaurarla al soltarlo.
        bool _fisicaGuardada;
        bool _gravedadOriginal, _cinematicoOriginal;
        RigidbodyInterpolation _interpolacionOriginal;
        CollisionDetectionMode _deteccionOriginal;
        float _maxVelAngularOriginal;
        bool[] _collidersActivos;

        Quaternion _rotacionRelativa = Quaternion.identity;
        Vector3 _posicionOriginal;
        Quaternion _rotacionOriginal;
        Quaternion _rotacionInspeccion;
        Vector3 _centroLocal;
        float _distanciaBase, _distanciaActual;
        float _inicioCarga;
        bool[] _bloqueadosActivos;
        bool _cursorVisibleAntes;
        CursorLockMode _cursorBloqueoAntes;

        Collider[] _collidersJugador = new Collider[0];
        readonly List<ColisionPendiente> _colisionesPendientes = new List<ColisionPendiente>();

        struct ColisionPendiente
        {
            public ObjetoAgarrable objeto;
            public float limite;
        }

        // ───────────────────────────── Ciclo de vida ─────────────────────────────

        void Awake()
        {
            if (camara == null) camara = GetComponent<Camera>();
            if (camara == null) camara = GetComponentInParent<Camera>();
            if (camara == null) camara = Camera.main;
            if (entrada == null) entrada = GetComponent<EntradaInspeccion>();
            if (entrada == null) entrada = gameObject.AddComponent<EntradaInspeccion>();
            if (raizJugador == null) raizJugador = transform.root;
            RecargarCollidersJugador();
        }

        void OnDisable()
        {
            if (Estado == EstadoAgarre.Inspeccionando) TerminarInspeccion();
            if (Estado == EstadoAgarre.Devolviendo) FinalizarDevolucion();
            Soltar();
            CambiarObjetivo(null);
        }

        /// <summary>Vuelve a leer los colliders del jugador (llámalo si cambias de cuerpo o añades colliders).</summary>
        public void RecargarCollidersJugador()
        {
            _collidersJugador = raizJugador != null ? raizJugador.GetComponentsInChildren<Collider>(true) : new Collider[0];
        }

        void Update()
        {
            if (camara == null || entrada == null) return;
            ComprobarObjetoPerdido();
            RestaurarColisionesPendientes();

            switch (Estado)
            {
                case EstadoAgarre.Libre: ActualizarLibre(); break;
                case EstadoAgarre.Sujetando: ActualizarSujetando(); break;
                case EstadoAgarre.Inspeccionando: ActualizarInspeccionEntrada(); break;
            }
        }

        void FixedUpdate()
        {
            if (Estado == EstadoAgarre.Sujetando && Sujeto != null) MoverSujeto();
        }

        void LateUpdate()
        {
            if (camara == null || Sujeto == null) return;
            if (Estado == EstadoAgarre.Inspeccionando) ColocarInspeccion();
            else if (Estado == EstadoAgarre.Devolviendo) ActualizarDevolucion();
        }

        // ───────────────────────────── Estados ─────────────────────────────

        void ActualizarLibre()
        {
            CambiarObjetivo(BuscarObjetivo());
            if (Objetivo == null) return;

            if (entrada.AgarrarPulsado)
            {
                if (PuedeAgarrar(Objetivo)) Agarrar(Objetivo);
                else if (Objetivo.sePuedeInspeccionar && permitirInspeccionSinAgarrar && !Objetivo.PuedeAgarrarse)
                    EmpezarInspeccion(Objetivo);
            }
            else if (entrada.InspeccionarPulsado && Objetivo.sePuedeInspeccionar && permitirInspeccionSinAgarrar)
            {
                EmpezarInspeccion(Objetivo);
            }
        }

        void ActualizarSujetando()
        {
            if (entrada.AgarrarPulsado) { Soltar(); return; }
            if (entrada.InspeccionarPulsado && Sujeto.sePuedeInspeccionar) { Cargando = false; CargaLanzamiento = 0f; EmpezarInspeccion(Sujeto); return; }
            if (!Sujeto.sePuedeLanzar) return;

            if (entrada.LanzarPulsado) { Cargando = true; _inicioCarga = Time.time; }
            if (Cargando)
            {
                CargaLanzamiento = tiempoCargaMaxima > 0f ? Mathf.Clamp01((Time.time - _inicioCarga) / tiempoCargaMaxima) : 1f;
                if (entrada.LanzarSoltado || !entrada.LanzarMantenido) Lanzar(CargaLanzamiento);
            }
        }

        void ActualizarInspeccionEntrada()
        {
            if (entrada.InspeccionarPulsado || entrada.SalirPulsado) { TerminarInspeccion(); return; }

            if (!rotarSoloConBoton || entrada.RotarMantenido)
            {
                Vector2 giro = entrada.Rotacion * velocidadRotacion;
                if (invertirX) giro.x = -giro.x;
                if (invertirY) giro.y = -giro.y;
                Transform cam = camara.transform;
                if (Sujeto.rotarHorizontal) _rotacionInspeccion = Quaternion.AngleAxis(-giro.x, cam.up) * _rotacionInspeccion;
                if (Sujeto.rotarVertical) _rotacionInspeccion = Quaternion.AngleAxis(giro.y, cam.right) * _rotacionInspeccion;
            }

            if (Mathf.Abs(entrada.Zoom) > 0f)
            {
                _distanciaActual -= entrada.Zoom * velocidadZoom * _distanciaBase;
                _distanciaActual = Mathf.Clamp(_distanciaActual, _distanciaBase * zoomMinimo, _distanciaBase * zoomMaximo);
            }
        }

        // ───────────────────────────── Detección ─────────────────────────────

        ObjetoAgarrable BuscarObjetivo()
        {
            Transform cam = camara.transform;
            var rayo = new Ray(cam.position, cam.forward);
            RaycastHit golpe;
            bool hay = radioDeteccion > 0f
                ? Physics.SphereCast(rayo, radioDeteccion, out golpe, alcance, capasDetectables, detectarTriggers)
                : Physics.Raycast(rayo, out golpe, alcance, capasDetectables, detectarTriggers);
            if (!hay) return null;

            var obj = golpe.collider.GetComponentInParent<ObjetoAgarrable>();
            if (obj == null || !obj.isActiveAndEnabled) return null;
            if (obj.EstaSujeto && obj.Portador != this) return null;
            if (!obj.sePuedeAgarrar && !obj.sePuedeInspeccionar) return null;
            return obj;
        }

        void CambiarObjetivo(ObjetoAgarrable nuevo)
        {
            if (nuevo == Objetivo) return;
            if (Objetivo != null)
            {
                Objetivo.Resaltar(false);
                Objetivo.alDejarDeApuntar?.Invoke();
            }
            Objetivo = nuevo;
            if (Objetivo != null)
            {
                Objetivo.Resaltar(true);
                Objetivo.alApuntar?.Invoke();
            }
            alCambiarObjetivo?.Invoke(Objetivo);
        }

        /// <summary>True si este agarrador puede coger el objeto (tiene Rigidbody, está permitido y no pesa demasiado).</summary>
        public bool PuedeAgarrar(ObjetoAgarrable obj)
        {
            return obj != null && obj.PuedeAgarrarse && obj.Cuerpo.mass <= masaMaxima
                   && (!obj.EstaSujeto || obj.Portador == this);
        }

        // ───────────────────────────── Agarrar y soltar ─────────────────────────────

        /// <summary>Coge un objeto (también se puede llamar desde otros scripts, por ejemplo un inventario).</summary>
        public bool Agarrar(ObjetoAgarrable obj)
        {
            if (Estado != EstadoAgarre.Libre || !PuedeAgarrar(obj)) return false;
            CambiarObjetivo(null);

            Sujeto = obj;
            obj.Portador = this;
            GuardarFisica(obj);
            PrepararParaSujetar(obj.Cuerpo);
            _rotacionRelativa = Quaternion.Inverse(GiroHorizontalCamara()) * obj.Cuerpo.rotation;
            IgnorarColisionJugador(obj, true);

            Estado = EstadoAgarre.Sujetando;
            Cargando = false;
            CargaLanzamiento = 0f;
            obj.Sonar(obj.sonidoAgarrar);
            obj.alAgarrar?.Invoke();
            alAgarrar?.Invoke(obj);
            return true;
        }

        /// <summary>Suelta el objeto que hay en la mano (si se está inspeccionando, termina la inspección soltándolo).</summary>
        public void Soltar()
        {
            if (Sujeto == null || Estado == EstadoAgarre.Libre) return;
            if (Estado == EstadoAgarre.Devolviendo) { FinalizarDevolucion(); return; }
            if (Estado == EstadoAgarre.Inspeccionando) SalirDeInspeccionSinEventosDeMano();

            var obj = Sujeto;
            var rb = obj.Cuerpo;
            LiberarObjeto(obj);
            if (rb != null && !rb.isKinematic)
                AsignarVelocidad(rb, Vector3.ClampMagnitude(LeerVelocidad(rb), velocidadMaximaAlSoltar));

            obj.Sonar(obj.sonidoSoltar);
            obj.alSoltar?.Invoke();
            alSoltar?.Invoke(obj);
        }

        /// <summary>Lanza el objeto de la mano. carga = 0 (fuerza mínima) a 1 (fuerza máxima).</summary>
        public void Lanzar(float carga)
        {
            Cargando = false;
            CargaLanzamiento = 0f;
            if (Estado != EstadoAgarre.Sujetando || Sujeto == null || !Sujeto.sePuedeLanzar) return;

            var obj = Sujeto;
            var rb = obj.Cuerpo;
            LiberarObjeto(obj);
            if (rb == null || rb.isKinematic) return;

            float fuerza = Mathf.Lerp(fuerzaMinima, fuerzaMaxima, Mathf.Clamp01(carga)) * obj.multiplicadorLanzamiento;
            AsignarVelocidad(rb, Vector3.zero);
            rb.AddForce(camara.transform.forward * fuerza, modoFuerza);
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * giroAleatorioAlLanzar;

            obj.Sonar(obj.sonidoLanzar);
            obj.alLanzar?.Invoke();
            alLanzar?.Invoke(obj);
        }

        void MoverSujeto()
        {
            var rb = Sujeto.Cuerpo;
            if (rb == null) { Soltar(); return; }

            Transform cam = camara.transform;
            float distancia = Sujeto.distanciaSujecion > 0f ? Sujeto.distanciaSujecion : distanciaSujecion;
            Vector3 destino = cam.position + cam.rotation * desplazamientoSujecion + cam.forward * distancia;
            Vector3 delta = destino - rb.worldCenterOfMass;

            if (delta.magnitude > distanciaRotura) { Soltar(); return; }

            AsignarVelocidad(rb, Vector3.ClampMagnitude(delta * rapidezSeguimiento, velocidadMaximaSujeto));

            Quaternion rotacionDeseada = GiroHorizontalCamara() * _rotacionRelativa;
            Quaternion diferencia = rotacionDeseada * Quaternion.Inverse(rb.rotation);
            diferencia.ToAngleAxis(out float angulo, out Vector3 eje);
            if (angulo > 180f) angulo -= 360f;
            if (Mathf.Abs(angulo) > 0.01f && !float.IsInfinity(eje.x) && !float.IsNaN(eje.x))
                rb.angularVelocity = eje.normalized * (angulo * Mathf.Deg2Rad * rapidezGiro);
            else
                rb.angularVelocity = Vector3.zero;
        }

        // ───────────────────────────── Inspección ─────────────────────────────

        /// <summary>
        /// Empieza a inspeccionar un objeto. Si es el que tienes en la mano, al terminar vuelve a la mano;
        /// si no, al terminar hace lo que diga <see cref="alTerminarSinAgarrar"/>.
        /// </summary>
        public bool EmpezarInspeccion(ObjetoAgarrable obj)
        {
            if (obj == null || !obj.sePuedeInspeccionar) return false;
            if (obj.EstaSujeto && obj.Portador != this) return false;

            if (Estado == EstadoAgarre.Sujetando && obj == Sujeto)
            {
                InspeccionDesdeMano = true;
            }
            else if (Estado == EstadoAgarre.Libre)
            {
                CambiarObjetivo(null);
                InspeccionDesdeMano = false;
                Sujeto = obj;
                obj.Portador = this;
                _posicionOriginal = obj.transform.position;
                _rotacionOriginal = obj.transform.rotation;
                GuardarFisica(obj);
            }
            else return false;

            var rb = obj.Cuerpo;
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    AsignarVelocidad(rb, Vector3.zero);
                    rb.angularVelocity = Vector3.zero;
                }
                // Los cuerpos cinemáticos no admiten la detección continua "dinámica".
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
            }
            ActivarColliders(obj, false);

            Bounds limites = obj.ObtenerLimites();
            _centroLocal = obj.transform.InverseTransformPoint(limites.center);
            float radio = limites.extents.magnitude;
            _distanciaBase = obj.distanciaInspeccion > 0f
                ? obj.distanciaInspeccion
                : Mathf.Max(distanciaInspeccion, radio * factorDistanciaPorTamano);
            _distanciaBase = Mathf.Max(_distanciaBase, camara.nearClipPlane + radio + 0.02f);
            _distanciaActual = _distanciaBase;

            _rotacionInspeccion = obj.usarRotacionInicialInspeccion
                ? camara.transform.rotation * Quaternion.Euler(obj.rotacionInicialInspeccion)
                : obj.transform.rotation;

            BloquearJugador(true);
            Estado = EstadoAgarre.Inspeccionando;
            _inspeccionesActivas++;

            obj.alEmpezarInspeccion?.Invoke();
            alEmpezarInspeccion?.Invoke(obj);
            return true;
        }

        /// <summary>Termina la inspección actual.</summary>
        public void TerminarInspeccion()
        {
            if (Estado != EstadoAgarre.Inspeccionando || Sujeto == null) return;
            var obj = Sujeto;
            SalirDeInspeccionComun();

            if (InspeccionDesdeMano)
            {
                VolverAMano(obj);
            }
            else
            {
                var fin = alTerminarSinAgarrar;
                if (fin == FinInspeccionSinAgarrar.QuedarseEnLaMano && !PuedeAgarrar(obj)) fin = FinInspeccionSinAgarrar.VolverASuSitio;
                if (fin != FinInspeccionSinAgarrar.VolverASuSitio && obj.Cuerpo == null) fin = FinInspeccionSinAgarrar.VolverASuSitio;

                switch (fin)
                {
                    case FinInspeccionSinAgarrar.VolverASuSitio:
                        Estado = EstadoAgarre.Devolviendo;
                        break;
                    case FinInspeccionSinAgarrar.QuedarseEnLaMano:
                        IgnorarColisionJugador(obj, true);
                        VolverAMano(obj);
                        obj.alAgarrar?.Invoke();
                        alAgarrar?.Invoke(obj);
                        break;
                    case FinInspeccionSinAgarrar.SoltarDelante:
                        LiberarObjeto(obj);
                        obj.alSoltar?.Invoke();
                        alSoltar?.Invoke(obj);
                        break;
                }
            }

            obj.alTerminarInspeccion?.Invoke();
            alTerminarInspeccion?.Invoke(obj);
        }

        void SalirDeInspeccionComun()
        {
            BloquearJugador(false);
            _inspeccionesActivas = Mathf.Max(0, _inspeccionesActivas - 1);
        }

        // Cuando se suelta un objeto en plena inspección (por ejemplo al desactivar el agarrador).
        void SalirDeInspeccionSinEventosDeMano()
        {
            var obj = Sujeto;
            SalirDeInspeccionComun();
            Estado = EstadoAgarre.Sujetando;
            obj.alTerminarInspeccion?.Invoke();
            alTerminarInspeccion?.Invoke(obj);
        }

        void VolverAMano(ObjetoAgarrable obj)
        {
            ActivarColliders(obj, true);
            PrepararParaSujetar(obj.Cuerpo);
            _rotacionRelativa = Quaternion.Inverse(GiroHorizontalCamara()) * obj.transform.rotation;
            Estado = EstadoAgarre.Sujetando;
        }

        void ColocarInspeccion()
        {
            Transform cam = camara.transform;
            Transform t = Sujeto.transform;
            Vector3 centro = cam.position + cam.forward * _distanciaActual + cam.rotation * desplazamientoInspeccion;
            Vector3 desplazamientoCentro = _rotacionInspeccion * Vector3.Scale(_centroLocal, t.lossyScale);
            Vector3 posicion = centro - desplazamientoCentro;

            float k = 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime);
            t.SetPositionAndRotation(Vector3.Lerp(t.position, posicion, k), Quaternion.Slerp(t.rotation, _rotacionInspeccion, k));
        }

        void ActualizarDevolucion()
        {
            Transform t = Sujeto.transform;
            float k = 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime);
            t.SetPositionAndRotation(Vector3.Lerp(t.position, _posicionOriginal, k), Quaternion.Slerp(t.rotation, _rotacionOriginal, k));
            if ((t.position - _posicionOriginal).sqrMagnitude < 0.00002f && Quaternion.Angle(t.rotation, _rotacionOriginal) < 0.5f)
                FinalizarDevolucion();
        }

        void FinalizarDevolucion()
        {
            var obj = Sujeto;
            if (obj == null) { Estado = EstadoAgarre.Libre; return; }
            obj.transform.SetPositionAndRotation(_posicionOriginal, _rotacionOriginal);
            ActivarColliders(obj, true);
            RestaurarFisica(obj);
            obj.Portador = null;
            Sujeto = null;
            Estado = EstadoAgarre.Libre;
        }

        void BloquearJugador(bool bloquear)
        {
            if (componentesABloquear != null)
            {
                if (bloquear)
                {
                    _bloqueadosActivos = new bool[componentesABloquear.Length];
                    for (int i = 0; i < componentesABloquear.Length; i++)
                    {
                        var c = componentesABloquear[i];
                        if (c == null) continue;
                        _bloqueadosActivos[i] = c.enabled;
                        c.enabled = false;
                    }
                }
                else if (_bloqueadosActivos != null)
                {
                    for (int i = 0; i < componentesABloquear.Length && i < _bloqueadosActivos.Length; i++)
                        if (componentesABloquear[i] != null && _bloqueadosActivos[i]) componentesABloquear[i].enabled = true;
                }
            }

            if (mostrarCursorAlInspeccionar)
            {
                if (bloquear)
                {
                    _cursorVisibleAntes = Cursor.visible;
                    _cursorBloqueoAntes = Cursor.lockState;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = _cursorBloqueoAntes;
                    Cursor.visible = _cursorVisibleAntes;
                }
            }
        }

        // ───────────────────────────── Física ─────────────────────────────

        void GuardarFisica(ObjetoAgarrable obj)
        {
            var rb = obj.Cuerpo;
            _fisicaGuardada = rb != null;
            if (rb == null) return;
            _gravedadOriginal = rb.useGravity;
            _cinematicoOriginal = rb.isKinematic;
            _interpolacionOriginal = rb.interpolation;
            _deteccionOriginal = rb.collisionDetectionMode;
            _maxVelAngularOriginal = rb.maxAngularVelocity;
        }

        void PrepararParaSujetar(Rigidbody rb)
        {
            if (rb == null) return;
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.maxAngularVelocity = 50f;
        }

        void RestaurarFisica(ObjetoAgarrable obj)
        {
            var rb = obj.Cuerpo;
            if (rb == null || !_fisicaGuardada) return;
            rb.isKinematic = _cinematicoOriginal && !(volverseFisicoAlSoltar && Estado != EstadoAgarre.Devolviendo);
            rb.useGravity = _gravedadOriginal;
            rb.interpolation = _interpolacionOriginal;
            rb.collisionDetectionMode = _deteccionOriginal;
            rb.maxAngularVelocity = _maxVelAngularOriginal;
            _fisicaGuardada = false;
        }

        // Deja el objeto con su física original y el agarrador con las manos vacías.
        void LiberarObjeto(ObjetoAgarrable obj)
        {
            ActivarColliders(obj, true);
            RestaurarFisica(obj);
            IgnorarColisionJugador(obj, false);
            obj.Portador = null;
            Sujeto = null;
            Estado = EstadoAgarre.Libre;
            Cargando = false;
            CargaLanzamiento = 0f;
        }

        void ActivarColliders(ObjetoAgarrable obj, bool activar)
        {
            var cols = obj.Colliders;
            if (cols == null) return;
            if (!activar)
            {
                _collidersActivos = new bool[cols.Length];
                for (int i = 0; i < cols.Length; i++)
                {
                    if (cols[i] == null) continue;
                    _collidersActivos[i] = cols[i].enabled;
                    cols[i].enabled = false;
                }
            }
            else if (_collidersActivos != null && _collidersActivos.Length == cols.Length)
            {
                for (int i = 0; i < cols.Length; i++)
                    if (cols[i] != null && _collidersActivos[i]) cols[i].enabled = true;
                _collidersActivos = null;
            }
        }

        void IgnorarColisionJugador(ObjetoAgarrable obj, bool ignorar)
        {
            if (!ignorarColisionConJugador || obj == null || obj.Colliders == null) return;
            if (ignorar)
            {
                _colisionesPendientes.RemoveAll(p => p.objeto == obj);
                AplicarIgnorar(obj, true);
            }
            else
            {
                // No se restaura de golpe: si el objeto está dentro del jugador lo empujaría.
                // Se espera a que deje de tocarlo (máximo 3 segundos).
                _colisionesPendientes.Add(new ColisionPendiente { objeto = obj, limite = Time.time + 3f });
            }
        }

        void AplicarIgnorar(ObjetoAgarrable obj, bool ignorar)
        {
            foreach (var propio in obj.Colliders)
            {
                if (propio == null) continue;
                foreach (var delJugador in _collidersJugador)
                    if (delJugador != null && delJugador != propio) Physics.IgnoreCollision(propio, delJugador, ignorar);
            }
        }

        void RestaurarColisionesPendientes()
        {
            for (int i = _colisionesPendientes.Count - 1; i >= 0; i--)
            {
                var p = _colisionesPendientes[i];
                if (p.objeto == null) { _colisionesPendientes.RemoveAt(i); continue; }
                if (p.objeto.EstaSujeto) continue;
                if (Time.time < p.limite && TocaAlJugador(p.objeto)) continue;
                AplicarIgnorar(p.objeto, false);
                _colisionesPendientes.RemoveAt(i);
            }
        }

        bool TocaAlJugador(ObjetoAgarrable obj)
        {
            Bounds limites = obj.ObtenerLimites();
            foreach (var c in _collidersJugador)
                if (c != null && c.enabled && c.bounds.Intersects(limites)) return true;
            return false;
        }

        void ComprobarObjetoPerdido()
        {
            if (Estado == EstadoAgarre.Libre || Sujeto != null) return;
            // El objeto se destruyó mientras lo teníamos.
            if (Estado == EstadoAgarre.Inspeccionando) SalirDeInspeccionComun();
            Estado = EstadoAgarre.Libre;
            Cargando = false;
            CargaLanzamiento = 0f;
        }

        Quaternion GiroHorizontalCamara()
        {
            Vector3 frente = Vector3.ProjectOnPlane(camara.transform.forward, Vector3.up);
            if (frente.sqrMagnitude < 0.0001f) frente = Vector3.ProjectOnPlane(camara.transform.up, Vector3.up);
            return Quaternion.LookRotation(frente.normalized, Vector3.up);
        }

        static Vector3 LeerVelocidad(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        static void AsignarVelocidad(Rigidbody rb, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }
    }
}
