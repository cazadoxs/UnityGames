using System;
using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Va en la cámara del jugador. Cada frame lanza un rayo hacia delante, detecta el
    /// <see cref="Interactuable"/> que se mira y gestiona el clic y el arrastre.
    /// Mientras se arrastra algo, <see cref="CamaraBloqueada"/> es true: tu controlador de cámara debe respetarlo.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Interactor (cámara del jugador)")]
    [DisallowMultipleComponent]
    public class Interactor : MonoBehaviour
    {
        [Header("Detección")]
        [Tooltip("Distancia máxima a la que se puede usar algo, en metros.")]
        [SerializeField] float distancia = 2.5f;
        [Tooltip("Capas que el rayo puede tocar. Quita la capa del jugador si tu cámara está dentro de su collider.")]
        [SerializeField] LayerMask capas = ~0;
        [Tooltip("Si los triggers cuentan como objetivo.")]
        [SerializeField] QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore;

        [Header("Arrastre")]
        [Tooltip("Metros que se mueve la mano por cada unidad de ratón.")]
        [SerializeField] float sensibilidadArrastre = 0.02f;
        [Tooltip("Un clic más corto que esto (segundos) y sin mover el ratón cuenta como interacción normal.")]
        [SerializeField] float tiempoClic = 0.25f;
        [Tooltip("Si el objeto arrastrado se aleja más de esto (distancia × factor), se suelta.")]
        [SerializeField] float factorDistanciaSoltar = 1.6f;

        [Header("Jugador")]
        [Tooltip("Componente que guarda las llaves. Si se deja vacío se busca uno (IPortadorLlaves) en los padres.")]
        [SerializeField] MonoBehaviour portadorLlaves;

        /// <summary>Lo que se está mirando ahora (o null).</summary>
        public Interactuable Objetivo { get; private set; }
        /// <summary>Texto de la acción del objetivo actual, ya calculado.</summary>
        public string TextoObjetivo { get; private set; }
        /// <summary>True mientras se arrastra un objeto: el controlador de cámara no debe girar.</summary>
        public bool CamaraBloqueada => arrastrando != null && arrastreConfirmado;
        public bool Arrastrando => arrastrando != null;
        public IPortadorLlaves Llaves { get; private set; }
        public Camera Camara { get; private set; }
        public float Distancia => distancia;

        /// <summary>Último mensaje mostrado y hasta cuándo (Time.time).</summary>
        public string Mensaje { get; private set; }
        public float MensajeHasta { get; private set; }

        public event Action<Interactuable> AlCambiarObjetivo;
        public event Action<string, float> AlMostrarMensaje;

        /// <summary>Instancia más reciente; útil para scripts que necesitan al jugador sin referencia directa.</summary>
        public static Interactor Principal { get; private set; }

        IArrastrable arrastrando;
        Interactuable objetivoArrastre;
        float inicioPulsacion;
        float movimientoAcumulado;
        bool arrastreConfirmado;
        RaycastHit impactoActual;

        void Awake()
        {
            Camara = GetComponent<Camera>();
            if (Camara == null) Camara = GetComponentInParent<Camera>();
            Llaves = portadorLlaves as IPortadorLlaves;
            if (Llaves == null) Llaves = GetComponentInParent<IPortadorLlaves>();
            if (Llaves == null) Llaves = GetComponentInChildren<IPortadorLlaves>();
        }

        void OnEnable() => Principal = this;

        void OnDisable()
        {
            SoltarArrastre();
            if (Principal == this) Principal = null;
        }

        /// <summary>Asigna otro inventario de llaves en tiempo de ejecución.</summary>
        public void AsignarPortadorLlaves(IPortadorLlaves portador) => Llaves = portador;

        void Update()
        {
            if (arrastrando != null)
            {
                ActualizarArrastre();
                return;
            }

            DetectarObjetivo();

            if (Objetivo != null && Entrada.InteractuarPulsado)
            {
                if (Objetivo is IArrastrable a && a.UsaArrastre)
                {
                    arrastrando = a;
                    objetivoArrastre = Objetivo;
                    inicioPulsacion = Time.time;
                    movimientoAcumulado = 0f;
                    arrastreConfirmado = false;
                    a.EmpezarArrastre(this, impactoActual);
                }
                else
                {
                    Objetivo.Interactuar(this);
                }
            }
        }

        void DetectarObjetivo()
        {
            Interactuable nuevo = null;
            Transform origen = Camara != null ? Camara.transform : transform;
            if (Physics.Raycast(origen.position, origen.forward, out impactoActual, distancia, capas, triggers))
            {
                nuevo = impactoActual.collider.GetComponentInParent<Interactuable>();
                if (nuevo != null && !nuevo.PuedeInteractuar(this)) nuevo = null;
            }

            if (nuevo != Objetivo)
            {
                Objetivo = nuevo;
                AlCambiarObjetivo?.Invoke(nuevo);
            }
            TextoObjetivo = Objetivo != null ? Objetivo.ObtenerTexto(this) : null;
        }

        void ActualizarArrastre()
        {
            bool soltar = !Entrada.InteractuarMantenido;
            var comp = objetivoArrastre;
            if (comp == null || !comp.isActiveAndEnabled) { SoltarArrastre(); return; }

            Transform origen = Camara != null ? Camara.transform : transform;
            if ((comp.transform.position - origen.position).sqrMagnitude > distancia * distancia * factorDistanciaSoltar * factorDistanciaSoltar)
            {
                SoltarArrastre();
                return;
            }

            Vector2 delta = Entrada.DeltaRaton;
            movimientoAcumulado += delta.magnitude;
            if (!arrastreConfirmado && (movimientoAcumulado > 0.5f || Time.time - inicioPulsacion > tiempoClic))
                arrastreConfirmado = true;

            if (arrastreConfirmado && delta.sqrMagnitude > 0f)
            {
                Vector3 vertical = arrastrando.ArrastreEnProfundidad
                    ? Vector3.ProjectOnPlane(origen.forward, Vector3.up).normalized
                    : origen.up;
                Vector3 mov = (origen.right * delta.x + vertical * delta.y) * sensibilidadArrastre;
                arrastrando.Arrastrar(this, mov);
            }

            TextoObjetivo = comp.ObtenerTexto(this);

            if (soltar)
            {
                bool fueClic = !arrastreConfirmado || (Time.time - inicioPulsacion < tiempoClic && movimientoAcumulado < 0.5f);
                SoltarArrastre();
                if (fueClic) comp.Interactuar(this);
            }
        }

        /// <summary>Suelta lo que se esté arrastrando (por ejemplo al abrir un menú).</summary>
        public void SoltarArrastre()
        {
            if (arrastrando == null) return;
            var a = arrastrando;
            arrastrando = null;
            objetivoArrastre = null;
            arrastreConfirmado = false;
            a.TerminarArrastre(this);
        }

        /// <summary>Muestra un mensaje en el HUD (por ejemplo "Necesitas la llave roja").</summary>
        public void MostrarMensaje(string texto, float segundos = 2.5f)
        {
            Mensaje = texto;
            MensajeHasta = Time.time + segundos;
            AlMostrarMensaje?.Invoke(texto, segundos);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.forward * distancia);
        }
    }
}
