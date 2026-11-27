using UnityEngine;

namespace Mecanicas.Inspeccion
{
    /// <summary>
    /// Interfaz sencilla para el agarrador: punto de mira, avisos de controles, barra de carga del
    /// lanzamiento y panel con el nombre y la descripción al inspeccionar.
    ///
    /// Se dibuja con OnGUI, así que no necesita Canvas, prefabs ni TextMeshPro: basta con añadirlo.
    /// Todos los textos y colores se cambian desde el Inspector. Si prefieres tu propia interfaz,
    /// no añadas este componente y lee el estado de <see cref="AgarradorPrimeraPersona"/>
    /// (Estado, Objetivo, Sujeto, CargaLanzamiento) o usa sus eventos.
    /// </summary>
    [AddComponentMenu("Mecánicas/Inspección/Interfaz Inspección")]
    public class InterfazInspeccion : MonoBehaviour
    {
        public AgarradorPrimeraPersona agarrador;

        [Header("Qué mostrar")]
        public bool mostrarPuntoDeMira = true;
        public bool mostrarAvisos = true;
        public bool mostrarBarraDeCarga = true;
        public bool mostrarPanelInspeccion = true;

        [Header("Textos ({0} = nombre del objeto)")]
        public string textoApuntandoAgarrable = "{0}\n[E] Coger    [R] Inspeccionar";
        public string textoApuntandoSoloAgarrable = "{0}\n[E] Coger";
        public string textoApuntandoSoloInspeccion = "{0}\n[E] / [R] Inspeccionar";
        public string textoDemasiadoPesado = "{0}\nDemasiado pesado";
        public string textoSujetando = "[E] Soltar    [Clic izq.] Lanzar (mantén para cargar)    [R] Inspeccionar";
        public string textoSujetandoNoLanzable = "[E] Soltar    [R] Inspeccionar";
        public string textoInspeccionando = "Ratón: girar    Rueda: acercar / alejar    [R] / [Q] Terminar";

        [Header("Estilo")]
        public Color colorTexto = Color.white;
        public Color colorSombra = new Color(0f, 0f, 0f, 0.8f);
        public Color colorPuntoDeMira = new Color(1f, 1f, 1f, 0.7f);
        public Color colorPuntoDeMiraActivo = new Color(1f, 0.85f, 0.3f, 1f);
        public Color colorBarra = new Color(1f, 0.6f, 0.15f, 0.95f);
        public Color colorFondoPanel = new Color(0f, 0f, 0f, 0.55f);
        [Tooltip("Tamaño de letra relativo a una pantalla de 1080 píxeles de alto.")]
        public int tamanoLetra = 22;
        public float tamanoPuntoDeMira = 6f;

        GUIStyle _estilo, _estiloTitulo, _estiloCuerpo;
        Texture2D _blanco;

        void Awake()
        {
            if (agarrador == null) agarrador = GetComponent<AgarradorPrimeraPersona>();
            if (agarrador == null) agarrador = FindAnyAgarrador();
        }

        static AgarradorPrimeraPersona FindAnyAgarrador()
        {
#if UNITY_2023_1_OR_NEWER
            return FindAnyObjectByType<AgarradorPrimeraPersona>();
#else
            return FindObjectOfType<AgarradorPrimeraPersona>();
#endif
        }

        void OnDestroy()
        {
            if (_blanco != null) Destroy(_blanco);
        }

        void PrepararEstilos(float escala)
        {
            if (_blanco == null)
            {
                _blanco = new Texture2D(1, 1);
                _blanco.SetPixel(0, 0, Color.white);
                _blanco.Apply();
            }
            if (_estilo == null)
            {
                _estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = true, richText = true };
                _estiloTitulo = new GUIStyle(_estilo) { fontStyle = FontStyle.Bold };
                _estiloCuerpo = new GUIStyle(_estilo) { alignment = TextAnchor.UpperLeft };
            }
            _estilo.fontSize = Mathf.RoundToInt(tamanoLetra * escala);
            _estiloTitulo.fontSize = Mathf.RoundToInt(tamanoLetra * 1.4f * escala);
            _estiloCuerpo.fontSize = Mathf.RoundToInt(tamanoLetra * 0.9f * escala);
        }

