using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Tienda.Demo
{
    /// <summary>
    /// Ejemplo de adaptador de inventario: implementa IInventarioCompras.
    /// En tu proyecto, haz lo mismo pero llamando a tu inventario real dentro de PuedeAnadir/Anadir.
    /// </summary>
    public class InventarioDemo : MonoBehaviour, IInventarioCompras
    {
        [Min(1)] public int capacidad = 5;
        public readonly Dictionary<string, int> objetos = new Dictionary<string, int>();

        public int Total
        {
            get { int t = 0; foreach (var kv in objetos) t += kv.Value; return t; }
        }

        public bool PuedeAnadir(ArticuloTienda articulo, int cantidad)
        {
            return Total + cantidad <= capacidad;
        }

        public bool Anadir(ArticuloTienda articulo, int cantidad)
        {
            if (!PuedeAnadir(articulo, cantidad)) return false;
            int n;
            objetos.TryGetValue(articulo.NombreVisible, out n);
            objetos[articulo.NombreVisible] = n + cantidad;
            return true;
        }

        /// <summary>Gasta uno (para probar a llenar y vaciar).</summary>
        public void UsarUno()
        {
            foreach (var k in new List<string>(objetos.Keys))
            {
                objetos[k]--;
                if (objetos[k] <= 0) objetos.Remove(k);
                return;
            }
        }
    }
}
