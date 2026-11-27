using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>Lo que devuelve GestorTienda.Comprar.</summary>
    public class ResultadoCompra
    {
        public bool Exito { get { return Motivo == MotivoCompra.Ok; } }
        public MotivoCompra Motivo;
        public ArticuloTienda Articulo;
        /// <summary>True si acabó en el inventario en vez de aparecer en el mundo.</summary>
        public bool EntregadoEnInventario;
        /// <summary>Objetos instanciados (vacío si fue al inventario o es SoloEvento).</summary>
        public readonly List<GameObject> ObjetosCreados = new List<GameObject>();
        /// <summary>Texto ya preparado para mostrar al jugador.</summary>
        public string Mensaje;

        public ResultadoCompra(ArticuloTienda articulo, MotivoCompra motivo)
        {
            Articulo = articulo;
            Motivo = motivo;
        }
    }
}
