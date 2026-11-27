using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Base común de los candados: es un objeto físico que bloquea la puerta (hazlo hijo de ella
    /// o añádelo a su lista de bloqueos). Al abrirse, el arco sube y el candado puede caer al suelo.
    /// </summary>
    public abstract class CandadoBase : Interactuable, IBloqueo
    {
        [Header("Candado")]
        [Tooltip("Arco (la U metálica). Vacío = sin animación.")]
        [SerializeField] protected Transform arco;
        [Tooltip("Cuánto se mueve el arco al abrirse, en local.")]
        [SerializeField] Vector3 desplazamientoArco = new Vector3(0f, 0.025f, 0f);
        [Tooltip("Giro del arco al abrirse (grados alrededor del eje Y local), como en los candados reales.")]
        [SerializeField] float giroArco = 0f;
        [SerializeField] float duracionApertura = 0.25f;
        [Tooltip("Si está marcado, al abrirse se suelta y cae con física.")]
        [SerializeField] bool caerAlAbrir = true;
        [Tooltip("Segundos tras caer para destruirlo (0 = nunca).")]
        [SerializeField] float destruirTras = 0f;
        [SerializeField] string motivo = "Tiene un candado.";

        [Header("Sonidos")]
        [SerializeField] protected AudioClip sonidoAbrir;
        [SerializeField] protected AudioClip sonidoFallo;

        public UnityEvent alAbrir = new UnityEvent();

        protected bool cerrado = true;

        public bool Bloqueado => cerrado;
        public bool ImpideCerrar => false;
        public string Motivo => motivo;
        public bool IntentarDesbloquearDesdePuerta(Interactor interactor, out string mensaje) { mensaje = null; return false; }

        public override bool PuedeInteractuar(Interactor interactor) => cerrado && base.PuedeInteractuar(interactor);

        /// <summary>Abre el candado sin condiciones (también para UnityEvents).</summary>
        public void AbrirCandado()
        {
            if (!cerrado) return;
            cerrado = false;
            ReproducirSonido(sonidoAbrir);
            alAbrir.Invoke();
            StartCoroutine(AnimarApertura());
        }

        IEnumerator AnimarApertura()
        {
            if (arco != null)
            {
                Vector3 p0 = arco.localPosition;
                Quaternion r0 = arco.localRotation;
                for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duracionApertura))
                {
                    float k = Mathf.SmoothStep(0f, 1f, t);
                    arco.localPosition = p0 + desplazamientoArco * k;
                    arco.localRotation = r0 * Quaternion.AngleAxis(giroArco * k, Vector3.up);
                    yield return null;
                }
                arco.localPosition = p0 + desplazamientoArco;
                arco.localRotation = r0 * Quaternion.AngleAxis(giroArco, Vector3.up);
            }

            if (caerAlAbrir)
            {
                transform.SetParent(null, true);
                foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = false;
                var rb = GetComponent<Rigidbody>();
                if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.mass = 0.4f;
                rb.AddForce(transform.forward * 0.4f + Vector3.up * 0.3f, ForceMode.Impulse);
                if (destruirTras > 0f) Destroy(gameObject, destruirTras);
            }
        }

        /// <summary>Pequeño temblor para indicar que no se abre.</summary>
        protected IEnumerator Sacudir()
        {
            Vector3 p0 = transform.localPosition;
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                transform.localPosition = p0 + transform.parent.InverseTransformVector(Vector3.up) * (Mathf.Sin(t * 80f) * 0.004f * (1f - t / 0.25f));
                yield return null;
            }
            transform.localPosition = p0;
        }

        protected void FalloAbrir(Interactor interactor, string mensaje)
        {
            if (interactor != null && !string.IsNullOrEmpty(mensaje)) interactor.MostrarMensaje(mensaje);
            ReproducirSonido(sonidoFallo);
            if (transform.parent != null) StartCoroutine(Sacudir());
        }
    }
}
