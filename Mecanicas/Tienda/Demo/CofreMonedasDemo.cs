using UnityEngine;

namespace Mecanicas.Tienda.Demo
{
    /// <summary>Cofre de la demo que da dinero al usarlo, para poder seguir probando compras.</summary>
    public class CofreMonedasDemo : MonoBehaviour, IInteractuableTienda
    {
        public string moneda = "monedas";
        public int cantidad = 100;
        public string monedaExtra = "gemas";
        public int cantidadExtra = 1;

        public string TextoAviso { get { return "Coger " + cantidad + " " + moneda + " y " + cantidadExtra + " " + monedaExtra; } }
        public bool PuedeInteractuar { get { return GestorTienda.Principal != null; } }

        public void Interactuar(GameObject quien)
        {
            var m = GestorTienda.Principal.Monedero;
            if (m == null) return;
            m.Anadir(moneda, cantidad);
            if (cantidadExtra > 0) m.Anadir(monedaExtra, cantidadExtra);
        }
    }
}
