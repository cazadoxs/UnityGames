using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Tienda en menú: lista de artículos por pestañas (categorías), saldo y botón Comprar.
    /// Si no le das una interfaz propia, la construye sola al abrirse por primera vez (ConstructorUITienda).
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/UI/Tienda en menú")]
    public class TiendaMenu : MonoBehaviour
    {
        /// <summary>True si hay alguna tienda en menú abierta (para bloquear la cámara o el movimiento del jugador).</summary>
        public static bool AlgunaAbierta { get { return abiertas > 0; } }
        static int abiertas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ReiniciarEstatico() { abiertas = 0; }

        [Header("Datos")]
        [Tooltip("Vacío = GestorTienda.Principal.")]
        public GestorTienda tienda;
        [Tooltip("Catálogo que se muestra. Vacío = el del GestorTienda. Una TerminalTienda puede sustituirlo al abrir.")]
        public CatalogoTienda catalogo;
        [Tooltip("Dónde aparece lo comprado desde el menú (IReceptorCompra). Vacío = el del GestorTienda.")]
        public MonoBehaviour receptor;

        [Header("Abrir y cerrar")]
        public bool abrirConTecla = true;
        public TeclaTienda teclaAbrir = TeclaTienda.Tab;
        public TeclaTienda teclaCerrar = TeclaTienda.Escape;
        public bool cerrarTrasComprar = false;
        [Tooltip("Libera y muestra el cursor mientras está abierta, y lo deja como estaba al cerrar.")]
        public bool gestionarCursor = true;
        [Tooltip("Pone Time.timeScale = 0 mientras está abierta.")]
        public bool pausarTiempo = false;
        [Tooltip("Componentes que se desactivan mientras está abierta (el control de cámara del jugador, su movimiento, el arma...).")]
        public Behaviour[] desactivarMientrasAbierta = new Behaviour[0];

        [Header("Interfaz propia (opcional: si 'panel' está vacío se genera una)")]
        public GameObject panel;
        public RectTransform contenedorFilas;
        public FilaArticuloUI prefabFila;
        [Tooltip("Donde se crean las pestañas de categorías. Vacío = sin pestañas.")]
        public RectTransform contenedorCategorias;
        public Button prefabPestana;
        public Text textoTitulo;
        public Text textoSaldo;
        public Text textoMensaje;
        public Button botonCerrar;

        [Header("Estilo de la interfaz generada")]
        public EstiloTiendaUI estilo = new EstiloTiendaUI();
        [Tooltip("Segundos que se ve el mensaje de compra / error.")]
        [Min(0)] public float duracionMensaje = 2.5f;

        [Header("Eventos")]
        public UnityEvent alAbrir = new UnityEvent();
        public UnityEvent alCerrar = new UnityEvent();

        public bool Abierta { get; private set; }
        public GestorTienda TiendaActual { get { return tienda != null ? tienda : GestorTienda.Principal; } }
        public CatalogoTienda CatalogoActual
        {
            get
            {
                if (catalogoTemporal != null) return catalogoTemporal;
                if (catalogo != null) return catalogo;
                var t = TiendaActual;
                return t != null ? t.catalogo : null;
            }
        }
        public string CategoriaActual { get; private set; }

        CatalogoTienda catalogoTemporal;
        IReceptorCompra receptorTemporal;
        readonly List<FilaArticuloUI> filas = new List<FilaArticuloUI>();
        readonly List<Button> pestanas = new List<Button>();
        bool[] estadoComponentes;
        CursorLockMode cursorPrevio;
        bool cursorVisiblePrevio;
        float tiempoPrevio = 1f;
        float finMensaje;

        void Start()
        {
            if (panel != null && !Abierta) panel.SetActive(false);
        }

        void Update()
        {
            if (!Abierta)
            {
                if (abrirConTecla && !AlgunaAbierta && EntradaTienda.Pulsada(teclaAbrir)) Abrir();
                return;
            }
            if (EntradaTienda.Pulsada(teclaCerrar) || (abrirConTecla && EntradaTienda.Pulsada(teclaAbrir)))
            {
                Cerrar();
                return;
            }
            ActualizarSaldo();
            if (textoMensaje != null && finMensaje > 0 && Time.unscaledTime > finMensaje)
            {
                textoMensaje.text = "";
                finMensaje = 0;
            }
        }

        void OnDisable()
        {
            if (Abierta) Cerrar();
        }

        // ---------------------------------------------------------------- API

        public void Abrir() { Abrir(null, null); }

        /// <summary>Abre mostrando otro catálogo y entregando en otro punto (lo usa TerminalTienda).</summary>
        public void Abrir(CatalogoTienda catalogoAlternativo, IReceptorCompra receptorAlternativo)
        {
            catalogoTemporal = catalogoAlternativo;
            receptorTemporal = receptorAlternativo;
            if (panel == null) ConstructorUITienda.Construir(this);
            if (CatalogoActual == null) Debug.LogWarning("[Tienda] La tienda en menú no tiene catálogo.", this);

            if (!Abierta)
            {
                Abierta = true;
                abiertas++;
                if (gestionarCursor)
                {
                    cursorPrevio = Cursor.lockState;
                    cursorVisiblePrevio = Cursor.visible;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if (pausarTiempo)
                {
                    tiempoPrevio = Time.timeScale;
                    Time.timeScale = 0f;
                }
                estadoComponentes = new bool[desactivarMientrasAbierta.Length];
                for (int i = 0; i < desactivarMientrasAbierta.Length; i++)
                {
                    var b = desactivarMientrasAbierta[i];
                    if (b == null) continue;
                    estadoComponentes[i] = b.enabled;
                    b.enabled = false;
                }
            }

            panel.SetActive(true);
            var cats = CatalogoActual != null ? CatalogoActual.Categorias() : new List<string>();
            if (string.IsNullOrEmpty(CategoriaActual) || !cats.Contains(CategoriaActual))
                CategoriaActual = cats.Count > 0 ? cats[0] : null;
            if (textoTitulo != null) textoTitulo.text = CatalogoActual != null ? CatalogoActual.nombreTienda : "Tienda";
            if (textoMensaje != null) textoMensaje.text = "";
            ReconstruirPestanas(cats);
            ReconstruirFilas();
            ActualizarSaldo();
            alAbrir.Invoke();
        }

        public void Cerrar()
        {
            if (!Abierta) return;
            Abierta = false;
            abiertas = Mathf.Max(0, abiertas - 1);
            if (panel != null) panel.SetActive(false);
            if (gestionarCursor)
            {
                Cursor.lockState = cursorPrevio;
                Cursor.visible = cursorVisiblePrevio;
            }
            if (pausarTiempo) Time.timeScale = tiempoPrevio;
            for (int i = 0; i < desactivarMientrasAbierta.Length; i++)
            {
                var b = desactivarMientrasAbierta[i];
                if (b != null && estadoComponentes != null && i < estadoComponentes.Length) b.enabled = estadoComponentes[i];
            }
            catalogoTemporal = null;
            receptorTemporal = null;
            alCerrar.Invoke();
        }

        public void Alternar() { if (Abierta) Cerrar(); else Abrir(); }

        public void MostrarCategoria(string categoria)
        {
            CategoriaActual = categoria;
            ReconstruirFilas();
            PintarPestanas();
        }

        /// <summary>Lo llaman las filas al pulsar Comprar.</summary>
        public ResultadoCompra ComprarDesdeMenu(ArticuloTienda articulo)
        {
            var t = TiendaActual;
            if (t == null) { Debug.LogWarning("[Tienda] No hay GestorTienda en la escena.", this); return null; }
            IReceptorCompra r = receptorTemporal ?? (receptor as IReceptorCompra);
            var resultado = t.Comprar(articulo, r);
            MostrarMensaje(resultado.Mensaje, resultado.Exito);
            foreach (var f in filas) if (f != null) f.Pintar();
            ActualizarSaldo();
            if (resultado.Exito && cerrarTrasComprar) Cerrar();
            return resultado;
        }

        public void MostrarMensaje(string mensaje, bool exito)
        {
            if (textoMensaje == null) return;
            textoMensaje.text = mensaje;
            textoMensaje.color = exito ? estilo.colorAcento : estilo.colorError;
            finMensaje = duracionMensaje > 0 ? Time.unscaledTime + duracionMensaje : 0;
        }

        // ---------------------------------------------------------------- interno

        void ActualizarSaldo()
        {
            if (textoSaldo == null) return;
            var t = TiendaActual;
            var cat = CatalogoActual;
            if (t == null) { textoSaldo.text = ""; return; }
            // Muestra las monedas que usa el catálogo.
            var monedas = new List<string>();
            if (cat != null)
                foreach (var a in cat.articulos)
                    if (a != null && a.visibleEnTienda && !monedas.Contains(a.moneda)) monedas.Add(a.moneda);
            if (monedas.Count == 0) monedas.Add("monedas");
            var sb = new System.Text.StringBuilder();
            foreach (var m in monedas)
            {
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(t.Saldo(m)).Append(' ').Append(m);
            }
            textoSaldo.text = sb.ToString();
        }

        void ReconstruirFilas()
        {
            foreach (var f in filas) if (f != null) Destroy(f.gameObject);
            filas.Clear();
            var cat = CatalogoActual;
            if (cat == null || contenedorFilas == null || prefabFila == null) return;
            bool usarCategorias = contenedorCategorias != null && !string.IsNullOrEmpty(CategoriaActual);
            foreach (var a in cat.articulos)
            {
                if (a == null || !a.visibleEnTienda) continue;
                string c = string.IsNullOrEmpty(a.categoria) ? "General" : a.categoria;
                if (usarCategorias && c != CategoriaActual) continue;
                var fila = Instantiate(prefabFila, contenedorFilas);
                fila.gameObject.SetActive(true);
                fila.Configurar(a, this);
                filas.Add(fila);
            }
        }

        void ReconstruirPestanas(List<string> cats)
        {
            foreach (var p in pestanas) if (p != null) Destroy(p.gameObject);
            pestanas.Clear();
            if (contenedorCategorias == null || prefabPestana == null) return;
            contenedorCategorias.gameObject.SetActive(cats.Count > 1);
            foreach (var c in cats)
            {
                var b = Instantiate(prefabPestana, contenedorCategorias);
                b.gameObject.SetActive(true);
                b.name = "Pestaña " + c;
                var txt = b.GetComponentInChildren<Text>();
                if (txt != null) txt.text = c;
                string categoria = c;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => MostrarCategoria(categoria));
                pestanas.Add(b);
            }
            PintarPestanas();
        }

        void PintarPestanas()
        {
            foreach (var b in pestanas)
            {
                if (b == null || b.targetGraphic == null) continue;
                bool activa = b.name == "Pestaña " + CategoriaActual;
                b.targetGraphic.color = activa ? estilo.colorPestanaActiva : estilo.colorPestana;
            }
        }
    }
}
