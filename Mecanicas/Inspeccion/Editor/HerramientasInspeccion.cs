using System.IO;
using Mecanicas.Inspeccion.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using MaterialFisico = UnityEngine.PhysicsMaterial;
using CombinarFisico = UnityEngine.PhysicsMaterialCombine;
#else
using MaterialFisico = UnityEngine.PhysicMaterial;
using CombinarFisico = UnityEngine.PhysicMaterialCombine;
#endif

namespace Mecanicas.Inspeccion.Editor
{
    /// <summary>
    /// Menús del editor: crear la escena de demostración, montar el jugador y convertir objetos en agarrables.
    /// Todo está en Herramientas > Mecánicas > Inspección.
    /// </summary>
    public static class HerramientasInspeccion
    {
        const string Menu = "Herramientas/Mecánicas/Inspección/";

        // ───────────────────────────── Menús ─────────────────────────────

        [MenuItem(Menu + "Crear escena de demostración", priority = 0)]
        public static void CrearEscenaDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string carpetaDemo = CarpetaRaiz() + "/Demo";
            CrearCarpeta(carpetaDemo);
            CrearCarpeta(carpetaDemo + "/Materiales");

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mats = new Materiales(carpetaDemo + "/Materiales");

            // Luz y ambiente
            var luz = new GameObject("Luz direccional").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.2f;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);

            // Sala
            var sala = new GameObject("Sala").transform;
            Bloque("Suelo", sala, new Vector3(0f, -0.05f, 2f), new Vector3(14f, 0.1f, 14f), mats.Get("Suelo", new Color(0.55f, 0.55f, 0.52f)));
            var pared = mats.Get("Pared", new Color(0.82f, 0.8f, 0.75f));
            Bloque("Pared norte", sala, new Vector3(0f, 1.5f, 9f), new Vector3(14f, 3f, 0.2f), pared);
            Bloque("Pared sur", sala, new Vector3(0f, 1.5f, -5f), new Vector3(14f, 3f, 0.2f), pared);
            Bloque("Pared este", sala, new Vector3(7f, 1.5f, 2f), new Vector3(0.2f, 3f, 14f), pared);
            Bloque("Pared oeste", sala, new Vector3(-7f, 1.5f, 2f), new Vector3(0.2f, 3f, 14f), pared);

            // Mesa
            var madera = mats.Get("Madera", new Color(0.45f, 0.3f, 0.18f));
            var mesa = new GameObject("Mesa").transform;
            mesa.SetParent(sala);
            Bloque("Tablero", mesa, new Vector3(0f, 0.78f, 2f), new Vector3(2.2f, 0.06f, 1f), madera);
            foreach (var p in new[] { new Vector3(-1f, 0.375f, 1.6f), new Vector3(1f, 0.375f, 1.6f), new Vector3(-1f, 0.375f, 2.4f), new Vector3(1f, 0.375f, 2.4f) })
                Bloque("Pata", mesa, p, new Vector3(0.08f, 0.75f, 0.08f), madera);

            var objetos = new GameObject("Objetos agarrables").transform;

            // 1. Libro: muestra la rotación inicial de inspección y una descripción larga.
            var libro = Agarrable(PrimitiveType.Cube, "Libro antiguo", objetos, new Vector3(-0.7f, 0.83f, 2f), new Vector3(0.22f, 0.05f, 0.3f), mats.Get("Libro", new Color(0.5f, 0.12f, 0.1f)), 0.8f);
            libro.descripcion = "Un diario de tapas rojas. En el lomo pone «1923».\nAl inspeccionarlo aparece siempre de frente gracias a «Rotación inicial de inspección».";
            libro.usarRotacionInicialInspeccion = true;
            libro.rotacionInicialInspeccion = new Vector3(-90f, 0f, 0f);

            // 2. Taza
            var taza = Agarrable(PrimitiveType.Cylinder, "Taza", objetos, new Vector3(-0.2f, 0.87f, 2.1f), new Vector3(0.09f, 0.06f, 0.09f), mats.Get("Ceramica", new Color(0.9f, 0.9f, 0.95f)), 0.3f);
            taza.descripcion = "Una taza de cerámica. Prueba a lanzarla contra la torre de cajas.";

