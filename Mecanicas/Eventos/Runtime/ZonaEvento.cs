using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Eventos
{
    /// <summary>Cuándo se activa la zona.</summary>
    public enum ModoActivacion
    {
        [Tooltip("Al entrar en la zona.")] AlEntrar,
        [Tooltip("Al salir de la zona.")] AlSalir,
        [Tooltip("Tras permanecer dentro unos segundos.")] AlPermanecer,
        [Tooltip("Solo cuando alguien llama a Activar() (botones, palancas, código...).")] Manual
    }

    /// <summary>Cuántas veces puede activarse la zona.</summary>
    public enum ModoRepeticion
    {
        UnaVez,
        CadaVez,
        NumeroDeVeces
    }

    /// <summary>Cómo se ejecuta la lista de acciones.</summary>
    public enum ModoEjecucion
    {
        [Tooltip("Una detrás de otra (cada acción decide si la siguiente la espera).")] Secuencia,
        [Tooltip("Todas a la vez.")] Paralelo
    }

    /// <summary>
    /// Zona que lanza una lista de acciones cuando el jugador (u otro objeto filtrado) pasa por ella.
    /// Ponla en un objeto con un Collider marcado como "Is Trigger".
    /// </summary>
    [AddComponentMenu("Mecánicas/Eventos/Zona de evento")]
    [DisallowMultipleComponent]
    public class ZonaEvento : MonoBehaviour
    {
        [Header("Activación")]
        [SerializeField] private ModoActivacion activacion = ModoActivacion.AlEntrar;

        [Tooltip("Segundos que hay que permanecer dentro (solo en modo Al Permanecer).")]
        [Min(0f)] [SerializeField] private float tiempoPermanencia = 2f;

        [Header("Quién la activa")]
        [Tooltip("Si está marcado, solo la activan objetos con esta etiqueta.")]
        [SerializeField] private bool filtrarPorEtiqueta = true;

        [SerializeField] private string etiqueta = "Player";

        [Tooltip("Capas que pueden activar la zona.")]
        [SerializeField] private LayerMask capas = ~0;

        [Header("Repetición")]
        [SerializeField] private ModoRepeticion repeticion = ModoRepeticion.UnaVez;

        [Tooltip("Número máximo de activaciones (solo en modo Numero De Veces).")]
        [Min(1)] [SerializeField] private int maxActivaciones = 3;

        [Tooltip("Segundos mínimos entre una activación y la siguiente.")]
        [Min(0f)] [SerializeField] private float enfriamiento = 0f;

        [Tooltip("Si el evento sigue en marcha, ignora nuevas activaciones hasta que termine.")]
        [SerializeField] private bool ignorarSiEnCurso = true;

        [Header("Acciones")]
        [SerializeField] private ModoEjecucion ejecucion = ModoEjecucion.Secuencia;

        [Tooltip("Acciones en orden. Pueden estar en este objeto o en cualquier otro de la escena.")]
        [SerializeField] private List<AccionEvento> acciones = new List<AccionEvento>();

        [Tooltip("Desactiva este objeto cuando el evento termina y ya no puede repetirse.")]
        [SerializeField] private bool desactivarAlAgotarse = false;

        [Header("Eventos (para conectar cualquier otra cosa)")]
        [SerializeField] private UnityEvent alActivar = new UnityEvent();
        [SerializeField] private UnityEvent alTerminar = new UnityEvent();
        [SerializeField] private UnityEvent alEntrar = new UnityEvent();
        [SerializeField] private UnityEvent alSalir = new UnityEvent();

        [Header("Depuración")]
        [SerializeField] private bool mostrarMensajes = false;
        [SerializeField] private Color colorGizmo = new Color(1f, 0.6f, 0f, 0.25f);

        private readonly Dictionary<GameObject, float> dentro = new Dictionary<GameObject, float>();
        private readonly Dictionary<GameObject, int> contadorColliders = new Dictionary<GameObject, int>();
        private int activaciones;
        private float ultimaActivacion = float.NegativeInfinity;
        private int rutinasEnCurso;
        private Coroutine rutinaPrincipal;

        public ModoActivacion Activacion { get => activacion; set => activacion = value; }
        public ModoEjecucion Ejecucion { get => ejecucion; set => ejecucion = value; }
        public List<AccionEvento> Acciones => acciones;
        public UnityEvent AlActivar => alActivar;
        public UnityEvent AlTerminar => alTerminar;
        public UnityEvent AlEntrar => alEntrar;
        public UnityEvent AlSalir => alSalir;

        /// <summary>Veces que se ha activado desde que empezó la escena (o desde el último Reiniciar).</summary>
        public int Activaciones => activaciones;

        /// <summary>True mientras alguna acción del evento sigue en marcha.</summary>
        public bool EnCurso => rutinasEnCurso > 0;

        /// <summary>True si ya no puede volver a activarse por su límite de repeticiones.</summary>
        public bool Agotada =>
            (repeticion == ModoRepeticion.UnaVez && activaciones >= 1) ||
            (repeticion == ModoRepeticion.NumeroDeVeces && activaciones >= maxActivaciones);

        /// <summary>Objetos válidos que están ahora mismo dentro de la zona.</summary>
        public IReadOnlyCollection<GameObject> ObjetosDentro => dentro.Keys;

        private void Reset()
        {
            Collider c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
            RecogerAcciones();
        }

        private void OnValidate()
        {
            Collider c = GetComponent<Collider>();
            if (c != null && !c.isTrigger)
                Debug.LogWarning($"[Eventos] '{name}': el Collider de la zona debería tener 'Is Trigger' marcado.", this);
        }

        /// <summary>Rellena la lista con las acciones de este objeto y sus hijos, en orden de jerarquía.</summary>
        [ContextMenu("Recoger acciones de este objeto y sus hijos")]
        public void RecogerAcciones()
        {
            acciones.Clear();
            acciones.AddRange(GetComponentsInChildren<AccionEvento>(true));
        }

        // ---------------------------------------------------------------- Detección

        private void OnTriggerEnter(Collider otro)
        {
            GameObject obj = Identidad(otro);
            if (!EsValido(obj)) return;

            contadorColliders.TryGetValue(obj, out int n);
            contadorColliders[obj] = n + 1;
            if (n > 0) return; // ya estaba dentro con otro de sus colliders

            dentro[obj] = Time.time;
            Log($"entra {obj.name}");
            alEntrar.Invoke();
            if (activacion == ModoActivacion.AlEntrar)
                Activar(obj);
        }

        private void OnTriggerExit(Collider otro)
        {
            GameObject obj = Identidad(otro);
            if (!contadorColliders.TryGetValue(obj, out int n)) return;

            if (n > 1)
            {
                contadorColliders[obj] = n - 1;
                return;
            }

            contadorColliders.Remove(obj);
            dentro.Remove(obj);
            Log($"sale {obj.name}");
            alSalir.Invoke();
            if (activacion == ModoActivacion.AlSalir)
                Activar(obj);
        }

        private void Update()
        {
            if (activacion != ModoActivacion.AlPermanecer || dentro.Count == 0) return;

            GameObject listo = null;
            foreach (KeyValuePair<GameObject, float> par in dentro)
            {
                if (par.Key == null) continue;
                if (Time.time - par.Value >= tiempoPermanencia)
                {
                    listo = par.Key;
                    break;
                }
            }

            if (listo != null)
            {
                dentro[listo] = float.PositiveInfinity; // no vuelve a contar hasta que salga y entre
                Activar(listo);
            }
        }

        private void OnDisable()
        {
            // Unity para las corrutinas al desactivar el objeto: avisamos a las acciones para que se limpien.
            if (EnCurso) Detener();
            dentro.Clear();
            contadorColliders.Clear();
        }

        private static GameObject Identidad(Collider c)
        {
            // Un jugador con varios colliders y un Rigidbody cuenta como un solo objeto.
            return c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
        }

        private bool EsValido(GameObject obj)
        {
            if (obj == null) return false;
            if ((capas.value & (1 << obj.layer)) == 0) return false;
            if (filtrarPorEtiqueta && !string.IsNullOrEmpty(etiqueta) && !obj.CompareTag(etiqueta)) return false;
            return true;
        }

        // ---------------------------------------------------------------- Activación

        /// <summary>Activa la zona a mano (para botones, palancas, UnityEvents...). Respeta repeticiones y enfriamiento.</summary>
        public void Activar() => Activar(null);

        /// <summary>Activa la zona indicando quién la activó.</summary>
        public bool Activar(GameObject activador)
        {
            if (!isActiveAndEnabled) return false;
            if (Agotada) { Log("agotada, no se activa"); return false; }
            if (Time.time - ultimaActivacion < enfriamiento) { Log("en enfriamiento"); return false; }
            if (ignorarSiEnCurso && EnCurso) { Log("ya en curso"); return false; }

            activaciones++;
            ultimaActivacion = Time.time;
            Log($"activada ({activaciones}) por {(activador != null ? activador.name : "código")}");
            alActivar.Invoke();
            rutinaPrincipal = StartCoroutine(RutinaEvento(new ContextoEvento(this, activador)));
            return true;
        }

        /// <summary>Activa la zona ignorando repeticiones, enfriamiento y si está en curso.</summary>
        public void ForzarActivacion()
        {
            activaciones++;
            ultimaActivacion = Time.time;
            alActivar.Invoke();
            rutinaPrincipal = StartCoroutine(RutinaEvento(new ContextoEvento(this, null)));
        }

        /// <summary>Corta el evento en curso y avisa a cada acción para que se limpie.</summary>
        public void Detener()
        {
            StopAllCoroutines();
            rutinaPrincipal = null;
            rutinasEnCurso = 0;
            foreach (AccionEvento a in acciones)
                if (a != null) a.Detener();
        }

        /// <summary>Pone el contador de activaciones a cero para que pueda volver a usarse.</summary>
        public void Reiniciar()
        {
            activaciones = 0;
            ultimaActivacion = float.NegativeInfinity;
        }

        private IEnumerator RutinaEvento(ContextoEvento ctx)
        {
            rutinasEnCurso++;

            if (ejecucion == ModoEjecucion.Paralelo)
            {
                foreach (AccionEvento a in acciones)
                    if (Usable(a)) StartCoroutine(Envolver(a, ctx));
            }
            else
            {
                foreach (AccionEvento a in acciones)
                {
                    if (!Usable(a)) continue;
                    if (a.EsperarAQueTermine)
                        yield return Envolver(a, ctx);
                    else
                        StartCoroutine(Envolver(a, ctx));
                }
            }

            rutinasEnCurso--;
            while (rutinasEnCurso > 0)
                yield return null;

            rutinaPrincipal = null;
            Log("terminado");
            alTerminar.Invoke();

            if (desactivarAlAgotarse && Agotada)
                gameObject.SetActive(false);
        }

        private IEnumerator Envolver(AccionEvento a, ContextoEvento ctx)
        {
            rutinasEnCurso++;
            yield return a.Ejecutar(ctx);
            rutinasEnCurso--;
        }

        private static bool Usable(AccionEvento a) => a != null && a.enabled;

        private void Log(string mensaje)
        {
            if (mostrarMensajes) Debug.Log($"[Eventos] '{name}': {mensaje}", this);
        }

        // ---------------------------------------------------------------- Gizmos

        private void OnDrawGizmos()
        {
            DibujarZona(colorGizmo);
        }

        private void OnDrawGizmosSelected()
        {
            Color borde = colorGizmo;
            borde.a = 1f;
            DibujarZona(borde, true);

            Gizmos.color = borde;
            foreach (AccionEvento a in acciones)
                if (a != null && a.gameObject != gameObject)
                    Gizmos.DrawLine(transform.position, a.transform.position);
        }

        private void DibujarZona(Color color, bool alambre = false)
        {
            Gizmos.color = color;
            foreach (Collider c in GetComponents<Collider>())
            {
                if (c is BoxCollider caja)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    if (alambre) Gizmos.DrawWireCube(caja.center, caja.size);
                    else Gizmos.DrawCube(caja.center, caja.size);
                    Gizmos.matrix = Matrix4x4.identity;
                }
                else if (c is SphereCollider esfera)
                {
                    Vector3 s = transform.lossyScale;
                    float r = esfera.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                    Vector3 centro = transform.TransformPoint(esfera.center);
                    if (alambre) Gizmos.DrawWireSphere(centro, r);
                    else Gizmos.DrawSphere(centro, r);
                }
                else
                {
                    Bounds b = c.bounds;
                    if (alambre) Gizmos.DrawWireCube(b.center, b.size);
                    else Gizmos.DrawCube(b.center, b.size);
                }
            }
        }
    }
}
