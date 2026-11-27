using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Efecto sencillo que crece (o encoge) y se destruye solo. Lo usa la fábrica como chispa de
    /// impacto; sustitúyelo por un ParticleSystem cuando tengas arte.
    /// </summary>
    [AddComponentMenu("Mecanicas/Torretas/Efecto Temporal")]
    public class EfectoTemporal : MonoBehaviour
    {
        [Min(0.01f)] public float duracion = 0.25f;
        public float escalaInicial = 0.2f;
        public float escalaFinal = 0.7f;
        public bool destruirAlTerminar = true;

        float t;

        void OnEnable()
        {
            t = 0f;
            transform.localScale = Vector3.one * escalaInicial;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracion);
            // Crece rápido al principio y desaparece encogiendo al final.
            float s = Mathf.Lerp(escalaInicial, escalaFinal, Mathf.Sqrt(k)) * (1f - k * k);
            transform.localScale = Vector3.one * Mathf.Max(0f, s);
            if (k >= 1f)
            {
                if (destruirAlTerminar) Destroy(gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}
