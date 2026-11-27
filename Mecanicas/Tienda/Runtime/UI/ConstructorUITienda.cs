using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Genera por código una interfaz de tienda completa (Canvas, panel, pestañas, lista con scroll y filas)
    /// para que el sistema funcione sin preparar nada. Si quieres tu propio diseño, asigna 'panel' y
    /// las demás referencias en TiendaMenu y esto no se usa. Truco: dale a Play, abre la tienda, copia el
    /// "Canvas Tienda" generado, pégalo fuera de Play y úsalo como punto de partida de tu prefab.
    /// </summary>
    public static class ConstructorUITienda
    {
        public static void Construir(TiendaMenu m)
        {
            var e = m.estilo;
            AsegurarEventSystem();

            // Canvas
            var canvasGO = new GameObject("Canvas Tienda", typeof(RectTransform));
            canvasGO.transform.SetParent(m.transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = e.ordenCanvas;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Fondo oscuro a pantalla completa (es el "panel" que se activa/desactiva)
            var fondo = Crear("Tienda", canvasGO.transform);
            Estirar(fondo);
            fondo.gameObject.AddComponent<Image>().color = e.colorFondo;
            m.panel = fondo.gameObject;

            // Ventana
            var ventana = Crear("Ventana", fondo);
            ventana.anchorMin = ventana.anchorMax = new Vector2(0.5f, 0.5f);
            ventana.sizeDelta = e.tamanoPanel;
            ventana.gameObject.AddComponent<Image>().color = e.colorPanel;
            var vl = ventana.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(24, 24, 18, 18);
            vl.spacing = 12;
            vl.childControlWidth = true; vl.childControlHeight = true;
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;

            // Cabecera: título · saldo · cerrar
            var cabecera = Crear("Cabecera", ventana);
            Alto(cabecera, 52, 0);
            var hl = cabecera.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;
            m.textoTitulo = Texto("Titulo", cabecera, "Tienda", e.tamanoTexto + 10, e.colorTexto, TextAnchor.MiddleLeft, e);
            m.textoTitulo.fontStyle = FontStyle.Bold;
            Ancho(m.textoTitulo.rectTransform, 0, 1);
            m.textoSaldo = Texto("Saldo", cabecera, "", e.tamanoTexto, e.colorAcento, TextAnchor.MiddleRight, e);
            Ancho(m.textoSaldo.rectTransform, 0, 1);
            m.botonCerrar = Boton("Cerrar", cabecera, "X", e.colorPestana, e);
            Ancho(m.botonCerrar.GetComponent<RectTransform>(), 52, 0);
            m.botonCerrar.onClick.AddListener(m.Cerrar);

            // Pestañas
            var pestanas = Crear("Pestañas", ventana);
            Alto(pestanas, 44, 0);
            var pl = pestanas.gameObject.AddComponent<HorizontalLayoutGroup>();
            pl.spacing = 8; pl.childAlignment = TextAnchor.MiddleLeft;
            pl.childControlWidth = true; pl.childControlHeight = true;
            pl.childForceExpandWidth = false; pl.childForceExpandHeight = true;
            m.contenedorCategorias = pestanas;
            var pestana = Boton("Plantilla pestaña", pestanas, "Categoría", e.colorPestana, e);
            Ancho(pestana.GetComponent<RectTransform>(), 170, 0);
            pestana.gameObject.SetActive(false);
            m.prefabPestana = pestana;

            // Lista con scroll
            var scrollRT = Crear("Lista", ventana);
            Alto(scrollRT, 0, 1);
            var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            var viewport = Crear("Viewport", scrollRT);
            Estirar(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f); // recibe la rueda del ratón
            var contenido = Crear("Contenido", viewport);
            contenido.anchorMin = new Vector2(0, 1); contenido.anchorMax = new Vector2(1, 1);
            contenido.pivot = new Vector2(0.5f, 1); contenido.sizeDelta = Vector2.zero;
            var cl = contenido.gameObject.AddComponent<VerticalLayoutGroup>();
            cl.spacing = 8;
            cl.childControlWidth = true; cl.childControlHeight = true;
            cl.childForceExpandWidth = true; cl.childForceExpandHeight = false;
            contenido.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = contenido;
            m.contenedorFilas = contenido;

            // Mensaje inferior
            m.textoMensaje = Texto("Mensaje", ventana, "", e.tamanoTexto, e.colorAcento, TextAnchor.MiddleCenter, e);
            Alto(m.textoMensaje.rectTransform, 34, 0);

            // Plantilla de fila (desactivada, vive fuera de la lista)
            m.prefabFila = CrearFila(fondo, e);
            m.prefabFila.gameObject.SetActive(false);

            m.panel.SetActive(false);
        }

        /// <summary>Crea una fila con el estilo dado. Útil también si construyes tu propia UI por código.</summary>
        public static FilaArticuloUI CrearFila(Transform padre, EstiloTiendaUI e)
        {
            var fila = Crear("Fila artículo", padre);
            Alto(fila, e.altoFila, 0);
            fila.gameObject.AddComponent<Image>().color = e.colorFila;
            var f = fila.gameObject.AddComponent<FilaArticuloUI>();
            f.grupo = fila.gameObject.AddComponent<CanvasGroup>();
            var hl = fila.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(12, 12, 10, 10);
            hl.spacing = 14; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;

            var icono = Crear("Icono", fila);
            Ancho(icono, e.altoFila - 20, 0);
            f.icono = icono.gameObject.AddComponent<Image>();
            f.icono.preserveAspect = true;

            var textos = Crear("Textos", fila);
            Ancho(textos, 0, 1);
            var tl = textos.gameObject.AddComponent<VerticalLayoutGroup>();
            tl.childControlWidth = true; tl.childControlHeight = true;
            tl.childForceExpandWidth = true; tl.childForceExpandHeight = false;
            tl.childAlignment = TextAnchor.MiddleLeft;
            f.textoNombre = Texto("Nombre", textos, "Artículo", e.tamanoTexto + 2, e.colorTexto, TextAnchor.MiddleLeft, e);
            f.textoNombre.fontStyle = FontStyle.Bold;
            f.textoDescripcion = Texto("Descripción", textos, "", e.tamanoTexto - 6, e.colorTextoSecundario, TextAnchor.UpperLeft, e);
            f.textoEstado = Texto("Estado", textos, "", e.tamanoTexto - 8, e.colorTextoSecundario, TextAnchor.MiddleLeft, e);

            f.textoPrecio = Texto("Precio", fila, "0", e.tamanoTexto, e.colorAcento, TextAnchor.MiddleRight, e);
            Ancho(f.textoPrecio.rectTransform, 150, 0);

            f.botonComprar = Boton("Comprar", fila, "Comprar", e.colorBoton, e);
            Ancho(f.botonComprar.GetComponent<RectTransform>(), 150, 0);
            f.textoBoton = f.botonComprar.GetComponentInChildren<Text>();
            return f;
        }

        public static void AsegurarEventSystem()
        {
            if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        // ---------------------------------------------------------------- utilidades

        static RectTransform Crear(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            return rt;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static LayoutElement Layout(RectTransform rt)
        {
            var le = rt.GetComponent<LayoutElement>();
            return le != null ? le : rt.gameObject.AddComponent<LayoutElement>();
        }

        static void Alto(RectTransform rt, float preferido, float flexible)
        {
            var le = Layout(rt);
            if (preferido > 0) { le.preferredHeight = preferido; le.minHeight = preferido; }
            le.flexibleHeight = flexible;
        }

        static void Ancho(RectTransform rt, float preferido, float flexible)
        {
            var le = Layout(rt);
            if (preferido > 0) { le.preferredWidth = preferido; le.minWidth = preferido; }
            le.flexibleWidth = flexible;
        }

        static Text Texto(string nombre, Transform padre, string contenido, int tamano, Color color, TextAnchor alineacion, EstiloTiendaUI e)
        {
            var rt = Crear(nombre, padre);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = e.Fuente();
            t.text = contenido;
            t.fontSize = Mathf.Max(8, tamano);
            t.color = color;
            t.alignment = alineacion;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        static Button Boton(string nombre, Transform padre, string texto, Color color, EstiloTiendaUI e)
        {
            var rt = Crear(nombre, padre);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colores = b.colors;
            colores.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colores.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colores.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            b.colors = colores;
            var t = Texto("Texto", rt, texto, e.tamanoTexto, e.colorTexto, TextAnchor.MiddleCenter, e);
            Estirar(t.rectTransform);
            return b;
        }
    }
}
