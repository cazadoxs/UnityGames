using System.Collections;
using UnityEngine;

namespace Mecanicas.Puertas
{
    public enum TipoTecla { Digito, Borrar, Aceptar }

    /// <summary>Tecla física de un <see cref="PanelNumerico"/>. Se hunde al pulsarla.</summary>
    [AddComponentMenu("Mecánicas/Puertas/Tecla de panel")]
    public class TeclaPanel : Interactuable
    {
        [SerializeField] TipoTecla tipo = TipoTecla.Digito;
        [Range(0, 9)] [SerializeField] int digito = 1;
        [Tooltip("Panel al que pertenece. Vacío = se busca en los padres.")]
        [SerializeField] PanelNumerico panel;
        [Tooltip("Dirección local en la que se hunde.")]
        [SerializeField] Vector3 direccionPulsado = Vector3.forward;
        [SerializeField] float profundidad = 0.004f;

        Vector3 posBase;
        Coroutine anim;

        public TipoTecla Tipo => tipo;
        public int Digito => digito;

        void Awake()
        {
            if (panel == null) panel = GetComponentInParent<PanelNumerico>();
            posBase = transform.localPosition;
        }

        /// <summary>Configura la tecla desde código (lo usa el generador de la escena demo).</summary>
        public void Configurar(TipoTecla t, int d, PanelNumerico p)
        {
            tipo = t; digito = d; panel = p;
        }

        public override string ObtenerTexto(Interactor interactor)
        {
            switch (tipo)
            {
                case TipoTecla.Borrar: return "Borrar";
                case TipoTecla.Aceptar: return "Aceptar";
                default: return "Pulsar " + digito;
            }
        }

        public override bool PuedeInteractuar(Interactor interactor) => panel != null && base.PuedeInteractuar(interactor);

        protected override void AlInteractuar(Interactor interactor)
        {
            switch (tipo)
            {
                case TipoTecla.Borrar: panel.Borrar(); break;
                case TipoTecla.Aceptar: panel.Confirmar(); break;
                default: panel.PulsarDigito((char)('0' + digito)); break;
            }
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Hundir());
        }

        IEnumerator Hundir()
        {
            Vector3 abajo = posBase + transform.localRotation * direccionPulsado.normalized * profundidad;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.06f) { transform.localPosition = Vector3.Lerp(posBase, abajo, t); yield return null; }
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.1f) { transform.localPosition = Vector3.Lerp(abajo, posBase, t); yield return null; }
            transform.localPosition = posBase;
        }
    }
}
