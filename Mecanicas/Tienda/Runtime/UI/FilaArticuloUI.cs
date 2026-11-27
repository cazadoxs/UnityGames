using UnityEngine;
using UnityEngine.UI;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Una fila de la tienda en menú (icono, nombre, descripción, precio y botón Comprar).
    /// Para usar tu propio diseño: haz un prefab con este componente y asigna sus referencias
    /// (todas son opcionales). Para usar TextMeshPro, hereda de esta clase y sobrescribe Pintar().
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/UI/Fila de artículo")]
    public class FilaArticuloUI : MonoBehaviour
    {
        public Image icono;
        public Text textoNombre;
        public Text textoDescripcion;
        public Text textoPrecio;
        [Tooltip("Texto pequeño de estado: agotado, ya comprado, quedan X...")]
        public Text textoEstado;
        public Button botonComprar;
        public Text textoBoton;
        [Tooltip("Se oscurece (alpha) cuando no se puede comprar.")]
        public CanvasGroup grupo;

        public ArticuloTienda Articulo { get; private set; }
        protected TiendaMenu menu;

        public virtual void Configurar(ArticuloTienda articulo, TiendaMenu menuTienda)
        {
            Articulo = articulo;
            menu = menuTienda;
            if (botonComprar != null)
            {
                botonComprar.onClick.RemoveAllListeners();
                botonComprar.onClick.AddListener(AlPulsarComprar);
            }
            Pintar();
        }

        protected virtual void AlPulsarComprar()
        {
            if (menu != null && Articulo != null) menu.ComprarDesdeMenu(Articulo);
        }

        /// <summary>Refresca textos y estado. Se llama al abrir la tienda y después de cada compra.</summary>
        public virtual void Pintar()
        {
            if (Articulo == null || menu == null) return;
            var tienda = menu.TiendaActual;
            var textos = tienda != null ? tienda.textos : new TextosTienda();
            MotivoCompra motivo = tienda != null ? tienda.PuedeComprar(Articulo) : MotivoCompra.PagoRechazado;

            if (icono != null)
            {
                if (Articulo.icono != null) { icono.sprite = Articulo.icono; icono.color = Color.white; }
                else { icono.sprite = null; icono.color = ColorDe(Articulo.Id); }
            }
            if (textoNombre != null) textoNombre.text = Articulo.NombreVisible;
            if (textoDescripcion != null) textoDescripcion.text = Articulo.descripcion;
            if (textoPrecio != null)
            {
                textoPrecio.text = textos.Precio(Articulo);
                textoPrecio.color = motivo == MotivoCompra.SinDinero ? menu.estilo.colorError : menu.estilo.colorAcento;
            }
            if (textoEstado != null) textoEstado.text = TextoEstado(tienda, motivo);
            bool comprable = motivo == MotivoCompra.Ok;
            if (botonComprar != null) botonComprar.interactable = comprable;
            if (textoBoton != null)
                textoBoton.text = motivo == MotivoCompra.LimiteAlcanzado ? "Comprado"
                                : motivo == MotivoCompra.SinStock ? "Agotado" : "Comprar";
            if (grupo != null) grupo.alpha = comprable || motivo == MotivoCompra.SinDinero ? 1f : 0.6f;
        }

        protected virtual string TextoEstado(GestorTienda tienda, MotivoCompra motivo)
        {
            if (tienda == null) return "";
            if (motivo == MotivoCompra.LimiteAlcanzado) return "Ya comprado";
            if (motivo == MotivoCompra.SinStock) return "Agotado";
            string s = "";
            int stock = tienda.StockRestante(Articulo);
            if (stock > 0) s = "Quedan " + stock;
            if (Articulo.cantidadPorCompra > 1) s += (s.Length > 0 ? " · " : "") + "x" + Articulo.cantidadPorCompra;
            if (Articulo.maxCompras > 1) s += (s.Length > 0 ? " · " : "") + tienda.ComprasHechas(Articulo) + "/" + Articulo.maxCompras;
            return s;
        }

        static Color ColorDe(string id)
        {
            int h = (id ?? "").GetHashCode();
            return Color.HSVToRGB(Mathf.Abs(h % 360) / 360f, 0.45f, 0.75f);
        }
    }
}
