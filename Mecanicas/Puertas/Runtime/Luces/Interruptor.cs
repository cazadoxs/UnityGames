using System;
using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Interruptor de pared. Cambia el estado de un <see cref="CircuitoLuz"/>.
    /// Si varios interruptores usan el mismo circuito, funcionan como conmutados: cada uno
    /// cambia la luz y su tecla basculante cambia de posición, igual que en una casa.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Interruptor de luz")]
    public class Interruptor : Interactuable, ISenal
    {
        [Tooltip("Circuito que controla. Vacío = se busca en este mismo objeto.")]
        [SerializeField] CircuitoLuz circuito;
        [Tooltip("Tecla basculante que gira al pulsar (opcional).")]
        [SerializeField] Transform tecla;
        [SerializeField] Vector3 ejeLocal = Vector3.right;
        [Tooltip("Grados de inclinación de la tecla en cada posición.")]
        [SerializeField] float angulo = 12f;
        [SerializeField] string textoEncender = "Encender luz";
        [SerializeField] string textoApagar = "Apagar luz";
        [SerializeField] AudioClip sonidoClic;

        Quaternion rotBase;
        bool posicionTecla;

        public event Action<bool> AlCambiarSenal;
        public bool Activa => circuito != null && circuito.Encendido;
        public CircuitoLuz Circuito => circuito;

        void Awake()
        {
            if (circuito == null) circuito = GetComponent<CircuitoLuz>();
            if (tecla != null) rotBase = tecla.localRotation;
        }

        void Start()
        {
            posicionTecla = Activa;
            AplicarTecla();
        }

        void OnEnable() { if (circuito != null) circuito.AlCambiarSenal += Reenviar; }
        void OnDisable() { if (circuito != null) circuito.AlCambiarSenal -= Reenviar; }
        void Reenviar(bool v) => AlCambiarSenal?.Invoke(v);

        public void Configurar(CircuitoLuz c, Transform t) { circuito = c; tecla = t; if (t != null) rotBase = t.localRotation; }

        public override string ObtenerTexto(Interactor interactor) =>
            circuito != null && !circuito.ConCorriente ? (Activa ? textoApagar : textoEncender) + " (sin corriente)"
            : (Activa ? textoApagar : textoEncender);

        protected override void AlInteractuar(Interactor interactor)
        {
            if (circuito == null) return;
            circuito.Alternar();
            posicionTecla = !posicionTecla;
            AplicarTecla();
            ReproducirSonido(sonidoClic);
        }

        void AplicarTecla()
        {
            if (tecla != null) tecla.localRotation = rotBase * Quaternion.AngleAxis(posicionTecla ? angulo : -angulo, ejeLocal);
        }
    }
}
