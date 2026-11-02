using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Cómo se entrega lo que se compra.
    /// </summary>
    public enum ModoEntrega
    {
        /// <summary>Se instancia el prefab en un punto de aparición.</summary>
        Aparecer,
        /// <summary>Se manda al inventario (IInventarioCompras). Si no cabe, la compra falla.</summary>
        Inventario,
        /// <summary>Intenta el inventario y, si no hay o no cabe, hace aparecer el objeto.</summary>
        InventarioOAparecer,
        /// <summary>No se entrega nada físico: solo se lanzan los eventos (mejoras, desbloqueos...).</summary>
        SoloEvento
    }

    /// <summary>
    /// Un artículo que se puede comprar. Es un asset (clic derecho &gt; Create &gt; Mecánicas &gt; Tienda &gt; Artículo).
    /// El asset no se modifica en juego: el stock y las compras hechas los lleva el GestorTienda.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecánicas/Tienda/Artículo", fileName = "NuevoArticulo", order = 0)]
    public class ArticuloTienda : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Identificador único. Si se deja vacío se usa el nombre del asset.")]
        public string id;
        [Tooltip("Nombre que ve el jugador. Si se deja vacío se usa el nombre del asset.")]
        public string nombre;
        [TextArea(2, 4)] public string descripcion;
        public Sprite icono;
        [Tooltip("Pestaña de la tienda en menú en la que aparece.")]
        public string categoria = "General";

        [Header("Precio")]
        [Tooltip("Id de la moneda con la que se paga (debe existir en el monedero).")]
        public string moneda = "monedas";
        [Min(0)] public int precio = 10;

        [Header("Qué se entrega")]
        public ModoEntrega modoEntrega = ModoEntrega.Aparecer;
        [Tooltip("Prefab que aparece al comprar (modos Aparecer e InventarioOAparecer).")]
        public GameObject prefab;
        [Min(1)] public int cantidadPorCompra = 1;
        [Tooltip("Id con el que se manda al inventario. Si se deja vacío se usa el id del artículo.")]
        public string idInventario;

        [Header("Límites")]
        [Tooltip("Cuántas veces se puede comprar en total. 0 = sin límite. Pon 1 para desbloqueos que se compran una sola vez.")]
        [Min(0)] public int maxCompras = 0;
        [Tooltip("Unidades disponibles en la tienda al empezar. -1 = infinitas.")]
        public int stockInicial = -1;
        [Tooltip("Si se desmarca, no sale en la tienda en menú (pero se puede comprar con botones o por código).")]
        public bool visibleEnTienda = true;

        public string Id { get { return string.IsNullOrEmpty(id) ? name : id; } }
        public string NombreVisible { get { return string.IsNullOrEmpty(nombre) ? name : nombre; } }
        public string IdInventario { get { return string.IsNullOrEmpty(idInventario) ? Id : idInventario; } }
        public bool EsGratis { get { return precio <= 0; } }

        /// <summary>Crea un artículo desde código (útil para tiendas generadas en juego).</summary>
        public static ArticuloTienda Crear(string id, string nombre, int precio, GameObject prefab, string moneda = "monedas")
        {
            var a = CreateInstance<ArticuloTienda>();
            a.name = id;
            a.id = id;
            a.nombre = nombre;
            a.precio = precio;
            a.prefab = prefab;
            a.moneda = moneda;
            return a;
        }
    }
}
