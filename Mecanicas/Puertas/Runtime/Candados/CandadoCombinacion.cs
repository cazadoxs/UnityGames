using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Candado de ruedas numeradas. Cada rueda es un <see cref="RuedaCombinacion"/> hijo.
    /// Se abre al acertar la combinación (automático) o al tirar del candado.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Candado de combinación")]
    public class CandadoCombinacion : CandadoBase
    {
        [Header("Combinación")]
        [Tooltip("Un dígito por rueda, de izquierda a derecha según el orden de la lista.")]
        [SerializeField] string combinacion = "372";
        [Tooltip("Ruedas en orden. Vacío = se buscan en los hijos.")]
        [SerializeField] List<RuedaCombinacion> ruedas = new List<RuedaCombinacion>();
        [Tooltip("Se abre solo al acertar. Si no, hay que usar el cuerpo del candado para tirar.")]
        [SerializeField] bool abrirAutomaticamente = true;

        public string Combinacion => combinacion;

        void Reset() => textoAccion = "Tirar del candado";

        void Awake()
        {
            if (ruedas.Count == 0) ruedas.AddRange(GetComponentsInChildren<RuedaCombinacion>());
            foreach (var r in ruedas) if (r != null) r.Configurar(this);
        }

        public string ValorActual()
        {
            var sb = new StringBuilder();
            foreach (var r in ruedas) sb.Append(r != null ? r.Valor.ToString() : "?");
            return sb.ToString();
        }

        public bool EsCorrecta() => ValorActual() == combinacion;

        /// <summary>Lo llaman las ruedas al girar.</summary>
        public void RuedaCambiada()
        {
            if (cerrado && abrirAutomaticamente && EsCorrecta()) AbrirCandado();
        }

        public void EstablecerCombinacion(string nueva) => combinacion = nueva;

        protected override void AlInteractuar(Interactor interactor)
        {
            if (EsCorrecta()) AbrirCandado();
            else FalloAbrir(interactor, "No se abre. La combinación no es correcta.");
        }
    }
}
