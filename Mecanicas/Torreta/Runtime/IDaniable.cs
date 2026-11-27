using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>Datos de un golpe: cuánto daño, dónde y quién lo causó.</summary>
    public struct InfoDanio
    {
        public float cantidad;
        public Vector3 punto;
        public Vector3 direccion;
        public GameObject atacante;

        public InfoDanio(float cantidad, Vector3 punto, Vector3 direccion, GameObject atacante)
        {
            this.cantidad = cantidad;
            this.punto = punto;
            this.direccion = direccion;
            this.atacante = atacante;
        }
    }

    /// <summary>
    /// Cualquier cosa que pueda recibir daño de un proyectil. Implementa esta interfaz en tus
    /// propios scripts (enemigos, barriles, puertas...) si no quieres usar el componente Salud.
    /// </summary>
    public interface IDaniable
    {
        bool EstaVivo { get; }
        float VidaActual { get; }
        void RecibirDanio(InfoDanio info);
    }
}
