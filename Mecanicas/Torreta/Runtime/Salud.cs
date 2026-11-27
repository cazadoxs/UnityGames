using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Vida sencilla para objetivos. Las torretas la usan para saber si un objetivo sigue vivo
    /// y para la prioridad "Menos vida".
    /// </summary>
    [AddComponentMenu("Mecanicas/Torretas/Salud")]
    public class Salud : MonoBehaviour, IDaniable
    {
        [Min(1f)] public float vidaMaxima = 100f;
        [Tooltip("Si está marcado, el objeto se destruye al morir.")]
        public bool destruirAlMorir = false;
        [Tooltip("Si está marcado (y no se destruye), el objeto se desactiva al morir.")]
        public bool desactivarAlMorir = false;
        [Tooltip("Efecto opcional que aparece al morir (explosión, humo...).")]
        public GameObject efectoMuerte;
        public float duracionEfectoMuerte = 3f;

        [Header("Eventos")]
        public UnityEvent<float> alRecibirDanio = new UnityEvent<float>();
        public UnityEvent alMorir = new UnityEvent();

        float vida;
        bool muerto;

        public bool EstaVivo => !muerto && isActiveAndEnabled;
        public float VidaActual => vida;
        public float Porcentaje => vidaMaxima > 0f ? vida / vidaMaxima : 0f;

        void Awake() { vida = vidaMaxima; }

        public void RecibirDanio(InfoDanio info)
        {
            if (muerto || info.cantidad <= 0f) return;
            vida = Mathf.Max(0f, vida - info.cantidad);
            alRecibirDanio.Invoke(info.cantidad);
            if (vida <= 0f) Morir();
        }

        public void Curar(float cantidad)
        {
            if (muerto) return;
            vida = Mathf.Min(vidaMaxima, vida + cantidad);
        }

        /// <summary>Devuelve la vida al máximo y vuelve a contar como vivo.</summary>
        public void Revivir()
        {
            muerto = false;
            vida = vidaMaxima;
        }

        void Morir()
        {
            muerto = true;
            if (efectoMuerte != null)
            {
                var fx = Instantiate(efectoMuerte, transform.position, Quaternion.identity);
                if (duracionEfectoMuerte > 0f) Destroy(fx, duracionEfectoMuerte);
            }
            alMorir.Invoke();
            if (destruirAlMorir) Destroy(gameObject);
            else if (desactivarAlMorir) gameObject.SetActive(false);
        }
    }
}
