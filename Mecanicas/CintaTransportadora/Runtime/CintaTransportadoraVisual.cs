using UnityEngine;

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Parte visual y sonora de la cinta: desplaza la textura de la banda, hace girar
    /// los rodillos y ajusta el sonido, todo sincronizado con la velocidad real de la
    /// <see cref="CintaTransportadora"/>. No afecta a la física.
    /// </summary>
    [AddComponentMenu("Mecanicas/Cinta Transportadora/Cinta Transportadora (Visual)")]
    public class CintaTransportadoraVisual : MonoBehaviour
    {
        [Tooltip("Cinta que manda. Vacío = la busca en este objeto o en sus padres.")]
        [SerializeField] CintaTransportadora cinta;

        [Header("Banda (textura en movimiento)")]
        [Tooltip("Renderer de la banda cuya textura se desplaza.")]
        [SerializeField] Renderer rendererBanda;

        [Tooltip("Índice del material del renderer que se anima (si tiene varios).")]
        [Min(0)] [SerializeField] int indiceMaterial = 0;

        [Tooltip("Propiedad de textura a desplazar. Vacío = la textura principal del shader (_BaseMap en URP/HDRP, _MainTex en Built-in).")]
        [SerializeField] string propiedadTextura = "";

        [Tooltip("Eje de la UV que va a lo largo de la cinta: (0,1) = V, (1,0) = U.")]
        [SerializeField] Vector2 ejeUV = new Vector2(0f, 1f);

        [Tooltip("Metros de cinta que ocupa UNA repetición de la textura. Debe coincidir con el tiling para que la banda vaya a la misma velocidad que los objetos.")]
        [Min(0.001f)] [SerializeField] float metrosPorRepeticion = 0.5f;

        [Tooltip("Ajusta el tiling automáticamente según 'Largo de la banda' y 'Metros por repetición'.")]
        [SerializeField] bool ajustarTiling = true;

        [Tooltip("Largo de la banda en metros (solo para ajustar el tiling).")]
        [Min(0.01f)] [SerializeField] float largoBanda = 4f;

        [Tooltip("Marca esto si la textura se mueve al revés que los objetos.")]
        [SerializeField] bool invertirTextura = false;

        [Tooltip("Si el material no tiene textura, genera una banda de goma con flechas.")]
        [SerializeField] bool generarTexturaSiFalta = true;

        [Header("Rodillos")]
        [Tooltip("Rodillos que giran con la cinta (opcional).")]
        [SerializeField] Transform[] rodillos = new Transform[0];

        [Tooltip("Radio de los rodillos en metros (para girar a la velocidad correcta).")]
        [Min(0.001f)] [SerializeField] float radioRodillos = 0.2f;

        [Tooltip("Eje de giro en espacio local del PADRE de cada rodillo (normalmente el eje X de la cinta).")]
        [SerializeField] Vector3 ejeGiroRodillos = Vector3.right;

        [Header("Sonido (opcional)")]
        [Tooltip("AudioSource con un sonido en bucle de motor/cinta.")]
        [SerializeField] AudioSource sonido;
        [SerializeField] float volumenMaximo = 0.5f;
        [SerializeField] Vector2 tonoMinMax = new Vector2(0.7f, 1.2f);
        [Tooltip("Velocidad (m/s) a la que el sonido llega al tono máximo.")]
        [Min(0.01f)] [SerializeField] float velocidadTonoMaximo = 3f;

        Material material;
        int idPropiedad = -1;
        Vector2 desplazamiento;

        /// <summary>Material instanciado de la banda (para cambiar color, etc. desde código).</summary>
        public Material MaterialBanda => material;

        void Awake()
        {
            if (cinta == null) cinta = GetComponentInParent<CintaTransportadora>();
            PrepararMaterial();
            if (sonido != null)
            {
                sonido.loop = true;
                sonido.volume = 0f;
                if (!sonido.isPlaying && sonido.clip != null) sonido.Play();
            }
        }

        void PrepararMaterial()
        {
            if (rendererBanda == null) return;
            var mats = rendererBanda.materials; // crea instancias para no tocar el material del proyecto
            if (indiceMaterial >= mats.Length) return;
            material = mats[indiceMaterial];

            if (!string.IsNullOrEmpty(propiedadTextura) && material.HasProperty(propiedadTextura))
                idPropiedad = Shader.PropertyToID(propiedadTextura);

            if (generarTexturaSiFalta && LeerTextura() == null)
                EscribirTextura(CintaTexturaProcedural.Obtener());

            if (ajustarTiling)
            {
                float rep = largoBanda / metrosPorRepeticion;
                Vector2 eje = EjeNormalizado();
                Vector2 tiling = new Vector2(Mathf.Abs(eje.x) > 0.5f ? rep : 1f, Mathf.Abs(eje.y) > 0.5f ? rep : 1f);
                if (idPropiedad >= 0) material.SetTextureScale(idPropiedad, tiling);
                else material.mainTextureScale = tiling;
            }
            desplazamiento = idPropiedad >= 0 ? material.GetTextureOffset(idPropiedad) : material.mainTextureOffset;
        }

        void Update()
        {
            if (cinta == null) return;
            float v = cinta.VelocidadActual;
            float dt = Time.deltaTime;

            if (material != null && Mathf.Abs(v) > 0f)
            {
                // Al sumar offset, el dibujo se desplaza hacia las UV menores; por eso se resta.
                float signo = invertirTextura ? 1f : -1f;
                desplazamiento += EjeNormalizado() * (signo * v / metrosPorRepeticion * dt);
                desplazamiento.x = Mathf.Repeat(desplazamiento.x, 1f);
                desplazamiento.y = Mathf.Repeat(desplazamiento.y, 1f);
                if (idPropiedad >= 0) material.SetTextureOffset(idPropiedad, desplazamiento);
                else material.mainTextureOffset = desplazamiento;
            }

            if (rodillos != null && rodillos.Length > 0 && Mathf.Abs(v) > 0f)
            {
                float grados = v / radioRodillos * Mathf.Rad2Deg * dt;
                Quaternion giro = Quaternion.AngleAxis(grados, ejeGiroRodillos);
                foreach (var r in rodillos)
                    if (r != null) r.localRotation = giro * r.localRotation;
            }

            if (sonido != null)
            {
                float t = Mathf.Clamp01(Mathf.Abs(v) / velocidadTonoMaximo);
                sonido.volume = volumenMaximo * t;
                sonido.pitch = Mathf.Lerp(tonoMinMax.x, tonoMinMax.y, t);
            }
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        Vector2 EjeNormalizado() => ejeUV.sqrMagnitude > 0f ? ejeUV.normalized : Vector2.up;

        Texture LeerTextura() => idPropiedad >= 0 ? material.GetTexture(idPropiedad) : material.mainTexture;

        void EscribirTextura(Texture t)
        {
            if (idPropiedad >= 0) material.SetTexture(idPropiedad, t);
            else material.mainTexture = t;
        }

        /// <summary>Usado por el constructor para configurar el componente desde código.</summary>
        public void Configurar(CintaTransportadora cinta, Renderer banda, float largo, float metrosRepeticion, Transform[] rodillos, float radio)
        {
            this.cinta = cinta;
            rendererBanda = banda;
            largoBanda = largo;
            metrosPorRepeticion = metrosRepeticion;
            this.rodillos = rodillos;
            radioRodillos = radio;
            if (Application.isPlaying) PrepararMaterial();
        }
    }
}
