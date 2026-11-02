using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Zona (Trigger) que "recibe" lo que llega por la cinta: cuenta los objetos,
    /// lanza un evento y, si quieres, los destruye. Úsalo al final de una cinta como
    /// caja de recogida, triturador, caja registradora, etc.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Mecanicas/Cinta Transportadora/Receptor de objetos")]
    public class CintaReceptor : MonoBehaviour
    {
        [Tooltip("Solo acepta objetos con esta etiqueta (Tag). Vacío = acepta todo.")]
        [SerializeField] string etiqueta = "";

        [Tooltip("Capas aceptadas.")]
        [SerializeField] LayerMask capas = ~0;

        [Tooltip("Solo acepta objetos con Rigidbody (evita contar al jugador o al suelo).")]
        [SerializeField] bool soloRigidbody = true;

        [Tooltip("Destruir el objeto al recibirlo.")]
        [SerializeField] bool destruirAlRecibir = true;

        [Tooltip("Segundos de espera antes de destruirlo.")]
        [Min(0f)] [SerializeField] float retrasoDestruccion = 0f;

        [Tooltip("Se llama con cada objeto recibido.")]
        public CintaTransportadora.GameObjectEvent alRecibir = new CintaTransportadora.GameObjectEvent();

        readonly HashSet<GameObject> recibidos = new HashSet<GameObject>();

        /// <summary>Total de objetos recibidos desde el inicio (o desde el último reinicio).</summary>
        public int Contador { get; private set; }

        public void ReiniciarContador() => Contador = 0;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider otro)
        {
            if (otro.isTrigger) return;
            var rb = otro.attachedRigidbody;
            if (soloRigidbody && rb == null) return;
            GameObject obj = rb != null ? rb.gameObject : otro.gameObject;

            if ((capas.value & (1 << obj.layer)) == 0) return;
            if (!string.IsNullOrEmpty(etiqueta) && !obj.CompareTag(etiqueta)) return;

            recibidos.RemoveWhere(o => o == null);
            if (!recibidos.Add(obj)) return; // ya contado (objetos con varios colliders)

            Contador++;
            alRecibir.Invoke(obj);
            if (destruirAlRecibir) Destroy(obj, retrasoDestruccion);
        }

        void OnTriggerExit(Collider otro)
        {
            // Si no se destruye, permitir volver a contarlo cuando salga y vuelva a entrar.
            if (destruirAlRecibir) return;
            var rb = otro.attachedRigidbody;
            GameObject obj = rb != null ? rb.gameObject : otro.gameObject;
            recibidos.Remove(obj);
        }
    }
}
