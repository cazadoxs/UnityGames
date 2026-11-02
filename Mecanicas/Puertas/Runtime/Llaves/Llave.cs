using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Llave que se recoge del escenario. Al usarla, se añade al llavero del jugador
    /// (<see cref="IPortadorLlaves"/>) y el objeto desaparece.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Llave (recogible)")]
    public class Llave : Interactuable
    {
        [Tooltip("Identificador. Debe coincidir con el de la cerradura o candado que abre.")]
        [SerializeField] string idLlave = "llave_roja";
        [Tooltip("Nombre que ve el jugador, por ejemplo \"la llave roja\".")]
        [SerializeField] string nombreVisible = "la llave roja";
        [SerializeField] AudioClip sonidoRecoger;
        [Tooltip("Destruir el objeto al recogerlo. Si no, solo se desactiva.")]
        [SerializeField] bool destruirAlRecoger = true;

        public string IdLlave => idLlave;
        public string NombreVisible => nombreVisible;

        void Reset() => textoAccion = "Coger llave";

        public override string ObtenerTexto(Interactor interactor) => "Coger " + nombreVisible;

        public override bool PuedeInteractuar(Interactor interactor) =>
            base.PuedeInteractuar(interactor) && interactor != null && interactor.Llaves != null;

        protected override void AlInteractuar(Interactor interactor)
        {
            interactor.Llaves.AnadirLlave(idLlave, nombreVisible);
            interactor.MostrarMensaje("Has cogido " + nombreVisible + ".");
            Sonido.Reproducir(null, sonidoRecoger, transform.position);
            if (destruirAlRecoger) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
