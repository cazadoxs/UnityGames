using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Bloquea una puerta mientras una señal (palanca, botón, interruptor, panel, combinador...) esté inactiva.
    /// Ejemplo: una reja que solo se puede abrir con la palanca bajada.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Bloqueo por señal")]
    public class BloqueoPorSenal : MonoBehaviour, IBloqueo
    {
        [Tooltip("Componente que implementa ISenal: Palanca, Boton, Interruptor, PanelNumerico, CombinadorSenales...")]
        [SerializeField] MonoBehaviour fuente;
        [Tooltip("Si está marcado, bloquea cuando la señal está ACTIVA en vez de inactiva.")]
        [SerializeField] bool invertir;
        [SerializeField] string motivo = "No se abre. Parece que la controla algún mecanismo.";

        ISenal senal;

        void Awake() => senal = fuente as ISenal;

        void OnValidate()
        {
            if (fuente != null && !(fuente is ISenal))
            {
                var s = fuente.GetComponent<ISenal>();
                fuente = s as MonoBehaviour;
            }
        }

        public bool Bloqueado => senal != null && (senal.Activa == invertir);
        public bool ImpideCerrar => false;
        public string Motivo => motivo;

        public bool IntentarDesbloquearDesdePuerta(Interactor interactor, out string mensaje)
        {
            mensaje = null;
            return !Bloqueado;
        }
    }
}
