using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Mecanicas.Eventos
{
    /// <summary>Dónde suena el sonido.</summary>
    public enum PosicionSonido
    {
        [Tooltip("En el AudioSource indicado (con su configuración).")] EnFuente,
        [Tooltip("En la posición de un punto de la escena (3D).")] EnPunto,
        [Tooltip("En el objeto que activó la zona (normalmente el jugador).")] EnActivador,
        [Tooltip("Sin posición: se oye igual en los dos oídos (música, voz en off, interfaz).")] Global2D
    }

    /// <summary>Reproduce (o detiene) un sonido, con variación aleatoria y fundido opcional.</summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Sonido")]
    public class AccionSonido : AccionEvento
    {
        [Tooltip("Si hay varios, se elige uno al azar cada vez.")]
        [SerializeField] private AudioClip[] clips = new AudioClip[0];

        [SerializeField] private PosicionSonido posicion = PosicionSonido.EnPunto;

        [Tooltip("AudioSource que se usa en modo En Fuente.")]
        [SerializeField] private AudioSource fuente;

        [Tooltip("Punto donde suena en modo En Punto. Vacío = esta acción.")]
        [SerializeField] private Transform punto;

        [Header("Sonido")]
        [Range(0f, 1f)] [SerializeField] private float volumen = 1f;
        [Tooltip("Tono mínimo y máximo (se elige uno al azar). 1 = normal.")]
        [SerializeField] private Vector2 tono = new Vector2(0.95f, 1.05f);
        [SerializeField] private AudioMixerGroup grupoMezclador;
        [SerializeField] private bool bucle = false;
        [Tooltip("Segundos de fundido de entrada.")]
        [Min(0f)] [SerializeField] private float fundidoEntrada = 0f;

        [Header("3D (En Punto y En Activador)")]
        [Min(0f)] [SerializeField] private float distanciaMinima = 1f;
        [Min(0.01f)] [SerializeField] private float distanciaMaxima = 30f;

        [Header("Detener")]
        [Tooltip("En vez de reproducir, para el sonido que lanzó esta acción (o el de la fuente).")]
        [SerializeField] private bool detenerEnVezDeReproducir = false;
        [Min(0f)] [SerializeField] private float fundidoSalida = 0.5f;

        [Header("Espera")]
        [Tooltip("La acción no termina hasta que el sonido acaba (no se aplica en bucle).")]
        [SerializeField] private bool esperarFinSonido = false;

        private AudioSource ultima;

        private void Reset()
        {
            fuente = GetComponent<AudioSource>();
            if (fuente != null) posicion = PosicionSonido.EnFuente;
        }

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            if (detenerEnVezDeReproducir)
            {
                AudioSource s = posicion == PosicionSonido.EnFuente && fuente != null ? fuente : ultima;
                if (s != null) yield return Fundir(s, s.volume, 0f, fundidoSalida, true);
                yield break;
            }

            AudioClip clip = ElegirClip();
            if (clip == null)
            {
                Debug.LogWarning($"[Eventos] '{name}': AccionSonido sin clips.", this);
                yield break;
            }

            AudioSource src = PrepararFuente(contexto);
            if (src == null) yield break;
            ultima = src;

            src.clip = clip;
            src.loop = bucle;
            src.pitch = Random.Range(Mathf.Min(tono.x, tono.y), Mathf.Max(tono.x, tono.y));
            if (grupoMezclador != null) src.outputAudioMixerGroup = grupoMezclador;
            src.volume = fundidoEntrada > 0f ? 0f : volumen;
            src.Play();

            bool temporal = src.gameObject.name == NombreTemporal;
            if (temporal && !bucle)
                Destroy(src.gameObject, clip.length / Mathf.Max(0.01f, Mathf.Abs(src.pitch)) + 0.1f);

            if (fundidoEntrada > 0f)
                yield return Fundir(src, 0f, volumen, fundidoEntrada, false);

            if (esperarFinSonido && !bucle)
                while (src != null && src.isPlaying) yield return null;
        }

        private const string NombreTemporal = "Sonido (Eventos)";

        private AudioSource PrepararFuente(ContextoEvento ctx)
        {
            switch (posicion)
            {
                case PosicionSonido.EnFuente:
                    if (fuente == null) Debug.LogWarning($"[Eventos] '{name}': falta el AudioSource.", this);
                    return fuente;

                case PosicionSonido.EnActivador:
                    Transform t = ctx.Activador != null ? ctx.Activador.transform : transform;
                    return CrearTemporal(t.position, 1f, t);

                case PosicionSonido.Global2D:
                    return CrearTemporal(transform.position, 0f, null);

                default:
                    return CrearTemporal((punto != null ? punto : transform).position, 1f, null);
            }
        }

        private AudioSource CrearTemporal(Vector3 pos, float espacial, Transform padre)
        {
            GameObject go = new GameObject(NombreTemporal);
            go.transform.position = pos;
            if (padre != null) go.transform.SetParent(padre, true);
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = espacial;
            s.minDistance = distanciaMinima;
            s.maxDistance = Mathf.Max(distanciaMinima + 0.01f, distanciaMaxima);
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            return s;
        }

        private AudioClip ElegirClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        private IEnumerator Fundir(AudioSource s, float desde, float hasta, float dur, bool pararAlFinal)
        {
            for (float t = 0f; t < dur && s != null; t += Time.deltaTime)
            {
                s.volume = Mathf.Lerp(desde, hasta, t / dur);
                yield return null;
            }
            if (s == null) yield break;
            s.volume = hasta;
            if (pararAlFinal)
            {
                s.Stop();
                s.volume = volumen;
                if (s.gameObject.name == NombreTemporal) Destroy(s.gameObject);
            }
        }

        public override void Detener()
        {
            base.Detener();
            if (ultima != null && ultima.loop) ultima.Stop();
        }
    }
}
