using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Clase base de todo lo que el jugador puede usar mirando y pulsando la tecla de interactuar.
    /// El <see cref="Interactor"/> busca este componente en el collider que mira (o en sus padres).
    /// Para crear un objeto interactuable propio: hereda de esta clase y sobrescribe <see cref="AlInteractuar"/>.
    /// </summary>
    public abstract class Interactuable : MonoBehaviour
    {
        [Header("Interacción")]
        [Tooltip("Texto que aparece en pantalla al mirarlo, por ejemplo \"Abrir\".")]
        [SerializeField] protected string textoAccion = "Usar";
        [Tooltip("Si está desactivado, el jugador no puede usarlo (útil para activarlo más tarde desde un evento).")]
        [SerializeField] bool habilitado = true;
        [Tooltip("Fuente de audio opcional. Si se deja vacía, los sonidos se reproducen en la posición del objeto.")]
        [SerializeField] protected AudioSource fuenteAudio;

        [Tooltip("Se lanza cada vez que el jugador lo usa con éxito.")]
        public UnityEvent alInteractuar = new UnityEvent();

        /// <summary>Permite activar o desactivar el objeto desde código o desde un UnityEvent.</summary>
        public bool Habilitado { get => habilitado; set => habilitado = value; }

        public void Habilitar() => habilitado = true;
        public void Deshabilitar() => habilitado = false;

        /// <summary>Texto de la acción a mostrar. Puede depender del estado (p. ej. "Abrir"/"Cerrar").</summary>
        public virtual string ObtenerTexto(Interactor interactor) => textoAccion;

        /// <summary>Si devuelve false, el HUD no muestra la acción y pulsar no hace nada.</summary>
        public virtual bool PuedeInteractuar(Interactor interactor) => habilitado && isActiveAndEnabled;

        /// <summary>Lo llama el <see cref="Interactor"/>. También se puede llamar desde otros scripts.</summary>
        public void Interactuar(Interactor interactor)
        {
            if (!PuedeInteractuar(interactor)) return;
            AlInteractuar(interactor);
            alInteractuar.Invoke();
        }

        /// <summary>Lógica concreta de cada objeto.</summary>
        protected abstract void AlInteractuar(Interactor interactor);

        protected void ReproducirSonido(AudioClip clip, float volumen = 1f) =>
            Sonido.Reproducir(fuenteAudio, clip, transform.position, volumen);
    }
}
