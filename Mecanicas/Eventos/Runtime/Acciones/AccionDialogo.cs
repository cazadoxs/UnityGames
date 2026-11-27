using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Eventos
{
    /// <summary>Por dónde sale el diálogo.</summary>
    public enum ModoDialogo
    {
        [Tooltip("Usa el sistema de diálogos registrado (IProveedorDialogo); si no hay ninguno, subtítulos simples.")] Automatico,
        [Tooltip("Siempre los subtítulos simples incluidos.")] Subtitulos,
        [Tooltip("Solo lanza 'Al Iniciar' (para conectar cualquier sistema por UnityEvent) y espera a NotificarFin().")] EventoExterno
    }

    /// <summary>
    /// Lanza un diálogo. Funciona sola (subtítulos simples) o conectada a cualquier sistema de diálogos,
    /// sin depender de él (ver README, sección "Conectar tu sistema de diálogos").
    /// </summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Diálogo")]
    public class AccionDialogo : AccionEvento
    {
        [SerializeField] private ModoDialogo modo = ModoDialogo.Automatico;

        [Tooltip("Frases para los subtítulos simples (y que también recibe el proveedor).")]
        [SerializeField] private List<LineaDialogo> lineas = new List<LineaDialogo>
        {
            new LineaDialogo { hablante = "Voz", texto = "¿Has oído eso?" }
        };

        [Tooltip("Asset de diálogo de tu sistema (por ejemplo, un DialogueAsset). Se pasa tal cual al proveedor.")]
        [SerializeField] private Object dialogoExterno;

        [Tooltip("Bloquea el control del jugador mientras dura el diálogo.")]
        [SerializeField] private bool bloquearControl = false;

        [Tooltip("La acción no termina hasta que acaba el diálogo (para que la secuencia espere).")]
        [SerializeField] private bool esperarFinDialogo = true;

        [Tooltip("Solo Evento Externo: segundos máximos esperando NotificarFin(). 0 = sin límite.")]
        [Min(0f)] [SerializeField] private float tiempoMaximo = 0f;

        [SerializeField] private UnityEvent alIniciar = new UnityEvent();
        [SerializeField] private UnityEvent alTerminar = new UnityEvent();

        private bool terminado;
        private readonly object llave = new object();

        public List<LineaDialogo> Lineas => lineas;
        public UnityEvent AlIniciar => alIniciar;
        public UnityEvent AlTerminar => alTerminar;

        /// <summary>Avisa de que el diálogo ha terminado. Conéctalo al evento de fin de tu sistema de diálogos.</summary>
        public void NotificarFin() => terminado = true;

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            terminado = false;
            if (bloquearControl) BloqueoControlJugador.Bloquear(llave);

            alIniciar.Invoke();

            switch (modo)
            {
                case ModoDialogo.EventoExterno:
                    break;

                case ModoDialogo.Automatico when ProveedorDialogos.Actual != null:
                    ProveedorDialogos.Actual.Reproducir(dialogoExterno, lineas, NotificarFin);
                    break;

                default:
                    SubtitulosSimples.Obtener().Mostrar(lineas, NotificarFin);
                    break;
            }

            if (esperarFinDialogo)
            {
                float inicio = Time.time;
                while (!terminado)
                {
                    if (modo == ModoDialogo.EventoExterno && tiempoMaximo > 0f && Time.time - inicio > tiempoMaximo)
                        break;
                    yield return null;
                }
                Terminar();
            }
            else
            {
                // No se espera, pero el bloqueo y 'Al Terminar' deben soltarse cuando acabe de verdad.
                LanzarCorrutina(EsperarYTerminar());
            }
        }

        private IEnumerator EsperarYTerminar()
        {
            float inicio = Time.time;
            while (!terminado && (tiempoMaximo <= 0f || Time.time - inicio < tiempoMaximo))
                yield return null;
            Terminar();
        }

        private void Terminar()
        {
            BloqueoControlJugador.Liberar(llave);
            alTerminar.Invoke();
        }

        public override void Detener()
        {
            base.Detener();
            BloqueoControlJugador.Liberar(llave);
        }
    }
}
