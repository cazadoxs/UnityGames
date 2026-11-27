using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    public enum TipoPuerta
    {
        /// <summary>Gira alrededor de una bisagra (puerta normal, armario, portón).</summary>
        Bisagra,
        /// <summary>Se desliza en línea recta (puerta corredera, reja que sube, compuerta).</summary>
        Corredera
    }

    public enum SentidoApertura
    {
        /// <summary>Siempre gira hacia el mismo lado (el signo de "Ángulo de apertura").</summary>
        Fijo,
        /// <summary>Se abre hacia el lado contrario al jugador (puertas de vaivén, de bar, de hospital).</summary>
        AlejandoseDelJugador
    }

    /// <summary>
    /// Puerta universal: bisagra o corredera, se abre con clic o arrastrando con el ratón,
    /// admite cualquier número de bloqueos (llave, candado, pestillo, panel, señales),
    /// cierre automático, puertas vinculadas (dobles) y sonidos.
    /// La "hoja" es la parte que se mueve: su origen (pivote) debe estar en la bisagra.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Puerta")]
    public class Puerta : Interactuable, IArrastrable
    {
        [Header("Tipo")]
        [SerializeField] TipoPuerta tipo = TipoPuerta.Bisagra;
        [Tooltip("Parte que se mueve. Vacío = este mismo objeto. Su pivote debe estar en la bisagra.")]
        [SerializeField] Transform hoja;

        [Header("Bisagra")]
        [Tooltip("Eje de giro en el espacio local de la hoja. Normalmente (0,1,0).")]
        [SerializeField] Vector3 ejeLocal = Vector3.up;
        [Tooltip("Grados al abrirse del todo. Cambia el signo para que abra hacia el otro lado.")]
        [SerializeField] float anguloApertura = 95f;
        [SerializeField] SentidoApertura sentido = SentidoApertura.Fijo;

        [Header("Corredera")]
        [Tooltip("Dirección de deslizamiento en el espacio local de la hoja. (0,1,0) para una reja que sube.")]
        [SerializeField] Vector3 direccionLocal = Vector3.right;
        [Tooltip("Metros que se desplaza al abrirse del todo.")]
        [SerializeField] float distancia = 1.2f;

        [Header("Movimiento")]
        [Tooltip("Segundos aproximados que tarda en abrirse o cerrarse sola.")]
        [SerializeField] float duracion = 0.8f;
        [SerializeField] bool empiezaAbierta;
        [Tooltip("Segundos tras los que se cierra sola (0 = nunca).")]
        [SerializeField] float cierreAutomatico;
        [Tooltip("Si está marcado, se puede mover manteniendo pulsado y arrastrando el ratón.")]
        [SerializeField] bool permitirArrastre = true;

        [Header("Bloqueos")]
        [Tooltip("Componentes que implementan IBloqueo: CerraduraLlave, Candado, PestilloCorredero, PanelNumerico, BloqueoPorSenal...")]
        [SerializeField] List<MonoBehaviour> bloqueos = new List<MonoBehaviour>();
        [Tooltip("Añade automáticamente los bloqueos que haya en este objeto y en sus hijos.")]
        [SerializeField] bool buscarBloqueosEnHijos = true;
        [Tooltip("Si se desbloquea al usarla (por ejemplo con la llave), se abre en la misma pulsación.")]
        [SerializeField] bool abrirTrasDesbloquear = true;

        [Header("Puertas vinculadas")]
        [Tooltip("Se abren y cierran a la vez que esta (la otra hoja de una puerta doble, por ejemplo).")]
        [SerializeField] List<Puerta> vinculadas = new List<Puerta>();

        [Header("Textos")]
        [SerializeField] string textoAbrir = "Abrir";
        [SerializeField] string textoCerrar = "Cerrar";
        [SerializeField] string textoArrastrando = "Mueve el ratón para empujar o tirar";
        [SerializeField] string textoChocaMarco = "Algo choca con el marco y no deja cerrar.";

        [Header("Sonidos")]
        [SerializeField] AudioClip sonidoAbrir;
        [SerializeField] AudioClip sonidoCerrar;
        [SerializeField] AudioClip sonidoBloqueada;
        [SerializeField] AudioClip sonidoGolpeMarco;

        [Header("Eventos")]
        public UnityEvent alAbrir = new UnityEvent();
        [Tooltip("Cuando queda cerrada del todo.")]
        public UnityEvent alCerrar = new UnityEvent();
        public UnityEvent alIntentarAbrirBloqueada = new UnityEvent();

        const float Cerrada = 0.01f;
        const float AperturaMinimaSiChoca = 0.05f;

        readonly List<IBloqueo> listaBloqueos = new List<IBloqueo>();
        Quaternion rotCerrada;
        Vector3 posCerrada;
        Vector3 centroLocal;
        float apertura, objetivo, velocidad;
        float tiempoAbierta;
        bool arrastrando, arrastreBloqueado;
        Vector3 agarreLocal;
        float sacudida = -1f;
        float ultimoAvisoArrastre = -10f;
        bool propagando;
        bool estabaCerrada = true;
        bool chocando;

        /// <summary>Apertura actual: 0 cerrada, 1 abierta (−1 abierta hacia el otro lado en vaivén).</summary>
        public float Apertura => apertura;
        public bool EstaCerrada => Mathf.Abs(apertura) <= Cerrada;
        public bool EstaAbriendoseOAbierta => Mathf.Abs(objetivo) > Cerrada;
        public Transform Hoja => hoja;
        public IReadOnlyList<IBloqueo> Bloqueos => listaBloqueos;

        public bool UsaArrastre => permitirArrastre;
        public bool ArrastreEnProfundidad => tipo == TipoPuerta.Bisagra;

        /// <summary>True si algún bloqueo impide abrirla ahora mismo.</summary>
        public bool EstaBloqueada
        {
            get
            {
                for (int i = 0; i < listaBloqueos.Count; i++) if (listaBloqueos[i].Bloqueado) return true;
                return false;
            }
        }

        bool AlgunoImpideCerrar
        {
            get
            {
                for (int i = 0; i < listaBloqueos.Count; i++) if (listaBloqueos[i].ImpideCerrar) return true;
                return false;
            }
        }

        void Reset() => textoAccion = "Abrir";

        void Awake()
        {
            if (hoja == null) hoja = transform;
            rotCerrada = hoja.localRotation;
            posCerrada = hoja.localPosition;
            centroLocal = CalcularCentroLocal();
            RecogerBloqueos();
            if (empiezaAbierta) { apertura = objetivo = 1f; estabaCerrada = false; }
            Aplicar();
        }

        void RecogerBloqueos()
        {
            listaBloqueos.Clear();
            foreach (var b in bloqueos)
                if (b is IBloqueo ib && !listaBloqueos.Contains(ib)) listaBloqueos.Add(ib);
            if (buscarBloqueosEnHijos)
                foreach (var ib in GetComponentsInChildren<IBloqueo>(true))
                    if (!listaBloqueos.Contains(ib)) listaBloqueos.Add(ib);
        }

        /// <summary>Añade un bloqueo en tiempo de ejecución.</summary>
        public void AnadirBloqueo(IBloqueo b) { if (b != null && !listaBloqueos.Contains(b)) listaBloqueos.Add(b); }
        public void QuitarBloqueo(IBloqueo b) => listaBloqueos.Remove(b);

        Vector3 CalcularCentroLocal()
        {
            var rs = hoja.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return Vector3.right * 0.5f;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return hoja.InverseTransformPoint(b.center);
        }

        void OnValidate()
        {
            for (int i = 0; i < bloqueos.Count; i++)
                if (bloqueos[i] != null && !(bloqueos[i] is IBloqueo))
                    bloqueos[i] = bloqueos[i].GetComponent<IBloqueo>() as MonoBehaviour;
            if (Mathf.Abs(distancia) < 0.01f) distancia = 0.01f;
            if (Mathf.Abs(anguloApertura) < 1f) anguloApertura = 1f;
        }

        // ---------------- API pública (se puede llamar desde UnityEvents) ----------------

        /// <summary>Abre si no está bloqueada. Para vaivén usa la posición del jugador principal.</summary>
        public void Abrir()
        {
            var i = Interactor.Principal;
            AbrirDesde(i != null ? i.transform.position : hoja.position + hoja.forward, false);
        }

        /// <summary>Abre aunque esté bloqueada (cinemáticas, trucos, eventos).</summary>
        public void AbrirIgnorandoBloqueos()
        {
            var i = Interactor.Principal;
            AbrirDesde(i != null ? i.transform.position : hoja.position + hoja.forward, true);
        }

        /// <summary>Abre alejándose de <paramref name="posicion"/> (solo importa en vaivén). Devuelve false si está bloqueada.</summary>
        public bool AbrirDesde(Vector3 posicion, bool ignorarBloqueos = false)
        {
            if (!ignorarBloqueos && EstaBloqueada) return false;
            float signo = 1f;
            if (tipo == TipoPuerta.Bisagra && sentido == SentidoApertura.AlejandoseDelJugador)
                signo = Mathf.Abs(apertura) > Cerrada ? Mathf.Sign(apertura) : SignoAlejandose(posicion);
            if (Mathf.Approximately(objetivo, signo)) return true;
            if (EstaCerrada) ReproducirSonido(sonidoAbrir);
            objetivo = signo;
            tiempoAbierta = 0f;
            alAbrir.Invoke();
            Propagar(p => p.AbrirDesde(posicion, ignorarBloqueos));
            return true;
        }

        public void Cerrar()
        {
            if (Mathf.Abs(objetivo) <= Cerrada) return;
            objetivo = 0f;
            Propagar(p => p.Cerrar());
        }

        public void Alternar()
        {
            if (EstaAbriendoseOAbierta) Cerrar(); else Abrir();
        }

        /// <summary>Para conectar con UnityEvent&lt;bool&gt;: true abre, false cierra.</summary>
        public void EstablecerAbierta(bool abierta)
        {
            if (abierta) Abrir(); else Cerrar();
        }

        /// <summary>Coloca la puerta al instante en una apertura (0..1, o −1..1 en vaivén).</summary>
        public void EstablecerAperturaInstantanea(float valor)
        {
            apertura = objetivo = LimitarApertura(valor);
            velocidad = 0f;
            estabaCerrada = EstaCerrada;
            Aplicar();
        }

        // ---------------- Interacción ----------------

        public override string ObtenerTexto(Interactor interactor)
        {
            if (arrastrando && !arrastreBloqueado) return textoArrastrando;
            return EstaAbriendoseOAbierta ? textoCerrar : textoAbrir;
        }

        protected override void AlInteractuar(Interactor interactor)
        {
            if (EstaAbriendoseOAbierta)
            {
                if (AlgunoImpideCerrar) AvisarChoque(interactor);
                Cerrar();
                return;
            }

            if (EstaBloqueada && !IntentarDesbloquear(interactor, true)) return;
            AbrirDesde(interactor != null ? interactor.transform.position : hoja.position);
        }

        /// <summary>Intenta quitar los bloqueos desde la puerta (llaves). Devuelve true si queda libre y debe abrirse.</summary>
        bool IntentarDesbloquear(Interactor interactor, bool avisar)
        {
            string mensajeOk = null;
            foreach (var b in listaBloqueos)
            {
                if (!b.Bloqueado) continue;
                if (b.IntentarDesbloquearDesdePuerta(interactor, out string m) && !string.IsNullOrEmpty(m)) mensajeOk = m;
            }

            if (EstaBloqueada)
            {
                if (avisar)
                {
                    string motivo = null;
                    foreach (var b in listaBloqueos) if (b.Bloqueado) { motivo = b.Motivo; break; }
                    if (interactor != null && !string.IsNullOrEmpty(motivo)) interactor.MostrarMensaje(motivo);
                    ReproducirSonido(sonidoBloqueada);
                    sacudida = 0f;
                    alIntentarAbrirBloqueada.Invoke();
                }
                return false;
            }

            if (interactor != null && mensajeOk != null) interactor.MostrarMensaje(mensajeOk);
            return abrirTrasDesbloquear;
        }

        void AvisarChoque(Interactor interactor)
        {
            if (interactor != null) interactor.MostrarMensaje(textoChocaMarco);
            ReproducirSonido(sonidoGolpeMarco);
        }

        // ---------------- Arrastre ----------------

        public void EmpezarArrastre(Interactor interactor, RaycastHit impacto)
        {
            arrastrando = true;
            agarreLocal = hoja.InverseTransformPoint(impacto.point);
            arrastreBloqueado = EstaCerrada && EstaBloqueada && !IntentarDesbloquear(interactor, false);
            velocidad = 0f;
        }

        public void Arrastrar(Interactor interactor, Vector3 mov)
        {
            if (arrastreBloqueado)
            {
                if (mov.sqrMagnitude > 0.00001f && Time.time - ultimoAvisoArrastre > 0.6f)
                {
                    ultimoAvisoArrastre = Time.time;
                    IntentarDesbloquear(interactor, true);
                }
                return;
            }

            float delta;
            if (tipo == TipoPuerta.Bisagra)
            {
                Vector3 eje = hoja.TransformDirection(ejeLocal);
                float grados = ArrastreUtil.GradosAlrededorDeEje(hoja.position, eje, hoja.TransformPoint(agarreLocal), mov);
                delta = grados / anguloApertura;
            }
            else
            {
                Vector3 dir = hoja.TransformDirection(direccionLocal).normalized;
                delta = Vector3.Dot(mov, dir) / distancia;
            }

            bool antesCerrada = EstaCerrada;
            apertura = LimitarApertura(apertura + delta);
            if (AlgunoImpideCerrar && Mathf.Abs(apertura) < AperturaMinimaSiChoca)
            {
                float s = Mathf.Abs(apertura) > 0.0001f ? Mathf.Sign(apertura) : Mathf.Sign(objetivo == 0f ? 1f : objetivo);
                apertura = s * AperturaMinimaSiChoca;
                if (!chocando) { chocando = true; AvisarChoque(interactor); }
            }
            else chocando = false;

            if (antesCerrada && !EstaCerrada) { ReproducirSonido(sonidoAbrir); alAbrir.Invoke(); }
            objetivo = apertura;
            tiempoAbierta = 0f;
        }

        public void TerminarArrastre(Interactor interactor)
        {
            arrastrando = false;
            arrastreBloqueado = false;
            chocando = false;
            objetivo = Mathf.Abs(apertura) < 0.04f && !AlgunoImpideCerrar ? 0f : apertura;
        }

        // ---------------- Movimiento ----------------

        float LimitarApertura(float v)
        {
            bool vaiven = tipo == TipoPuerta.Bisagra && sentido == SentidoApertura.AlejandoseDelJugador;
            return Mathf.Clamp(v, vaiven ? -1f : 0f, 1f);
        }

        float SignoAlejandose(Vector3 posicion)
        {
            Vector3 eje = hoja.TransformDirection(ejeLocal);
            Vector3 pivote = hoja.position;
            Vector3 c = hoja.TransformPoint(centroLocal) - pivote;
            Vector3 d = Quaternion.AngleAxis(Mathf.Sign(anguloApertura) * 5f, eje) * c - c;
            return Vector3.Dot(d, posicion - pivote) < 0f ? 1f : -1f;
        }

        void Update()
        {
            if (!arrastrando)
            {
                if (AlgunoImpideCerrar && Mathf.Abs(objetivo) < AperturaMinimaSiChoca && !EstaCerrada)
                {
                    objetivo = Mathf.Sign(apertura) * AperturaMinimaSiChoca;
                }
                apertura = Mathf.SmoothDamp(apertura, objetivo, ref velocidad, Mathf.Max(0.01f, duracion * 0.3f));
                if (Mathf.Abs(apertura - objetivo) < 0.0005f) { apertura = objetivo; velocidad = 0f; }

                if (cierreAutomatico > 0f && Mathf.Abs(objetivo) > Cerrada)
                {
                    tiempoAbierta += Time.deltaTime;
                    if (tiempoAbierta >= cierreAutomatico) Cerrar();
                }
            }

            bool cerrada = EstaCerrada;
            if (cerrada && !estabaCerrada)
            {
                apertura = 0f;
                ReproducirSonido(sonidoCerrar);
                alCerrar.Invoke();
            }
            estabaCerrada = cerrada;

            Aplicar();
        }

        void Aplicar()
        {
            if (hoja == null) return;
            float temblor = 0f;
            if (sacudida >= 0f)
            {
                sacudida += Time.deltaTime;
                const float dur = 0.3f;
                temblor = sacudida < dur ? Mathf.Sin(sacudida * 70f) * (1f - sacudida / dur) : 0f;
                if (sacudida >= dur) sacudida = -1f;
            }

            if (tipo == TipoPuerta.Bisagra)
            {
                float ang = apertura * anguloApertura + temblor * 1.2f;
                hoja.localRotation = rotCerrada * Quaternion.AngleAxis(ang, ejeLocal);
                hoja.localPosition = posCerrada;
            }
            else
            {
                Vector3 dir = rotCerrada * direccionLocal.normalized;
                hoja.localPosition = posCerrada + dir * (apertura * distancia + temblor * 0.006f);
                hoja.localRotation = rotCerrada;
            }
        }

        void Propagar(System.Action<Puerta> accion)
        {
            if (propagando) return;
            propagando = true;
            foreach (var p in vinculadas) if (p != null && p != this) accion(p);
            propagando = false;
        }

        void OnDrawGizmosSelected()
        {
            Transform h = hoja != null ? hoja : transform;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            if (tipo == TipoPuerta.Bisagra)
            {
                Vector3 eje = h.TransformDirection(ejeLocal).normalized;
                Gizmos.DrawLine(h.position - eje * 0.2f, h.position + eje * 2.2f);
                Vector3 radio = Vector3.ProjectOnPlane(h.right, eje).normalized * 0.9f;
                bool vaiven = sentido == SentidoApertura.AlejandoseDelJugador;
                float desde = vaiven ? -anguloApertura : 0f;
                Vector3 previo = h.position + Quaternion.AngleAxis(desde, eje) * radio;
                for (int i = 1; i <= 24; i++)
                {
                    float a = Mathf.Lerp(desde, anguloApertura, i / 24f);
                    Vector3 p = h.position + Quaternion.AngleAxis(a, eje) * radio;
                    Gizmos.DrawLine(previo, p);
                    previo = p;
                }
            }
            else
            {
                Vector3 dir = h.TransformDirection(direccionLocal).normalized;
                Gizmos.DrawLine(h.position, h.position + dir * distancia);
                Gizmos.DrawWireSphere(h.position + dir * distancia, 0.05f);
            }
        }
    }
}
