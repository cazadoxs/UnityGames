using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Compra "personalizable": un componente sin interfaz que compra cuando se lo pides.
    /// Conéctalo a cualquier UnityEvent (un Button de UI propio, una animación, un trigger del sistema de eventos,
    /// Timeline, una misión...) llamando a Comprar() o ComprarEste(articulo).
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/Disparador de compra")]
    public class DisparadorCompra : MonoBehaviour
    {
        public ArticuloTienda articulo;
        [Tooltip("Vacío = GestorTienda.Principal.")]
        public GestorTienda tienda;
        [Tooltip("Dónde aparece (IReceptorCompra). Vacío = el de la tienda.")]
        public MonoBehaviour receptor;

        public GestorTienda.EventoArticulo alComprar = new GestorTienda.EventoArticulo();
        public GestorTienda.EventoTexto alFallar = new GestorTienda.EventoTexto();

        public ResultadoCompra UltimoResultado { get; private set; }

        public void Comprar() { ComprarEste(articulo); }

        public void ComprarEste(ArticuloTienda a)
        {
            var t = tienda != null ? tienda : GestorTienda.Principal;
            if (t == null) { Debug.LogWarning("[Tienda] No hay GestorTienda en la escena.", this); return; }
            UltimoResultado = t.Comprar(a, receptor as IReceptorCompra);
            if (UltimoResultado.Exito) alComprar.Invoke(a); else alFallar.Invoke(UltimoResultado.Mensaje);
        }
    }
}