            // 3. Manzana
            var manzana = Agarrable(PrimitiveType.Sphere, "Manzana", objetos, new Vector3(0.25f, 0.87f, 1.9f), Vector3.one * 0.09f, mats.Get("Manzana", new Color(0.75f, 0.1f, 0.08f)), 0.2f);
            manzana.descripcion = "Roja y brillante.";

            // 4. Llave: objeto pequeño, se inspecciona muy de cerca.
            var llave = Agarrable(PrimitiveType.Cube, "Llave del sótano", objetos, new Vector3(0.65f, 0.82f, 2.15f), new Vector3(0.12f, 0.015f, 0.035f), mats.Get("Laton", new Color(0.8f, 0.65f, 0.2f)), 0.1f);
            llave.descripcion = "Pequeña y de latón. Tiene grabada una «S».";
            llave.distanciaInspeccion = 0.3f;

            // 5. Estatuilla compuesta: Rigidbody en el padre, colliders en los hijos.
            var estatuilla = new GameObject("Estatuilla");
            estatuilla.transform.SetParent(objetos);
            estatuilla.transform.position = new Vector3(0.95f, 0.81f, 1.8f);
            var piedra = mats.Get("Piedra", new Color(0.6f, 0.62f, 0.65f));
            Hijo(PrimitiveType.Cylinder, "Base", estatuilla.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.02f, 0.12f), piedra);
            Hijo(PrimitiveType.Capsule, "Cuerpo", estatuilla.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.06f, 0.08f, 0.06f), piedra);
            Hijo(PrimitiveType.Sphere, "Cabeza", estatuilla.transform, new Vector3(0f, 0.24f, 0f), Vector3.one * 0.07f, piedra);
            var rbEst = estatuilla.AddComponent<Rigidbody>();
            rbEst.mass = 1.5f;
            var est = estatuilla.AddComponent<ObjetoAgarrable>();
            est.nombre = "Estatuilla";
            est.descripcion = "Hecha de varias piezas: el Rigidbody y ObjetoAgarrable van en el padre y los colliders en los hijos.";

            // 6. Pelota que rebota
            var pelota = Agarrable(PrimitiveType.Sphere, "Pelota", objetos, new Vector3(-2f, 0.15f, 0.5f), Vector3.one * 0.25f, mats.Get("Pelota", new Color(0.15f, 0.45f, 0.9f)), 0.4f);
            pelota.GetComponent<Collider>().sharedMaterial = MaterialFisicoRebote(carpetaDemo + "/Materiales");
            pelota.descripcion = "Rebota. Su multiplicador de lanzamiento es 1,5.";
            pelota.multiplicadorLanzamiento = 1.5f;

            // 7. Caja pesada: supera la masa máxima, solo se puede inspeccionar.
            var pesada = Agarrable(PrimitiveType.Cube, "Caja fuerte", objetos, new Vector3(2.5f, 0.4f, 0.5f), Vector3.one * 0.8f, mats.Get("Metal", new Color(0.25f, 0.27f, 0.3f)), 120f);
            pesada.descripcion = "Pesa 120 kg (más que la masa máxima del jugador). Se puede inspeccionar pero no coger.";

            // 8. Cuadro de la pared: sin Rigidbody, solo inspección y solo gira en horizontal.
            var cuadro = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cuadro.name = "Cuadro";
            cuadro.transform.SetParent(objetos);
            cuadro.transform.position = new Vector3(-3f, 1.6f, 8.88f);
            cuadro.transform.localScale = new Vector3(0.8f, 0.6f, 0.04f);
            cuadro.GetComponent<Renderer>().sharedMaterial = mats.Get("Cuadro", new Color(0.2f, 0.45f, 0.3f));
            var cuadroObj = cuadro.AddComponent<ObjetoAgarrable>();
            cuadroObj.descripcion = "No tiene Rigidbody: solo se puede mirar de cerca y vuelve a la pared al terminar. Solo gira en horizontal.";
            cuadroObj.sePuedeAgarrar = false;
            cuadroObj.sePuedeLanzar = false;
            cuadroObj.rotarVertical = false;
            cuadroObj.usarRotacionInicialInspeccion = true;
            cuadroObj.rotacionInicialInspeccion = Vector3.zero;

            // 9. Botella que no se puede lanzar
            var botella = Agarrable(PrimitiveType.Capsule, "Botella de ácido", objetos, new Vector3(-1.6f, 0.15f, 3.5f), new Vector3(0.1f, 0.15f, 0.1f), mats.Get("Vidrio", new Color(0.3f, 0.8f, 0.4f)), 0.6f);
            botella.descripcion = "Mejor no lanzarla: «Se Puede Lanzar» está desactivado.";
            botella.sePuedeLanzar = false;

            // Torre de cajas para lanzar cosas
            var torre = new GameObject("Torre de cajas").transform;
            var carton = mats.Get("Carton", new Color(0.7f, 0.55f, 0.35f));
            int n = 0;
            for (int fila = 0; fila < 4; fila++)
                for (int i = 0; i < 4 - fila; i++)
                {
                    float x = 3.5f + (i - (3 - fila) / 2f) * 0.42f;
                    var caja = Agarrable(PrimitiveType.Cube, "Caja " + (++n), torre, new Vector3(x, 0.2f + fila * 0.4f, 6f), Vector3.one * 0.4f, carton, 2f);
                    caja.descripcion = "Una caja de cartón vacía.";
                }

            // Jugador
            var jugador = CrearJugador(new Vector3(0f, 0f, -1.5f));
            jugador.transform.rotation = Quaternion.identity;

            string ruta = carpetaDemo + "/DemoInspeccion.unity";
            EditorSceneManager.SaveScene(escena, ruta);
            AssetDatabase.SaveAssets();
            Debug.Log("[Inspección] Escena de demostración creada en " + ruta + ". Dale a Play.");
        }

        [MenuItem(Menu + "Añadir jugador de ejemplo a la escena", priority = 20)]
        public static void MenuAnadirJugador()
        {
            var j = CrearJugador(Vector3.zero);
            Selection.activeGameObject = j;
            EditorSceneManager.MarkSceneDirty(j.scene);
        }

        [MenuItem(Menu + "Añadir agarrador a la cámara seleccionada", priority = 21)]
        public static void MenuConfigurarCamara()
        {
            var go = Selection.activeGameObject;
            var cam = go != null ? go.GetComponentInChildren<Camera>() : null;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Inspección", "Selecciona la cámara de tu jugador (o un objeto que la contenga).", "Vale");
                return;
            }
            ConfigurarCamara(cam.gameObject, null);
            Selection.activeGameObject = cam.gameObject;
            EditorUtility.DisplayDialog("Inspección",
                "Listo. Arrastra el script de tu controlador de jugador (y el de tu cámara, si es otro) a «Componentes A Bloquear» del Agarrador para que la vista se quede quieta al inspeccionar.",
                "Vale");
        }

        [MenuItem(Menu + "Hacer agarrables los objetos seleccionados", priority = 22)]
        public static void MenuHacerAgarrables()
        {
            int cuenta = 0;
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<ObjetoAgarrable>() != null) continue;
                Undo.RegisterFullObjectHierarchyUndo(go, "Hacer agarrable");
                if (go.GetComponentInChildren<Collider>() == null)
                {
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var mc = Undo.AddComponent<MeshCollider>(go);
                        mc.convex = true;
                    }
                    else Undo.AddComponent<BoxCollider>(go);
                }
                if (go.GetComponent<Rigidbody>() == null) Undo.AddComponent<Rigidbody>(go);
                Undo.AddComponent<ObjetoAgarrable>(go);
                cuenta++;
            }
            Debug.Log("[Inspección] " + cuenta + " objeto(s) convertidos en agarrables.");
        }

        [MenuItem(Menu + "Hacer agarrables los objetos seleccionados", true)]
        static bool ValidarHacerAgarrables() => Selection.gameObjects.Length > 0;

        // ───────────────────────────── Montaje ─────────────────────────────

        static GameObject CrearJugador(Vector3 posicion)
        {
            var jugador = new GameObject("Jugador");
            jugador.transform.position = posicion;
            var cc = jugador.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var camGo = new GameObject("Cámara");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(jugador.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();

            var controlador = jugador.AddComponent<ControladorJugadorDemo>();
            controlador.camara = camGo.transform;

            ConfigurarCamara(camGo, controlador);
            Undo.RegisterCreatedObjectUndo(jugador, "Crear jugador");
            return jugador;
        }

        static void ConfigurarCamara(GameObject camGo, Behaviour controlador)
        {
            if (camGo.GetComponent<EntradaInspeccion>() == null) Undo.AddComponent<EntradaInspeccion>(camGo);
            var agarrador = camGo.GetComponent<AgarradorPrimeraPersona>();
            if (agarrador == null) agarrador = Undo.AddComponent<AgarradorPrimeraPersona>(camGo);
            agarrador.camara = camGo.GetComponent<Camera>();
            agarrador.entrada = camGo.GetComponent<EntradaInspeccion>();
            agarrador.raizJugador = camGo.transform.root;
            if (controlador != null) agarrador.componentesABloquear = new[] { controlador };
            var hud = camGo.GetComponent<InterfazInspeccion>();
            if (hud == null) hud = Undo.AddComponent<InterfazInspeccion>(camGo);
            hud.agarrador = agarrador;
        }

        static ObjetoAgarrable Agarrable(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat, float masa)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre);
            go.transform.position = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (tipo == PrimitiveType.Cylinder)
            {
                // El collider por defecto del cilindro es una cápsula: lo cambiamos por uno convexo que no rueda raro.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<MeshCollider>().convex = true;
            }
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = masa;
            var obj = go.AddComponent<ObjetoAgarrable>();
            obj.nombre = nombre;
            return obj;
        }

        static void Bloque(string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.isStatic = true;
            go.transform.SetParent(padre);
            go.transform.position = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void Hijo(PrimitiveType tipo, string nombre, Transform padre, Vector3 posLocal, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = posLocal;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // ───────────────────────────── Recursos ─────────────────────────────

        /// <summary>Carpeta donde está instalada la mecánica (la que contiene Runtime y Editor).</summary>
        static string CarpetaRaiz()
        {
            foreach (var guid in AssetDatabase.FindAssets("HerramientasInspeccion t:MonoScript"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!ruta.EndsWith("/HerramientasInspeccion.cs")) continue;
                return Path.GetDirectoryName(Path.GetDirectoryName(ruta)).Replace('\\', '/');
            }
            return "Assets/Mecanicas/Inspeccion";
        }

        static void CrearCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;
            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            CrearCarpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }

        static MaterialFisico MaterialFisicoRebote(string carpeta)
        {
            string ruta = carpeta + "/Rebote.asset";
            var mat = AssetDatabase.LoadAssetAtPath<MaterialFisico>(ruta);
            if (mat != null) return mat;
            mat = new MaterialFisico("Rebote") { bounciness = 0.8f, bounceCombine = CombinarFisico.Maximum };
            AssetDatabase.CreateAsset(mat, ruta);
            return mat;
        }

        /// <summary>Crea (o reutiliza) materiales de color compatibles con URP, HDRP o Built-in.</summary>
        class Materiales
        {
            readonly string _carpeta;
            readonly Shader _shader;

            public Materiales(string carpeta)
            {
                _carpeta = carpeta;
                var rp = GraphicsSettings.currentRenderPipeline;
                string nombre = rp == null ? "Standard"
                    : rp.GetType().Name.Contains("HD") ? "HDRP/Lit" : "Universal Render Pipeline/Lit";
                _shader = Shader.Find(nombre) ?? Shader.Find("Standard");
            }

            public Material Get(string nombre, Color color)
            {
                string ruta = _carpeta + "/" + nombre + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (mat == null)
                {
                    mat = new Material(_shader) { name = nombre };
                    AssetDatabase.CreateAsset(mat, ruta);
                }
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                EditorUtility.SetDirty(mat);
                return mat;
            }
        }
    }
}
