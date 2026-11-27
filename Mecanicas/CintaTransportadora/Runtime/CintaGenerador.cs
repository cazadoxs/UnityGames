using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Hace aparecer objetos (normalmente sobre una cinta) cada cierto tiempo o cuando
    /// se le llama con <see cref="Generar"/>. Si no le das prefabs, crea cajas y
    /// esferas de colores para pruebas.
    /// </summary>
    [AddComponentMenu("Mecanicas/Cinta Transportadora/Generador de objetos")]
    public class CintaGenerador : MonoBehaviour
    {
        [Tooltip("Prefabs a generar. Vacío = cajas y esferas de prueba.")]
        [SerializeField] GameObject[] prefabs = new GameObject[0];

        [Tooltip("Elegir el prefab al azar (si no, van en orden).")]
        [SerializeField] bool aleatorio = true;

        [Tooltip("Generar solo cada 'intervalo' segundos.")]
        [SerializeField] bool automatico = true;

        [Min(0.05f)] [SerializeField] float intervalo = 1.5f;

        [Tooltip("Máximo de objetos vivos a la vez (0 = sin límite). Si se llega, no se generan más.")]
        [Min(0)] [SerializeField] int maximoVivos = 30;

        [Tooltip("Desplazamiento aleatorio de la posición (m) en X y Z locales.")]
        [SerializeField] Vector2 dispersion = new Vector2(0.2f, 0f);

        [Tooltip("Girar el objeto al azar en el eje vertical al crearlo.")]
        [SerializeField] bool rotacionAleatoria = true;

        [Tooltip("Padre de los objetos creados (vacío = raíz de la escena).")]
        [SerializeField] Transform contenedor;

        [Tooltip("Tamaño de los objetos de prueba (solo si no hay prefabs).")]
        [Min(0.05f)] [SerializeField] float tamanoPrueba = 0.35f;

        [Tooltip("Se llama con cada objeto nuevo.")]
        public CintaTransportadora.GameObjectEvent alGenerar = new CintaTransportadora.GameObjectEvent();

        readonly List<GameObject> vivos = new List<GameObject>();
        float temporizador;
        int siguiente;

        public bool Automatico { get => automatico; set => automatico = value; }
        public float Intervalo { get => intervalo; set => intervalo = Mathf.Max(0.05f, value); }

        void Update()
        {
            if (!automatico) return;
            temporizador += Time.deltaTime;
            if (temporizador >= intervalo)
            {
                temporizador = 0f;
                Generar();
            }
        }

        /// <summary>Genera un objeto ahora (respeta el máximo de vivos). Devuelve null si no pudo.</summary>
        public GameObject Generar()
        {
            vivos.RemoveAll(o => o == null);
            if (maximoVivos > 0 && vivos.Count >= maximoVivos) return null;

            Vector3 pos = transform.TransformPoint(new Vector3(
                Random.Range(-dispersion.x, dispersion.x), 0f, Random.Range(-dispersion.y, dispersion.y)));
            Quaternion rot = transform.rotation;
            if (rotacionAleatoria) rot = Quaternion.AngleAxis(Random.Range(0f, 360f), transform.up) * rot;

            GameObject obj;
            if (prefabs != null && prefabs.Length > 0)
            {
                int i = aleatorio ? Random.Range(0, prefabs.Length) : (siguiente++ % prefabs.Length);
                if (prefabs[i] == null) return null;
                obj = Instantiate(prefabs[i], pos, rot, contenedor);
            }
            else
            {
                obj = CrearObjetoPrueba(pos, rot);
            }

            vivos.Add(obj);
            alGenerar.Invoke(obj);
            return obj;
        }

        /// <summary>Generar sin devolver nada (para enlazar desde UnityEvents / botones).</summary>
        public void GenerarUno() => Generar();

        /// <summary>Destruye todos los objetos que creó este generador.</summary>
        public void DestruirTodos()
        {
            foreach (var o in vivos) if (o != null) Destroy(o);
            vivos.Clear();
        }

        GameObject CrearObjetoPrueba(Vector3 pos, Quaternion rot)
        {
            bool esfera = Random.value < 0.3f;
            var obj = GameObject.CreatePrimitive(esfera ? PrimitiveType.Sphere : PrimitiveType.Cube);
            obj.name = esfera ? "Esfera de prueba" : "Caja de prueba";
            obj.transform.SetParent(contenedor, false);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.transform.localScale = Vector3.one * tamanoPrueba;
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var r = obj.GetComponent<Renderer>();
            r.sharedMaterial = CintaMateriales.Prueba();
            var bloque = new MaterialPropertyBlock();
            Color color = Color.HSVToRGB(Random.value, 0.65f, 0.9f);
            bloque.SetColor("_BaseColor", color); // URP / HDRP
            bloque.SetColor("_Color", color);     // Built-in
            r.SetPropertyBlock(bloque);
            return obj;
        }
    }
}
