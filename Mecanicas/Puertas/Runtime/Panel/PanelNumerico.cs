using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Panel táctil numérico. Las teclas físicas son <see cref="TeclaPanel"/> hijas.
    /// Funciona como bloqueo de puertas (añádelo a la lista de la puerta) y como señal (ISenal).
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Panel numérico")]
    public class PanelNumerico : MonoBehaviour, IBloqueo, ISenal
    {
        [Header("Código")]
        [SerializeField] string codigo = "1234";
        [Tooltip("Comprueba el código en cuanto se teclean tantos dígitos como tiene. Si no, hay que pulsar Aceptar.")]
        [SerializeField] bool confirmarAutomatico = true;
        [SerializeField] int maximoDigitos = 8;
        [Tooltip("Muestra asteriscos en lugar de los números.")]
        [SerializeField] bool ocultarDigitos;
        [SerializeField] bool empiezaDesbloqueado;

        [Header("Seguridad")]
        [Tooltip("Fallos seguidos antes de bloquearse (0 = sin límite).")]
        [SerializeField] int intentosMaximos = 0;
        [SerializeField] float segundosBloqueo = 10f;
        [Tooltip("Vuelve a bloquearse tantos segundos después de acertar (0 = nunca).")]
        [SerializeField] float rebloquearTras = 0f;

        [Header("Pantalla y luz")]
        [SerializeField] TextMesh pantalla;
        [SerializeField] Renderer luzEstado;
        [SerializeField] Color colorEspera = new Color(1f, 0.6f, 0.1f);
        [SerializeField] Color colorCorrecto = new Color(0.2f, 1f, 0.3f);
        [SerializeField] Color colorError = new Color(1f, 0.15f, 0.1f);
        [SerializeField] string textoVacio = "----";
        [SerializeField] string textoCorrecto = "ABIERTO";
        [SerializeField] string textoError = "ERROR";
        [SerializeField] string textoBloqueado = "BLOQUEADO";

        [Header("Sonidos")]
        [SerializeField] AudioSource fuenteAudio;
        [SerializeField] AudioClip sonidoTecla;
        [SerializeField] AudioClip sonidoCorrecto;
        [SerializeField] AudioClip sonidoError;

        [Header("Eventos")]
        public UnityEvent alCodigoCorrecto = new UnityEvent();
        public UnityEvent alCodigoIncorrecto = new UnityEvent();
        public UnityEvent alRebloquear = new UnityEvent();

        [SerializeField] string motivo = "Necesita un código en el panel.";

        string entrada = "";
        bool desbloqueado;
        int fallos;
        float bloqueadoHasta = -1f;
        float mensajeHasta = -1f;
        float rebloqueoEn = -1f;

        public event Action<bool> AlCambiarSenal;

        public bool Activa => desbloqueado;
        public bool Bloqueado => !desbloqueado;
        public bool ImpideCerrar => false;
        public string Motivo => motivo;
        public bool IntentarDesbloquearDesdePuerta(Interactor interactor, out string mensaje) { mensaje = null; return false; }
        public string EntradaActual => entrada;
        public bool FueraDeServicio => Time.time < bloqueadoHasta;

        void Awake()
        {
            desbloqueado = empiezaDesbloqueado;
            Refrescar();
        }

        public void PulsarDigito(char d)
        {
            if (!char.IsDigit(d) || !PuedeTeclear()) return;
            if (entrada.Length >= maximoDigitos) return;
            entrada += d;
            Sonido.Reproducir(fuenteAudio, sonidoTecla, transform.position, 0.7f);
            Refrescar();
            if (confirmarAutomatico && entrada.Length >= codigo.Length) Confirmar();
        }

        /// <summary>Versión para UnityEvent (0-9).</summary>
        public void PulsarDigito(int d) => PulsarDigito((char)('0' + Mathf.Clamp(d, 0, 9)));

        public void Borrar()
        {
            if (!PuedeTeclear()) return;
            entrada = "";
            Sonido.Reproducir(fuenteAudio, sonidoTecla, transform.position, 0.7f);
            Refrescar();
        }

        public void Confirmar()
        {
            if (!PuedeTeclear() || entrada.Length == 0) return;
            if (entrada == codigo)
            {
                fallos = 0;
                entrada = "";
                Sonido.Reproducir(fuenteAudio, sonidoCorrecto, transform.position);
                MostrarTemporal(textoCorrecto, colorCorrecto, 1.5f);
                if (!desbloqueado)
                {
                    desbloqueado = true;
                    AlCambiarSenal?.Invoke(true);
                }
                if (rebloquearTras > 0f) rebloqueoEn = Time.time + rebloquearTras;
                alCodigoCorrecto.Invoke();
            }
            else
            {
                fallos++;
                entrada = "";
                Sonido.Reproducir(fuenteAudio, sonidoError, transform.position);
                alCodigoIncorrecto.Invoke();
                if (intentosMaximos > 0 && fallos >= intentosMaximos)
                {
                    fallos = 0;
                    bloqueadoHasta = Time.time + segundosBloqueo;
                    MostrarTemporal(textoBloqueado, colorError, segundosBloqueo);
                }
                else MostrarTemporal(textoError, colorError, 1f);
            }
        }

        /// <summary>Vuelve a bloquear el panel (y la puerta que dependa de él).</summary>
        public void Rebloquear()
        {
            rebloqueoEn = -1f;
            if (!desbloqueado) return;
            desbloqueado = false;
            AlCambiarSenal?.Invoke(false);
            alRebloquear.Invoke();
            Refrescar();
        }

        public void EstablecerCodigo(string nuevo) { if (!string.IsNullOrEmpty(nuevo)) codigo = nuevo; }

        bool PuedeTeclear() => !FueraDeServicio && Time.time >= mensajeHasta;

        void Update()
        {
            if (rebloqueoEn > 0f && Time.time >= rebloqueoEn) Rebloquear();
            if (mensajeHasta > 0f && Time.time >= mensajeHasta) { mensajeHasta = -1f; Refrescar(); }
        }

        void MostrarTemporal(string texto, Color color, float segundos)
        {
            mensajeHasta = Time.time + segundos;
            if (pantalla != null) pantalla.text = texto;
            ColorUtil.Aplicar(luzEstado, color, 2f);
        }

        void Refrescar()
        {
            if (pantalla != null)
                pantalla.text = entrada.Length == 0 ? textoVacio : (ocultarDigitos ? new string('*', entrada.Length) : entrada);
            ColorUtil.Aplicar(luzEstado, desbloqueado ? colorCorrecto : colorEspera, 2f);
        }
    }
}
