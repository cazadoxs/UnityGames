using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Rueda de un candado de combinación. Clic = siguiente número; la rueda del ratón gira en ambos sentidos.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Rueda de combinación")]
    public class RuedaCombinacion : Interactuable
    {
        [SerializeField] int valorInicial;
        [SerializeField] int numeros = 10;
        [Tooltip("Parte que gira. Vacío = este objeto.")]
        [SerializeField] Transform visual;
        [Tooltip("Eje de giro local.")]
        [SerializeField] Vector3 ejeLocal = Vector3.right;
        [Tooltip("Texto 3D opcional que muestra el número actual.")]
        [SerializeField] TextMesh etiqueta;
        [SerializeField] AudioClip sonidoClic;

        CandadoCombinacion candado;
        Quaternion rotBase;
        float anguloActual;
        int valor;

        public int Valor => valor;

        void Reset() => textoAccion = "Girar rueda";

        void Awake()
        {
            if (visual == null) visual = transform;
            rotBase = visual.localRotation;
            valor = Mathf.Clamp(valorInicial, 0, numeros - 1);
            anguloActual = valor * 360f / numeros;
            if (candado == null) candado = GetComponentInParent<CandadoCombinacion>();
            Refrescar(true);
        }

        public void Configurar(CandadoCombinacion c) => candado = c;

        public override string ObtenerTexto(Interactor interactor) => textoAccion + " (" + valor + ")";

        public override bool PuedeInteractuar(Interactor interactor) =>
            base.PuedeInteractuar(interactor) && (candado == null || candado.Bloqueado);

        protected override void AlInteractuar(Interactor interactor) => Girar(1);

        public void Girar(int pasos)
        {
            valor = ((valor + pasos) % numeros + numeros) % numeros;
            ReproducirSonido(sonidoClic);
            Refrescar(false);
            if (candado != null) candado.RuedaCambiada();
        }

        void Update()
        {
            var i = Interactor.Principal;
            if (i != null && i.Objetivo == this && PuedeInteractuar(i))
            {
                float r = Entrada.Rueda;
                if (r != 0f) Girar(r > 0f ? 1 : -1);
            }

            float destino = valor * 360f / numeros;
            anguloActual = Mathf.MoveTowardsAngle(anguloActual, destino, 720f * Time.deltaTime);
            visual.localRotation = rotBase * Quaternion.AngleAxis(anguloActual, ejeLocal);
        }

        void Refrescar(bool instantaneo)
        {
            if (etiqueta != null) etiqueta.text = valor.ToString();
            if (instantaneo && visual != null) visual.localRotation = rotBase * Quaternion.AngleAxis(anguloActual, ejeLocal);
        }
    }
}
