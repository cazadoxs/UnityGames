using System.Collections;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>
    /// Hace que la cámara del jugador mire hacia un objeto o dirección, de golpe o con recorrido,
    /// y luego vuelva o se quede mirando.
    /// </summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Mirar con la cámara")]
    public class AccionMirarCamara : AccionEvento
    {
        [Tooltip("Cámara a mover. Vacío = la cámara principal (etiqueta MainCamera).")]
        [SerializeField] private Camera camara;

        [SerializeField] private ParametrosMirada mirada = new ParametrosMirada();

        private CamaraEventos usada;
        private int token;

        /// <summary>Parámetros editables por código.</summary>
        public ParametrosMirada Mirada => mirada;

        private void Reset()
        {
            mirada.objetivo = transform;
        }

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            usada = CamaraEventos.Obtener(camara);
            if (usada == null) yield break;

            token = usada.Mirar(mirada);
            while (usada != null && usada.MiradaActiva(token))
                yield return null;
        }

        /// <summary>Si la mirada estaba esperando indefinidamente (tiempo -1), la termina.</summary>
        public void TerminarMirada()
        {
            if (usada != null && usada.MiradaActiva(token)) usada.TerminarEspera();
        }

        public override void Detener()
        {
            base.Detener();
            if (usada != null && usada.MiradaActiva(token)) usada.CancelarMirada(true);
        }

        private void OnDrawGizmosSelected()
        {
            if (mirada.objetivo == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(mirada.objetivo.position + mirada.desplazamiento, 0.25f);
        }
    }
}
