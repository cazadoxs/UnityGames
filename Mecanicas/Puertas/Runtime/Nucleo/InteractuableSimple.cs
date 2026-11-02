using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Interactuable genérico sin lógica propia: solo lanza el evento "alInteractuar" y muestra un mensaje opcional.
    /// Úsalo para notas, carteles, timbres o para disparar cualquier cosa desde el Inspector.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Interactuable simple")]
    public class InteractuableSimple : Interactuable
    {
        [Tooltip("Mensaje que se enseña al usarlo (vacío = ninguno).")]
        [TextArea] [SerializeField] string mensaje = "";
        [SerializeField] float duracionMensaje = 3f;
        [SerializeField] AudioClip sonido;
        [Tooltip("Si está marcado, solo se puede usar una vez.")]
        [SerializeField] bool unSoloUso;

        protected override void AlInteractuar(Interactor interactor)
        {
            if (!string.IsNullOrEmpty(mensaje) && interactor != null) interactor.MostrarMensaje(mensaje, duracionMensaje);
            ReproducirSonido(sonido);
            if (unSoloUso) Habilitado = false;
        }
    }
}
