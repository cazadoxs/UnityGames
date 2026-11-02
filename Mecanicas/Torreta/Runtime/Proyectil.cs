using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Bala que vuela sin Rigidbody: cada frame avanza y lanza un rayo (o una esfera) por el tramo
    /// recorrido, así nunca atraviesa paredes aunque vaya muy rápido. Al chocar hace daño a
    /// cualquier <see cref="IDaniable"/>, empuja Rigidbodies, crea un efecto y vuelve al pool.
    /// </summary>
    [AddComponentMenu("Mecanicas/Torretas/Proyectil")]
    public class Proyectil : MonoBehaviour
    {
        [Tooltip("Segundos antes de desaparecer si no choca con nada.")]
        [Min(0.1f)] public float vidaMaxima = 4f;
        [Tooltip("0 = rayo fino. Mayor que 0 = la bala es una esfera de este radio (más fácil de acertar).")]
        [Min(0f)] public float radio = 0f;
        [Tooltip("Fuerza con la que empuja los Rigidbody que golpea.")]
        [Min(0f)] public float fuerzaEmpuje = 2f;
        [Tooltip("Gira la bala para que mire hacia donde vuela (útil con gravedad).")]
        public bool alinearConVelocidad = true;

        [Header("Impacto")]
        [Tooltip("Efecto que aparece en el punto de impacto (chispas, agujero...).")]
        public GameObject efectoImpacto;
        [Min(0f)] public float duracionEfectoImpacto = 2f;
        public AudioClip[] sonidosImpacto = new AudioClip[0];
        [Range(0f, 1f)] public float volumenImpacto = 0.6f;
        [Tooltip("Si no hay sonidos de impacto, usa uno generado por código.")]
        public bool sonidoImpactoGenerado = true;

        /// <summary>Se dispara al chocar: (bala, información del choque).</summary>
        public event System.Action<Proyectil, RaycastHit> Impacto;

        /// <summary>Prefab del que salió (lo usa el pool para devolverlo a su sitio).</summary>
        public GameObject PrefabOrigen { get; internal set; }
        public Vector3 Velocidad => velocidad;
        public Transform Duenio => duenio;

        Vector3 velocidad;
        Vector3 gravedad;
        float danio;
        LayerMask capas;
        Transform duenio;
        float tiempoVivo;
        bool activa;
        TrailRenderer[] estelas;

        static readonly RaycastHit[] buffer = new RaycastHit[16];

        void Awake()
        {
            estelas = GetComponentsInChildren<TrailRenderer>(true);
        }

        /// <summary>Pone la bala en marcha. Lo llama la torreta justo después de sacarla del pool.</summary>
        public void Lanzar(Vector3 velocidadInicial, float danio, Vector3 gravedad, LayerMask capas, Transform duenio)
        {
            velocidad = velocidadInicial;
            this.danio = danio;
            this.gravedad = gravedad;
            this.capas = capas;
            this.duenio = duenio;
            tiempoVivo = 0f;
            activa = true;
            if (estelas == null) estelas = GetComponentsInChildren<TrailRenderer>(true);
            foreach (var e in estelas) if (e != null) e.Clear();
            if (alinearConVelocidad && velocidad.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(velocidad);
        }

        void Update()
        {
            if (!activa) return;
            float dt = Time.deltaTime;
            tiempoVivo += dt;
            if (tiempoVivo >= vidaMaxima) { Devolver(); return; }

            Vector3 inicio = transform.position;
            Vector3 paso = velocidad * dt + 0.5f * gravedad * dt * dt;
            float dist = paso.magnitude;

            if (dist > 1e-6f && BuscarChoque(inicio, paso / dist, dist, out RaycastHit hit))
            {
                transform.position = hit.point;
                Chocar(hit, paso / dist);
                return;
            }

            transform.position = inicio + paso;
            velocidad += gravedad * dt;
            if (alinearConVelocidad && velocidad.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(velocidad);
        }

        bool BuscarChoque(Vector3 origen, Vector3 dir, float dist, out RaycastHit mejor)
        {
            int n = radio > 0f
                ? Physics.SphereCastNonAlloc(origen, radio, dir, buffer, dist, capas, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(origen, dir, buffer, dist, capas, QueryTriggerInteraction.Ignore);

            mejor = default;
            float mejorDist = float.MaxValue;
            bool encontrado = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = buffer[i];
                if (h.collider == null) continue;
                if (duenio != null && h.collider.transform.IsChildOf(duenio)) continue; // no se dispara a sí misma
                if (h.distance < mejorDist)
                {
                    mejorDist = h.distance;
                    mejor = h;
                    encontrado = true;
                }
            }
            // Un SphereCast que empieza ya dentro de algo devuelve punto (0,0,0): lo corregimos.
            if (encontrado && mejor.distance <= 0f && mejor.point == Vector3.zero) mejor.point = origen;
            return encontrado;
        }

        void Chocar(RaycastHit hit, Vector3 dir)
        {
            var objetivo = hit.collider.GetComponentInParent<IDaniable>();
            if (objetivo != null && danio > 0f)
                objetivo.RecibirDanio(new InfoDanio(danio, hit.point, dir, duenio != null ? duenio.gameObject : null));

            if (hit.rigidbody != null && fuerzaEmpuje > 0f && !hit.rigidbody.isKinematic)
                hit.rigidbody.AddForceAtPosition(dir * fuerzaEmpuje, hit.point, ForceMode.Impulse);

            if (efectoImpacto != null)
            {
                Quaternion rot = hit.normal.sqrMagnitude > 0f ? Quaternion.LookRotation(hit.normal) : Quaternion.identity;
                var fx = Instantiate(efectoImpacto, hit.point, rot);
                fx.SetActive(true);
                if (duracionEfectoImpacto > 0f) Destroy(fx, duracionEfectoImpacto);
            }

            AudioClip clip = null;
            if (sonidosImpacto != null && sonidosImpacto.Length > 0) clip = sonidosImpacto[Random.Range(0, sonidosImpacto.Length)];
            else if (sonidoImpactoGenerado) clip = SonidoProcedural.Impacto();
            if (clip != null && volumenImpacto > 0f) AudioSource.PlayClipAtPoint(clip, hit.point, volumenImpacto);

            Impacto?.Invoke(this, hit);
            Devolver();
        }

        /// <summary>Quita la bala de la escena (vuelve al pool o se destruye).</summary>
        public void Devolver()
        {
            activa = false;
            PoolProyectiles.Devolver(this);
        }
    }
}
