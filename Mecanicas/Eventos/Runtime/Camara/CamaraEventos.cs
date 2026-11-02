using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>Qué hace la cámara cuando termina de mirar.</summary>
    public enum FinMirada
    {
        [Tooltip("Vuelve suavemente a donde miraba antes.")] Volver,
        [Tooltip("Vuelve de golpe a donde miraba antes.")] VolverDeGolpe,
        [Tooltip("Se queda mirando hacia allí (necesita un controlador con IControlCamaraJugador).")] Mantener
    }

    /// <summary>Datos de un movimiento de "mirar hacia".</summary>
    [Serializable]
    public class ParametrosMirada
    {
        [Tooltip("Objeto al que mirar. Si está vacío se usa 'Direccion'.")]
        public Transform objetivo;

        [Tooltip("Desplazamiento sobre el objetivo (por ejemplo, 1.6 en Y para mirar a la cara).")]
        public Vector3 desplazamiento = Vector3.zero;

        [Tooltip("Dirección en el mundo hacia la que mirar si no hay objetivo.")]
        public Vector3 direccion = Vector3.forward;

        [Tooltip("Si el objetivo se mueve, la cámara lo sigue mientras mira.")]
        public bool seguirObjetivo = true;

        [Tooltip("0 = de golpe. Más de 0 = segundos que tarda en girar (con recorrido).")]
        [Min(0f)] public float duracionIda = 0.8f;

        [Tooltip("Forma del giro de ida (y de vuelta).")]
        public AnimationCurve curva = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Segundos mirando antes de terminar. -1 = hasta que algo llame a CancelarMirada().")]
        public float tiempoMirando = 1.5f;

        public FinMirada alTerminar = FinMirada.Volver;

        [Tooltip("Segundos que tarda en volver (solo con Volver).")]
        [Min(0f)] public float duracionVuelta = 0.6f;

        [Tooltip("Bloquea el control del jugador mientras dura (ver BloqueoControlJugador).")]
        public bool bloquearControl = true;

        [Tooltip("Componentes que se desactivan mientras dura la mirada (por ejemplo, el controlador de tu jugador si no usa BloqueoControlJugador).")]
        public Behaviour[] desactivarDurante = new Behaviour[0];
    }

    /// <summary>Datos de un temblor de cámara.</summary>
    [Serializable]
    public class ParametrosTemblor
    {
        [Tooltip("Multiplicador general de la fuerza del temblor.")]
        [Min(0f)] public float intensidad = 1f;

        [Min(0.01f)] public float duracion = 0.5f;

        [Tooltip("Rapidez de la vibración. 8-12 = terremoto lento, 20-30 = explosión o golpe.")]
        [Min(0.1f)] public float frecuencia = 22f;

        [Tooltip("Desplazamiento máximo en metros en cada eje local.")]
        public Vector3 amplitudPosicion = new Vector3(0.08f, 0.08f, 0.02f);

        [Tooltip("Giro máximo en grados (X = cabeceo, Y = guiñada, Z = alabeo).")]
        public Vector3 amplitudRotacion = new Vector3(2f, 2f, 1.5f);

        [Tooltip("Fuerza a lo largo del tiempo (0 = inicio, 1 = final).")]
        public AnimationCurve atenuacion = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
    }

    /// <summary>
    /// Va en la cámara del jugador y aplica, encima de lo que haga tu controlador, los efectos de los eventos:
    /// mirar hacia un punto y temblores. No necesitas añadirlo a mano: las acciones lo añaden a la cámara principal.
    /// Funciona tanto si tu controlador gira la propia cámara cada frame como si no la toca.
    /// </summary>
    [AddComponentMenu("Mecánicas/Eventos/Cámara de eventos")]
    [DefaultExecutionOrder(10000)] // después de los controladores de cámara habituales
    [DisallowMultipleComponent]
    public class CamaraEventos : MonoBehaviour
    {
        [Tooltip("Multiplicador global de todos los temblores (para una opción de accesibilidad en tu menú, por ejemplo).")]
        [Range(0f, 2f)] [SerializeField] private float escalaTemblores = 1f;

        private class Temblor
        {
            public ParametrosTemblor p;
            public float inicio;
            public float factor;
            public float semilla;
        }

        private readonly List<Temblor> temblores = new List<Temblor>();

        // Mirada
        private float peso;
        private ParametrosMirada mirada;
        private Quaternion miradaMundoFija;
        private int tokenActual;
        private Coroutine rutinaMirada;
        private readonly object llaveBloqueo = new object();
        private readonly List<Behaviour> desactivados = new List<Behaviour>();

        // Base sobre la que se suman los efectos
        private bool escribiendo;
        private Quaternion baseRot;
        private Vector3 basePos;
        private Quaternion escritaRot;
        private Vector3 escritaPos;

        /// <summary>Multiplicador global de temblores (0 los quita).</summary>
        public float EscalaTemblores { get => escalaTemblores; set => escalaTemblores = Mathf.Max(0f, value); }

        /// <summary>True mientras la cámara está mirando hacia algo por un evento.</summary>
        public bool Mirando => rutinaMirada != null;

        // ---------------------------------------------------------------- Acceso

        /// <summary>
        /// Devuelve el CamaraEventos de la cámara indicada (o de la principal), añadiéndolo si no lo tiene.
        /// </summary>
        public static CamaraEventos Obtener(Camera camara = null)
        {
            if (camara == null) camara = Camera.main;
            if (camara == null)
            {
                Debug.LogWarning("[Eventos] No hay cámara principal (etiqueta MainCamera) para los efectos de cámara.");
                return null;
            }

            CamaraEventos ce = camara.GetComponent<CamaraEventos>();
            return ce != null ? ce : camara.gameObject.AddComponent<CamaraEventos>();
        }

        /// <summary>Atajo para lanzar un temblor en la cámara principal desde cualquier script.</summary>
        public static void TemblarPrincipal(float intensidad = 1f, float duracion = 0.5f)
        {
            CamaraEventos ce = Obtener();
            if (ce == null) return;
            ce.Temblar(new ParametrosTemblor { intensidad = intensidad, duracion = duracion });
        }

        // ---------------------------------------------------------------- Temblor

        /// <summary>Añade un temblor. Varios temblores a la vez se suman.</summary>
        /// <param name="factor">Multiplicador extra (por ejemplo, por distancia a la explosión).</param>
        public void Temblar(ParametrosTemblor parametros, float factor = 1f)
        {
            if (parametros == null || factor <= 0f) return;
            temblores.Add(new Temblor
            {
                p = parametros,
                inicio = Time.time,
                factor = factor,
                semilla = UnityEngine.Random.Range(0f, 1000f)
            });
        }

        /// <summary>Corta todos los temblores.</summary>
        public void PararTemblores() => temblores.Clear();

        // ---------------------------------------------------------------- Mirada

        /// <summary>
        /// Empieza a mirar hacia un objetivo o dirección. Si ya había otra mirada, la sustituye.
        /// Devuelve un número para consultar con <see cref="MiradaActiva"/>.
        /// </summary>
        public int Mirar(ParametrosMirada parametros)
        {
            if (parametros == null) return 0;
            CancelarMirada(false);

            tokenActual++;
            mirada = parametros;
            miradaMundoFija = RotacionHacia(parametros);
            rutinaMirada = StartCoroutine(RutinaMirada(tokenActual));
            return tokenActual;
        }

        /// <summary>True si la mirada con ese número sigue en curso.</summary>
        public bool MiradaActiva(int token) => token != 0 && token == tokenActual && rutinaMirada != null;

        /// <summary>Termina la mirada actual. Con volverDeGolpe la cámara vuelve al momento; si no, se queda donde esté.</summary>
        public void CancelarMirada(bool volverDeGolpe = true)
        {
            if (rutinaMirada != null)
            {
                StopCoroutine(rutinaMirada);
                rutinaMirada = null;
            }

            if (volverDeGolpe) peso = 0f;
            else if (peso > 0f) FijarMiradaComoBase();
            LiberarControl();
        }

        /// <summary>Termina la espera de una mirada con tiempoMirando = -1 y ejecuta su final normal.</summary>
        public void TerminarEspera() => esperaTerminada = true;

        private bool esperaTerminada;

        private IEnumerator RutinaMirada(int token)
        {
            ParametrosMirada p = mirada;
            BloquearControl(p);
            esperaTerminada = false;

            // Ida
            if (p.duracionIda <= 0f) peso = 1f;
            else
            {
                for (float t = 0f; t < p.duracionIda; t += Time.deltaTime)
                {
                    peso = Evaluar(p.curva, t / p.duracionIda);
                    yield return null;
                }
                peso = 1f;
            }

            // Espera
            if (p.tiempoMirando < 0f)
            {
                while (!esperaTerminada) yield return null;
            }
            else if (p.tiempoMirando > 0f)
            {
                for (float t = 0f; t < p.tiempoMirando && !esperaTerminada; t += Time.deltaTime)
                    yield return null;
            }

            // Final
            switch (p.alTerminar)
            {
                case FinMirada.Volver:
                    if (p.duracionVuelta > 0f)
                    {
                        for (float t = 0f; t < p.duracionVuelta; t += Time.deltaTime)
                        {
                            peso = 1f - Evaluar(p.curva, t / p.duracionVuelta);
                            yield return null;
                        }
                    }
                    peso = 0f;
                    break;

                case FinMirada.VolverDeGolpe:
                    peso = 0f;
                    break;

                case FinMirada.Mantener:
                    FijarMiradaComoBase();
                    break;
            }

            LiberarControl();
            if (token == tokenActual) rutinaMirada = null;
        }

        private void FijarMiradaComoBase()
        {
            // La rotación que se ve ahora pasa a ser la "normal": se la damos al controlador si sabe recibirla.
            if (!escribiendo)
            {
                baseRot = transform.localRotation;
                basePos = transform.localPosition;
                escritaPos = basePos;
                escribiendo = true;
            }
            Quaternion local = Quaternion.Slerp(baseRot, LocalDesdeMundo(RotacionMiradaMundo()), peso);
            Quaternion mundo = transform.parent != null ? transform.parent.rotation * local : local;

            baseRot = local;
            peso = 0f;
            transform.localRotation = local;
            escritaRot = local;

            IControlCamaraJugador control = GetComponentInParent<IControlCamaraJugador>();
            control?.AplicarRotacionMundo(mundo);
        }

        private void BloquearControl(ParametrosMirada p)
        {
            if (p.bloquearControl) BloqueoControlJugador.Bloquear(llaveBloqueo);
            desactivados.Clear();
            if (p.desactivarDurante == null) return;
            foreach (Behaviour b in p.desactivarDurante)
            {
                if (b == null || !b.enabled) continue;
                b.enabled = false;
                desactivados.Add(b);
            }
        }

        private void LiberarControl()
        {
            BloqueoControlJugador.Liberar(llaveBloqueo);
            foreach (Behaviour b in desactivados)
                if (b != null) b.enabled = true;
            desactivados.Clear();
        }

        private Quaternion RotacionMiradaMundo()
        {
            if (mirada == null) return transform.rotation;
            if (mirada.seguirObjetivo && mirada.objetivo != null) return RotacionHacia(mirada);
            return miradaMundoFija;
        }

        private Quaternion RotacionHacia(ParametrosMirada p)
        {
            Vector3 dir = p.objetivo != null
                ? p.objetivo.position + p.desplazamiento - transform.position
                : p.direccion;
            if (dir.sqrMagnitude < 1e-6f) return transform.rotation;
            dir.Normalize();
            // Evita el giro raro al mirar justo arriba o abajo.
            Vector3 arriba = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.999f ? transform.forward : Vector3.up;
            return Quaternion.LookRotation(dir, arriba);
        }

        private Quaternion LocalDesdeMundo(Quaternion mundo)
        {
            return transform.parent != null ? Quaternion.Inverse(transform.parent.rotation) * mundo : mundo;
        }

        private static float Evaluar(AnimationCurve c, float t)
        {
            t = Mathf.Clamp01(t);
            return c != null && c.length > 0 ? c.Evaluate(t) : t;
        }

        // ---------------------------------------------------------------- Aplicación

        private void LateUpdate()
        {
            bool activo = peso > 0f || temblores.Count > 0;

            if (!activo)
            {
                if (escribiendo)
                {
                    // Si nadie más tocó la cámara, la dejamos como estaba antes de los efectos.
                    if (transform.localRotation == escritaRot) transform.localRotation = baseRot;
                    if (transform.localPosition == escritaPos) transform.localPosition = basePos;
                    escribiendo = false;
                }
                return;
            }

            if (!escribiendo)
            {
                baseRot = transform.localRotation;
                basePos = transform.localPosition;
                escribiendo = true;
            }
            else
            {
                // Si otro script (tu controlador) ha escrito este frame, eso es la nueva base.
                if (transform.localRotation != escritaRot) baseRot = transform.localRotation;
                if (transform.localPosition != escritaPos) basePos = transform.localPosition;
            }

            Quaternion rot = baseRot;
            if (peso > 0f)
                rot = Quaternion.Slerp(baseRot, LocalDesdeMundo(RotacionMiradaMundo()), peso);

            CalcularTemblor(out Vector3 offPos, out Vector3 offRot);

            escritaRot = rot * Quaternion.Euler(offRot);
            escritaPos = basePos + offPos;
            transform.localRotation = escritaRot;
            transform.localPosition = escritaPos;
        }

        private void CalcularTemblor(out Vector3 pos, out Vector3 rot)
        {
            pos = Vector3.zero;
            rot = Vector3.zero;

            for (int i = temblores.Count - 1; i >= 0; i--)
            {
                Temblor t = temblores[i];
                float u = (Time.time - t.inicio) / t.p.duracion;
                if (u >= 1f)
                {
                    temblores.RemoveAt(i);
                    continue;
                }

                float fuerza = t.p.intensidad * t.factor * escalaTemblores * Evaluar(t.p.atenuacion, u);
                float x = (Time.time - t.inicio) * t.p.frecuencia;
                pos += Vector3.Scale(t.p.amplitudPosicion, Ruido(t.semilla, x)) * fuerza;
                rot += Vector3.Scale(t.p.amplitudRotacion, Ruido(t.semilla + 50f, x)) * fuerza;
            }
        }

        private static Vector3 Ruido(float semilla, float x)
        {
            return new Vector3(
                Mathf.PerlinNoise(semilla, x) * 2f - 1f,
                Mathf.PerlinNoise(semilla + 17f, x) * 2f - 1f,
                Mathf.PerlinNoise(semilla + 31f, x) * 2f - 1f);
        }

        private void OnDisable()
        {
            CancelarMirada(true);
            temblores.Clear();
            if (escribiendo)
            {
                transform.localRotation = baseRot;
                transform.localPosition = basePos;
                escribiendo = false;
            }
        }
    }
}
