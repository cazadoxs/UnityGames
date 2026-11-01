using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.MiradaNPC
{
    /// <summary>¿Qué hace el NPC cuando el objetivo se sale de lo que su cuello puede girar?</summary>
    public enum ModoFueraDeLimite
    {
        /// <summary>Gira hasta el tope del cuello y se queda ahí mirando "de reojo" (los ojos compensan lo que puedan).</summary>
        LimitarAlBorde,
        /// <summary>Deja de mirar y vuelve la cabeza al frente.</summary>
        VolverAlFrente
    }

    /// <summary>Un hueso de la cadena cabeza-cuello-columna y cuánto del giro total aporta.</summary>
    [Serializable]
    public class HuesoMirada
    {
        [Tooltip("Hueso (Transform) que gira: columna, pecho, cuello o cabeza.")]
        public Transform hueso;

        [Tooltip("Parte del giro total que aporta este hueso. Se reparten proporcionalmente entre todos los huesos de la cadena.")]
        [Range(0f, 1f)] public float peso = 0.5f;

        public HuesoMirada() { }

        public HuesoMirada(Transform hueso, float peso)
        {
            this.hueso = hueso;
            this.peso = peso;
        }
    }

    /// <summary>Un ojo. La calibración guarda hacia dónde mira el ojo en reposo, así funciona con cualquier orientación de ejes del modelo.</summary>
    [Serializable]
    public class OjoMirada
    {
        [Tooltip("Hueso (Transform) del ojo.")]
        public Transform hueso;

        [HideInInspector] public bool calibrado;
        [HideInInspector] public Quaternion calibracion = Quaternion.identity;

        [NonSerialized] public float yaw, pitch, velocidadYaw, velocidadPitch;

        public OjoMirada() { }

        public OjoMirada(Transform hueso)
        {
            this.hueso = hueso;
        }
    }

    /// <summary>
    /// Hace que un NPC siga con la cabeza y los ojos al jugador (o a cualquier objetivo), respetando los límites de giro del cuello
    /// y de los ojos. Funciona con modelos Humanoid, Generic o con jerarquías hechas a mano, con o sin Animator:
    /// se aplica en LateUpdate, encima de la animación.
    /// </summary>
    [AddComponentMenu("Mecánicas/Mirada NPC/Seguimiento de Mirada NPC")]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public class SeguimientoMiradaNPC : MonoBehaviour
    {
        // ───────────────────────── Objetivo ─────────────────────────
        [Header("Objetivo")]
        [Tooltip("Objetivo fijo. Si está vacío y 'Buscar automáticamente' está activo, se busca solo.")]
        [SerializeField] Transform objetivo;

        [Tooltip("Si no hay objetivo fijo: busca un ObjetivoMirada en la escena y, si no hay, usa la cámara principal (el jugador en primera persona).")]
        public bool buscarAutomaticamente = true;

        [Tooltip("Cada cuántos segundos se vuelve a buscar el mejor objetivo (0 = cada frame).")]
        [Min(0f)] public float intervaloBusqueda = 0.25f;

        [Tooltip("Desplazamiento (en el espacio del objetivo) cuando el objetivo NO tiene componente ObjetivoMirada.")]
        public Vector3 desplazamientoObjetivo = Vector3.zero;

        // ───────────────────────── Detección ─────────────────────────
        [Header("Detección")]
        [Tooltip("Distancia máxima a la que el NPC se fija en el objetivo.")]
        [Min(0f)] public float distanciaMaxima = 8f;

        [Tooltip("Campo de visión horizontal total del NPC, en grados (medido desde el frente del cuerpo). 360 = ve en todas direcciones.")]
        [Range(1f, 360f)] public float campoVision = 220f;

        [Tooltip("Exige que no haya paredes entre los ojos del NPC y el objetivo.")]
        public bool requiereLineaDeVision = false;

        [Tooltip("Capas que bloquean la vista.")]
        public LayerMask capasQueBloquean = ~0;

        [Tooltip("Segundos que tarda en reaccionar desde que ve al objetivo.")]
        [Min(0f)] public float tiempoReaccion = 0.15f;

        [Tooltip("Segundos que sigue mirando después de perder al objetivo (memoria).")]
        [Min(0f)] public float tiempoMemoria = 0.75f;

        // ───────────────────────── Cabeza y cuello ─────────────────────────
        [Header("Cabeza y cuello")]
        [Tooltip("Marco de referencia del cuerpo: los límites se miden respecto a su frente (+Z) y su arriba (+Y). Normalmente la raíz del NPC.")]
        public Transform referenciaCuerpo;

        [Tooltip("Desde dónde mira el NPC (entre los ojos). Si está vacío se usa el punto medio de los ojos o, si no hay, el último hueso de la cadena.")]
        public Transform origenMirada;

        [Tooltip("Huesos que giran, de padre a hijo (columna → cuello → cabeza). El giro total se reparte según sus pesos.")]
        public List<HuesoMirada> cadena = new List<HuesoMirada>();

        [Tooltip("Lo máximo que puede girar el conjunto cuello + cabeza respecto al cuerpo.")]
        public LimitesMirada limitesCuello = LimitesMirada.CuelloHumano;

        [Tooltip("Qué hacer cuando el objetivo se sale de los límites del cuello.")]
        public ModoFueraDeLimite fueraDeLimite = ModoFueraDeLimite.LimitarAlBorde;

        [Tooltip("Grados extra de tolerancia antes de considerar que el objetivo está fuera de límite (evita que la cabeza 'parpadee' en el borde).")]
        [Min(0f)] public float margenLimite = 15f;

        [Tooltip("Tiempo (s) que tarda la cabeza en alcanzar la dirección deseada. Más alto = más perezoso.")]
        [Min(0.001f)] public float suavizadoCabeza = 0.25f;

        [Tooltip("Velocidad máxima de giro de la cabeza en grados/segundo.")]
        [Min(1f)] public float velocidadMaximaCabeza = 360f;

        // ───────────────────────── Ojos ─────────────────────────
        [Header("Ojos")]
        [Tooltip("Huesos de los ojos (opcional).")]
        public List<OjoMirada> ojos = new List<OjoMirada>();

        [Tooltip("Lo máximo que pueden girar los ojos respecto a la cabeza.")]
        public LimitesMirada limitesOjos = LimitesMirada.OjosHumanos;

        [Tooltip("Tiempo (s) que tardan los ojos en alcanzar el objetivo. Los ojos suelen ser mucho más rápidos que la cabeza.")]
        [Min(0.001f)] public float suavizadoOjos = 0.06f;

        [Tooltip("Velocidad máxima de giro de los ojos en grados/segundo.")]
        [Min(1f)] public float velocidadMaximaOjos = 900f;

        [Tooltip("Si está activo, los ojos siguen mirando al objetivo aunque la cabeza haya vuelto al frente por estar fuera de límite (mirada de reojo).")]
        public bool ojosSiguenFueraDeLimite = true;

        // ───────────────────────── Cuerpo ─────────────────────────
        [Header("Girar el cuerpo (opcional)")]
        [Tooltip("Si el objetivo se queda muy de lado, el NPC gira todo el cuerpo hacia él. Desactívalo si otro sistema (NavMeshAgent, IA) controla la rotación.")]
        public bool girarCuerpo = false;

        [Tooltip("Transform que se gira. Si está vacío se usa la referencia del cuerpo.")]
        public Transform cuerpoAGirar;

        [Tooltip("A partir de cuántos grados de giro de cabeza empieza a girar el cuerpo.")]
        [Range(0f, 179f)] public float umbralGiroCuerpo = 50f;

        [Tooltip("Velocidad de giro del cuerpo en grados/segundo.")]
        [Min(0f)] public float velocidadGiroCuerpo = 90f;

        // ───────────────────────── General ─────────────────────────
        [Header("General")]
        [Tooltip("Intensidad global del efecto (0 = no hace nada, 1 = completo). Puedes animarlo o cambiarlo por código.")]
        [Range(0f, 1f)] public float peso = 1f;

        [Tooltip("Dibuja en la vista de escena una línea hacia el objetivo mientras se juega.")]
        public bool depuracion = false;

        [Header("Eventos")]
        public UnityEvent alEmpezarAMirar = new UnityEvent();
        public UnityEvent alDejarDeMirar = new UnityEvent();

        // ───────────────────────── Estado interno ─────────────────────────
        struct EstadoHueso
        {
            public Transform t;
            public float pesoNormalizado;
            public Quaternion baseLocal;
            public Quaternion resultadoLocal;
            public bool tieneDatos;
        }

        EstadoHueso[] estadoCadena = Array.Empty<EstadoHueso>();
        Quaternion[] baseOjos = Array.Empty<Quaternion>();
        Quaternion[] resultadoOjos = Array.Empty<Quaternion>();
        bool[] datosOjos = Array.Empty<bool>();

        Transform objetivoActual;
        bool usarPuntoFijo;
        Vector3 puntoFijo;
        bool forzado;

        float yawCabeza, pitchCabeza, velYawCabeza, velPitchCabeza;
        float proximaBusqueda;
        float tiempoViendo, tiempoSinVer;
        bool mirando;
        bool cabezaFueraDeLimite;
        readonly RaycastHit[] impactos = new RaycastHit[8];

        /// <summary>¿Está mirando ahora mismo a un objetivo?</summary>
        public bool EstaMirando => mirando;

        /// <summary>Objetivo que se está usando ahora (puede ser null).</summary>
        public Transform ObjetivoActual => objetivoActual;

        /// <summary>Giro actual de la cabeza respecto al cuerpo (grados, + = derecha).</summary>
        public float GiroHorizontal => yawCabeza;

        /// <summary>Inclinación actual de la cabeza respecto al cuerpo (grados, + = arriba).</summary>
        public float GiroVertical => pitchCabeza;

        /// <summary>Objetivo fijo asignado en el inspector o por código.</summary>
        public Transform Objetivo
        {
            get => objetivo;
            set { objetivo = value; usarPuntoFijo = false; forzado = false; proximaBusqueda = 0f; }
        }

        // ───────────────────────── API pública ─────────────────────────

        /// <summary>Mira a un Transform. Con forzar = true ignora distancia, campo de visión y línea de visión (pero no los límites del cuello).</summary>
        public void MirarA(Transform nuevoObjetivo, bool forzar = false)
        {
            objetivo = nuevoObjetivo;
            usarPuntoFijo = false;
            forzado = forzar;
            proximaBusqueda = 0f;
        }

        /// <summary>Mira a un punto fijo del mundo (siempre forzado).</summary>
        public void MirarAPunto(Vector3 punto)
        {
            puntoFijo = punto;
            usarPuntoFijo = true;
            forzado = true;
        }

        /// <summary>Deja de mirar al objetivo asignado/punto y vuelve al comportamiento automático (si está activo).</summary>
        public void DejarDeMirar()
        {
            objetivo = null;
            usarPuntoFijo = false;
            forzado = false;
            proximaBusqueda = 0f;
        }

        /// <summary>Rellena la cadena y los ojos automáticamente (Humanoid o nombres típicos: Head, Neck, Spine, Eye...).</summary>
        [ContextMenu("Autoconfigurar huesos")]
        public void AutoConfigurar()
        {
            if (referenciaCuerpo == null) referenciaCuerpo = transform;
            cadena = new List<HuesoMirada>();
            ojos = new List<OjoMirada>();

            var animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman && animator.avatar != null)
            {
                AnadirSiExiste(animator.GetBoneTransform(HumanBodyBones.Spine), 0.1f);
                AnadirSiExiste(animator.GetBoneTransform(HumanBodyBones.Chest), 0.15f);
                AnadirSiExiste(animator.GetBoneTransform(HumanBodyBones.Neck), 0.3f);
                AnadirSiExiste(animator.GetBoneTransform(HumanBodyBones.Head), 0.45f);
                var ojoI = animator.GetBoneTransform(HumanBodyBones.LeftEye);
                var ojoD = animator.GetBoneTransform(HumanBodyBones.RightEye);
                if (ojoI != null) ojos.Add(new OjoMirada(ojoI));
                if (ojoD != null) ojos.Add(new OjoMirada(ojoD));
            }

            if (cadena.Count == 0)
            {
                // Rig genérico: buscar por nombre.
                Transform cabeza = BuscarPorNombre(transform, "head", "cabeza");
                Transform cuello = BuscarPorNombre(transform, "neck", "cuello");
                Transform columna = BuscarPorNombre(transform, "spine", "columna", "chest", "pecho");
                if (columna != null && cuello != null && columna != cuello && cuello.IsChildOf(columna)) AnadirSiExiste(columna, 0.15f);
                if (cuello != null && cuello != cabeza) AnadirSiExiste(cuello, 0.35f);
                AnadirSiExiste(cabeza, 0.5f);
            }

            if (ojos.Count == 0)
            {
                var candidatos = new List<Transform>();
                BuscarTodosPorNombre(transform, candidatos, "eye", "ojo");
                foreach (var c in candidatos)
                {
                    // Solo ojos "raíz" (no sus hijos, como pupilas o párpados que contengan la palabra).
                    bool hijoDeOtro = false;
                    foreach (var otro in candidatos)
                        if (otro != c && c.IsChildOf(otro)) { hijoDeOtro = true; break; }
                    if (!hijoDeOtro) ojos.Add(new OjoMirada(c));
                }
            }

            CalibrarOjos();
        }

        /// <summary>
        /// Guarda la orientación de reposo de los ojos. Llamar con el modelo en pose de reposo y los ojos mirando al frente
        /// del cuerpo (por ejemplo en el editor). Si no se calibra antes, se calibra solo al empezar.
        /// </summary>
        [ContextMenu("Calibrar ojos (pose de reposo)")]
        public void CalibrarOjos()
        {
            Transform referencia = referenciaCuerpo != null ? referenciaCuerpo : transform;
            foreach (var ojo in ojos)
            {
                if (ojo == null || ojo.hueso == null) continue;
                ojo.calibracion = Quaternion.Inverse(ojo.hueso.rotation) * referencia.rotation;
                ojo.calibrado = true;
            }
        }

        // ───────────────────────── Ciclo de vida ─────────────────────────

        void Reset()
        {
            referenciaCuerpo = transform;
            AutoConfigurar();
        }

        void Awake()
        {
            if (referenciaCuerpo == null) referenciaCuerpo = transform;
            if (cadena.Count == 0 && ojos.Count == 0) AutoConfigurar();

            foreach (var ojo in ojos)
            {
                if (ojo == null || ojo.hueso == null || ojo.calibrado) continue;
                ojo.calibracion = Quaternion.Inverse(ojo.hueso.rotation) * referenciaCuerpo.rotation;
                ojo.calibrado = true;
            }
            PrepararCadena();
        }

        void OnValidate()
        {
            if (Application.isPlaying) PrepararCadena();
        }

        void OnDisable()
        {
            // Si nadie anima los huesos, los dejamos como estaban para que no se queden torcidos.
            for (int i = 0; i < estadoCadena.Length; i++)
            {
                ref var e = ref estadoCadena[i];
                if (e.t != null && e.tieneDatos && e.t.localRotation == e.resultadoLocal) e.t.localRotation = e.baseLocal;
                e.tieneDatos = false;
            }
            for (int i = 0; i < ojos.Count && i < datosOjos.Length; i++)
            {
                var t = ojos[i]?.hueso;
                if (t != null && datosOjos[i] && t.localRotation == resultadoOjos[i]) t.localRotation = baseOjos[i];
                datosOjos[i] = false;
            }
            if (mirando)
            {
                mirando = false;
                alDejarDeMirar.Invoke();
            }
            yawCabeza = pitchCabeza = velYawCabeza = velPitchCabeza = 0f;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (estadoCadena.Length != ContarHuesosValidos() || baseOjos.Length != ojos.Count) PrepararCadena();

            RestaurarPoseBase();

            Transform referencia = referenciaCuerpo != null ? referenciaCuerpo : transform;
            Vector3 origen = CalcularOrigen();

            // 1. Elegir objetivo y decidir si lo "ve".
            bool hayPunto = ObtenerPuntoObjetivo(out Vector3 punto);
            bool visible = hayPunto && (forzado || EsVisible(referencia, origen, punto));
            ActualizarInteres(visible, dt);

            // 2. Ángulos deseados de la cabeza respecto al cuerpo.
            float yawDeseado = 0f, pitchDeseado = 0f;
            cabezaFueraDeLimite = false;
            if (mirando && hayPunto)
            {
                Vector3 local = Quaternion.Inverse(referencia.rotation) * (punto - origen);
                MiradaMatematica.DireccionAAngulos(local, out yawDeseado, out pitchDeseado);

                if (girarCuerpo) GirarCuerpo(referencia, yawDeseado, dt, ref yawDeseado);

                bool dentro = MiradaMatematica.DentroDeLimites(yawDeseado, pitchDeseado, limitesCuello, cabezaFueraDeLimitePrevio ? 0f : margenLimite);
                if (!dentro && fueraDeLimite == ModoFueraDeLimite.VolverAlFrente)
                {
                    cabezaFueraDeLimite = true;
                    yawDeseado = pitchDeseado = 0f;
                }
                else
                {
                    MiradaMatematica.Limitar(ref yawDeseado, ref pitchDeseado, limitesCuello);
                }
            }
            cabezaFueraDeLimitePrevio = cabezaFueraDeLimite;

            // 3. Suavizar.
            yawCabeza = Mathf.SmoothDampAngle(yawCabeza, yawDeseado, ref velYawCabeza, suavizadoCabeza, velocidadMaximaCabeza, dt);
            pitchCabeza = Mathf.SmoothDampAngle(pitchCabeza, pitchDeseado, ref velPitchCabeza, suavizadoCabeza, velocidadMaximaCabeza, dt);

            // 4. Aplicar a la cadena, repartiendo el giro.
            AplicarCadena(referencia);

            // 5. Ojos (después de la cabeza, para compensar lo que el cuello no llega).
            bool ojosMiran = mirando && hayPunto && (!cabezaFueraDeLimite || ojosSiguenFueraDeLimite);
            AplicarOjos(punto, ojosMiran, dt);

            if (depuracion && hayPunto)
                Debug.DrawLine(origen, punto, mirando ? (cabezaFueraDeLimite ? Color.yellow : Color.green) : Color.red);
        }

        bool cabezaFueraDeLimitePrevio;

        // ───────────────────────── Objetivo y detección ─────────────────────────

        bool ObtenerPuntoObjetivo(out Vector3 punto)
        {
            if (usarPuntoFijo)
            {
                objetivoActual = null;
                punto = puntoFijo;
                return true;
            }

            if (objetivo != null)
            {
                objetivoActual = objetivo;
            }
            else if (buscarAutomaticamente)
            {
                if (Time.time >= proximaBusqueda || objetivoActual == null || !objetivoActual.gameObject.activeInHierarchy)
                {
                    proximaBusqueda = Time.time + intervaloBusqueda;
                    objetivoActual = BuscarMejorObjetivo();
                }
            }
            else
            {
                objetivoActual = null;
            }

            if (objetivoActual == null)
            {
                punto = default;
                return false;
            }

            punto = PuntoDe(objetivoActual);
            return true;
        }

        Vector3 PuntoDe(Transform t)
        {
            var marca = t.GetComponent<ObjetivoMirada>();
            return marca != null ? marca.Punto : t.TransformPoint(desplazamientoObjetivo);
        }

        Transform BuscarMejorObjetivo()
        {
            Transform referencia = referenciaCuerpo != null ? referenciaCuerpo : transform;
            Vector3 origen = CalcularOrigen();
            Transform mejor = null;
            int mejorPrioridad = int.MinValue;
            float mejorDistancia = float.MaxValue;

            var lista = ObjetivoMirada.Activos;
            for (int i = 0; i < lista.Count; i++)
            {
                var o = lista[i];
                if (o == null || o.transform.IsChildOf(transform)) continue; // no mirarse a sí mismo
                Vector3 p = o.Punto;
                if (!EsVisible(referencia, origen, p, o.transform)) continue;
                float d = (p - origen).sqrMagnitude;
                if (o.prioridad > mejorPrioridad || (o.prioridad == mejorPrioridad && d < mejorDistancia))
                {
                    mejor = o.transform;
                    mejorPrioridad = o.prioridad;
                    mejorDistancia = d;
                }
            }

            if (mejor == null && lista.Count == 0)
            {
                var cam = Camera.main;
                if (cam != null && !cam.transform.IsChildOf(transform)) mejor = cam.transform;
            }
            return mejor;
        }

        bool EsVisible(Transform referencia, Vector3 origen, Vector3 punto, Transform objetivoParaVista = null)
        {
            Vector3 hacia = punto - origen;
            float distancia = hacia.magnitude;
            if (distancia > distanciaMaxima) return false;

            if (campoVision < 360f)
            {
                Vector3 local = Quaternion.Inverse(referencia.rotation) * hacia;
                float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                if (Mathf.Abs(yaw) > campoVision * 0.5f) return false;
            }

            if (requiereLineaDeVision && distancia > 0.01f)
            {
                Transform t = objetivoParaVista != null ? objetivoParaVista : objetivoActual;
                int n = Physics.RaycastNonAlloc(origen, hacia / distancia, impactos, distancia, capasQueBloquean, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    Transform golpe = impactos[i].transform;
                    if (golpe.IsChildOf(transform)) continue;                    // su propio cuerpo
                    if (t != null && (golpe.IsChildOf(t) || t.IsChildOf(golpe))) continue; // el propio objetivo o su cuerpo
                    if (impactos[i].distance >= distancia - 0.05f) continue;    // justo en el punto
                    return false;
                }
            }
            return true;
        }

        void ActualizarInteres(bool visible, float dt)
        {
            if (visible)
            {
                tiempoViendo += dt;
                tiempoSinVer = 0f;
                if (!mirando && tiempoViendo >= tiempoReaccion)
                {
                    mirando = true;
                    alEmpezarAMirar.Invoke();
                }
            }
            else
            {
                tiempoViendo = 0f;
                tiempoSinVer += dt;
                if (mirando && tiempoSinVer >= tiempoMemoria)
                {
                    mirando = false;
                    alDejarDeMirar.Invoke();
                }
            }
        }

        void GirarCuerpo(Transform referencia, float yaw, float dt, ref float yawDeseado)
        {
            if (Mathf.Abs(yaw) <= umbralGiroCuerpo) return;
            Transform cuerpo = cuerpoAGirar != null ? cuerpoAGirar : referencia;
            float exceso = yaw - Mathf.Sign(yaw) * umbralGiroCuerpo;
            float paso = Mathf.Clamp(exceso, -velocidadGiroCuerpo * dt, velocidadGiroCuerpo * dt);
            cuerpo.Rotate(referencia.up, paso, Space.World);
            yawDeseado -= paso;
        }

        // ───────────────────────── Aplicación a los huesos ─────────────────────────

        Vector3 CalcularOrigen()
        {
            if (origenMirada != null) return origenMirada.position;
            int n = 0;
            Vector3 suma = Vector3.zero;
            foreach (var ojo in ojos)
            {
                if (ojo?.hueso == null) continue;
                suma += ojo.hueso.position;
                n++;
            }
            if (n > 0) return suma / n;
            for (int i = cadena.Count - 1; i >= 0; i--)
                if (cadena[i]?.hueso != null) return cadena[i].hueso.position;
            return transform.position + Vector3.up * 1.6f;
        }

        int ContarHuesosValidos()
        {
            int n = 0;
            foreach (var h in cadena) if (h?.hueso != null) n++;
            return n;
        }

        void PrepararCadena()
        {
            var validos = new List<HuesoMirada>();
            float total = 0f;
            foreach (var h in cadena)
            {
                if (h?.hueso == null) continue;
                validos.Add(h);
                total += h.peso;
            }
            // De padre a hijo: así cada hueso hereda el giro de los anteriores y el total suma bien.
            validos.Sort((a, b) => Profundidad(a.hueso).CompareTo(Profundidad(b.hueso)));

            var anterior = estadoCadena;
            estadoCadena = new EstadoHueso[validos.Count];
            for (int i = 0; i < validos.Count; i++)
            {
                estadoCadena[i].t = validos[i].hueso;
                estadoCadena[i].pesoNormalizado = total > 0f ? validos[i].peso / total : 1f / validos.Count;
                foreach (var a in anterior)
                    if (a.t == validos[i].hueso) { estadoCadena[i].baseLocal = a.baseLocal; estadoCadena[i].resultadoLocal = a.resultadoLocal; estadoCadena[i].tieneDatos = a.tieneDatos; }
            }

            if (baseOjos.Length != ojos.Count)
            {
                baseOjos = new Quaternion[ojos.Count];
                resultadoOjos = new Quaternion[ojos.Count];
                datosOjos = new bool[ojos.Count];
            }
        }

        static int Profundidad(Transform t)
        {
            int d = 0;
            while (t.parent != null) { t = t.parent; d++; }
            return d;
        }

        /// <summary>
        /// Si un Animator ha escrito la pose este frame, partimos de ella. Si nadie la ha tocado (no hay animación),
        /// devolvemos el hueso a la pose sin nuestro giro, para no acumular rotaciones frame a frame.
        /// </summary>
        void RestaurarPoseBase()
        {
            for (int i = 0; i < estadoCadena.Length; i++)
            {
                ref var e = ref estadoCadena[i];
                if (e.tieneDatos && e.t.localRotation == e.resultadoLocal) e.t.localRotation = e.baseLocal;
                e.baseLocal = e.t.localRotation;
            }
            for (int i = 0; i < ojos.Count; i++)
            {
                var t = ojos[i]?.hueso;
                if (t == null) continue;
                if (datosOjos[i] && t.localRotation == resultadoOjos[i]) t.localRotation = baseOjos[i];
                baseOjos[i] = t.localRotation;
            }
        }

        void AplicarCadena(Transform referencia)
        {
            float y = yawCabeza * peso;
            float p = pitchCabeza * peso;
            Quaternion r = referencia.rotation;
            Quaternion rInv = Quaternion.Inverse(r);
            for (int i = 0; i < estadoCadena.Length; i++)
            {
                ref var e = ref estadoCadena[i];
                float w = e.pesoNormalizado;
                Quaternion delta = r * MiradaMatematica.AngulosARotacion(y * w, p * w) * rInv;
                e.t.rotation = delta * e.t.rotation;
                e.resultadoLocal = e.t.localRotation;
                e.tieneDatos = true;
            }
        }

        void AplicarOjos(Vector3 punto, bool miran, float dt)
        {
            for (int i = 0; i < ojos.Count; i++)
            {
                var ojo = ojos[i];
                if (ojo?.hueso == null) continue;
                Transform t = ojo.hueso;
                Quaternion marco = t.rotation * ojo.calibracion; // hacia dónde mira el ojo "en reposo" ahora mismo

                float yawDeseado = 0f, pitchDeseado = 0f;
                if (miran)
                {
                    Vector3 local = Quaternion.Inverse(marco) * (punto - t.position);
                    MiradaMatematica.DireccionAAngulos(local, out yawDeseado, out pitchDeseado);
                    MiradaMatematica.Limitar(ref yawDeseado, ref pitchDeseado, limitesOjos);
                }

                ojo.yaw = Mathf.SmoothDampAngle(ojo.yaw, yawDeseado, ref ojo.velocidadYaw, suavizadoOjos, velocidadMaximaOjos, dt);
                ojo.pitch = Mathf.SmoothDampAngle(ojo.pitch, pitchDeseado, ref ojo.velocidadPitch, suavizadoOjos, velocidadMaximaOjos, dt);

                Quaternion delta = marco * MiradaMatematica.AngulosARotacion(ojo.yaw * peso, ojo.pitch * peso) * Quaternion.Inverse(marco);
                t.rotation = delta * t.rotation;
                resultadoOjos[i] = t.localRotation;
                datosOjos[i] = true;
            }
        }

        // ───────────────────────── Utilidades ─────────────────────────

        void AnadirSiExiste(Transform t, float w)
        {
            if (t == null) return;
            foreach (var h in cadena) if (h.hueso == t) return;
            cadena.Add(new HuesoMirada(t, w));
        }

        static Transform BuscarPorNombre(Transform raiz, params string[] palabras)
        {
            // Primero el más cercano a la raíz que coincida (búsqueda en anchura).
            var cola = new Queue<Transform>();
            cola.Enqueue(raiz);
            while (cola.Count > 0)
            {
                var t = cola.Dequeue();
                if (t != raiz && Coincide(t.name, palabras)) return t;
                for (int i = 0; i < t.childCount; i++) cola.Enqueue(t.GetChild(i));
            }
            return null;
        }

        static void BuscarTodosPorNombre(Transform t, List<Transform> resultado, params string[] palabras)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var hijo = t.GetChild(i);
                if (Coincide(hijo.name, palabras)) resultado.Add(hijo);
                BuscarTodosPorNombre(hijo, resultado, palabras);
            }
        }

        static bool Coincide(string nombre, string[] palabras)
        {
            string n = nombre.ToLowerInvariant();
            foreach (var p in palabras)
                if (n.Contains(p)) return true;
            return false;
        }

        void OnDrawGizmosSelected()
        {
            Transform referencia = referenciaCuerpo != null ? referenciaCuerpo : transform;
            Vector3 origen = CalcularOrigen();
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(origen, distanciaMaxima);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(origen, origen + referencia.forward * 0.6f);
            if (Application.isPlaying && objetivoActual != null)
            {
                Gizmos.color = mirando ? Color.green : Color.red;
                Gizmos.DrawLine(origen, PuntoDe(objetivoActual));
            }
        }
    }
}
