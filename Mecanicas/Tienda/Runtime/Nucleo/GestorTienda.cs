using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Cerebro del sistema. Cobra, comprueba stock y límites, entrega el artículo y avisa con eventos.
    /// Todas las formas de comprar (botón en el mundo, tienda en menú, disparador, tu propio código)
    /// acaban llamando a <see cref="Comprar(ArticuloTienda, IReceptorCompra)"/>.
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/Gestor de tienda")]
    [DefaultExecutionOrder(-50)]
    public class GestorTienda : MonoBehaviour
    {
        /// <summary>La primera tienda activa de la escena. La usan los botones que no tienen tienda asignada.</summary>
        public static GestorTienda Principal { get; private set; }

        /// <summary>Se lanza tras cualquier intento de compra en cualquier tienda (para logros, analíticas, misiones...).</summary>
        public static event Action<ResultadoCompra> CualquierCompra;

        [Tooltip("Id de esta tienda (para el guardado de compras). Cámbialo si hay varias tiendas.")]
        public string idTienda = "Tienda";
        public CatalogoTienda catalogo;

        [Header("Conexiones")]
        [Tooltip("Componente que implementa IMonedero. Vacío = busca un Monedero en este objeto o en la escena.")]
        public MonoBehaviour monedero;
        [Tooltip("OPCIONAL. Componente que implementa IInventarioCompras. Vacío = sin inventario (o búsqueda automática).")]
        public MonoBehaviour inventario;
        [Tooltip("Si no hay inventario asignado, busca en la escena cualquier componente que implemente IInventarioCompras.")]
        public bool buscarInventarioEnEscena = true;
        [Tooltip("Dónde aparecen los objetos si quien compra no indica otro punto (componente que implementa IReceptorCompra).")]
        public MonoBehaviour receptorPorDefecto;

        [Header("Guardado")]
        [Tooltip("Guarda en PlayerPrefs cuántas veces se ha comprado cada artículo (útil para desbloqueos de una sola compra).")]
        public bool guardarCompras = false;

        [Header("Sonido")]
        public AudioClip sonidoCompra;
        public AudioClip sonidoError;
        [Range(0, 1)] public float volumen = 1f;

        [Header("Textos (usa {nombre}, {precio}, {moneda}, {saldo})")]
        public TextosTienda textos = new TextosTienda();

        [Serializable] public class EventoArticulo : UnityEvent<ArticuloTienda> { }
        [Serializable] public class EventoTexto : UnityEvent<string> { }
        [Header("Eventos")]
        public EventoArticulo alComprar = new EventoArticulo();
        [Tooltip("Recibe el mensaje de error ya formateado.")]
        public EventoTexto alFallar = new EventoTexto();

        /// <summary>Se lanza tras cada intento de compra en esta tienda (éxito o fallo).</summary>
        public event Action<ResultadoCompra> CompraProcesada;

        readonly Dictionary<string, int> comprasHechas = new Dictionary<string, int>();
        readonly Dictionary<string, int> stock = new Dictionary<string, int>();
        IMonedero monederoCache;
        IInventarioCompras inventarioCache;
        bool inventarioBuscado;
        AudioSource audioSource;

        // ---------------------------------------------------------------- ciclo de vida

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstatico()
        {
            Principal = null;
            CualquierCompra = null;
        }

        void OnEnable()
        {
            if (Principal == null) Principal = this;
        }

        void OnDisable()
        {
            if (Principal == this)
            {
                Principal = null;
                var otra = FindFirstObjectByType<GestorTienda>();
                if (otra != null && otra != this && otra.isActiveAndEnabled) Principal = otra;
            }
        }

        // ---------------------------------------------------------------- conexiones

        public IMonedero Monedero
        {
            get
            {
                if (monederoCache != null) return monederoCache;
                monederoCache = monedero as IMonedero;
                if (monederoCache == null) monederoCache = GetComponent<IMonedero>();
                if (monederoCache == null) monederoCache = FindFirstObjectByType<Monedero>();
                if (monederoCache == null)
                    Debug.LogWarning("[Tienda] No hay monedero: añade un componente Monedero o asigna uno que implemente IMonedero.", this);
                return monederoCache;
            }
            set { monederoCache = value; monedero = value as MonoBehaviour; }
        }

        public IInventarioCompras Inventario
        {
            get
            {
                if (inventarioCache != null) return inventarioCache;
                inventarioCache = inventario as IInventarioCompras;
                if (inventarioCache == null && buscarInventarioEnEscena && !inventarioBuscado)
                {
                    inventarioBuscado = true;
                    foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    {
                        var inv = mb as IInventarioCompras;
                        if (inv != null) { inventarioCache = inv; break; }
                    }
                }
                return inventarioCache;
            }
            set { inventarioCache = value; inventario = value as MonoBehaviour; }
        }

        public IReceptorCompra ReceptorPorDefecto
        {
            get
            {
                var r = receptorPorDefecto as IReceptorCompra;
                if (r == null) r = GetComponentInChildren<PuntoAparicion>();
                return r;
            }
        }

        // ---------------------------------------------------------------- consultas

        public int ComprasHechas(ArticuloTienda a)
        {
            if (a == null) return 0;
            int n;
            if (comprasHechas.TryGetValue(a.Id, out n)) return n;
            n = guardarCompras ? PlayerPrefs.GetInt(ClaveCompras(a), 0) : 0;
            comprasHechas[a.Id] = n;
            return n;
        }

        /// <summary>Unidades que quedan. -1 = infinitas.</summary>
        public int StockRestante(ArticuloTienda a)
        {
            if (a == null) return 0;
            int n;
            if (stock.TryGetValue(a.Id, out n)) return n;
            stock[a.Id] = a.stockInicial;
            return a.stockInicial;
        }

        public void Reponer(ArticuloTienda a, int unidades)
        {
            if (a == null) return;
            int s = StockRestante(a);
            if (s < 0) return;
            stock[a.Id] = s + unidades;
        }

        public int Saldo(string moneda)
        {
            var m = Monedero;
            return m != null ? m.Saldo(moneda) : 0;
        }

        /// <summary>Comprueba (sin cobrar) si el artículo se puede comprar ahora mismo.</summary>
        public MotivoCompra PuedeComprar(ArticuloTienda a)
        {
            if (a == null) return MotivoCompra.ArticuloNulo;
            return ReglasCompra.Validar(Saldo(a.moneda), a.precio, StockRestante(a), ComprasHechas(a), a.maxCompras);
        }

        // ---------------------------------------------------------------- comprar

        /// <summary>Compra con el receptor por defecto. Pensado para UnityEvents (botones de UI, Timeline...).</summary>
        public void ComprarArticulo(ArticuloTienda articulo) { Comprar(articulo, null); }

        /// <summary>Compra por id del catálogo. Pensado para UnityEvents con texto.</summary>
        public void ComprarPorId(string idArticulo)
        {
            var a = catalogo != null ? catalogo.Buscar(idArticulo) : null;
            if (a == null) Debug.LogWarning("[Tienda] No hay ningún artículo con id '" + idArticulo + "' en el catálogo.", this);
            Comprar(a, null);
        }

        /// <summary>
        /// Intenta comprar un artículo. <paramref name="receptor"/> = dónde aparece (null = el de por defecto).
        /// Si la entrega falla después de cobrar, se devuelve el dinero.
        /// </summary>
        public ResultadoCompra Comprar(ArticuloTienda articulo, IReceptorCompra receptor = null)
        {
            var motivo = PuedeComprar(articulo);
            if (motivo != MotivoCompra.Ok) return Terminar(new ResultadoCompra(articulo, motivo));

            var monederoActual = Monedero;
            if (monederoActual == null && articulo.precio > 0) return Terminar(new ResultadoCompra(articulo, MotivoCompra.PagoRechazado));

            // 1) Decidir la entrega ANTES de cobrar.
            int cantidad = Mathf.Max(1, articulo.cantidadPorCompra);
            if (receptor == null) receptor = ReceptorPorDefecto;
            var inv = Inventario;
            bool alInventario = false;
            switch (articulo.modoEntrega)
            {
                case ModoEntrega.Inventario:
                    if (inv == null || !inv.PuedeAnadir(articulo, cantidad))
                        return Terminar(new ResultadoCompra(articulo, MotivoCompra.EntregaImposible));
                    alInventario = true;
                    break;
                case ModoEntrega.InventarioOAparecer:
                    alInventario = inv != null && inv.PuedeAnadir(articulo, cantidad);
                    if (!alInventario && (receptor == null || !receptor.PuedeEntregar(articulo, cantidad)))
                        return Terminar(new ResultadoCompra(articulo, MotivoCompra.EntregaImposible));
                    break;
                case ModoEntrega.Aparecer:
                    if (receptor == null || !receptor.PuedeEntregar(articulo, cantidad))
                    {
                        if (receptor == null)
                            Debug.LogWarning("[Tienda] '" + articulo.NombreVisible + "' debe aparecer pero no hay ningún PuntoAparicion asignado.", this);
                        return Terminar(new ResultadoCompra(articulo, MotivoCompra.EntregaImposible));
                    }
                    break;
            }

            // 2) Cobrar.
            if (articulo.precio > 0 && !monederoActual.Gastar(articulo.moneda, articulo.precio))
                return Terminar(new ResultadoCompra(articulo, MotivoCompra.SinDinero));

            // 3) Entregar (y devolver el dinero si algo sale mal).
            var resultado = new ResultadoCompra(articulo, MotivoCompra.Ok);
            bool entregado = true;
            if (articulo.modoEntrega != ModoEntrega.SoloEvento)
            {
                if (alInventario)
                {
                    entregado = inv.Anadir(articulo, cantidad);
                    resultado.EntregadoEnInventario = entregado;
                }
                else
                {
                    entregado = receptor.Entregar(articulo, cantidad, resultado.ObjetosCreados);
                }
            }
            if (!entregado)
            {
                if (articulo.precio > 0) monederoActual.Anadir(articulo.moneda, articulo.precio);
                resultado.Motivo = MotivoCompra.EntregaImposible;
                return Terminar(resultado);
            }

            // 4) Apuntar la compra.
            int hechas = ComprasHechas(articulo) + 1;
            comprasHechas[articulo.Id] = hechas;
            if (guardarCompras) PlayerPrefs.SetInt(ClaveCompras(articulo), hechas);
            stock[articulo.Id] = ReglasCompra.StockTrasCompra(StockRestante(articulo));

            return Terminar(resultado);
        }

        ResultadoCompra Terminar(ResultadoCompra r)
        {
            r.Mensaje = textos.Formatear(r, Saldo(r.Articulo != null ? r.Articulo.moneda : ""));
            if (r.Exito)
            {
                Sonar(sonidoCompra);
                alComprar.Invoke(r.Articulo);
            }
            else
            {
                Sonar(sonidoError);
                alFallar.Invoke(r.Mensaje);
            }
            if (CompraProcesada != null) CompraProcesada(r);
            if (CualquierCompra != null) CualquierCompra(r);
            return r;
        }

        void Sonar(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f;
                }
            }
            audioSource.PlayOneShot(clip, volumen);
        }

        string ClaveCompras(ArticuloTienda a) { return "Tienda_" + idTienda + "_" + a.Id; }

        /// <summary>Borra las compras guardadas y el stock (reinicia la tienda).</summary>
        public void ReiniciarTienda()
        {
            foreach (var id in new List<string>(comprasHechas.Keys))
                PlayerPrefs.DeleteKey("Tienda_" + idTienda + "_" + id);
            comprasHechas.Clear();
            stock.Clear();
        }
    }

    /// <summary>Mensajes que ve el jugador. Edítalos en el inspector del GestorTienda para traducir o cambiar el tono.</summary>
    [Serializable]
    public class TextosTienda
    {
        public string compraHecha = "Has comprado {nombre}";
        public string compraAlInventario = "{nombre} añadido al inventario";
        public string sinDinero = "No tienes suficientes {moneda} (necesitas {precio}, tienes {saldo})";
        public string sinStock = "{nombre} está agotado";
        public string limite = "Ya has comprado {nombre}";
        public string entregaImposible = "No se puede entregar {nombre} ahora mismo";
        public string pagoRechazado = "No se ha podido cobrar";
        public string enEspera = "Espera un momento...";
        public string gratis = "Gratis";

        public string Formatear(ResultadoCompra r, int saldo)
        {
            string plantilla;
            switch (r.Motivo)
            {
                case MotivoCompra.Ok: plantilla = r.EntregadoEnInventario ? compraAlInventario : compraHecha; break;
                case MotivoCompra.SinDinero: plantilla = sinDinero; break;
                case MotivoCompra.SinStock: plantilla = sinStock; break;
                case MotivoCompra.LimiteAlcanzado: plantilla = limite; break;
                case MotivoCompra.EntregaImposible: plantilla = entregaImposible; break;
                case MotivoCompra.EnEspera: plantilla = enEspera; break;
                case MotivoCompra.ArticuloNulo: return "No hay artículo asignado";
                default: plantilla = pagoRechazado; break;
            }
            return Rellenar(plantilla, r.Articulo, saldo);
        }

        public string Precio(ArticuloTienda a)
        {
            if (a == null) return "";
            return a.EsGratis ? gratis : a.precio + " " + a.moneda;
        }

        public static string Rellenar(string plantilla, ArticuloTienda a, int saldo)
        {
            if (string.IsNullOrEmpty(plantilla)) return "";
            if (a == null) return plantilla;
            return plantilla
                .Replace("{nombre}", a.NombreVisible)
                .Replace("{precio}", a.precio.ToString())
                .Replace("{moneda}", a.moneda)
                .Replace("{saldo}", saldo.ToString());
        }
    }
}
