using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>Candado que se abre con una llave del llavero del jugador.</summary>
    [AddComponentMenu("Mecánicas/Puertas/Candado con llave")]
    public class Candado : CandadoBase
    {
        [Header("Llave")]
        [SerializeField] string idLlave = "llave_azul";
        [SerializeField] string nombreLlave = "la llave azul";
        [SerializeField] bool gastarLlave = true;
        [SerializeField] bool aceptaLlaveMaestra = true;

        bool TieneLlave(Interactor i, out bool maestra)
        {
            maestra = false;
            var ll = i != null ? i.Llaves : null;
            if (ll == null) return false;
            if (ll.TieneLlave(idLlave)) return true;
            maestra = aceptaLlaveMaestra && ll.TieneLlave(CerraduraLlave.IdLlaveMaestra);
            return maestra;
        }

        public override string ObtenerTexto(Interactor interactor) =>
            TieneLlave(interactor, out _) ? "Abrir candado con " + nombreLlave : "Candado";

        protected override void AlInteractuar(Interactor interactor)
        {
            if (!TieneLlave(interactor, out bool maestra))
            {
                FalloAbrir(interactor, "Necesitas " + nombreLlave + ".");
                return;
            }
            if (gastarLlave && !maestra) interactor.Llaves.QuitarLlave(idLlave);
            interactor.MostrarMensaje("Has abierto el candado.");
            AbrirCandado();
        }
    }
}
