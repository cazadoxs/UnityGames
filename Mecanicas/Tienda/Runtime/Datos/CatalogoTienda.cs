using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Lista de artículos que ofrece una tienda. Puedes tener varios catálogos (uno por tienda o por nivel).
    /// </summary>
    [CreateAssetMenu(menuName = "Mecánicas/Tienda/Catálogo", fileName = "NuevoCatalogo", order = 1)]
    public class CatalogoTienda : ScriptableObject
    {
        public string nombreTienda = "Tienda";
        public List<ArticuloTienda> articulos = new List<ArticuloTienda>();

        /// <summary>Categorías en el orden en que aparecen por primera vez.</summary>
        public List<string> Categorias()
        {
            var lista = new List<string>();
            foreach (var a in articulos)
            {
                if (a == null || !a.visibleEnTienda) continue;
                string c = string.IsNullOrEmpty(a.categoria) ? "General" : a.categoria;
                if (!lista.Contains(c)) lista.Add(c);
            }
            return lista;
        }

        public ArticuloTienda Buscar(string id)
        {
            foreach (var a in articulos)
                if (a != null && a.Id == id) return a;
            return null;
        }
    }
}
