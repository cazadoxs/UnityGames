using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Sitio donde aparecen los objetos comprados. Ponlo como hijo vacío donde quieras que salga el objeto
    /// (la flecha azul, eje Z, marca hacia dónde mira el objeto y hacia dónde se empuja).
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/Punto de aparición")]
    public class PuntoAparicion : MonoBehaviour, IReceptorCompra
    {
        [Header("Colocación")]
        [Tooltip("Radio en el que se reparten los objetos si se compran varios a la vez.")]
        [Min(0)] public float radioDispersion = 0.4f;
        [Tooltip("Separación vertical entre objetos de una misma compra (para que no se solapen).")]
        [Min(0)] public float separacionVertical = 0.3f;
        [Tooltip("Padre de los objetos creados (vacío = raíz de la escena).")]
        public Transform padre;
        [Tooltip("Si está activo, no entrega mientras haya algo encima del punto (radio de comprobación).")]
        public bool exigirHuecoLibre = false;
        [Min(0.01f)] public float radioComprobacion = 0.4f;
        public LayerMask capasOcupan = ~0;

        [Header("Física")]
        [Tooltip("Velocidad inicial en espacio local (Z = hacia delante). Solo afecta a objetos con Rigidbody.")]
        public Vector3 impulsoLocal = Vector3.zero;

        [Header("Efectos")]
        [Tooltip("El objeto crece desde 0 a su tamaño real en este tiempo. 0 = aparece de golpe.")]
        [Min(0)] public float duracionEscalado = 0.2f;
        [Tooltip("Prefab de efecto (partículas, destello...) que se crea en el punto. Se destruye pasados unos segundos.")]
        public GameObject efectoAparicion;
        [Min(0)] public float vidaEfecto = 3f;
        public AudioClip sonidoAparicion;
        [Range(0, 1)] public float volumen = 1f;

        [Header("Límite")]
        [Tooltip("Máximo de objetos vivos creados por este punto. Al pasarse se borra el más antiguo. 0 = sin límite.")]
        [Min(0)] public int maxObjetosVivos = 0;

        [Serializable] public class EventoObjeto : UnityEvent<GameObject> { }
        public EventoObjeto alAparecer = new EventoObjeto();

        readonly Queue<GameObject> vivos = new Queue<GameObject>();

        public bool PuedeEntregar(ArticuloTienda articulo, int cantidad)
        {
            if (articulo == null || articulo.prefab == null) return false;
            if (exigirHuecoLibre && Physics.CheckSphere(transform.position, radioComprobacion, capasOcupan, QueryTriggerInteraction.Ignore))
                return false;
            return true;
        }

        public bool Entregar(ArticuloTienda articulo, int cantidad, List<GameObject> creados)
        {
            if (!PuedeEntregar(articulo, cantidad)) return false;
            for (int i = 0; i < cantidad; i++)
            {
                Vector3 pos = transform.position + Vector3.up * (separacionVertical * i);
                if (cantidad > 1 && radioDispersion > 0)
                {
                    Vector2 r = UnityEngine.Random.insideUnitCircle * radioDispersion;
                    pos += transform.right * r.x + transform.forward * r.y;
                }
                GameObject go = Crear(articulo.prefab, pos, transform.rotation);
                if (creados != null) creados.Add(go);
            }
            if (efectoAparicion != null)
            {
                var fx = Instantiate(efectoAparicion, transform.position, transform.rotation);
                if (vidaEfecto > 0) Destroy(fx, vidaEfecto);
            }
            if (sonidoAparicion != null) AudioSource.PlayClipAtPoint(sonidoAparicion, transform.position, volumen);
            return true;
        }

        /// <summary>Crea un objeto en el punto sin pasar por la tienda (útil para pruebas).</summary>
        public GameObject Crear(GameObject prefab, Vector3 posicion, Quaternion rotacion)
        {
            GameObject go = Instantiate(prefab, posicion, rotacion, padre);
            if (!go.activeSelf) go.SetActive(true); // por si el prefab es una plantilla desactivada
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null && impulsoLocal != Vector3.zero)
                rb.AddForce(transform.TransformDirection(impulsoLocal), ForceMode.VelocityChange);
            if (duracionEscalado > 0) StartCoroutine(Escalar(go.transform, duracionEscalado));
            alAparecer.Invoke(go);

            if (maxObjetosVivos > 0)
            {
                vivos.Enqueue(go);
                while (vivos.Count > maxObjetosVivos)
                {
                    var viejo = vivos.Dequeue();
                    if (viejo != null) Destroy(viejo);
                }
            }
            return go;
        }

        static IEnumerator Escalar(Transform t, float duracion)
        {
            Vector3 final = t.localScale;
            float tiempo = 0f;
            t.localScale = Vector3.zero;
            while (tiempo < duracion && t != null)
            {
                tiempo += Time.deltaTime;
                float k = Mathf.Clamp01(tiempo / duracion);
                k = 1f - (1f - k) * (1f - k); // ease-out
                t.localScale = final * k;
                yield return null;
            }
            if (t != null) t.localScale = final;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, exigirHuecoLibre ? radioComprobacion : 0.15f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f);
            if (radioDispersion > 0)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.25f);
                Gizmos.DrawWireSphere(transform.position, radioDispersion);
            }
        }
    }
}
