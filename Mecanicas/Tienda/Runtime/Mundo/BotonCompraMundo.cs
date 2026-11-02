using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Botón físico en el mundo que compra un artículo. Se activa mirándolo y pulsando E (con InteractorTienda),
    /// pisándolo/entrando en su trigger (opcional) o llamando a Interactuar() desde tu código.
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/Botón de compra (mundo)")]
    public class BotonCompraMundo : MonoBehaviour, IInteractuableTienda
    {
        [Header("Qué compra")]
        public ArticuloTienda articulo;
        [Tooltip("Vacío = GestorTienda.Principal.")]
        public GestorTienda tienda;
        [Tooltip("Dónde aparece lo comprado (componente que implemente IReceptorCompra, normalmente un PuntoAparicion). Vacío = el de la tienda.")]
        public MonoBehaviour receptor;

        [Header("Activación")]
        [Tooltip("Segundos entre pulsaciones.")]
        [Min(0)] public float enfriamiento = 0.4f;
        [Tooltip("Comprar al entrar en el trigger de este objeto (botón de suelo / zona de compra). Necesita un Collider con Is Trigger.")]
        public bool comprarAlEntrarEnTrigger = false;
        [Tooltip("Solo los objetos con esta etiqueta activan el trigger. Vacío = cualquiera con Rigidbody o CharacterController.")]
        public string etiquetaJugador = "Player";
        [Tooltip("Texto del aviso. Usa {nombre}, {precio}, {moneda}, {saldo}.")]
        public string textoAviso = "Comprar {nombre} · {precio} {moneda}";

        [Header("Animación del botón")]
        [Tooltip("Parte que se hunde al pulsar (la tapa del botón). Vacío = no se mueve nada.")]
        public Transform parteMovil;
        public Vector3 recorridoPulsado = new Vector3(0, -0.03f, 0);
        [Min(0.01f)] public float duracionPulsado = 0.12f;
        [Tooltip("Animator opcional: se lanza este trigger al pulsar.")]
        public Animator animador;
        public string triggerAnimador = "Pulsar";

        [Header("Color de estado")]
        [Tooltip("Renderer que cambia de color (luz o tapa del botón). Vacío = sin colores.")]
        public Renderer indicador;
        public Color colorDisponible = new Color(0.2f, 0.8f, 0.3f);
        public Color colorNoDisponible = new Color(0.5f, 0.5f, 0.5f);
        public Color colorExito = new Color(0.4f, 1f, 0.5f);
        public Color colorError = new Color(1f, 0.25f, 0.2f);
        [Min(0)] public float duracionDestello = 0.35f;

        [Header("Etiqueta 3D (opcional)")]
        [Tooltip("TextMesh donde se escribe el nombre y el precio.")]
        public TextMesh etiqueta;
        public string formatoEtiqueta = "{nombre}\n{precio} {moneda}";

        [Header("Sonido")]
        public AudioClip sonidoPulsar;
        [Range(0, 1)] public float volumen = 1f;

        [Header("Eventos")]
        public UnityEvent alPulsar = new UnityEvent();
        public GestorTienda.EventoArticulo alComprar = new GestorTienda.EventoArticulo();
        public GestorTienda.EventoTexto alFallar = new GestorTienda.EventoTexto();

        float ultimoUso = -999f;
        Vector3 posReposo;
        MaterialPropertyBlock bloque;
        bool destellando;
        Coroutine rutinaPulsado;

        public GestorTienda TiendaActual { get { return tienda != null ? tienda : GestorTienda.Principal; } }

        public string TextoAviso
        {
            get
            {
                var t = TiendaActual;
                if (articulo == null) return "Sin artículo";
                int saldo = t != null ? t.Saldo(articulo.moneda) : 0;
                string txt = TextosTienda.Rellenar(textoAviso, articulo, saldo);
                if (t != null)
                {
                    var m = t.PuedeComprar(articulo);
                    if (m == MotivoCompra.SinStock) txt += " (agotado)";
                    else if (m == MotivoCompra.LimiteAlcanzado) txt += " (ya comprado)";
                    else if (m == MotivoCompra.SinDinero) txt += " (no te llega)";
                }
                return txt;
            }
        }

        public bool PuedeInteractuar { get { return isActiveAndEnabled && articulo != null; } }

        void Start()
        {
            if (parteMovil != null) posReposo = parteMovil.localPosition;
            ActualizarEtiqueta();
        }

        void Update()
        {
            if (!destellando) PintarEstado();
        }

        public void Interactuar(GameObject quien)
        {
            if (Time.time - ultimoUso < enfriamiento) return;
            ultimoUso = Time.time;

            alPulsar.Invoke();
            if (animador != null && !string.IsNullOrEmpty(triggerAnimador)) animador.SetTrigger(triggerAnimador);
            if (sonidoPulsar != null) AudioSource.PlayClipAtPoint(sonidoPulsar, transform.position, volumen);
            if (parteMovil != null)
            {
                if (rutinaPulsado != null) StopCoroutine(rutinaPulsado);
                rutinaPulsado = StartCoroutine(AnimarPulsado());
            }

            var t = TiendaActual;
            if (t == null)
            {
                Debug.LogWarning("[Tienda] El botón '" + name + "' no encuentra ningún GestorTienda en la escena.", this);
                return;
            }
            var r = t.Comprar(articulo, receptor as IReceptorCompra);
            if (r.Exito) alComprar.Invoke(articulo); else alFallar.Invoke(r.Mensaje);
            if (indicador != null && duracionDestello > 0) StartCoroutine(Destello(r.Exito ? colorExito : colorError));
            ActualizarEtiqueta();
        }

        /// <summary>Para UnityEvents (sin parámetro).</summary>
        public void Pulsar() { Interactuar(null); }

        void OnTriggerEnter(Collider otro)
        {
            if (!comprarAlEntrarEnTrigger) return;
            if (!string.IsNullOrEmpty(etiquetaJugador))
            {
                if (!otro.CompareTag(etiquetaJugador)) return;
            }
            else if (otro.attachedRigidbody == null && otro.GetComponent<CharacterController>() == null) return;
            Interactuar(otro.gameObject);
        }

        public void ActualizarEtiqueta()
        {
            if (etiqueta == null || articulo == null) return;
            var t = TiendaActual;
            etiqueta.text = TextosTienda.Rellenar(formatoEtiqueta, articulo, t != null ? t.Saldo(articulo.moneda) : 0);
        }

        void PintarEstado()
        {
            if (indicador == null) return;
            var t = TiendaActual;
            bool ok = t != null && t.PuedeComprar(articulo) == MotivoCompra.Ok;
            PintarColor(ok ? colorDisponible : colorNoDisponible);
        }

        void PintarColor(Color c)
        {
            if (bloque == null) bloque = new MaterialPropertyBlock();
            indicador.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", c); // URP / HDRP
            bloque.SetColor("_Color", c);     // Built-in
            indicador.SetPropertyBlock(bloque);
        }

        IEnumerator Destello(Color c)
        {
            destellando = true;
            PintarColor(c);
            yield return new WaitForSeconds(duracionDestello);
            destellando = false;
        }

        IEnumerator AnimarPulsado()
        {
            float mitad = duracionPulsado * 0.5f;
            for (float t = 0; t < mitad; t += Time.deltaTime)
            {
                parteMovil.localPosition = posReposo + recorridoPulsado * (t / mitad);
                yield return null;
            }
            for (float t = 0; t < mitad; t += Time.deltaTime)
            {
                parteMovil.localPosition = posReposo + recorridoPulsado * (1f - t / mitad);
                yield return null;
            }
            parteMovil.localPosition = posReposo;
            rutinaPulsado = null;
        }

        void OnValidate()
        {
            if (etiqueta != null && articulo != null && !Application.isPlaying)
                etiqueta.text = TextosTienda.Rellenar(formatoEtiqueta, articulo, 0);
        }
    }
}
