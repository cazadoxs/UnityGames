using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Algo con lo que el jugador puede interactuar mirándolo (botón de compra, terminal de tienda...).
    /// Si tu juego ya tiene un sistema de interacción, llama tú a Interactuar() desde él
    /// y no hace falta usar InteractorTienda.
    /// </summary>
    public interface IInteractuableTienda
    {
        /// <summary>Texto que se muestra al mirarlo, p. ej. "Comprar Caja (20 monedas)".</summary>
        string TextoAviso { get; }
        bool PuedeInteractuar { get; }
        void Interactuar(GameObject quien);
    }
}
