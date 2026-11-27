using System;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Cualquier cosa que guarde dinero. Implementa esta interfaz en un MonoBehaviour para
    /// conectar la tienda a la economía que ya tenga tu juego (y arrástralo al campo Monedero del GestorTienda).
    /// </summary>
    public interface IMonedero
    {
        int Saldo(string moneda);
        /// <summary>Resta dinero. Devuelve false (sin cambiar nada) si no hay bastante.</summary>
        bool Gastar(string moneda, int cantidad);
        void Anadir(string moneda, int cantidad);
        /// <summary>(moneda, saldo nuevo)</summary>
        event Action<string, int> SaldoCambiado;
    }
}
