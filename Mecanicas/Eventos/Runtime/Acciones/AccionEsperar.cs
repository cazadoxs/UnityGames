using System.Collections;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>Pausa la secuencia unos segundos (o hasta que el jugador salga de la zona).</summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Esperar")]
    public class AccionEsperar : AccionEvento
    {
        [Min(0f)] [SerializeField] private float segundos = 1f;

        [Tooltip("Usa tiempo real (sigue contando aunque el juego esté en pausa con Time.timeScale = 0).")]
        [SerializeField] private bool tiempoReal = false;

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            if (tiempoReal) yield return new WaitForSecondsRealtime(segundos);
            else yield return new WaitForSeconds(segundos);
        }
    }
}
