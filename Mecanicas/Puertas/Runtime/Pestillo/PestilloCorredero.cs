using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    public enum ModoPestillo
    {
        /// <summary>Se maneja a mano con el ratón: levantar la manilla, deslizar y bajarla en la muesca.</summary>
        Realista,
        /// <summary>Un clic hace la secuencia completa automáticamente.</summary>
        Simple
    }

    /// <summary>
    /// Pestillo corredero (cerrojo de pasador) como los reales:
    /// 1) la manilla está bajada dentro de una muesca y el pasador no se mueve;
    /// 2) se levanta la manilla (gira el pasador sobre su eje);
    /// 3) se desliza el pasador hasta el otro extremo;
    /// 4) se baja la manilla en la muesca del extremo y queda fijado.
    /// Con la puerta cerrada y el pasador echado, la puerta no se abre. Si se echa con la puerta abierta,
    /// el pasador sobresale y choca con el marco al intentar cerrar.
    /// Colócalo como hijo de la <see cref="Puerta"/> (o añádelo a su lista de bloqueos).
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Pestillo corredero")]
    public class PestilloCorredero : Interactuable, IArrastrable, IBloqueo
    {
        [Header("Piezas")]
        [Tooltip("Pasador (barra con su manilla). Su origen debe estar en el eje de la barra.")]
        [SerializeField] Transform pasador;
        [Tooltip("Eje de deslizamiento y de giro, en el espacio local del pasador. Positivo = hacia donde se echa.")]
        [SerializeField] Vector3 ejeLocal = Vector3.right;
        [Tooltip("Metros que recorre el pasador.")]
        [SerializeField] float recorrido = 0.06f;
        [Tooltip("Grados que gira la manilla al levantarla.")]
        [SerializeField] float anguloManilla = 90f;

        [Header("Comportamiento")]
        [SerializeField] ModoPestillo modo = ModoPestillo.Realista;
        [SerializeField] bool empiezaEchado;
        [Tooltip("Margen (0..1 del recorrido) en el que la manilla entra en la muesca de cada extremo.")]
        [Range(0.01f, 0.3f)] [SerializeField] float toleranciaMuesca = 0.08f;
        [Tooltip("Por debajo de este giro (0..1) la manilla está dentro de la muesca y bloquea el deslizamiento.")]
        [Range(0.5f, 1f)] [SerializeField] float giroParaDeslizar = 0.85f;
        [Tooltip("Puerta a la que pertenece. Vacío = se busca en los padres.")]
        [SerializeField] Puerta puerta;

        [Header("Textos")]
        [SerializeField] string textoEchar = "Echar pestillo";
        [SerializeField] string textoQuitar = "Quitar pestillo";
        [SerializeField] string textoArrastrando = "Sube la manilla, desliza y bájala";
        [SerializeField] string motivo = "Tiene el pestillo echado.";

        [Header("Sonidos")]
        [SerializeField] AudioClip sonidoManilla;
        [SerializeField] AudioClip sonidoTope;

        [Header("Eventos")]
        public UnityEvent alEchar = new UnityEvent();
        public UnityEvent alQuitar = new UnityEvent();

        Vector3 posBase;
        Quaternion rotBase;
        float giro;      // 0 = manilla bajada, 1 = levantada
        float posicion;  // 0 = retirado, 1 = echado
        bool arrastrando;
        bool secuencia;
        Vector3 agarreLocal;
        bool estabaEchado;

        public float Posicion => posicion;
        public float Giro => giro;
        public bool Echado => posicion > 0.5f;

        public bool Bloqueado => Echado && (puerta == null || puerta.EstaCerrada);
        public bool ImpideCerrar => posicion > 0.15f && puerta != null && !puerta.EstaCerrada;
        public string Motivo => motivo;
        public bool IntentarDesbloquearDesdePuerta(Interactor interactor, out string mensaje) { mensaje = null; return false; }

        public bool UsaArrastre => modo == ModoPestillo.Realista && !secuencia;
        public bool ArrastreEnProfundidad => false;

        void Awake()
        {
            if (pasador == null) pasador = transform;
            if (puerta == null) puerta = GetComponentInParent<Puerta>();
            posBase = pasador.localPosition;
            rotBase = pasador.localRotation;
            posicion = empiezaEchado ? 1f : 0f;
            estabaEchado = Echado;
            giro = 0f;
            Aplicar();
        }

        public override string ObtenerTexto(Interactor interactor)
        {
            if (arrastrando) return textoArrastrando;
            string t = Echado ? textoQuitar : textoEchar;
            return modo == ModoPestillo.Realista ? t + " (clic o mantén y arrastra)" : t;
        }

        protected override void AlInteractuar(Interactor interactor)
        {
            if (secuencia) return;
            StartCoroutine(Secuencia(Echado ? 0f : 1f));
        }

        /// <summary>Echa o quita el pestillo con la secuencia animada (para UnityEvents).</summary>
        public void Echar() { if (!secuencia && !Echado) StartCoroutine(Secuencia(1f)); }
        public void Quitar() { if (!secuencia && Echado) StartCoroutine(Secuencia(0f)); }

        IEnumerator Secuencia(float destino)
        {
            secuencia = true;
            yield return Animar(() => giro, v => giro = v, 1f, 0.15f);
            ReproducirSonido(sonidoManilla, 0.6f);
            yield return Animar(() => posicion, v => posicion = v, destino, 0.25f);
            ReproducirSonido(sonidoTope);
            yield return Animar(() => giro, v => giro = v, 0f, 0.12f);
            ReproducirSonido(sonidoManilla);
            secuencia = false;
        }

        IEnumerator Animar(System.Func<float> leer, System.Action<float> escribir, float destino, float dur)
        {
            float inicio = leer();
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                escribir(Mathf.Lerp(inicio, destino, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
            escribir(destino);
        }

        // ---------------- Arrastre ----------------

        bool EnMuesca => posicion <= toleranciaMuesca || posicion >= 1f - toleranciaMuesca;

        public void EmpezarArrastre(Interactor interactor, RaycastHit impacto)
        {
            arrastrando = true;
            agarreLocal = pasador.InverseTransformPoint(impacto.point);
        }

        public void Arrastrar(Interactor interactor, Vector3 mov)
        {
            Vector3 eje = pasador.TransformDirection(ejeLocal).normalized;
            Vector3 agarre = pasador.TransformPoint(agarreLocal);

            // 1) Girar la manilla.
            float dGiro = ArrastreUtil.GradosAlrededorDeEje(pasador.position, eje, agarre, mov) / anguloManilla;
            float giroAntes = giro;
            giro = Mathf.Clamp01(giro + dGiro);
            if (!EnMuesca) giro = Mathf.Max(giro, giroParaDeslizar); // fuera de las muescas la manilla va por la guía
            if (giroAntes > 0.15f && giro <= 0.15f) ReproducirSonido(sonidoManilla);

            // 2) Deslizar solo si la manilla está levantada.
            if (giro >= giroParaDeslizar)
            {
                float antes = posicion;
                posicion = Mathf.Clamp01(posicion + Vector3.Dot(mov, eje) / recorrido);
                if ((antes > 0f && posicion <= 0f) || (antes < 1f && posicion >= 1f)) ReproducirSonido(sonidoTope);
            }

            // 3) Con la manilla bajada en una muesca, el pasador encaja en el extremo exacto.
            if (giro < giroParaDeslizar && EnMuesca) posicion = posicion < 0.5f ? 0f : 1f;
        }

        public void TerminarArrastre(Interactor interactor)
        {
            arrastrando = false;
            if (EnMuesca && giro < 0.5f)
            {
                giro = 0f;
                posicion = posicion < 0.5f ? 0f : 1f;
            }
        }

        void Update()
        {
            if (!arrastrando && !secuencia && !EnMuesca)
                giro = Mathf.MoveTowards(giro, 1f, Time.deltaTime * 4f);

            bool echado = Echado;
            if (echado != estabaEchado)
            {
                estabaEchado = echado;
                if (echado) alEchar.Invoke(); else alQuitar.Invoke();
            }
            Aplicar();
        }

        void Aplicar()
        {
            if (pasador == null) return;
            Vector3 dir = rotBase * ejeLocal.normalized;
            pasador.localPosition = posBase + dir * (recorrido * posicion);
            pasador.localRotation = rotBase * Quaternion.AngleAxis(giro * anguloManilla, ejeLocal);
        }

        void OnDrawGizmosSelected()
        {
            Transform p = pasador != null ? pasador : transform;
            Vector3 eje = p.TransformDirection(ejeLocal).normalized;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p.position, p.position + eje * recorrido);
            Gizmos.DrawWireSphere(p.position + eje * recorrido, 0.01f);
        }
    }
}
