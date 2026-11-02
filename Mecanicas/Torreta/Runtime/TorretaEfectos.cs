using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Todo lo que se ve y se oye: retroceso del cañón, fogonazo, luz, partículas, parámetros del
    /// Animator y sonidos. No toma decisiones: solo reacciona a los eventos de <see cref="Torreta"/>.
    /// Puedes quitarlo, sustituirlo o tener varios.
    /// </summary>
    [RequireComponent(typeof(Torreta))]
    [AddComponentMenu("Mecanicas/Torretas/Torreta Efectos")]
    public class TorretaEfectos : MonoBehaviour
    {
        [Header("Retroceso (animación por código)")]
        [Tooltip("Piezas que retroceden al disparar, en el mismo orden que los puntos de disparo. " +
                 "Si solo hay una, retrocede con cualquier cañón.")]
        public Transform[] piezasRetroceso = new Transform[0];
        [Tooltip("Dirección del retroceso en coordenadas locales de la pieza. (0,0,-1) = hacia atrás.")]
        public Vector3 direccionRetroceso = Vector3.back;
        [Min(0f)] public float distanciaRetroceso = 0.15f;
        [Tooltip("Tiempo en ir hacia atrás.")]
        [Min(0.001f)] public float tiempoGolpe = 0.03f;
        [Tooltip("Tiempo en volver a su sitio.")]
        [Min(0.001f)] public float tiempoRecuperacion = 0.18f;

        [Header("Fogonazo")]
        [Tooltip("Objetos que se encienden un instante al disparar (uno por cañón, en el mismo orden).")]
        public GameObject[] fogonazos = new GameObject[0];
        [Min(0.005f)] public float duracionFogonazo = 0.05f;
        [Tooltip("Gira el fogonazo al azar sobre su eje Z para que no se vea siempre igual.")]
        public bool girarFogonazoAlAzar = true;
        [Tooltip("Partículas que se reproducen al disparar (uno por cañón, en el mismo orden).")]
        public ParticleSystem[] particulasDisparo = new ParticleSystem[0];
        public Light luzFogonazo;
        [Min(0f)] public float intensidadLuz = 4f;
        [Min(0.005f)] public float duracionLuz = 0.06f;

        [Header("Animator (opcional)")]
        [Tooltip("Si tienes animaciones hechas a mano, se avisan con estos parámetros. Los que no existan se ignoran.")]
        public Animator animator;
        public string triggerDisparo = "Disparar";
        public string boolTieneObjetivo = "TieneObjetivo";
        public string boolRecargando = "Recargando";
        public string floatVelocidadGiro = "VelocidadGiro";

        [Header("Sonido")]
        [Tooltip("Fuente para disparos y avisos. Si está vacía se crea una.")]
        public AudioSource fuenteEfectos;
        public AudioClip[] sonidosDisparo = new AudioClip[0];
        [Range(0f, 1f)] public float volumenDisparo = 0.8f;
        [Tooltip("Variación aleatoria del tono para que no suene repetitivo.")]
        public Vector2 rangoTono = new Vector2(0.92f, 1.08f);
        public AudioClip sonidoObjetivoFijado;
        public AudioClip sonidoRecarga;
        [Range(0f, 1f)] public float volumenAvisos = 0.5f;

        [Header("Sonido del motor de giro")]
        [Tooltip("Fuente en bucle para el motor. Si está vacía y hay clip de motor, se crea una.")]
        public AudioSource fuenteMotor;
        public AudioClip sonidoMotor;
        [Range(0f, 1f)] public float volumenMaximoMotor = 0.35f;
        [Tooltip("Velocidad de giro (grados/s) a la que el motor suena al máximo.")]
        [Min(1f)] public float velocidadParaVolumenMaximo = 120f;

        [Header("Sonidos generados")]
        [Tooltip("Si falta algún clip, usa uno generado por código (útil para prototipos).")]
        public bool generarSonidosSiFaltan = true;
        [Tooltip("Distancia a la que el sonido 3D empieza a bajar.")]
        public float distanciaMinimaSonido = 3f;
        public float distanciaMaximaSonido = 60f;

        Torreta torreta;
        Vector3[] posBaseRetroceso;
        float[] tiempoDesdeDisparo;
        float[] apagarFogonazo;
        float apagarLuz = -1f;
        readonly HashSet<int> parametrosAnimator = new HashSet<int>();

        void Awake()
        {
            torreta = GetComponent<Torreta>();

            posBaseRetroceso = new Vector3[piezasRetroceso.Length];
            tiempoDesdeDisparo = new float[piezasRetroceso.Length];
            for (int i = 0; i < piezasRetroceso.Length; i++)
            {
                if (piezasRetroceso[i] != null) posBaseRetroceso[i] = piezasRetroceso[i].localPosition;
                tiempoDesdeDisparo[i] = float.MaxValue;
            }

            apagarFogonazo = new float[fogonazos.Length];
            for (int i = 0; i < fogonazos.Length; i++) if (fogonazos[i] != null) fogonazos[i].SetActive(false);
            if (luzFogonazo != null) luzFogonazo.intensity = 0f;

            if (generarSonidosSiFaltan)
            {
                if (sonidosDisparo == null || sonidosDisparo.Length == 0) sonidosDisparo = new[] { SonidoProcedural.Disparo() };
                if (sonidoObjetivoFijado == null) sonidoObjetivoFijado = SonidoProcedural.Pitido();
                if (sonidoRecarga == null) sonidoRecarga = SonidoProcedural.Recarga();
                if (sonidoMotor == null && torreta.modo != ModoApuntado.Fija) sonidoMotor = SonidoProcedural.Motor();
            }

            if (fuenteEfectos == null) fuenteEfectos = CrearFuente("Sonido efectos", false);
            if (fuenteMotor == null && sonidoMotor != null) fuenteMotor = CrearFuente("Sonido motor", true);
            if (fuenteMotor != null && sonidoMotor != null)
            {
                fuenteMotor.clip = sonidoMotor;
                fuenteMotor.loop = true;
                fuenteMotor.volume = 0f;
                fuenteMotor.Play();
            }

            if (animator != null)
                foreach (var p in animator.parameters) parametrosAnimator.Add(p.nameHash);
        }

        void OnEnable()
        {
            if (torreta == null) torreta = GetComponent<Torreta>();
            torreta.Disparo += AlDisparar;
            torreta.ObjetivoCambiado += AlCambiarObjetivo;
            torreta.RecargaIniciada += AlEmpezarRecarga;
            torreta.RecargaTerminada += AlTerminarRecarga;
        }

        void OnDisable()
        {
            if (torreta == null) return;
            torreta.Disparo -= AlDisparar;
            torreta.ObjetivoCambiado -= AlCambiarObjetivo;
            torreta.RecargaIniciada -= AlEmpezarRecarga;
            torreta.RecargaTerminada -= AlTerminarRecarga;
        }

        AudioSource CrearFuente(string nombre, bool bucle)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(torreta.pivotGiro != null ? torreta.pivotGiro : transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = bucle;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = distanciaMinimaSonido;
            s.maxDistance = distanciaMaximaSonido;
            return s;
        }

        // ------------------------------------------------------------------ Reacciones
        void AlDisparar(int canon)
        {
            // Retroceso
            if (piezasRetroceso.Length > 0)
            {
                int i = piezasRetroceso.Length == 1 ? 0 : Mathf.Clamp(canon, 0, piezasRetroceso.Length - 1);
                tiempoDesdeDisparo[i] = 0f;
            }

            // Fogonazo
            if (fogonazos.Length > 0)
            {
                int i = fogonazos.Length == 1 ? 0 : Mathf.Clamp(canon, 0, fogonazos.Length - 1);
                var f = fogonazos[i];
                if (f != null)
                {
                    f.SetActive(true);
                    if (girarFogonazoAlAzar) f.transform.localRotation *= Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                    apagarFogonazo[i] = Time.time + duracionFogonazo;
                }
            }
            if (particulasDisparo.Length > 0)
            {
                int i = particulasDisparo.Length == 1 ? 0 : Mathf.Clamp(canon, 0, particulasDisparo.Length - 1);
                if (particulasDisparo[i] != null) particulasDisparo[i].Play(true);
            }
            if (luzFogonazo != null)
            {
                if (torreta.puntosDisparo != null && canon < torreta.puntosDisparo.Length && torreta.puntosDisparo[canon] != null)
                    luzFogonazo.transform.position = torreta.puntosDisparo[canon].position;
                luzFogonazo.intensity = intensidadLuz;
                apagarLuz = Time.time + duracionLuz;
            }

            // Animator
            Trigger(triggerDisparo);

            // Sonido
            if (fuenteEfectos != null && sonidosDisparo != null && sonidosDisparo.Length > 0)
            {
                var clip = sonidosDisparo[Random.Range(0, sonidosDisparo.Length)];
                if (clip != null)
                {
                    fuenteEfectos.pitch = Random.Range(rangoTono.x, rangoTono.y);
                    fuenteEfectos.PlayOneShot(clip, volumenDisparo);
                }
            }
        }

        void AlCambiarObjetivo(Transform nuevo)
        {
            Bool(boolTieneObjetivo, nuevo != null);
            if (nuevo != null) Aviso(sonidoObjetivoFijado);
        }

        void AlEmpezarRecarga()
        {
            Bool(boolRecargando, true);
            Aviso(sonidoRecarga);
        }

        void AlTerminarRecarga() => Bool(boolRecargando, false);

        void Aviso(AudioClip clip)
        {
            if (clip == null || fuenteEfectos == null) return;
            fuenteEfectos.pitch = 1f;
            fuenteEfectos.PlayOneShot(clip, volumenAvisos);
        }

        // ------------------------------------------------------------------ Animación por frame
        void LateUpdate()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < piezasRetroceso.Length; i++)
            {
                var p = piezasRetroceso[i];
                if (p == null) continue;
                float t = tiempoDesdeDisparo[i];
                if (t == float.MaxValue) continue;
                t += dt;
                tiempoDesdeDisparo[i] = t;
                float k; // 0 = en su sitio, 1 = retroceso máximo
                if (t < tiempoGolpe) k = t / tiempoGolpe;
                else if (t < tiempoGolpe + tiempoRecuperacion) k = 1f - Mathf.SmoothStep(0f, 1f, (t - tiempoGolpe) / tiempoRecuperacion);
                else { k = 0f; tiempoDesdeDisparo[i] = float.MaxValue; }
                p.localPosition = posBaseRetroceso[i] + direccionRetroceso.normalized * (distanciaRetroceso * k);
            }

            for (int i = 0; i < fogonazos.Length; i++)
                if (fogonazos[i] != null && fogonazos[i].activeSelf && Time.time >= apagarFogonazo[i]) fogonazos[i].SetActive(false);

            if (luzFogonazo != null && apagarLuz > 0f)
            {
                float resto = apagarLuz - Time.time;
                luzFogonazo.intensity = resto > 0f ? intensidadLuz * (resto / duracionLuz) : 0f;
                if (resto <= 0f) apagarLuz = -1f;
            }

            if (fuenteMotor != null)
            {
                float objetivoVol = Mathf.Clamp01(torreta.VelocidadAngularActual / velocidadParaVolumenMaximo) * volumenMaximoMotor;
                fuenteMotor.volume = Mathf.MoveTowards(fuenteMotor.volume, objetivoVol, dt * 2f);
                fuenteMotor.pitch = 0.8f + 0.4f * Mathf.Clamp01(torreta.VelocidadAngularActual / velocidadParaVolumenMaximo);
            }

            Float(floatVelocidadGiro, torreta.VelocidadAngularActual);
        }

        // ------------------------------------------------------------------ Animator seguro
        void Trigger(string nombre)
        {
            if (animator == null || string.IsNullOrEmpty(nombre)) return;
            int h = Animator.StringToHash(nombre);
            if (parametrosAnimator.Contains(h)) animator.SetTrigger(h);
        }

        void Bool(string nombre, bool valor)
        {
            if (animator == null || string.IsNullOrEmpty(nombre)) return;
            int h = Animator.StringToHash(nombre);
            if (parametrosAnimator.Contains(h)) animator.SetBool(h, valor);
        }

        void Float(string nombre, float valor)
        {
            if (animator == null || string.IsNullOrEmpty(nombre)) return;
            int h = Animator.StringToHash(nombre);
            if (parametrosAnimator.Contains(h)) animator.SetFloat(h, valor);
        }
    }
}
