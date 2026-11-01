using UnityEngine;

namespace Mecanicas.MiradaNPC
{
    /// <summary>
    /// Cálculos puros de la mirada (sin estado). Separados para poder probarlos y reutilizarlos.
    /// Convención: yaw positivo = hacia la derecha, pitch positivo = hacia arriba, en grados.
    /// </summary>
    public static class MiradaMatematica
    {
        /// <summary>Convierte una dirección expresada en el espacio local de un marco (adelante = +Z) en yaw y pitch.</summary>
        public static void DireccionAAngulos(Vector3 direccionLocal, out float yaw, out float pitch)
        {
            if (direccionLocal.sqrMagnitude < 1e-8f)
            {
                yaw = 0f;
                pitch = 0f;
                return;
            }
            yaw = Mathf.Atan2(direccionLocal.x, direccionLocal.z) * Mathf.Rad2Deg;
            float horizontal = Mathf.Sqrt(direccionLocal.x * direccionLocal.x + direccionLocal.z * direccionLocal.z);
            pitch = Mathf.Atan2(direccionLocal.y, horizontal) * Mathf.Rad2Deg;
        }

        /// <summary>Rotación (en el espacio del marco) que gira el "adelante" yaw grados a la derecha y pitch grados hacia arriba.</summary>
        public static Quaternion AngulosARotacion(float yaw, float pitch)
        {
            return Quaternion.Euler(-pitch, yaw, 0f);
        }

        /// <summary>¿Están los ángulos dentro de los límites (con un margen extra opcional)?</summary>
        public static bool DentroDeLimites(float yaw, float pitch, LimitesMirada limites, float margen = 0f)
        {
            return yaw >= -(limites.izquierda + margen) && yaw <= limites.derecha + margen
                && pitch >= -(limites.abajo + margen) && pitch <= limites.arriba + margen;
        }

        /// <summary>Recorta yaw y pitch a los límites.</summary>
        public static void Limitar(ref float yaw, ref float pitch, LimitesMirada limites)
        {
            yaw = Mathf.Clamp(yaw, -limites.izquierda, limites.derecha);
            pitch = Mathf.Clamp(pitch, -limites.abajo, limites.arriba);
        }
    }

    /// <summary>Límites de giro en grados, medidos desde la posición de reposo (mirando al frente).</summary>
    [System.Serializable]
    public struct LimitesMirada
    {
        [Tooltip("Grados máximos de giro hacia la izquierda.")]
        [Range(0f, 179f)] public float izquierda;
        [Tooltip("Grados máximos de giro hacia la derecha.")]
        [Range(0f, 179f)] public float derecha;
        [Tooltip("Grados máximos mirando hacia arriba.")]
        [Range(0f, 89f)] public float arriba;
        [Tooltip("Grados máximos mirando hacia abajo.")]
        [Range(0f, 89f)] public float abajo;

        public LimitesMirada(float izquierda, float derecha, float arriba, float abajo)
        {
            this.izquierda = izquierda;
            this.derecha = derecha;
            this.arriba = arriba;
            this.abajo = abajo;
        }

        /// <summary>Límites de un cuello humano aproximado.</summary>
        public static LimitesMirada CuelloHumano => new LimitesMirada(70f, 70f, 40f, 45f);

        /// <summary>Límites de unos ojos humanos aproximados (relativos a la cabeza).</summary>
        public static LimitesMirada OjosHumanos => new LimitesMirada(30f, 30f, 20f, 25f);
    }
}
