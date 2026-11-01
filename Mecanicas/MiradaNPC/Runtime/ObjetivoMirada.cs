using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.MiradaNPC
{
    /// <summary>
    /// Marca un objeto como "algo que los NPC pueden mirar" (el jugador, otro NPC, un objeto llamativo...).
    /// En primera persona lo normal es ponerlo en la cámara del jugador, así los NPC miran a los ojos.
    /// </summary>
    [AddComponentMenu("Mecánicas/Mirada NPC/Objetivo de Mirada")]
    [DisallowMultipleComponent]
    public class ObjetivoMirada : MonoBehaviour
    {
        static readonly List<ObjetivoMirada> activos = new List<ObjetivoMirada>();

        /// <summary>Todos los objetivos activos en la escena.</summary>
        public static IReadOnlyList<ObjetivoMirada> Activos => activos;

        [Tooltip("Desplazamiento local del punto exacto que se mira (por ejemplo, la altura de los ojos si este componente está en los pies).")]
        public Vector3 desplazamiento = Vector3.zero;

        [Tooltip("Si varios objetivos son válidos, el NPC elige el de mayor prioridad; si empatan, el más cercano.")]
        public int prioridad = 0;

        /// <summary>Punto del mundo al que miran los NPC.</summary>
        public Vector3 Punto => transform.TransformPoint(desplazamiento);

        void OnEnable()
        {
            if (!activos.Contains(this)) activos.Add(this);
        }

        void OnDisable()
        {
            activos.Remove(this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Punto, 0.08f);
        }
    }
}