        void OnGUI()
        {
            if (agarrador == null || !agarrador.isActiveAndEnabled) return;
            float escala = Screen.height / 1080f;
            PrepararEstilos(escala);

            float ancho = Screen.width, alto = Screen.height;
            var estado = agarrador.Estado;

            if (mostrarPuntoDeMira && estado != EstadoAgarre.Inspeccionando)
            {
                bool activo = agarrador.Objetivo != null;
                float t = tamanoPuntoDeMira * escala * (activo ? 1.6f : 1f);
                Rellenar(new Rect(ancho / 2f - t / 2f, alto / 2f - t / 2f, t, t), activo ? colorPuntoDeMiraActivo : colorPuntoDeMira);
            }

            if (mostrarAvisos)
            {
                string aviso = TextoAviso();
                if (!string.IsNullOrEmpty(aviso))
                {
                    float y = estado == EstadoAgarre.Inspeccionando ? alto - 70f * escala : alto / 2f + 30f * escala;
                    Texto(new Rect(0f, y, ancho, 80f * escala), aviso, _estilo);
                }
            }

            if (mostrarBarraDeCarga && estado == EstadoAgarre.Sujetando && agarrador.Cargando)
            {
                float w = 220f * escala, h = 8f * escala;
                var r = new Rect(ancho / 2f - w / 2f, alto / 2f + 90f * escala, w, h);
                Rellenar(r, colorFondoPanel);
                Rellenar(new Rect(r.x, r.y, w * agarrador.CargaLanzamiento, h), colorBarra);
            }

            if (mostrarPanelInspeccion && estado == EstadoAgarre.Inspeccionando && agarrador.Sujeto != null)
            {
                var obj = agarrador.Sujeto;
                float w = 420f * escala, margen = 40f * escala, relleno = 18f * escala;
                float altoTitulo = _estiloTitulo.CalcHeight(new GUIContent(obj.NombreVisible), w - relleno * 2f);
                float altoCuerpo = string.IsNullOrEmpty(obj.descripcion) ? 0f : _estiloCuerpo.CalcHeight(new GUIContent(obj.descripcion), w - relleno * 2f);
                var panel = new Rect(margen, alto / 2f - (altoTitulo + altoCuerpo) / 2f - relleno, w, altoTitulo + altoCuerpo + relleno * 2.5f);
                Rellenar(panel, colorFondoPanel);
                _estiloTitulo.alignment = TextAnchor.UpperLeft;
                Texto(new Rect(panel.x + relleno, panel.y + relleno, w - relleno * 2f, altoTitulo), obj.NombreVisible, _estiloTitulo);
                if (altoCuerpo > 0f)
                    Texto(new Rect(panel.x + relleno, panel.y + relleno * 1.5f + altoTitulo, w - relleno * 2f, altoCuerpo), obj.descripcion, _estiloCuerpo);
            }
        }

        string TextoAviso()
        {
            switch (agarrador.Estado)
            {
                case EstadoAgarre.Libre:
                {
                    var obj = agarrador.Objetivo;
                    if (obj == null) return null;
                    string nombre = obj.NombreVisible;
                    bool inspeccionable = obj.sePuedeInspeccionar && agarrador.permitirInspeccionSinAgarrar;
                    if (obj.PuedeAgarrarse && !agarrador.PuedeAgarrar(obj))
                        return string.Format(textoDemasiadoPesado, nombre);
                    if (agarrador.PuedeAgarrar(obj))
                        return string.Format(inspeccionable ? textoApuntandoAgarrable : textoApuntandoSoloAgarrable, nombre);
                    return inspeccionable ? string.Format(textoApuntandoSoloInspeccion, nombre) : null;
                }
                case EstadoAgarre.Sujetando:
                    return agarrador.Sujeto != null && agarrador.Sujeto.sePuedeLanzar ? textoSujetando : textoSujetandoNoLanzable;
                case EstadoAgarre.Inspeccionando:
                    return textoInspeccionando;
                default:
                    return null;
            }
        }

        void Rellenar(Rect r, Color c)
        {
            var antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _blanco);
            GUI.color = antes;
        }

        void Texto(Rect r, string texto, GUIStyle estilo)
        {
            var antes = estilo.normal.textColor;
            estilo.normal.textColor = colorSombra;
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), texto, estilo);
            estilo.normal.textColor = colorTexto;
            GUI.Label(r, texto, estilo);
            estilo.normal.textColor = antes;
        }
    }
}
