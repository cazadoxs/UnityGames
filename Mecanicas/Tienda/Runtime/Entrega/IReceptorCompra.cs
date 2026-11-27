using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Algo que recibe un artículo comprado y lo hace existir en el mundo.
    /// PuntoAparicion es la implementación incluida; crea la tuya para entregas especiales
    /// (un dron que lo trae, una cinta transportadora, un buzón...).
    /// </summary>
    public interface IReceptorCompra
    {
        /// <summary>Comprueba antes de cobrar si se podrá entregar.</summary>
        bool PuedeEntregar(ArticuloTienda articulo, int cantidad);
        /// <summary>Entrega. Añade a <paramref name="creados"/> los objetos instanciados. Devuelve false si falló (se devuelve el dinero).</summary>
        bool Entregar(ArticuloTienda articulo, int cantidad, List<GameObject> creados);
    }

    /// <summary>
    /// Gancho OPCIONAL para mandar las compras a un inventario. La tienda no depende de ningún inventario concreto:
    /// escribe un adaptador pequeño (un MonoBehaviour que implemente esta interfaz y llame a tu inventario)
    /// y arrástralo al campo Inventario del GestorTienda. Mira el README, sección «Conectar un inventario».
    /// </summary>
    public interface IInventarioCompras
    {
        bool PuedeAnadir(ArticuloTienda articulo, int cantidad);
        bool Anadir(ArticuloTienda articulo, int cantidad);
    }
}
