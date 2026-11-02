using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    [Serializable] public class EventoEntero : UnityEvent<int> { }
    [Serializable] public class EventoBool : UnityEvent<bool> { }

    /// <summary>
    /// Palanca de 2 o más posiciones. Clic = siguiente posición; mantener y arrastrar = moverla a mano
    /// (al soltar encaja en la posición más cercana). Con "Retorno automático" vuelve sola a la posición 0
    /// (palanca de hombre muerto / pulsador).
    /// Es una señal (ISenal): activa cuando está en la última posición.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Palanca")]
    public class Palanca : Interactuable, IArrastrable, ISenal
    {
        [Header("Piezas")]
        [Tooltip("Mango que gira. Su origen debe estar en el punto de giro. Vacío = este objeto.")]
        [SerializeField] Transform mango;
        [SerializeField] Vector3 ejeLocal = Vector3.right;
        [SerializeField] float anguloMinimo = -40f;
        [SerializeField] float anguloMaximo = 40f;

        [Header("Posiciones")]
        [Min(2)] [SerializeField] int posiciones = 2;
        [SerializeField] int posicionInicial;
        [SerializeField] bool retornoAutomatico;
        [SerializeField] float retrasoRetorno = 0.4f;
        [SerializeField] bool permitirArrastre = true;
        [Tooltip("Segundos para encajar en la posición.")]
        [SerializeField] float suavizado = 0.08f;

        [Header("Sonidos")]
        [SerializeField] AudioClip sonidoClic;

        [Header("Eventos")]
        public EventoEntero alCambiarPosicion = new EventoEntero();
        [Tooltip("Al llegar a la última posición.")]
        public UnityEvent alActivar = new UnityEvent();
        [Tooltip("Al salir de la última posición.")]
        public UnityEvent alDesactivar = new UnityEvent();
        public EventoBool alCambiarEstado = new EventoBool();

        Quaternion rotBase;
        float angulo, velocidad;
        int posicion;
        int direccion = 1;
        bool arrastrando;
        Vector3 agarreLocal;
        float retornarEn = -1f;

        public event Action<bool> AlCambiarSenal;
        public int Posicion => posicion;
        public bool Activa => posicion == posiciones - 1;
        public bool UsaArrastre => permitirArrastre;
        public bool ArrastreEnProfundidad => false;

        void Reset() => textoAccion = "Accionar palanca";

        void Awake()
        {
            if (mango == null) mango = transform;
            rotBase = mango.localRotation;
            posicion = Mathf.Clamp(posicionInicial, 0, posiciones - 1);
            angulo = AnguloDe(posicion);
            Aplicar();
        }

        float AnguloDe(int p) => Mathf.Lerp(anguloMinimo, anguloMaximo, posiciones <= 1 ? 0f : p / (float)(posiciones - 1));

        public override string ObtenerTexto(Interactor interactor) => arrastrando ? "Mueve el ratón" : textoAccion;

        protected override void AlInteractuar(Interactor interactor)
        {
            int siguiente = posicion + direccion;
            if (siguiente >= posiciones || siguiente < 0) { direccion = -direccion; siguiente = posicion + direccion; }
            EstablecerPosicion(siguiente);
        }

        /// <summary>Mueve la palanca a una posición (para UnityEvents o scripts).</summary>
        public void EstablecerPosicion(int nueva)
        {
            nueva = Mathf.Clamp(nueva, 0, posiciones - 1);
            if (nueva == posicion) return;
            bool estabaActiva = Activa;
            posicion = nueva;
            ReproducirSonido(sonidoClic);
            alCambiarPosicion.Invoke(posicion);
            if (Activa != estabaActiva)
            {
                if (Activa) alActivar.Invoke(); else alDesactivar.Invoke();
                alCambiarEstado.Invoke(Activa);
                AlCambiarSenal?.Invoke(Activa);
            }
            retornarEn = retornoAutomatico && posicion != 0 ? Time.time + retrasoRetorno : -1f;
        }

        public void Activar() => EstablecerPosicion(posiciones - 1);
        public void Desactivar() => EstablecerPosicion(0);

        public void EmpezarArrastre(Interactor interactor, RaycastHit impacto)
        {
            arrastrando = true;
            agarreLocal = mango.InverseTransformPoint(impacto.point);
            retornarEn = -1f;
        }

        public void Arrastrar(Interactor interactor, Vector3 mov)
        {
            Vector3 eje = mango.TransformDirection(ejeLocal).normalized;
            float d = ArrastreUtil.GradosAlrededorDeEje(mango.position, eje, mango.TransformPoint(agarreLocal), mov);
            float lo = Mathf.Min(anguloMinimo, anguloMaximo), hi = Mathf.Max(anguloMinimo, anguloMaximo);
            angulo = Mathf.Clamp(angulo + d, lo, hi);
        }

        public void TerminarArrastre(Interactor interactor)
        {
            arrastrando = false;
            int mejor = 0; float dist = float.MaxValue;
            for (int i = 0; i < posiciones; i++)
            {
                float di = Mathf.Abs(Mathf.DeltaAngle(angulo, AnguloDe(i)));
                if (di < dist) { dist = di; mejor = i; }
            }
            EstablecerPosicion(mejor);
        }

        void Update()
        {
            if (retornarEn > 0f && Time.time >= retornarEn) { retornarEn = -1f; EstablecerPosicion(0); }
            if (!arrastrando) angulo = Mathf.SmoothDamp(angulo, AnguloDe(posicion), ref velocidad, suavizado);
            Aplicar();
        }

        void Aplicar()
        {
            if (mango != null) mango.localRotation = rotBase * Quaternion.AngleAxis(angulo, ejeLocal);
        }
    }
}
