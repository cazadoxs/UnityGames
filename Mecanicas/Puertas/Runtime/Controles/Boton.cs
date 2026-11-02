using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    public enum ModoBoton
    {
        /// <summary>Se activa un momento y vuelve (timbre, botón de ascensor).</summary>
        Pulsador,
        /// <summary>Cada pulsación cambia entre activo e inactivo y se queda hundido mientras está activo.</summary>
        Conmutador
    }

    /// <summary>Botón físico. Es una señal (ISenal) y lanza eventos al pulsarlo.</summary>
    [AddComponentMenu("Mecánicas/Puertas/Botón")]
    public class Boton : Interactuable, ISenal
    {
        [SerializeField] ModoBoton modo = ModoBoton.Pulsador;
        [Tooltip("Pieza que se hunde. Vacío = este objeto.")]
        [SerializeField] Transform pieza;
        [Tooltip("Dirección local en la que se hunde.")]
        [SerializeField] Vector3 direccionPulsado = Vector3.forward;
        [SerializeField] float recorrido = 0.012f;
        [Tooltip("Segundos que dura activo un pulsador.")]
        [SerializeField] float duracionPulso = 0.3f;
        [Tooltip("Segundos mínimos entre pulsaciones.")]
        [SerializeField] float enfriamiento = 0.25f;
        [Tooltip("Veces que se puede pulsar (0 = sin límite).")]
        [SerializeField] int usosMaximos;

        [Header("Indicador (opcional)")]
        [SerializeField] Renderer indicador;
        [SerializeField] Color colorInactivo = new Color(0.6f, 0.05f, 0.05f);
        [SerializeField] Color colorActivo = new Color(0.1f, 1f, 0.2f);

        [SerializeField] AudioClip sonidoPulsar;

        [Header("Eventos")]
        public UnityEvent alPulsar = new UnityEvent();
        public UnityEvent alActivar = new UnityEvent();
        public UnityEvent alDesactivar = new UnityEvent();

        Vector3 posBase;
        bool activo;
        float desactivarEn = -1f;
        float siguientePermitido;
        float hundido;
        int usos;

        public event Action<bool> AlCambiarSenal;
        public bool Activa => activo;

        void Reset() => textoAccion = "Pulsar botón";

        void Awake()
        {
            if (pieza == null) pieza = transform;
            posBase = pieza.localPosition;
            ColorUtil.Aplicar(indicador, colorInactivo, 1.5f);
        }

        public override bool PuedeInteractuar(Interactor interactor) =>
            base.PuedeInteractuar(interactor) && Time.time >= siguientePermitido && (usosMaximos <= 0 || usos < usosMaximos);

        protected override void AlInteractuar(Interactor interactor) => Pulsar();

        /// <summary>Pulsa el botón desde código o desde otro evento.</summary>
        public void Pulsar()
        {
            usos++;
            siguientePermitido = Time.time + enfriamiento;
            hundido = 1f;
            ReproducirSonido(sonidoPulsar);
            alPulsar.Invoke();
            if (modo == ModoBoton.Pulsador)
            {
                CambiarActivo(true);
                desactivarEn = Time.time + duracionPulso;
            }
            else CambiarActivo(!activo);
        }

        void CambiarActivo(bool v)
        {
            if (activo == v) return;
            activo = v;
            ColorUtil.Aplicar(indicador, v ? colorActivo : colorInactivo, 1.5f);
            if (v) alActivar.Invoke(); else alDesactivar.Invoke();
            AlCambiarSenal?.Invoke(v);
        }

        void Update()
        {
            if (desactivarEn > 0f && Time.time >= desactivarEn) { desactivarEn = -1f; CambiarActivo(false); }
            float objetivo = activo && modo == ModoBoton.Conmutador ? 0.6f : (activo ? 1f : 0f);
            hundido = Mathf.MoveTowards(hundido, objetivo, Time.deltaTime * 8f);
            pieza.localPosition = posBase + pieza.localRotation * direccionPulsado.normalized * (recorrido * hundido);
        }
    }
}
