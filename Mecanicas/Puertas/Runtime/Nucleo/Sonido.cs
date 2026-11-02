using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>Ayuda para reproducir sonidos opcionales sin repetir comprobaciones de null.</summary>
    public static class Sonido
    {
        /// <summary>
        /// Reproduce <paramref name="clip"/> en <paramref name="fuente"/> si existe;
        /// si no hay fuente, lo reproduce como sonido 3D en <paramref name="posicion"/>.
        /// </summary>
        public static void Reproducir(AudioSource fuente, AudioClip clip, Vector3 posicion, float volumen = 1f)
        {
            if (clip == null) return;
            if (fuente != null) fuente.PlayOneShot(clip, volumen);
            else AudioSource.PlayClipAtPoint(clip, posicion, volumen);
        }
    }
}
