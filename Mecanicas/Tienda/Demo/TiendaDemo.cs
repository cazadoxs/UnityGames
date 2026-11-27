using UnityEngine;

namespace Mecanicas.Tienda.Demo
{
    /// <summary>
    /// Monta una escena de prueba completa al darle a Play: suelo, luz, jugador en primera persona,
    /// tres botones de compra, una plataforma de compra (se compra al pisarla), un mostrador que abre la
    /// tienda en menú, un cofre que da dinero y un inventario de ejemplo.
    /// Uso: escena vacía → objeto vacío → añadir TiendaDemo → Play. (O menú Herramientas › Mecánicas › Tienda).
    /// </summary>
    public class TiendaDemo : MonoBehaviour
    {
        public int monedasIniciales = 300;
        public int gemasIniciales = 2;

        GestorTienda tienda;
        InventarioDemo inventario;
        JugadorDemo jugador;
        string ultimoMensaje = "";
        float finMensaje;
        GUIStyle estiloHud;

        void Awake()
        {
            CrearEntorno();
            var catalogo = CrearCatalogo();

            // Sistema: gestor + monedero + tienda en menú + punto de entrega por defecto
            tienda = FabricaTienda.CrearSistema(new Vector3(0, 2.5f, 6f), catalogo);
            tienda.idTienda = "Demo";
            var monedero = tienda.GetComponent<Monedero>();
            monedero.saldoInicial.Clear();
            monedero.saldoInicial.Add(new Monedero.SaldoMoneda { moneda = "monedas", cantidad = monedasIniciales });
            monedero.saldoInicial.Add(new Monedero.SaldoMoneda { moneda = "gemas", cantidad = gemasIniciales });
            var entrega = tienda.GetComponentInChildren<PuntoAparicion>();
            entrega.radioDispersion = 1.2f;
            entrega.maxObjetosVivos = 40;

            // Inventario de ejemplo (gancho opcional)
            inventario = tienda.gameObject.AddComponent<InventarioDemo>();
            tienda.inventario = inventario;

            // Mejora que no crea objeto (ModoEntrega.SoloEvento)
            tienda.CompraProcesada += r =>
            {
                if (r.Exito && r.Articulo != null && r.Articulo.Id == "botas" && jugador != null) jugador.velocidad *= 1.6f;
            };
            GestorTienda.CualquierCompra += MostrarMensaje;

            CrearJugador();
            var menu = tienda.GetComponent<TiendaMenu>();
            menu.desactivarMientrasAbierta = new Behaviour[] { jugador };

            // Botones en el mundo
            FabricaTienda.CrearBotonPedestal(new Vector3(-2.5f, 0, 0), Quaternion.identity, catalogo.Buscar("caja"), tienda);
            FabricaTienda.CrearBotonPedestal(new Vector3(0, 0, 0), Quaternion.identity, catalogo.Buscar("pelota"), tienda);
            FabricaTienda.CrearBotonPedestal(new Vector3(2.5f, 0, 0), Quaternion.identity, catalogo.Buscar("pack_cajas"), tienda);
            CrearPlataforma(new Vector3(6f, 0, -2f), catalogo.Buscar("barril"));

            // Mostrador con la tienda en menú
            FabricaTienda.CrearTerminal(new Vector3(-6.5f, 0, -1f), Quaternion.Euler(0, -90, 0), menu, catalogo);

            // Cofre de monedas
            var cofre = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cofre.name = "Cofre de monedas";
            cofre.transform.position = new Vector3(-3f, 0.35f, -4.5f);
            cofre.transform.localScale = new Vector3(0.9f, 0.7f, 0.6f);
            FabricaTienda.Colorear(cofre, new Color(0.85f, 0.65f, 0.15f));
            cofre.AddComponent<CofreMonedasDemo>();
        }

        void OnDestroy() { GestorTienda.CualquierCompra -= MostrarMensaje; }

        void MostrarMensaje(ResultadoCompra r)
        {
            ultimoMensaje = r.Mensaje;
            finMensaje = Time.unscaledTime + 2.5f;
        }

        // ------------------------------------------------------------ escena

        void CrearEntorno()
        {
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None)) cam.gameObject.SetActive(false);

