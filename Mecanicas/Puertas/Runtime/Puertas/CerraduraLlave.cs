using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Cerradura integrada en la puerta. Si el jugador lleva la llave con <see cref="idLlave"/>,
    /// al usar la puerta se desbloquea. Ponla en la misma puerta (o en un hijo).
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Cerradura con llave")]
    public class CerraduraLlave : MonoBehaviour, IBloqueo
    {
        /// <summary>Id de llave que abre todas las cerraduras que lo acepten.</summary>
        public const string IdLlaveMaestra = "maestra";

        [Tooltip("Identificador de la llave que la abre. Debe coincidir con el de la Llave.")]
        [SerializeField] string idLlave = "llave_roja";
        [Tooltip("Nombre que se enseña al jugador, por ejemplo \"la llave roja\".")]
        [SerializeField] string nombreLlave = "la llave roja";
        [SerializeField] bool empiezaBloqueada = true;
        [Tooltip("Si está marcado, la llave desaparece del llavero al usarla.")]
        [SerializeField] bool gastarLlave;
        [Tooltip("Si está marcado, también la abre la llave maestra (id \"maestra\").")]
        [SerializeField] bool aceptaLlaveMaestra = true;
        [Tooltip("Vuelve a cerrarse con llave cada vez que la puerta queda cerrada.")]
        [SerializeField] bool rebloquearAlCerrar;

        [Header("Sonido")]
        [SerializeField] AudioSource fuenteAudio;
        [SerializeField] AudioClip sonidoDesbloquear;

        public UnityEvent alDesbloquear = new UnityEvent();
        public UnityEvent alBloquear = new UnityEvent();

        bool bloqueada;
        Puerta puerta;

        public bool Bloqueado => bloqueada;
        public bool ImpideCerrar => false;
        public string Motivo => "Cerrada con llave. Necesitas " + nombreLlave + ".";
        public string IdLlave => idLlave;

        void Awake()
        {
            bloqueada = empiezaBloqueada;
            puerta = GetComponentInParent<Puerta>();
            if (puerta != null) puerta.alCerrar.AddListener(OnPuertaCerrada);
        }

        void OnPuertaCerrada()
        {
            if (rebloquearAlCerrar && !bloqueada) Bloquear();
        }

        public bool IntentarDesbloquearDesdePuerta(Interactor interactor, out string mensaje)
        {
            mensaje = null;
            if (!bloqueada) return true;
            var llaves = interactor != null ? interactor.Llaves : null;
            if (llaves == null) return false;

            bool tiene = llaves.TieneLlave(idLlave);
            bool maestra = !tiene && aceptaLlaveMaestra && llaves.TieneLlave(IdLlaveMaestra);
            if (!tiene && !maestra) return false;

            if (gastarLlave && tiene) llaves.QuitarLlave(idLlave);
            Desbloquear();
            mensaje = "Has usado " + (maestra ? "la llave maestra" : nombreLlave) + ".";
            return true;
        }

        /// <summary>Desbloquea sin llave (desde un evento, una cinemática, etc.).</summary>
        public void Desbloquear()
        {
            if (!bloqueada) return;
            bloqueada = false;
            Sonido.Reproducir(fuenteAudio, sonidoDesbloquear, transform.position);
            alDesbloquear.Invoke();
        }

        public void Bloquear()
        {
            if (bloqueada) return;
            bloqueada = true;
            alBloquear.Invoke();
        }
    }
}
