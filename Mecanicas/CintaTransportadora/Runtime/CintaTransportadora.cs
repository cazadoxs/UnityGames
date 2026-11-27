using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Cinta transportadora: arrastra los Rigidbody y CharacterController que estén
    /// encima de ella en la dirección indicada.
    ///
    /// Cómo detecta lo que hay encima: usa una "zona de detección" (un BoxCollider
    /// marcado como Trigger que cubre la superficie de la cinta). Si no asignas una,
    /// se crea sola a partir del BoxCollider de este objeto.
    ///
    /// Cómo mueve los objetos: cada paso de física cambia poco a poco la velocidad
    /// del objeto (en el plano de la cinta) hasta igualarla con la de la cinta. La
    /// rapidez de ese cambio es el "agarre". Así el objeto acelera de forma natural,
    /// sigue pudiendo caer, rodar o chocar, y si la cinta se para, se para con ella.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecanicas/Cinta Transportadora/Cinta Transportadora")]
    public class CintaTransportadora : MonoBehaviour
    {
        [Header("Estado")]
        [Tooltip("Si la cinta arranca encendida.")]
        [SerializeField] bool encendida = true;

        [Tooltip("Velocidad de la cinta en metros por segundo.")]
        [Min(0f)] [SerializeField] float velocidad = 1.5f;

        [Tooltip("Dirección de avance en espacio LOCAL de este objeto (por defecto, su eje Z azul).")]
        [SerializeField] Vector3 direccionLocal = Vector3.forward;

        [Tooltip("Metros por segundo que gana o pierde la cinta al encenderse, apagarse o cambiar de velocidad. 0 = instantáneo.")]
        [Min(0f)] [SerializeField] float aceleracion = 3f;

        [Header("Arrastre de objetos")]
        [Tooltip("Aceleración máxima (m/s²) con la que la cinta lleva a los objetos a su velocidad. Más alto = la cinta 'agarra' más.")]
        [Min(0f)] [SerializeField] float agarre = 25f;

        [Tooltip("Empuja los objetos hacia la línea central de la cinta.")]
        [SerializeField] bool centrarObjetos = false;

        [Tooltip("Velocidad lateral (m/s) por cada metro de separación del centro.")]
        [Min(0f)] [SerializeField] float fuerzaCentrado = 2f;

        [Tooltip("Capas que la cinta puede arrastrar.")]
        [SerializeField] LayerMask capas = ~0;

        [Tooltip("Arrastra también al jugador si usa CharacterController.")]
        [SerializeField] bool moverCharacterControllers = true;

        [Tooltip("Ignora los Rigidbody cinemáticos (por ejemplo, objetos agarrados por el jugador).")]
        [SerializeField] bool ignorarCinematicos = true;

        [Header("Zona de detección")]
        [Tooltip("Trigger que cubre la superficie. Vacío = se crea solo encima del BoxCollider de este objeto.")]
        [SerializeField] Collider zonaDeteccion;

        [Tooltip("Altura (m) de la zona de detección que se crea automáticamente.")]
        [Min(0.01f)] [SerializeField] float alturaZonaAutomatica = 0.35f;

        [Header("Eventos")]
        public UnityEvent alEncender = new UnityEvent();
        public UnityEvent alApagar = new UnityEvent();
        [Tooltip("Se llama cuando un objeto empieza a ser arrastrado.")]
        public GameObjectEvent alEntrarObjeto = new GameObjectEvent();
        [Tooltip("Se llama cuando un objeto deja la cinta.")]
        public GameObjectEvent alSalirObjeto = new GameObjectEvent();

        [System.Serializable] public class GameObjectEvent : UnityEvent<GameObject> { }

        // Cuántos colliders de cada cuerpo están dentro (un objeto puede tener varios).
        readonly Dictionary<Rigidbody, int> cuerpos = new Dictionary<Rigidbody, int>();
        readonly Dictionary<CharacterController, int> personajes = new Dictionary<CharacterController, int>();
        readonly List<Rigidbody> bufferCuerpos = new List<Rigidbody>();
        readonly List<CharacterController> bufferPersonajes = new List<CharacterController>();

        float velocidadActual;   // con signo: negativo = sentido inverso
        float sentido = 1f;

        // ---------------------------------------------------------------- API pública

        /// <summary>True si la cinta está encendida (aunque aún esté acelerando).</summary>
        public bool EstaEncendida => encendida;

        /// <summary>Velocidad objetivo en m/s (sin signo).</summary>
        public float Velocidad => velocidad;

        /// <summary>Velocidad real en este momento, con signo (negativa si va en sentido inverso).</summary>
        public float VelocidadActual => velocidadActual;

        /// <summary>Dirección de avance en el mundo (normalizada, sin tener en cuenta el sentido).</summary>
        public Vector3 Direccion => transform.TransformDirection(direccionLocal.sqrMagnitude > 0f ? direccionLocal.normalized : Vector3.forward);

        /// <summary>Velocidad lineal con la que la superficie se mueve ahora mismo (m/s, en el mundo).</summary>
        public Vector3 VelocidadSuperficie => Direccion * velocidadActual;

        /// <summary>Objetos (Rigidbody) que la cinta está arrastrando ahora.</summary>
        public IReadOnlyCollection<Rigidbody> ObjetosEncima => cuerpos.Keys;

        public void Encender()
        {
            if (encendida) return;
            encendida = true;
            alEncender.Invoke();
        }

        public void Apagar()
        {
            if (!encendida) return;
            encendida = false;
            alApagar.Invoke();
        }

        public void Alternar()
        {
            if (encendida) Apagar(); else Encender();
        }

        /// <summary>Cambia el sentido de la cinta (con la aceleración configurada).</summary>
        public void InvertirSentido() => sentido = -sentido;

        /// <summary>true = sentido normal, false = sentido inverso.</summary>
        public void EstablecerSentido(bool normal) => sentido = normal ? 1f : -1f;

        public void EstablecerVelocidad(float metrosPorSegundo) => velocidad = Mathf.Max(0f, metrosPorSegundo);

        public void SumarVelocidad(float metrosPorSegundo) => EstablecerVelocidad(velocidad + metrosPorSegundo);

        // ---------------------------------------------------------------- Ciclo de vida

        void Reset()
        {
            // Si ya hay un BoxCollider, la zona se creará sobre él al dar al Play.
            if (GetComponent<Collider>() == null) gameObject.AddComponent<BoxCollider>();
        }

        void Awake()
        {
            velocidadActual = encendida ? velocidad * sentido : 0f;
            PrepararZona();
        }

        void OnDisable()
        {
            cuerpos.Clear();
            personajes.Clear();
        }

        void PrepararZona()
        {
            if (zonaDeteccion == null)
            {
                var existente = GetComponentInChildren<CintaZonaDeteccion>(true);
                if (existente != null) zonaDeteccion = existente.GetComponent<Collider>();
            }

            if (zonaDeteccion == null)
            {
                var caja = GetComponent<BoxCollider>();
                if (caja == null || caja.isTrigger)
                {
                    if (caja != null) { zonaDeteccion = caja; return; } // el propio collider ya es la zona
                    Debug.LogWarning($"[CintaTransportadora] '{name}' no tiene BoxCollider ni zona de detección: no moverá nada.", this);
                    return;
                }

                var go = new GameObject("ZonaDeteccion");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var zona = go.AddComponent<BoxCollider>();
                zona.isTrigger = true;
                Vector3 superior = caja.center + Vector3.up * (caja.size.y * 0.5f);
                // La altura se da en metros: se divide por la escala para que no dependa de ella.
                float escalaY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
                float altura = alturaZonaAutomatica / escalaY;
                zona.size = new Vector3(caja.size.x, altura, caja.size.z);
                zona.center = superior + Vector3.up * (altura * 0.5f - 0.02f / escalaY);
                zonaDeteccion = zona;
            }

            if (zonaDeteccion.gameObject != gameObject && zonaDeteccion.GetComponent<CintaZonaDeteccion>() == null)
                zonaDeteccion.gameObject.AddComponent<CintaZonaDeteccion>();

            var relevo = zonaDeteccion.GetComponent<CintaZonaDeteccion>();
            if (relevo != null) relevo.cinta = this;
            zonaDeteccion.isTrigger = true;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float objetivo = encendida ? velocidad * sentido : 0f;
            velocidadActual = aceleracion <= 0f ? objetivo : Mathf.MoveTowards(velocidadActual, objetivo, aceleracion * dt);

            if (cuerpos.Count == 0) return;

            Vector3 normal = transform.up;
            Vector3 dir = Direccion;
            Vector3 lateral = Vector3.Cross(normal, dir).normalized;
            Vector3 velCinta = dir * velocidadActual;
            float maxCambio = agarre * dt;

            bufferCuerpos.Clear();
            bufferCuerpos.AddRange(cuerpos.Keys);
            foreach (var rb in bufferCuerpos)
            {
                if (rb == null || !rb.gameObject.activeInHierarchy || !rb.detectCollisions)
                {
                    Quitar(rb);
                    continue;
                }
                if (rb.isKinematic && ignorarCinematicos) continue;

                Vector3 objetivoVel = velCinta;
                if (centrarObjetos)
                {
                    float separacion = Vector3.Dot(rb.worldCenterOfMass - transform.position, lateral);
                    objetivoVel -= lateral * (separacion * fuerzaCentrado);
                }

                if (rb.isKinematic)
                {
                    rb.MovePosition(rb.position + objetivoVel * dt);
                    continue;
                }

                Vector3 v = LeerVelocidad(rb);
                Vector3 vPlano = v - Vector3.Project(v, normal);   // se respeta la gravedad / saltos
                Vector3 cambio = Vector3.ClampMagnitude(objetivoVel - vPlano, maxCambio);
                if (cambio.sqrMagnitude > 1e-8f) rb.AddForce(cambio, ForceMode.VelocityChange);
            }
        }

        void Update()
        {
            if (!moverCharacterControllers || personajes.Count == 0) return;

            Vector3 desplazamiento = VelocidadSuperficie * Time.deltaTime;
            bufferPersonajes.Clear();
            bufferPersonajes.AddRange(personajes.Keys);
            foreach (var cc in bufferPersonajes)
            {
                if (cc == null || !cc.enabled || !cc.gameObject.activeInHierarchy)
                {
                    personajes.Remove(cc);
                    continue;
                }
                // El pequeño empuje hacia abajo mantiene cc.isGrounded = true (si no, el jugador no podría saltar).
                if (desplazamiento.sqrMagnitude > 0f) cc.Move(desplazamiento - transform.up * 0.001f);
            }
        }

        // ---------------------------------------------------------------- Detección

        // Si la zona de detección es el propio collider de este objeto, Unity llama aquí.
        void OnTriggerEnter(Collider otro) { if (zonaDeteccion != null && zonaDeteccion.gameObject == gameObject) Entrar(otro); }
        void OnTriggerExit(Collider otro) { if (zonaDeteccion != null && zonaDeteccion.gameObject == gameObject) Salir(otro); }

        internal void Entrar(Collider otro)
        {
            if (otro == null || otro.isTrigger) return;
            if ((capas.value & (1 << otro.gameObject.layer)) == 0) return;

            if (otro is CharacterController cc)
            {
                if (!moverCharacterControllers) return;
                personajes.TryGetValue(cc, out int n);
                personajes[cc] = n + 1;
                if (n == 0) alEntrarObjeto.Invoke(cc.gameObject);
                return;
            }

            var rb = otro.attachedRigidbody;
            if (rb == null || EsParteDeLaCinta(rb)) return;
            cuerpos.TryGetValue(rb, out int c);
            cuerpos[rb] = c + 1;
            if (c == 0) alEntrarObjeto.Invoke(rb.gameObject);
        }

        internal void Salir(Collider otro)
        {
            if (otro == null) return;

            if (otro is CharacterController cc)
            {
                if (personajes.TryGetValue(cc, out int n))
                {
                    if (n <= 1) { personajes.Remove(cc); alSalirObjeto.Invoke(cc.gameObject); }
                    else personajes[cc] = n - 1;
                }
                return;
            }

            var rb = otro.attachedRigidbody;
            if (rb == null || !cuerpos.TryGetValue(rb, out int c)) return;
            if (c <= 1) Quitar(rb);
            else cuerpos[rb] = c - 1;
        }

        void Quitar(Rigidbody rb)
        {
            if (!cuerpos.Remove(rb)) return;
            if (rb != null) alSalirObjeto.Invoke(rb.gameObject);
        }

        bool EsParteDeLaCinta(Rigidbody rb) => rb.transform == transform || rb.transform.IsChildOf(transform);

        static Vector3 LeerVelocidad(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        // ---------------------------------------------------------------- Ayudas del editor

        [ContextMenu("Encender o apagar")] void MenuAlternar() => Alternar();
        [ContextMenu("Invertir sentido")] void MenuInvertir() => InvertirSentido();

        void OnValidate()
        {
            if (direccionLocal.sqrMagnitude < 1e-6f) direccionLocal = Vector3.forward;
        }

        void OnDrawGizmos()
        {
            var caja = GetComponent<BoxCollider>();
            Vector3 centro = caja != null
                ? transform.TransformPoint(caja.center + Vector3.up * (caja.size.y * 0.5f + 0.05f))
                : transform.position;
            float signo = Application.isPlaying ? (Mathf.Abs(velocidadActual) > 0.001f ? Mathf.Sign(velocidadActual) : sentido) : 1f;
            Vector3 dir = Direccion * signo;
            float largo = caja != null ? Mathf.Max(0.5f, Mathf.Abs(Vector3.Dot(transform.TransformVector(caja.size), Direccion)) * 0.4f) : 1f;

            Gizmos.color = encendida ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.3f, 0.2f);
            Vector3 ini = centro - dir * largo;
            Vector3 fin = centro + dir * largo;
            Gizmos.DrawLine(ini, fin);
            Vector3 lado = Vector3.Cross(transform.up, dir).normalized * (largo * 0.25f);
            Gizmos.DrawLine(fin, fin - dir * (largo * 0.3f) + lado);
            Gizmos.DrawLine(fin, fin - dir * (largo * 0.3f) - lado);
        }
    }
}