            if (FindFirstObjectByType<Light>() == null)
            {
                var luz = new GameObject("Luz").AddComponent<Light>();
                luz.type = LightType.Directional;
                luz.intensity = 1.1f;
                luz.shadows = LightShadows.Soft;
                luz.transform.rotation = Quaternion.Euler(50, -30, 0);
            }

            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.localScale = new Vector3(3, 1, 3);
            FabricaTienda.Colorear(suelo, new Color(0.45f, 0.47f, 0.5f));

            // Paredes bajas para que no se escapen las pelotas
            Pared(new Vector3(0, 0.5f, 15), new Vector3(30, 1, 0.3f));
            Pared(new Vector3(0, 0.5f, -15), new Vector3(30, 1, 0.3f));
            Pared(new Vector3(15, 0.5f, 0), new Vector3(0.3f, 1, 30));
            Pared(new Vector3(-15, 0.5f, 0), new Vector3(0.3f, 1, 30));
        }

        static void Pared(Vector3 pos, Vector3 escala)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "Pared";
            p.transform.position = pos;
            p.transform.localScale = escala;
            FabricaTienda.Colorear(p, new Color(0.3f, 0.32f, 0.36f));
        }

        void CrearJugador()
        {
            var go = new GameObject("Jugador");
            go.tag = "Player";
            go.transform.position = new Vector3(0, 1f, -6f);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0, 0);

            var camGO = new GameObject("Cámara");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(go.transform, false);
            camGO.transform.localPosition = new Vector3(0, 0.7f, 0);
            camGO.AddComponent<Camera>().nearClipPlane = 0.05f;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<InteractorTienda>();

            jugador = go.AddComponent<JugadorDemo>();
            jugador.camara = camGO.transform;
        }

        void CrearPlataforma(Vector3 pos, ArticuloTienda articulo)
        {
            // Botón de suelo: se compra al pisarlo (trigger)
            var boton = FabricaTienda.CrearBotonPedestal(pos, Quaternion.identity, articulo, tienda);
            boton.name = "Plataforma de compra - " + articulo.NombreVisible;
            boton.comprarAlEntrarEnTrigger = true;
            boton.enfriamiento = 1f;
            var pedestal = boton.transform.Find("Pedestal");
            pedestal.localPosition = new Vector3(0, 0.03f, 0);
            pedestal.localScale = new Vector3(1.4f, 0.06f, 1.4f);
            var tapa = boton.parteMovil;
            tapa.localPosition = new Vector3(0, 0.07f, 0);
            tapa.localScale = new Vector3(1.1f, 0.02f, 1.1f);
            boton.etiqueta.transform.localPosition = new Vector3(0, 1.2f, 0.8f);
            boton.textoAviso = "Pisa la plataforma para comprar {nombre} · {precio} {moneda}";
            var trigger = boton.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 0.5f, 0);
            trigger.size = new Vector3(1.2f, 1f, 1.2f);
            var punto = boton.receptor as PuntoAparicion;
            punto.transform.localPosition = new Vector3(0, 1.5f, 2f);
        }

        // ------------------------------------------------------------ datos

        CatalogoTienda CrearCatalogo()
        {
            var plantillas = new GameObject("Plantillas (desactivado)");
            plantillas.SetActive(false);
            plantillas.transform.SetParent(transform, false);

            var cat = ScriptableObject.CreateInstance<CatalogoTienda>();
            cat.name = "Catálogo demo";
            cat.nombreTienda = "Bazar de pruebas";

            var caja = Articulo("caja", "Caja de madera", 20, Plantilla(plantillas, PrimitiveType.Cube, new Color(0.6f, 0.4f, 0.2f), 0.5f), "Objetos");
            caja.descripcion = "Una caja normal y corriente.";
            var pelota = Articulo("pelota", "Pelota", 35, Plantilla(plantillas, PrimitiveType.Sphere, new Color(0.9f, 0.2f, 0.2f), 0.45f), "Objetos");
            pelota.descripcion = "Rebota un poco.";
            var barril = Articulo("barril", "Barril", 50, Plantilla(plantillas, PrimitiveType.Cylinder, new Color(0.2f, 0.4f, 0.85f), 0.5f), "Objetos");
            barril.descripcion = "Se compra pisando la plataforma.";

            var pack = Articulo("pack_cajas", "Pack de 5 cajas", 80, caja.prefab, "Packs");
            pack.cantidadPorCompra = 5;
            pack.descripcion = "Cinco cajas a precio de cuatro.";

            var lampara = Articulo("lampara", "Lámpara dorada", 3, Plantilla(plantillas, PrimitiveType.Capsule, new Color(1f, 0.85f, 0.2f), 0.4f), "Premium");
            lampara.moneda = "gemas";
            lampara.maxCompras = 1;
            lampara.descripcion = "Se paga con gemas y solo se puede comprar una vez.";

            var estatua = Articulo("estatua", "Estatua edición limitada", 120, Plantilla(plantillas, PrimitiveType.Cube, new Color(0.8f, 0.8f, 0.85f), 0.9f), "Premium");
            estatua.stockInicial = 2;
            estatua.descripcion = "Solo quedan dos unidades.";

            var botiquin = Articulo("botiquin", "Botiquín", 15, null, "Inventario");
            botiquin.modoEntrega = ModoEntrega.Inventario;
            botiquin.descripcion = "Va directo al inventario (capacidad 5). No aparece en el mundo.";

            var municion = Articulo("municion", "Caja de munición", 10, Plantilla(plantillas, PrimitiveType.Cube, new Color(0.3f, 0.5f, 0.25f), 0.3f), "Inventario");
            municion.modoEntrega = ModoEntrega.InventarioOAparecer;
            municion.descripcion = "Al inventario si cabe; si no, aparece en el suelo.";

            var botas = Articulo("botas", "Botas rápidas", 60, null, "Mejoras");
            botas.modoEntrega = ModoEntrega.SoloEvento;
            botas.maxCompras = 1;
            botas.descripcion = "Mejora sin objeto: corres un 60% más rápido.";

            cat.articulos.AddRange(new[] { caja, pelota, barril, pack, lampara, estatua, botiquin, municion, botas });
            return cat;
        }

        static ArticuloTienda Articulo(string id, string nombre, int precio, GameObject prefab, string categoria)
        {
            var a = ArticuloTienda.Crear(id, nombre, precio, prefab);
            a.categoria = categoria;
            return a;
        }

        static GameObject Plantilla(GameObject padre, PrimitiveType tipo, Color color, float tamano)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = tipo.ToString();
            go.transform.SetParent(padre.transform, false);
            go.transform.localScale = Vector3.one * tamano;
            FabricaTienda.Colorear(go, color);
            go.AddComponent<Rigidbody>().mass = 2f;
            return go;
        }

        // ------------------------------------------------------------ HUD

        void OnGUI()
        {
            if (estiloHud == null)
            {
                estiloHud = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
                estiloHud.normal.textColor = Color.white;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(tienda.Saldo("monedas")).Append(" monedas   ")
              .Append(tienda.Saldo("gemas")).Append(" gemas</b>\n");
            sb.Append("Inventario (").Append(inventario.Total).Append('/').Append(inventario.capacidad).Append("): ");
            if (inventario.objetos.Count == 0) sb.Append("vacío");
            foreach (var kv in inventario.objetos) sb.Append(kv.Key).Append(" x").Append(kv.Value).Append("  ");
            sb.Append("\n\n<color=#bbbbbb>WASD moverse · ratón mirar · E usar/comprar · Tab tienda · Esc cerrar\n" +
                      "Clic para capturar el ratón · U gasta un objeto del inventario</color>");
            GUI.Box(new Rect(10, 10, 560, 110), GUIContent.none);
            GUI.Label(new Rect(20, 16, 540, 100), sb.ToString(), estiloHud);

            if (!string.IsNullOrEmpty(ultimoMensaje) && Time.unscaledTime < finMensaje && !TiendaMenu.AlgunaAbierta)
            {
                var r = new Rect(Screen.width / 2f - 300, Screen.height * 0.25f, 600, 30);
                GUI.Box(r, ultimoMensaje);
            }

            // Mira
            if (!TiendaMenu.AlgunaAbierta)
                GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 12, 20, 24), "+", estiloHud);
        }

        void Update()
        {
            if (EntradaTienda.Pulsada(TeclaTienda.U)) inventario.UsarUno();
        }
    }
}
