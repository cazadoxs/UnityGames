using System.Collections;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>Hace temblar la cámara del jugador (explosión, terremoto, golpe...).</summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Temblor de cámara")]
    public class AccionTemblorCamara : AccionEvento
    {
        [Tooltip("Cámara a mover. Vacío = la cámara principal.")]
        [SerializeField] private Camera camara;

        [SerializeField] private ParametrosTemblor temblor = new ParametrosTemblor();

        [Header("Distancia")]
        [Tooltip("Si está marcado, el temblor se debilita cuanto más lejos esté la cámara del origen.")]
        [SerializeField] private bool atenuarPorDistancia = false;

        [Tooltip("Origen del temblor. Vacío = esta acción.")]
        [SerializeField] private Transform origen;

        [Tooltip("Hasta esta distancia el temblor es completo.")]
        [Min(0f)] [SerializeField] private float radioCompleto = 3f;

        [Tooltip("A partir de esta distancia ya no se nota.")]
        [Min(0.01f)] [SerializeField] private float radioMaximo = 20f;

        [Tooltip("Si está marcado, la acción no termina hasta que acaba el temblor.")]
        [SerializeField] private bool esperarDuracion = false;

        /// <summary>Parámetros editables por código.</summary>
        public ParametrosTemblor Temblor => temblor;

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            CamaraEventos ce = CamaraEventos.Obtener(camara);
            if (ce == null) yield break;

            float factor = 1f;
            if (atenuarPorDistancia)
            {
                Vector3 o = (origen != null ? origen : transform).position;
                float d = Vector3.Distance(o, ce.transform.position);
                factor = 1f - Mathf.InverseLerp(radioCompleto, Mathf.Max(radioCompleto + 0.01f, radioMaximo), d);
            }

            ce.Temblar(temblor, factor);
            if (esperarDuracion) yield return new WaitForSeconds(temblor.duracion);
        }

        private void OnDrawGizmosSelected()
        {
            if (!atenuarPorDistancia) return;
            Vector3 o = (origen != null ? origen : transform).position;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(o, radioCompleto);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(o, radioMaximo);
        }
    }
}
