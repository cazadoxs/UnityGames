using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Eventos
{
    /// <summary>
    /// Llama a cualquier método público de cualquier objeto (UnityEvent) en su sitio dentro de la secuencia.
    /// Es la forma de conectar otras mecánicas sin escribir código: abrir una puerta, dar un objeto, lanzar un DialogueStarter...
    /// </summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Llamar evento (UnityEvent)")]
    public class AccionUnityEvent : AccionEvento
    {
        [SerializeField] private UnityEvent evento = new UnityEvent();

        public UnityEvent Evento => evento;

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            evento.Invoke();
            yield break;
        }
    }
}
