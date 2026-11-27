using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Objetos que se manejan manteniendo pulsado y moviendo el ratón (puertas, palancas, pestillos).
    /// El <see cref="Interactor"/> bloquea la cámara mientras dura el arrastre.
    /// Un clic corto sin mover el ratón se trata como interacción normal.
    /// </summary>
    public interface IArrastrable
    {
        /// <summary>Si es false en este momento, el objeto se usa solo con clic.</summary>
        bool UsaArrastre { get; }
        void EmpezarArrastre(Interactor interactor, RaycastHit impacto);
        /// <summary><paramref name="movimientoMundo"/> es el desplazamiento de la mano en metros, en coordenadas de mundo.</summary>
        void Arrastrar(Interactor interactor, Vector3 movimientoMundo);
        void TerminarArrastre(Interactor interactor);
        /// <summary>Si es true, el movimiento vertical del ratón se interpreta como empujar/tirar (puertas).</summary>
        bool ArrastreEnProfundidad { get; }
    }

    /// <summary>Cálculos compartidos por los objetos arrastrables.</summary>
    public static class ArrastreUtil
    {
        /// <summary>
        /// Convierte el movimiento de la mano en un cambio de ángulo (grados) alrededor de un eje,
        /// como si se empujara el punto <paramref name="puntoAgarre"/>.
        /// </summary>
        public static float GradosAlrededorDeEje(Vector3 pivote, Vector3 ejeMundo, Vector3 puntoAgarre, Vector3 movimiento)
        {
            Vector3 radio = Vector3.ProjectOnPlane(puntoAgarre - pivote, ejeMundo);
            float r = radio.magnitude;
            if (r < 0.01f) return 0f;
            Vector3 tangente = Vector3.Cross(ejeMundo.normalized, radio).normalized;
            return Vector3.Dot(movimiento, tangente) / r * Mathf.Rad2Deg;
        }
    }
}
