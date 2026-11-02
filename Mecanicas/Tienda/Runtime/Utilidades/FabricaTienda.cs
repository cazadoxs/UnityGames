using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Crea por código los montajes típicos (sistema completo, botón con pedestal, terminal).
    /// La usan la escena demo y los menús del editor (GameObject &gt; Mecánicas &gt; Tienda).
    /// </summary>
    public static class FabricaTienda
    {
        /// <summary>Crea "Sistema Tienda" con GestorTienda + Monedero + TiendaMenu + un PuntoAparicion hijo, ya conectados.</summary>
        public static GestorTienda CrearSistema(Vector3 posicionEntrega, CatalogoTienda catalogo = null)
        {
            var go = new GameObject("Sistema Tienda");
            var monedero = go.AddComponent<Monedero>();
            var gestor = go.AddComponent<GestorTienda>();
            gestor.monedero = monedero;
            gestor.catalogo = catalogo;
            var menu = go.AddComponent<TiendaMenu>();
            menu.tienda = gestor;

            var punto = new GameObject("Punto de entrega").AddComponent<PuntoAparicion>();
            punto.transform.SetParent(go.transform, false);
            punto.transform.position = posicionEntrega;
            gestor.receptorPorDefecto = punto;
            return gestor;
        }

        /// <summary>
        /// Crea un pedestal con botón (tapa que se hunde y cambia de color), etiqueta 3D con el precio
        /// y un punto de aparición delante. Devuelve el BotonCompraMundo ya configurado.
        /// </summary>
        public static BotonCompraMundo CrearBotonPedestal(Vector3 posicion, Quaternion rotacion, ArticuloTienda articulo, GestorTienda tienda = null)
        {
            var raiz = new GameObject("Botón de compra" + (articulo != null ? " - " + articulo.NombreVisible : ""));
            raiz.transform.SetPositionAndRotation(posicion, rotacion);

            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(raiz.transform, false);
            pedestal.transform.localPosition = new Vector3(0, 0.5f, 0);
            pedestal.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            Colorear(pedestal, new Color(0.35f, 0.36f, 0.4f));

            var tapa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tapa.name = "Tapa del botón";
            tapa.transform.SetParent(raiz.transform, false);
            tapa.transform.localPosition = new Vector3(0, 1.03f, 0);
            tapa.transform.localScale = new Vector3(0.32f, 0.04f, 0.32f);

            var etiquetaGO = new GameObject("Etiqueta");
            etiquetaGO.transform.SetParent(raiz.transform, false);
            etiquetaGO.transform.localPosition = new Vector3(0, 1.55f, 0);
            var tm = etiquetaGO.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.05f;
            tm.fontSize = 48;
            tm.color = Color.white;

            var punto = new GameObject("Punto de aparición").AddComponent<PuntoAparicion>();
            punto.transform.SetParent(raiz.transform, false);
            punto.transform.localPosition = new Vector3(0, 1.3f, 1.3f);
            punto.radioDispersion = 0.5f;

            var boton = raiz.AddComponent<BotonCompraMundo>();
            boton.articulo = articulo;
            boton.tienda = tienda;
            boton.receptor = punto;
            boton.parteMovil = tapa.transform;
            boton.indicador = tapa.GetComponent<Renderer>();
            boton.etiqueta = tm;
            boton.ActualizarEtiqueta();
            return boton;
        }

        /// <summary>Crea un mostrador que abre la tienda en menú, con su propio punto de entrega delante.</summary>
        public static TerminalTienda CrearTerminal(Vector3 posicion, Quaternion rotacion, TiendaMenu menu = null, CatalogoTienda catalogo = null)
        {
            var raiz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            raiz.name = "Terminal de tienda";
            raiz.transform.SetPositionAndRotation(posicion + rotacion * new Vector3(0, 0.8f, 0), rotacion);
            raiz.transform.localScale = new Vector3(1.2f, 1.6f, 0.6f);
            Colorear(raiz, new Color(0.25f, 0.35f, 0.6f));

            var cartel = new GameObject("Cartel");
            cartel.transform.SetParent(raiz.transform, false);
            cartel.transform.localPosition = new Vector3(0, 0.62f, -0.51f);
            var tm = cartel.AddComponent<TextMesh>();
            tm.text = catalogo != null ? catalogo.nombreTienda.ToUpperInvariant() : "TIENDA";
            tm.anchor = TextAnchor.MiddleCenter;
            tm.characterSize = 0.03f;
            tm.fontSize = 64;
            tm.color = new Color(1f, 0.85f, 0.3f);
            // compensa la escala no uniforme del cubo
            cartel.transform.localScale = new Vector3(1f / 1.2f, 1f / 1.6f, 1f / 0.6f);

            var punto = new GameObject("Punto de entrega").AddComponent<PuntoAparicion>();
            punto.transform.SetParent(raiz.transform, true);
            punto.transform.position = posicion + rotacion * new Vector3(0, 1.4f, -1.2f);
            punto.transform.rotation = rotacion * Quaternion.Euler(0, 180, 0);
            punto.radioDispersion = 0.5f;

            var terminal = raiz.AddComponent<TerminalTienda>();
            terminal.menu = menu;
            terminal.catalogo = catalogo;
            terminal.receptor = punto;
            return terminal;
        }

        /// <summary>Pinta un renderer con un material propio (funciona en Built-in, URP y HDRP).</summary>
        public static void Colorear(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = Application.isPlaying ? r.material : new Material(r.sharedMaterial);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            r.sharedMaterial = mat;
        }
    }
}
