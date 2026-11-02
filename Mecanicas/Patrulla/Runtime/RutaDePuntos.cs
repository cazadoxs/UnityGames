using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Patrulla
{
    /// <summary>Forma de una esquina de la ruta.</summary>
    public enum TipoEsquina
    {
        /// <summary>Giro seco en el punto exacto (esquina en pico).</summary>
        Pico,
        /// <summary>Giro redondeado con una curva que empieza antes del punto y termina después.</summary>
        Suave
    }

    /// <summary>
    /// Ruta hecha de puntos (Transforms). Calcula ("hornea") una línea con las esquinas en pico
    /// o redondeadas y permite preguntar la posición y la dirección a cualquier distancia del recorrido.
    /// Los puntos son, por defecto, los hijos directos de este objeto, en orden de la jerarquía.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Mecanicas/Patrulla/Ruta De Puntos")]
    public class RutaDePuntos : MonoBehaviour
    {
        [Header("Forma")]
        [Tooltip("Si está activo, el último punto se une con el primero y la ruta forma un circuito.")]
        public bool cerrada = true;

        [Tooltip("Esquina que usan los puntos que no tienen una propia (componente PuntoDeRuta).")]
        public TipoEsquina esquinaPorDefecto = TipoEsquina.Suave;

        [Tooltip("Distancia (metros) antes y después del punto en la que empieza y acaba la curva. " +
                 "Más grande = curva más abierta y natural. Se recorta solo si el tramo es corto.")]
        [Min(0f)] public float radioPorDefecto = 3f;

        [Tooltip("Número de segmentos con los que se dibuja cada curva. 8-16 suele bastar.")]
        [Range(2, 64)] public int resolucionCurva = 12;

        [Header("Puntos")]
        [Tooltip("Si está activo, los puntos son los hijos directos de este objeto (en orden). " +
                 "Si no, se usa la lista de abajo, que puede apuntar a cualquier Transform de la escena.")]
        public bool usarHijosComoPuntos = true;

        [Tooltip("Puntos de la ruta (solo se usa si 'Usar Hijos Como Puntos' está desactivado).")]
        public List<Transform> puntos = new List<Transform>();

        [Header("Tiempo real")]
        [Tooltip("Vuelve a calcular la ruta durante el juego si algún punto se mueve (p. ej. puntos animados). " +
                 "En el editor siempre se recalcula.")]
        public bool rutaDinamica = false;

        [Header("Gizmos")]
        public Color colorRuta = new Color(0.1f, 0.8f, 1f, 1f);
        public bool mostrarSiempre = true;

        // ---- Datos horneados ----
        readonly List<Transform> puntosActuales = new List<Transform>();
        readonly List<Vector3> muestras = new List<Vector3>();
        readonly List<float> distancias = new List<float>();
        float[] distanciaDePunto = new float[0];
        readonly List<Vector3> firmaPosiciones = new List<Vector3>();
        int firmaAjustes;
        bool sucia = true;
        float longitud;

        /// <summary>Se lanza cada vez que la ruta se vuelve a calcular.</summary>
        public event System.Action AlReconstruir;

        // ---------------------------------------------------------------- API pública

        /// <summary>Longitud total del recorrido horneado, en metros.</summary>
        public float Longitud { get { AsegurarConstruida(); return longitud; } }

        /// <summary>Número de puntos válidos de la ruta.</summary>
        public int NumeroDePuntos { get { AsegurarConstruida(); return puntosActuales.Count; } }

        /// <summary>Muestras de la línea horneada (posición en mundo). Solo lectura.</summary>
        public IReadOnlyList<Vector3> Muestras { get { AsegurarConstruida(); return muestras; } }

        /// <summary>True si la ruta tiene al menos dos puntos y se puede recorrer.</summary>
        public bool EsValida { get { AsegurarConstruida(); return puntosActuales.Count >= 2 && longitud > 0.0001f; } }

        /// <summary>Transform del punto i.</summary>
        public Transform ObtenerPunto(int i) { AsegurarConstruida(); return puntosActuales[i]; }

        /// <summary>Componente PuntoDeRuta del punto i (o null si no lo tiene).</summary>
        public PuntoDeRuta ObtenerAjustes(int i)
        {
            AsegurarConstruida();
            var t = puntosActuales[i];
            return t != null ? t.GetComponent<PuntoDeRuta>() : null;
        }

        /// <summary>Distancia a lo largo de la ruta a la que se considera que se "pasa" por el punto i
        /// (el punto exacto en esquinas en pico, el centro de la curva en esquinas suaves).</summary>
        public float DistanciaDePunto(int i) { AsegurarConstruida(); return distanciaDePunto[i]; }

        /// <summary>Marca la ruta para recalcularla en el próximo uso.</summary>
        public void MarcarSucia() { sucia = true; }

        /// <summary>Recalcula la ruta ahora.</summary>
        public void Reconstruir()
        {
            sucia = false; // antes de hornear: el horneado consulta la propia ruta
            RecogerPuntos();
            Hornear();
            GuardarFirma();
            AlReconstruir?.Invoke();
        }

        /// <summary>Ajusta una distancia al rango de la ruta (da la vuelta si es cerrada, recorta si es abierta).</summary>
        public float NormalizarDistancia(float d)
        {
            float L = Longitud;
            if (L <= 0f) return 0f;
            return cerrada ? Mathf.Repeat(d, L) : Mathf.Clamp(d, 0f, L);
        }

        /// <summary>Posición en mundo a una distancia del inicio de la ruta.</summary>
        public Vector3 PosicionEn(float distancia)
        {
            Evaluar(distancia, out Vector3 p, out _);
            return p;
        }

        /// <summary>Dirección de avance (normalizada) a una distancia del inicio de la ruta.</summary>
        public Vector3 DireccionEn(float distancia)
        {
            Evaluar(distancia, out _, out Vector3 dir);
            return dir;
        }

        /// <summary>Posición y dirección de avance a una distancia del inicio de la ruta.</summary>
        public void Evaluar(float distancia, out Vector3 posicion, out Vector3 direccion)
        {
            AsegurarConstruida();
            int n = muestras.Count;
            if (n == 0) { posicion = transform.position; direccion = transform.forward; return; }
            if (n == 1 || longitud <= 0f) { posicion = muestras[0]; direccion = transform.forward; return; }

            float d = NormalizarDistancia(distancia);
            int i = BuscarSegmento(d);
            float d0 = distancias[i], d1 = distancias[i + 1];
            float t = d1 > d0 ? (d - d0) / (d1 - d0) : 0f;
            posicion = Vector3.LerpUnclamped(muestras[i], muestras[i + 1], t);
            Vector3 seg = muestras[i + 1] - muestras[i];
            direccion = seg.sqrMagnitude > 1e-10f ? seg.normalized : transform.forward;
        }

        /// <summary>Distancia a lo largo de la ruta del punto horneado más cercano a una posición en mundo.</summary>
        public float DistanciaMasCercana(Vector3 posicionMundo)
        {
            AsegurarConstruida();
            float mejor = float.MaxValue, resultado = 0f;
            for (int i = 0; i + 1 < muestras.Count; i++)
            {
                Vector3 a = muestras[i], b = muestras[i + 1];
                Vector3 ab = b - a;
                float t = ab.sqrMagnitude > 1e-10f ? Mathf.Clamp01(Vector3.Dot(posicionMundo - a, ab) / ab.sqrMagnitude) : 0f;
                float dist = (a + ab * t - posicionMundo).sqrMagnitude;
                if (dist < mejor) { mejor = dist; resultado = Mathf.Lerp(distancias[i], distancias[i + 1], t); }
            }
            return resultado;
        }

        /// <summary>Índice del punto de la ruta más cercano a una posición en mundo.</summary>
        public int PuntoMasCercano(Vector3 posicionMundo)
        {
            AsegurarConstruida();
            int mejor = 0; float mejorD = float.MaxValue;
            for (int i = 0; i < puntosActuales.Count; i++)
            {
                float d = (puntosActuales[i].position - posicionMundo).sqrMagnitude;
                if (d < mejorD) { mejorD = d; mejor = i; }
            }
            return mejor;
        }

        /// <summary>Esquina efectiva del punto i (la suya propia o la de la ruta).</summary>
        public TipoEsquina EsquinaDe(int i)
        {
            var a = ObtenerAjustes(i);
            return a != null && a.usarEsquinaPropia ? a.esquina : esquinaPorDefecto;
        }

        /// <summary>Radio de curva efectivo del punto i (el suyo propio o el de la ruta).</summary>
        public float RadioDe(int i)
        {
            var a = ObtenerAjustes(i);
            return a != null && a.usarEsquinaPropia ? a.radio : radioPorDefecto;
        }

        // ---------------------------------------------------------------- Ciclo de vida

        void OnEnable() { sucia = true; }
        void OnValidate() { sucia = true; }
        void OnTransformChildrenChanged() { sucia = true; }

        void Update()
        {
            if (!Application.isPlaying || rutaDinamica)
            {
                if (HaCambiado()) sucia = true;
            }
        }

        void AsegurarConstruida()
        {
            if (sucia) Reconstruir();
        }

        // ---------------------------------------------------------------- Horneado

        void RecogerPuntos()
        {
            puntosActuales.Clear();
            if (usarHijosComoPuntos)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var hijo = transform.GetChild(i);
                    if (hijo.gameObject.activeSelf) puntosActuales.Add(hijo);
                }
            }
            else
            {
                foreach (var p in puntos) if (p != null) puntosActuales.Add(p);
            }
        }

        void Hornear()
        {
            muestras.Clear();
            distancias.Clear();
            longitud = 0f;
            int n = puntosActuales.Count;
            distanciaDePunto = new float[n];
            if (n == 0) return;

            var P = new Vector3[n];
            for (int i = 0; i < n; i++) P[i] = puntosActuales[i].position;

            if (n == 1) { Anadir(P[0]); return; }

            bool circuito = cerrada && n >= 3;

            // Entrada y salida de cada esquina.
            var entrada = new Vector3[n];
            var salida = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                entrada[i] = salida[i] = P[i];
                bool tieneEsquina = circuito || (i > 0 && i < n - 1);
                if (!tieneEsquina || EsquinaDe(i) == TipoEsquina.Pico) continue;

                Vector3 anterior = P[(i - 1 + n) % n];
                Vector3 siguiente = P[(i + 1) % n];
                Vector3 haciaAtras = anterior - P[i];
                Vector3 haciaDelante = siguiente - P[i];
                float la = haciaAtras.magnitude, ld = haciaDelante.magnitude;
                if (la < 1e-4f || ld < 1e-4f) continue;
                // Mitad de cada tramo como máximo, para que dos curvas seguidas no se pisen.
                float r = Mathf.Min(RadioDe(i), la * 0.5f, ld * 0.5f);
                if (r < 1e-4f) continue;
                // Si el tramo es recto (sin giro), no hace falta curva.
                if (Vector3.Angle(-haciaAtras, haciaDelante) < 0.5f) continue;
                entrada[i] = P[i] + haciaAtras / la * r;
                salida[i] = P[i] + haciaDelante / ld * r;
            }

            if (circuito)
            {
                // Empieza a la salida del punto 0 y acaba recorriendo su curva.
                Anadir(salida[0]);
                for (int k = 1; k <= n; k++)
                {
                    int i = k % n;
                    Esquina(entrada[i], P[i], salida[i], i);
                }
            }
            else
            {
                Anadir(P[0]);
                distanciaDePunto[0] = 0f;
                for (int i = 1; i < n - 1; i++) Esquina(entrada[i], P[i], salida[i], i);
                Anadir(P[n - 1]);
                distanciaDePunto[n - 1] = longitud;
            }
        }

        void Esquina(Vector3 entrada, Vector3 punto, Vector3 salida, int indice)
        {
            Anadir(entrada);
            float dEntrada = longitud;
            if ((salida - entrada).sqrMagnitude > 1e-8f)
            {
                // Curva de Bézier cuadrática: tangente a ambos tramos, sin saltos de dirección.
                for (int j = 1; j <= resolucionCurva; j++)
                {
                    float t = j / (float)resolucionCurva;
                    float u = 1f - t;
                    Anadir(u * u * entrada + 2f * u * t * punto + t * t * salida);
                }
            }
            distanciaDePunto[indice] = (dEntrada + longitud) * 0.5f;
        }

        void Anadir(Vector3 p)
        {
            if (muestras.Count > 0)
            {
                float paso = Vector3.Distance(muestras[muestras.Count - 1], p);
                if (paso < 1e-5f) return; // evita muestras repetidas
                longitud += paso;
            }
            muestras.Add(p);
            distancias.Add(longitud);
        }

        int BuscarSegmento(float d)
        {
            int lo = 0, hi = distancias.Count - 2;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (distancias[mid] <= d) lo = mid; else hi = mid - 1;
            }
            return lo;
        }

        // ---------------------------------------------------------------- Detección de cambios

        int CalcularFirmaAjustes()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (cerrada ? 1 : 0);
                h = h * 31 + (int)esquinaPorDefecto;
                h = h * 31 + radioPorDefecto.GetHashCode();
                h = h * 31 + resolucionCurva;
                foreach (var t in puntosActuales)
                {
                    var a = t != null ? t.GetComponent<PuntoDeRuta>() : null;
                    if (a == null) { h = h * 31; continue; }
                    h = h * 31 + (a.usarEsquinaPropia ? 1 : 2);
                    h = h * 31 + (int)a.esquina;
                    h = h * 31 + a.radio.GetHashCode();
                }
                return h;
            }
        }

        void GuardarFirma()
        {
            firmaPosiciones.Clear();
            foreach (var t in puntosActuales) firmaPosiciones.Add(t.position);
            firmaAjustes = CalcularFirmaAjustes();
        }

        bool HaCambiado()
        {
            int cuenta = 0;
            if (usarHijosComoPuntos)
            {
                for (int i = 0; i < transform.childCount; i++) if (transform.GetChild(i).gameObject.activeSelf) cuenta++;
            }
            else
            {
                foreach (var p in puntos) if (p != null) cuenta++;
            }
            if (cuenta != puntosActuales.Count) return true;
            for (int i = 0; i < puntosActuales.Count; i++)
            {
                if (puntosActuales[i] == null) return true;
                if (puntosActuales[i].position != firmaPosiciones[i]) return true;
            }
            return CalcularFirmaAjustes() != firmaAjustes;
        }

        // ---------------------------------------------------------------- Gizmos

        void OnDrawGizmos() { if (mostrarSiempre) DibujarGizmos(false); }
        void OnDrawGizmosSelected() { DibujarGizmos(true); }

        void DibujarGizmos(bool seleccionada)
        {
            AsegurarConstruida();
            Color c = colorRuta;
            if (!seleccionada) c.a *= 0.6f;
            Gizmos.color = c;
            for (int i = 0; i + 1 < muestras.Count; i++) Gizmos.DrawLine(muestras[i], muestras[i + 1]);

            // Flechas de sentido cada pocos metros.
            if (longitud > 0f)
            {
                float paso = Mathf.Max(2f, longitud / 30f);
                for (float d = paso * 0.5f; d < longitud; d += paso)
                {
                    Evaluar(d, out Vector3 p, out Vector3 dir);
                    Vector3 lado = Vector3.Cross(Vector3.up, dir).normalized * 0.25f;
                    Gizmos.DrawLine(p, p - dir * 0.5f + lado);
                    Gizmos.DrawLine(p, p - dir * 0.5f - lado);
                }
            }

            for (int i = 0; i < puntosActuales.Count; i++)
            {
                var t = puntosActuales[i];
                if (t == null) continue;
                var a = t.GetComponent<PuntoDeRuta>();
                bool bloqueado = a != null && a.bloqueado;
                bool pico = EsquinaDe(i) == TipoEsquina.Pico;
                Gizmos.color = bloqueado ? Color.red : (i == 0 ? Color.yellow : (pico ? new Color(1f, 0.5f, 0.1f) : new Color(0.3f, 1f, 0.4f)));
                if (pico) Gizmos.DrawWireCube(t.position, Vector3.one * 0.4f);
                else Gizmos.DrawWireSphere(t.position, 0.25f);
            }
        }
    }
}
