using UnityEngine;

namespace Mecanicas.Patrulla
{
    /// <summary>
    /// Ajustes opcionales de un punto concreto de una RutaDePuntos: su propia esquina (pico o suave),
    /// tiempo de espera, límite de velocidad y si está bloqueado (semáforo, paso cerrado, etc.).
    /// Un punto sin este componente usa los valores por defecto de la ruta.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecanicas/Patrulla/Punto De Ruta")]
    public class PuntoDeRuta : MonoBehaviour
    {
        [Header("Esquina")]
        [Tooltip("Si está activo, este punto usa su propia esquina y radio en vez de los de la ruta.")]
        public bool usarEsquinaPropia = false;
        public TipoEsquina esquina = TipoEsquina.Pico;
        [Tooltip("Distancia antes y después del punto donde empieza y acaba la curva (solo esquinas suaves).")]
        [Min(0f)] public float radio = 3f;

        [Header("Espera (patrullas)")]
        [Tooltip("Segundos que se para aquí quien siga la ruta. 0 = no se para.")]
        [Min(0f)] public float espera = 0f;
        [Tooltip("Segundos extra al azar (0..este valor) sumados a la espera, para que no parezca robótico.")]
        [Min(0f)] public float esperaAleatoria = 0f;
        [Tooltip("Si está activo, durante la espera el seguidor gira para mirar hacia donde apunta el eje Z (azul) de este punto.")]
        public bool mirarEnDireccionDelPunto = false;

        [Header("Tráfico")]
        [Tooltip("Velocidad máxima (m/s) al pasar por este punto. 0 = sin límite.")]
        [Min(0f)] public float velocidadMaxima = 0f;
        [Tooltip("Si está bloqueado, los seguidores se detienen antes de llegar y esperan a que se desbloquee (semáforos, barreras...).")]
        [SerializeField] bool _bloqueado = false;

        [Tooltip("En qué sentido de marcha afecta el bloqueo. 'SoloIda' = solo a quien recorre la ruta en el orden de los puntos; " +
                 "'SoloVuelta' = solo a quien va al revés. Útil en pasos de peatones de ida y vuelta.")]
        public SentidoBloqueo sentidoBloqueo = SentidoBloqueo.Ambos;

        public enum SentidoBloqueo { Ambos, SoloIda, SoloVuelta }

        /// <summary>True si el punto está bloqueado para alguien que circula en ese sentido (+1 ida, -1 vuelta).</summary>
        public bool EstaBloqueadoPara(int sentido)
        {
            if (!_bloqueado) return false;
            switch (sentidoBloqueo)
            {
                case SentidoBloqueo.SoloIda: return sentido > 0;
                case SentidoBloqueo.SoloVuelta: return sentido < 0;
                default: return true;
            }
        }

        /// <summary>Bloquea o desbloquea el paso por este punto.</summary>
        public bool bloqueado
        {
            get => _bloqueado;
            set => _bloqueado = value;
        }

        /// <summary>Espera total para una pasada (espera + parte aleatoria).</summary>
        public float CalcularEspera()
        {
            return espera + (esperaAleatoria > 0f ? Random.Range(0f, esperaAleatoria) : 0f);
        }

        void OnValidate()
        {
            var ruta = GetComponentInParent<RutaDePuntos>();
            if (ruta != null) ruta.MarcarSucia();
        }
    }
}
