using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Inspeccion
{
    /// <summary>
    /// Marca un objeto como agarrable, lanzable y/o inspeccionable.
    ///
    /// Ponlo en la raíz del objeto (la que tiene el Rigidbody). Sin Rigidbody el objeto solo se
    /// puede inspeccionar: el jugador lo mira de cerca y vuelve a su sitio al terminar.
    /// Todos los ajustes de este componente son opcionales; los valores por defecto los da el
    /// <see cref="AgarradorPrimeraPersona"/> del jugador.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecánicas/Inspección/Objeto Agarrable")]
    public class ObjetoAgarrable : MonoBehaviour
    {
        [Header("Información (se muestra al inspeccionar)")]
        [Tooltip("Nombre que aparece en pantalla. Si está vacío se usa el nombre del GameObject.")]
        public string nombre = "";
        [TextArea(2, 6)]
        [Tooltip("Texto que aparece debajo del nombre al inspeccionar.")]
        public string descripcion = "";

        [Header("Qué se puede hacer")]
        public bool sePuedeAgarrar = true;
        public bool sePuedeLanzar = true;
        public bool sePuedeInspeccionar = true;

        [Header("Ajustes propios (opcionales)")]
        [Tooltip("Multiplica la fuerza de lanzamiento del jugador para este objeto.")]
        public float multiplicadorLanzamiento = 1f;
        [Tooltip("Si es mayor que 0, sustituye la distancia a la que se sujeta el objeto.")]
        public float distanciaSujecion = 0f;
        [Tooltip("Si es mayor que 0, sustituye la distancia de inspección (si no, se calcula según el tamaño).")]
        public float distanciaInspeccion = 0f;
        [Tooltip("Si está activo, al empezar a inspeccionar el objeto gira a esta orientación (en grados, relativa a la cámara).")]
        public bool usarRotacionInicialInspeccion;
        public Vector3 rotacionInicialInspeccion = Vector3.zero;
        [Tooltip("Rotaciones permitidas al inspeccionar. Desactiva un eje para bloquearlo (por ejemplo, un cuadro que solo gira en horizontal).")]
        public bool rotarHorizontal = true;
        public bool rotarVertical = true;

        [Header("Resaltado al apuntar")]
        public bool resaltarAlApuntar = true;
        public Color colorResaltado = new Color(1f, 0.92f, 0.55f, 1f);
        [Range(0f, 1f)] public float intensidadResaltado = 0.35f;
        [Tooltip("Renderers a resaltar. Si está vacío se usan todos los hijos.")]
        public Renderer[] renderers;

        [Header("Sonidos (opcionales)")]
        public AudioClip sonidoAgarrar;
        public AudioClip sonidoSoltar;
        public AudioClip sonidoLanzar;
        public AudioClip sonidoGolpe;
        [Tooltip("Velocidad mínima del choque para que suene el golpe.")]
        public float velocidadMinimaGolpe = 1.5f;
        [Range(0f, 1f)] public float volumen = 0.8f;

        [Header("Eventos")]
        public UnityEvent alApuntar;
        public UnityEvent alDejarDeApuntar;
        public UnityEvent alAgarrar;
        public UnityEvent alSoltar;
        public UnityEvent alLanzar;
        public UnityEvent alEmpezarInspeccion;
        public UnityEvent alTerminarInspeccion;

        /// <summary>Rigidbody del objeto (puede ser null: entonces solo se inspecciona).</summary>
        public Rigidbody Cuerpo { get; private set; }
        /// <summary>Agarrador que lo tiene ahora mismo (null si nadie).</summary>
        public AgarradorPrimeraPersona Portador { get; internal set; }
        public bool EstaSujeto => Portador != null;
        public string NombreVisible => string.IsNullOrEmpty(nombre) ? gameObject.name : nombre;
        public bool PuedeAgarrarse => sePuedeAgarrar && Cuerpo != null && isActiveAndEnabled;

        Collider[] _colliders;
        MaterialPropertyBlock _bloque;
        bool _resaltado;
        float _ultimoGolpe;

        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int IdColor = Shader.PropertyToID("_Color");

        void Awake()
        {
            Cuerpo = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>(true);
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
        }

        void OnDisable()
        {
            Resaltar(false);
            if (Portador != null) Portador.Soltar();
        }

        /// <summary>Colliders del objeto y de sus hijos.</summary>
        public Collider[] Colliders => _colliders;

        /// <summary>Caja que envuelve todos los renderers (o colliders) del objeto, en el mundo.</summary>
        public Bounds ObtenerLimites()
        {
            bool hay = false;
            var limites = new Bounds(transform.position, Vector3.zero);
            if (renderers != null)
                foreach (var r in renderers)
                {
                    if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                    if (!hay) { limites = r.bounds; hay = true; }
                    else limites.Encapsulate(r.bounds);
                }
            if (!hay && _colliders != null)
                foreach (var c in _colliders)
                {
                    if (c == null) continue;
                    if (!hay) { limites = c.bounds; hay = true; }
                    else limites.Encapsulate(c.bounds);
                }
            return limites;
        }

        /// <summary>Activa o desactiva el resaltado de color.</summary>
        public void Resaltar(bool activo)
        {
            activo &= resaltarAlApuntar;
            if (activo == _resaltado || renderers == null) return;
            _resaltado = activo;
            if (_bloque == null) _bloque = new MaterialPropertyBlock();

            foreach (var r in renderers)
            {
                if (r == null) continue;
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    _bloque.Clear();
                    if (!activo || mats[i] == null) { r.SetPropertyBlock(_bloque, i); continue; }
                    if (mats[i].HasProperty(IdBaseColor))
                        _bloque.SetColor(IdBaseColor, Color.Lerp(mats[i].GetColor(IdBaseColor), colorResaltado, intensidadResaltado));
                    if (mats[i].HasProperty(IdColor))
                        _bloque.SetColor(IdColor, Color.Lerp(mats[i].GetColor(IdColor), colorResaltado, intensidadResaltado));
                    r.SetPropertyBlock(_bloque, i);
                }
            }
        }

        internal void Sonar(AudioClip clip)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, volumen);
        }

        void OnCollisionEnter(Collision choque)
        {
            if (sonidoGolpe == null || Time.time - _ultimoGolpe < 0.1f) return;
            if (choque.relativeVelocity.magnitude < velocidadMinimaGolpe) return;
            _ultimoGolpe = Time.time;
            float vol = Mathf.Clamp01(choque.relativeVelocity.magnitude / (velocidadMinimaGolpe * 4f)) * volumen;
            AudioSource.PlayClipAtPoint(sonidoGolpe, choque.GetContact(0).point, vol);
        }
    }
}
