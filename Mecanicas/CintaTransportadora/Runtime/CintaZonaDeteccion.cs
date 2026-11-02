using UnityEngine;

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Va en el objeto que tiene el Trigger de la cinta y le pasa a la
    /// <see cref="CintaTransportadora"/> los objetos que entran y salen.
    /// La propia cinta lo añade sola; no hace falta configurarlo.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Mecanicas/Cinta Transportadora/Zona de deteccion (interna)")]
    public class CintaZonaDeteccion : MonoBehaviour
    {
        [Tooltip("Cinta a la que avisa. Vacío = la busca en los padres.")]
        public CintaTransportadora cinta;

        void Awake()
        {
            if (cinta == null) cinta = GetComponentInParent<CintaTransportadora>();
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider otro) { if (cinta != null) cinta.Entrar(otro); }
        void OnTriggerExit(Collider otro) { if (cinta != null) cinta.Salir(otro); }
    }
}
