using System.Collections;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>
    /// Datos que recibe cada acción cuando se ejecuta: qué zona la lanzó y qué objeto entró en ella.
    /// </summary>
    public sealed class ContextoEvento
    {
        /// <summary>Zona que lanzó el evento. Es null si la acción se ejecutó suelta (desde un UnityEvent o por código).</summary>
        public ZonaEvento Zona { get; }

        /// <summary>Objeto que activó el evento (normalmente el jugador). Puede ser null si se activó a mano.</summary>
        public GameObject Activador { get; }

        public ContextoEvento(ZonaEvento zona, GameObject activador)
        {
            Zona = zona;
            Activador = activador;
        }
    }

    /// <summary>
    /// Base de todas las acciones. Para crear una acción nueva hereda de esta clase
    /// e implementa <see cref="EjecutarAccion"/> (ver README, sección "Crear tus propias acciones").
    /// </summary>
    public abstract class AccionEvento : MonoBehaviour
    {
        [Tooltip("Nota libre para recordar qué hace esta acción. Solo se usa en el editor.")]
        [SerializeField] private string nota = "";

        [Tooltip("Segundos que se espera antes de ejecutar la acción.")]
        [Min(0f)] [SerializeField] private float retraso = 0f;

        [Tooltip("En una zona en modo Secuencia: si está marcado, la siguiente acción espera a que esta termine. " +
                 "Si no, la siguiente empieza a la vez.")]
        [SerializeField] private bool esperarAQueTermine = true;

        /// <summary>Nota del editor.</summary>
        public string Nota => nota;

        public float Retraso
        {
            get => retraso;
            set => retraso = Mathf.Max(0f, value);
        }

        public bool EsperarAQueTermine
        {
            get => esperarAQueTermine;
            set => esperarAQueTermine = value;
        }

        /// <summary>True mientras la acción está en marcha.</summary>
        public bool EnEjecucion { get; private set; }

        /// <summary>
        /// Rutina completa: espera el retraso y ejecuta la acción. La llama <see cref="ZonaEvento"/>;
        /// también puedes lanzarla tú con StartCoroutine.
        /// </summary>
        public IEnumerator Ejecutar(ContextoEvento contexto)
        {
            anfitrion = contexto != null && contexto.Zona != null ? (MonoBehaviour)contexto.Zona : this;
            EnEjecucion = true;
            if (retraso > 0f)
                yield return new WaitForSeconds(retraso);

            IEnumerator rutina = EjecutarAccion(contexto ?? new ContextoEvento(null, null));
            if (rutina != null)
                yield return rutina;

            EnEjecucion = false;
        }

        /// <summary>
        /// Ejecuta solo esta acción, sin zona. Pensado para enlazarlo desde cualquier UnityEvent
        /// (un botón de UI, otra mecánica, una animación...).
        /// </summary>
        public void EjecutarSuelta()
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogWarning($"[Eventos] '{name}': no se puede ejecutar la acción con el objeto o el componente desactivado.", this);
                return;
            }
            StartCoroutine(Ejecutar(new ContextoEvento(null, null)));
        }

        private MonoBehaviour anfitrion;

        /// <summary>
        /// Lanza una corrutina que debe seguir viva después de que la acción termine (por ejemplo, quitar un objeto
        /// a los pocos segundos). Usa la zona como anfitriona, así funciona aunque este objeto esté desactivado.
        /// </summary>
        protected Coroutine LanzarCorrutina(IEnumerator rutina)
        {
            MonoBehaviour host = anfitrion != null && anfitrion.isActiveAndEnabled ? anfitrion : this;
            if (!host.isActiveAndEnabled)
            {
                Debug.LogWarning($"[Eventos] '{name}': no hay ningún objeto activo para lanzar la corrutina.", this);
                return null;
            }
            return host.StartCoroutine(rutina);
        }

        /// <summary>
        /// Lo que hace la acción. Puede durar varios frames (usa yield) o terminar al momento (yield break).
        /// </summary>
        protected abstract IEnumerator EjecutarAccion(ContextoEvento contexto);

        /// <summary>
        /// Se llama cuando la zona detiene el evento a medias (<see cref="ZonaEvento.Detener"/>).
        /// Sobrescríbelo para dejar las cosas en un estado limpio (parar un sonido, liberar la cámara...).
        /// </summary>
        public virtual void Detener()
        {
            EnEjecucion = false;
        }
    }
}
