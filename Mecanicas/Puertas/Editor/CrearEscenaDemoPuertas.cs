using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Mecanicas.Puertas.EditorTools
{
    /// <summary>
    /// Genera la escena de demostración con todos los mecanismos montados con cubos.
    /// Menú: Herramientas > Mecánicas > Puertas > Crear escena demo.
    /// La escena y sus materiales se guardan en la carpeta Demo del asset.
    /// </summary>
    public static class CrearEscenaDemoPuertas
    {
        const float AltoMuro = 3.2f;
        const float GrosorMuro = 0.15f;
        const float AnchoPasillo = 7f;   // el pasillo va de x = -7 a x = 7
        const float AltoPuerta = 2.1f;

        static string carpetaDemo;
        static Material mSuelo, mPared, mMadera, mMetal, mRojo, mAzul, mAmarillo, mVerde, mNegro, mLampara, mLed, mTecho;
        static Font fuente;

        [MenuItem("Herramientas/Mecánicas/Puertas/Crear escena demo")]
        public static void Crear()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            carpetaDemo = CarpetaDemo();
            Directory.CreateDirectory(Path.Combine(carpetaDemo, "Materiales"));
            AssetDatabase.Refresh();
            CrearMateriales();
            fuente = CargarFuente();

            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var luz = new GameObject("Sol").AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.33f, 0.36f);

            var raiz = new GameObject("Escenario").transform;
            ConstruirPasillo(raiz);

            // Lado izquierdo (x = -7)
            EstacionLlave(raiz, 5f);
            EstacionCandado(raiz, 12f);
            EstacionCombinacion(raiz, 19f);
            EstacionPanel(raiz, 26f);
            EstacionPestillo(raiz, 33f);
            // Lado derecho (x = +7)
            EstacionBoton(raiz, 5f);
            EstacionPalancas(raiz, 12f);
            EstacionLuces(raiz, 19f);
            EstacionPuertaDoble(raiz, 26f);
            EstacionArmario(raiz, 33f);

            CrearJugador();

            string ruta = Path.Combine(carpetaDemo, "DemoPuertas.unity").Replace('\\', '/');
            EditorSceneManager.SaveScene(escena, ruta);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mecánicas/Puertas] Escena demo creada en " + ruta + ". Pulsa Play para probarla.");
        }

        // ------------------------------------------------------------------ Utilidades

        static string CarpetaDemo()
        {
            string[] guids = AssetDatabase.FindAssets("CrearEscenaDemoPuertas t:Script");
            if (guids.Length > 0)
            {
                string script = AssetDatabase.GUIDToAssetPath(guids[0]);          // .../Puertas/Editor/CrearEscenaDemoPuertas.cs
                string raiz = Path.GetDirectoryName(Path.GetDirectoryName(script)); // .../Puertas
                return Path.Combine(raiz, "Demo").Replace('\\', '/');
            }
            return "Assets/Mecanicas/Puertas/Demo";
        }

        static Font CargarFuente()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        static Shader ShaderBase()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null && rp.defaultMaterial != null) return rp.defaultMaterial.shader;
            return Shader.Find("Standard");
        }

        static Material Mat(string nombre, Color color, float emision = 0f)
        {
            string ruta = carpetaDemo + "/Materiales/" + nombre + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(ShaderBase());
                AssetDatabase.CreateAsset(m, ruta);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (emision > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", color * emision);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static void CrearMateriales()
        {
            mSuelo = Mat("Suelo", new Color(0.36f, 0.36f, 0.38f));
            mPared = Mat("Pared", new Color(0.78f, 0.76f, 0.7f));
            mTecho = Mat("Techo", new Color(0.6f, 0.6f, 0.6f));
            mMadera = Mat("Madera", new Color(0.45f, 0.28f, 0.15f));
            mMetal = Mat("Metal", new Color(0.55f, 0.57f, 0.6f));
            mRojo = Mat("Rojo", new Color(0.85f, 0.12f, 0.1f), 0.3f);
            mAzul = Mat("Azul", new Color(0.12f, 0.35f, 0.9f), 0.3f);
            mAmarillo = Mat("Amarillo", new Color(0.95f, 0.8f, 0.2f));
            mVerde = Mat("Verde", new Color(0.2f, 0.7f, 0.3f));
            mNegro = Mat("Negro", new Color(0.06f, 0.06f, 0.07f));
            mLampara = Mat("Lampara", new Color(1f, 0.95f, 0.8f), 0.01f);
            mLed = Mat("Led", new Color(1f, 0.6f, 0.1f), 1f);
        }

        static GameObject Cubo(string nombre, Transform padre, Vector3 posLocal, Vector3 escala, Material m, bool collider = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = nombre;
            g.transform.SetParent(padre, false);
            g.transform.localPosition = posLocal;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 posLocal, Vector3 escala, Quaternion rot, Material m, bool collider = true)
        {
            var g = GameObject.CreatePrimitive(tipo);
            g.name = nombre;
            g.transform.SetParent(padre, false);
            g.transform.localPosition = posLocal;
            g.transform.localRotation = rot;
            g.transform.localScale = escala;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static Transform Vacio(string nombre, Transform padre, Vector3 posLocal, Quaternion rotLocal)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            t.localPosition = posLocal;
            t.localRotation = rotLocal;
            return t;
        }

        static TextMesh Texto(string nombre, Transform padre, Vector3 posLocal, Quaternion rot, string texto, float tamano, Color color, TextAnchor ancla = TextAnchor.MiddleCenter)
        {
            var t = Vacio(nombre, padre, posLocal, rot);
            var tm = t.gameObject.AddComponent<TextMesh>();
            tm.text = texto;
            tm.font = fuente;
            tm.fontSize = 64;
            tm.characterSize = tamano;
            tm.anchor = ancla;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            if (fuente != null) t.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
            return tm;
        }

        /// <summary>Asigna un campo serializado (también privado) por su nombre.</summary>
        static void Set(Object o, string campo, object valor)
        {
            var so = new SerializedObject(o);
            var p = so.FindProperty(campo);
            if (p == null) { Debug.LogError("Campo no encontrado: " + o.GetType().Name + "." + campo); return; }
            if (valor is bool) p.boolValue = (bool)valor;
            else if (valor is int) p.intValue = (int)valor;
            else if (valor is float) p.floatValue = (float)valor;
            else if (valor is string) p.stringValue = (string)valor;
            else if (valor is Vector3) p.vector3Value = (Vector3)valor;
            else if (valor is Color) p.colorValue = (Color)valor;
            else if (valor is Enum) p.enumValueIndex = Convert.ToInt32(valor);
            else p.objectReferenceValue = valor as Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AnadirALista(Object o, string campo, Object elemento)
        {
            var so = new SerializedObject(o);
            var p = so.FindProperty(campo);
            int n = p.arraySize;
            p.arraySize = n + 1;
            p.GetArrayElementAtIndex(n).objectReferenceValue = elemento;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ Geometría

        static readonly float[] Huecos = { 5f, 12f, 19f, 26f, 33f };
        const float LargoPasillo = 38f;

        static void ConstruirPasillo(Transform raiz)
        {
            Cubo("Suelo", raiz, new Vector3(0f, -0.05f, LargoPasillo * 0.5f), new Vector3(AnchoPasillo * 2f + 8f, 0.1f, LargoPasillo + 2f), mSuelo);
            MuroConHuecos(raiz, "MuroIzquierdo", -AnchoPasillo, Huecos, new[] { 1f, 1f, 1f, 1f, 1f });
            MuroConHuecos(raiz, "MuroDerecho", AnchoPasillo, Huecos, new[] { 1f, 1f, 1f, 1.2f, 1f });
            Cubo("MuroFondo", raiz, new Vector3(0f, AltoMuro * 0.5f, LargoPasillo), new Vector3(AnchoPasillo * 2f, AltoMuro, GrosorMuro), mPared);
            Cubo("MuroInicio", raiz, new Vector3(0f, AltoMuro * 0.5f, 0f), new Vector3(AnchoPasillo * 2f, AltoMuro, GrosorMuro), mPared);

            foreach (float z in Huecos)
            {
                Habitacion(raiz, -1f, z, z == 19f);
                Habitacion(raiz, 1f, z, z == 19f);
            }

            Texto("Bienvenida", raiz, new Vector3(0f, 2.3f, 4.5f), Quaternion.identity,
                "MECÁNICAS: PUERTAS, LLAVES E INTERRUPTORES\nWASD moverse · Ratón mirar · E o clic usar\nMantén pulsado y arrastra: puertas, palancas y pestillos", 0.035f, Color.black);
        }

        /// <summary>Muro a lo largo de Z en la x indicada, con huecos de puerta centrados en "centros".</summary>
        static void MuroConHuecos(Transform raiz, string nombre, float x, float[] centros, float[] anchos)
        {
            var padre = Vacio(nombre, raiz, Vector3.zero, Quaternion.identity);
            float z0 = 0f;
            for (int i = 0; i < centros.Length; i++)
            {
                float a = centros[i] - anchos[i] * 0.5f, b = centros[i] + anchos[i] * 0.5f;
                Cubo("Tramo", padre, new Vector3(x, AltoMuro * 0.5f, (z0 + a) * 0.5f), new Vector3(GrosorMuro, AltoMuro, a - z0), mPared);
                Cubo("Dintel", padre, new Vector3(x, (AltoPuerta + AltoMuro) * 0.5f, centros[i]), new Vector3(GrosorMuro, AltoMuro - AltoPuerta, anchos[i]), mPared);
                z0 = b;
            }
            Cubo("Tramo", padre, new Vector3(x, AltoMuro * 0.5f, (z0 + LargoPasillo) * 0.5f), new Vector3(GrosorMuro, AltoMuro, LargoPasillo - z0), mPared);
        }

        static void Habitacion(Transform raiz, float lado, float z, bool conTecho)
        {
            float fondo = 3f, ancho = 5f;
            var h = Vacio("Habitacion_" + (lado < 0 ? "I" : "D") + z, raiz, new Vector3(lado * (AnchoPasillo + fondo * 0.5f), 0f, z), Quaternion.identity);
            Cubo("Fondo", h, new Vector3(lado * fondo * 0.5f, AltoMuro * 0.5f, 0f), new Vector3(GrosorMuro, AltoMuro, ancho), mPared);
            Cubo("Lateral", h, new Vector3(0f, AltoMuro * 0.5f, ancho * 0.5f), new Vector3(fondo, AltoMuro, GrosorMuro), mPared);
            Cubo("Lateral", h, new Vector3(0f, AltoMuro * 0.5f, -ancho * 0.5f), new Vector3(fondo, AltoMuro, GrosorMuro), mPared);
            if (conTecho) Cubo("Techo", h, new Vector3(0f, AltoMuro + 0.05f, 0f), new Vector3(fondo + 0.3f, 0.1f, ancho + 0.3f), mTecho);
        }

        /// <summary>Rotación del marco: su -Z local mira al pasillo y su +X recorre el hueco.</summary>
        static Quaternion RotMarco(float lado) => Quaternion.Euler(0f, lado < 0 ? -90f : 90f, 0f);

        /// <summary>Cartel encima de la puerta, legible desde el pasillo.</summary>
        static void Cartel(Transform raiz, float lado, float z, string texto)
        {
            float x = lado * (AnchoPasillo - GrosorMuro * 0.5f - 0.01f);
            Texto("Cartel", raiz, new Vector3(x, 2.65f, z), RotMarco(lado), texto, 0.022f, Color.black);
        }

        /// <summary>Crea una puerta de bisagra: el pivote es la bisagra y la hoja se extiende hacia +X local.</summary>
        static Puerta PuertaBisagra(Transform raiz, string nombre, float lado, float z, float ancho, Material m, float angulo)
        {
            float x = lado * AnchoPasillo;
            // Con la rotación del marco, +X local recorre el hueco hacia +Z (izquierda) o hacia -Z (derecha).
            float zBisagra = lado < 0 ? z - ancho * 0.5f : z + ancho * 0.5f;
            var pivote = Vacio(nombre, raiz, new Vector3(x, 0f, zBisagra), RotMarco(lado));
            Cubo("Hoja", pivote, new Vector3(ancho * 0.5f, AltoPuerta * 0.5f - 0.01f, 0f), new Vector3(ancho - 0.02f, AltoPuerta - 0.03f, 0.05f), m);
            Cubo("Pomo", pivote, new Vector3(ancho - 0.1f, 1.0f, -0.04f), new Vector3(0.03f, 0.03f, 0.05f), mMetal);
            Cubo("Pomo", pivote, new Vector3(ancho - 0.1f, 1.0f, 0.04f), new Vector3(0.03f, 0.03f, 0.05f), mMetal);
            var p = pivote.gameObject.AddComponent<Puerta>();
            Set(p, "textoAccion", "Abrir");
            Set(p, "anguloApertura", angulo);
            return p;
        }

        static void Nota(Transform raiz, Vector3 pos, string mensaje)
        {
            Cubo("Mesa", raiz, pos + new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.6f), mMadera);
            var nota = Cubo("Nota", raiz, pos + new Vector3(0f, 0.81f, 0f), new Vector3(0.2f, 0.01f, 0.28f), mAmarillo);
            var i = nota.AddComponent<InteractuableSimple>();
            Set(i, "textoAccion", "Leer nota");
            Set(i, "mensaje", mensaje);
            Set(i, "duracionMensaje", 4f);
        }

        static Llave LlaveEn(Transform raiz, Vector3 pos, string id, string nombre, Material m)
        {
            var t = Vacio("Llave_" + id, raiz, pos, Quaternion.Euler(0f, 30f, 0f));
            Cubo("Cabeza", t, new Vector3(-0.05f, 0f, 0f), new Vector3(0.05f, 0.012f, 0.05f), m);
            Cubo("Cana", t, new Vector3(0.02f, 0f, 0f), new Vector3(0.1f, 0.01f, 0.015f), m);
            Cubo("Diente", t, new Vector3(0.06f, 0f, 0.015f), new Vector3(0.015f, 0.01f, 0.02f), m);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.2f, 0.06f, 0.1f);
            foreach (var c in t.GetComponentsInChildren<BoxCollider>()) if (c != col) Object.DestroyImmediate(c);
            var ll = t.gameObject.AddComponent<Llave>();
            Set(ll, "idLlave", id);
            Set(ll, "nombreVisible", nombre);
            return ll;
        }

        // ------------------------------------------------------------------ Estaciones

        static void EstacionLlave(Transform raiz, float z)
        {
            Cartel(raiz, -1f, z, "1. PUERTA CON LLAVE\nCoge la llave roja de la mesa");
            var p = PuertaBisagra(raiz, "Puerta_Llave", -1f, z, 1f, mRojo, -95f);
            var c = p.gameObject.AddComponent<CerraduraLlave>();
            Set(c, "idLlave", "llave_roja");
            Set(c, "nombreLlave", "la llave roja");
            Cubo("Mesa", raiz, new Vector3(-5.5f, 0.4f, z - 2f), new Vector3(0.8f, 0.8f, 0.6f), mMadera);
            LlaveEn(raiz, new Vector3(-5.5f, 0.82f, z - 2f), "llave_roja", "la llave roja", mRojo);
            // Dentro: la llave azul para el candado.
            Cubo("Mesa", raiz, new Vector3(-9f, 0.4f, z), new Vector3(0.8f, 0.8f, 0.6f), mMadera);
            LlaveEn(raiz, new Vector3(-9f, 0.82f, z), "llave_azul", "la llave azul", mAzul);
        }

        static void EstacionCandado(Transform raiz, float z)
        {
            Cartel(raiz, -1f, z, "2. CANDADO CON LLAVE\nLa llave azul está tras la puerta roja");
            var p = PuertaBisagra(raiz, "Puerta_Candado", -1f, z, 1f, mMadera, -95f);
            Transform pv = p.transform;
            Cubo("Aldaba", pv, new Vector3(0.85f, 1.15f, -0.035f), new Vector3(0.12f, 0.04f, 0.01f), mMetal, false);
            var cand = Vacio("Candado", pv, new Vector3(0.88f, 1.08f, -0.06f), Quaternion.identity);
            Cubo("Cuerpo", cand, Vector3.zero, new Vector3(0.06f, 0.06f, 0.025f), mAzul);
            var arco = Vacio("Arco", cand, new Vector3(0f, 0.03f, 0f), Quaternion.identity);
            Cubo("Lado", arco, new Vector3(-0.02f, 0.025f, 0f), new Vector3(0.008f, 0.05f, 0.008f), mMetal, false);
            Cubo("Lado", arco, new Vector3(0.02f, 0.025f, 0f), new Vector3(0.008f, 0.05f, 0.008f), mMetal, false);
            Cubo("Arriba", arco, new Vector3(0f, 0.05f, 0f), new Vector3(0.048f, 0.008f, 0.008f), mMetal, false);
            var c = cand.gameObject.AddComponent<Candado>();
            Set(c, "textoAccion", "Candado");
            Set(c, "arco", arco);
            Set(c, "idLlave", "llave_azul");
            Set(c, "nombreLlave", "la llave azul");
        }

        static void EstacionCombinacion(Transform raiz, float z)
        {
            Cartel(raiz, -1f, z, "3. CANDADO DE COMBINACIÓN\nClic o rueda del ratón en cada número");
            var p = PuertaBisagra(raiz, "Puerta_Combinacion", -1f, z, 1f, mMadera, -95f);
            Transform pv = p.transform;
            Cubo("Aldaba", pv, new Vector3(0.85f, 1.17f, -0.035f), new Vector3(0.12f, 0.04f, 0.01f), mMetal, false);
            var cand = Vacio("CandadoCombinacion", pv, new Vector3(0.86f, 1.08f, -0.065f), Quaternion.identity);
            Cubo("Cuerpo", cand, Vector3.zero, new Vector3(0.1f, 0.07f, 0.03f), mNegro);
            var arco = Vacio("Arco", cand, new Vector3(0f, 0.035f, 0f), Quaternion.identity);
            Cubo("Lado", arco, new Vector3(-0.03f, 0.025f, 0f), new Vector3(0.008f, 0.05f, 0.008f), mMetal, false);
            Cubo("Lado", arco, new Vector3(0.03f, 0.025f, 0f), new Vector3(0.008f, 0.05f, 0.008f), mMetal, false);
            Cubo("Arriba", arco, new Vector3(0f, 0.05f, 0f), new Vector3(0.068f, 0.008f, 0.008f), mMetal, false);
            var c = cand.gameObject.AddComponent<CandadoCombinacion>();
            Set(c, "textoAccion", "Tirar del candado");
            Set(c, "arco", arco);
            Set(c, "combinacion", "372");
            for (int i = 0; i < 3; i++)
            {
                float x = -0.03f + i * 0.03f;
                var rueda = Primitiva(PrimitiveType.Cylinder, "Rueda" + (i + 1), cand, new Vector3(x, 0f, -0.018f),
                    new Vector3(0.026f, 0.012f, 0.026f), Quaternion.Euler(0f, 0f, 90f), mMetal);
                var r = rueda.AddComponent<RuedaCombinacion>();
                Set(r, "textoAccion", "Girar rueda " + (i + 1));
                Set(r, "ejeLocal", Vector3.up);
                var tm = Texto("Numero" + (i + 1), cand, new Vector3(x, 0f, -0.032f), Quaternion.identity, "0", 0.0022f, Color.white);
                Set(r, "etiqueta", tm);
                AnadirALista(c, "ruedas", r);
            }
            Nota(raiz, new Vector3(-5.5f, 0f, z - 2f), "Nota: \"La combinación del trastero es 3 - 7 - 2\".");
        }

        static void EstacionPanel(Transform raiz, float z)
        {
            Cartel(raiz, -1f, z, "4. PANEL NUMÉRICO\nEl código está en la nota");
            var p = PuertaBisagra(raiz, "Puerta_Panel", -1f, z, 1f, mMetal, -95f);

            float x = -AnchoPasillo + GrosorMuro * 0.5f;
            var panelT = Vacio("PanelNumerico", raiz, new Vector3(x - 0.0f, 1.35f, z - 0.95f), RotMarco(-1f));
            Cubo("Carcasa", panelT, new Vector3(0f, 0f, -0.015f), new Vector3(0.2f, 0.32f, 0.03f), mNegro);
            var pantalla = Texto("Pantalla", panelT, new Vector3(0f, 0.115f, -0.032f), Quaternion.identity, "----", 0.0045f, new Color(0.3f, 1f, 0.4f));
            var led = Cubo("Led", panelT, new Vector3(0.075f, 0.14f, -0.032f), new Vector3(0.015f, 0.015f, 0.005f), mLed, false);
            var panel = panelT.gameObject.AddComponent<PanelNumerico>();
            Set(panel, "codigo", "4321");
            Set(panel, "pantalla", pantalla);
            Set(panel, "luzEstado", led.GetComponent<Renderer>());
            Set(panel, "intentosMaximos", 3);
            Set(panel, "segundosBloqueo", 5f);

            string[] etiquetas = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "C", "0", "OK" };
            for (int i = 0; i < 12; i++)
            {
                int fila = i / 3, col = i % 3;
                var tt = Vacio("Tecla_" + etiquetas[i], panelT, new Vector3(-0.055f + col * 0.055f, 0.05f - fila * 0.055f, -0.035f), Quaternion.identity);
                Cubo("Visual", tt, Vector3.zero, new Vector3(0.045f, 0.045f, 0.012f), etiquetas[i] == "OK" ? mVerde : (etiquetas[i] == "C" ? mRojo : mMetal), false);
                var col2 = tt.gameObject.AddComponent<BoxCollider>();
                col2.size = new Vector3(0.045f, 0.045f, 0.012f);
                Texto("Etiqueta", tt, new Vector3(0f, 0f, -0.007f), Quaternion.identity, etiquetas[i], 0.0038f, Color.black);
                var tecla = tt.gameObject.AddComponent<TeclaPanel>();
                TipoTecla tipo = etiquetas[i] == "C" ? TipoTecla.Borrar : etiquetas[i] == "OK" ? TipoTecla.Aceptar : TipoTecla.Digito;
                int d = tipo == TipoTecla.Digito ? int.Parse(etiquetas[i]) : 0;
                Set(tecla, "tipo", tipo);
                Set(tecla, "digito", d);
                Set(tecla, "panel", panel);
            }
            AnadirALista(p, "bloqueos", panel);
            Nota(raiz, new Vector3(-5.5f, 0f, z - 2f), "Nota: \"Código de la sala de máquinas: 4321\". (3 fallos bloquean el panel 5 s)");
        }

        static void EstacionPestillo(Transform raiz, float z)
        {
            Cartel(raiz, -1f, z, "5. PESTILLO CORREDERO (puerta de vaivén)\nMantén pulsado: sube la manilla, desliza y bájala");
            var p = PuertaBisagra(raiz, "Puerta_Pestillo", -1f, z, 1f, mMadera, 95f);
            Set(p, "sentido", SentidoApertura.AlejandoseDelJugador);
            Transform pv = p.transform;

            var grupo = Vacio("Pestillo", pv, new Vector3(0.8f, 1.1f, -0.03f), Quaternion.identity);
            Cubo("Placa", grupo, new Vector3(0.02f, 0f, 0f), new Vector3(0.2f, 0.05f, 0.008f), mMetal);
            Cubo("Guia", grupo, new Vector3(0.02f, 0f, -0.01f), new Vector3(0.16f, 0.03f, 0.012f), mMetal, false);
            var pasador = Vacio("Pasador", grupo, new Vector3(0f, 0f, -0.02f), Quaternion.identity);
            Primitiva(PrimitiveType.Cylinder, "Barra", pasador, Vector3.zero, new Vector3(0.014f, 0.07f, 0.014f), Quaternion.Euler(0f, 0f, 90f), mMetal);
            // Manilla colgando hacia abajo: al girar +90º alrededor de X apunta hacia el jugador.
            Cubo("Manilla", pasador, new Vector3(-0.03f, -0.022f, 0f), new Vector3(0.012f, 0.045f, 0.012f), mNegro);
            Primitiva(PrimitiveType.Sphere, "Bola", pasador, new Vector3(-0.03f, -0.048f, 0f), Vector3.one * 0.02f, Quaternion.identity, mNegro);
            var pest = grupo.gameObject.AddComponent<PestilloCorredero>();
            Set(pest, "pasador", pasador);
            Set(pest, "ejeLocal", Vector3.right);
            Set(pest, "recorrido", 0.15f);
            Set(pest, "anguloManilla", 90f);
            Set(pest, "empiezaEchado", true);
            Set(pest, "puerta", p);
            // Cerradero en el marco (el muro) donde entra el pasador.
            Cubo("Cerradero", raiz, new Vector3(-AnchoPasillo + GrosorMuro * 0.5f + 0.004f, 1.1f, z + 0.56f), new Vector3(0.008f, 0.05f, 0.08f), mMetal, false);
        }

        static void EstacionBoton(Transform raiz, float z)
        {
            Cartel(raiz, 1f, z, "6. PUERTA CORREDERA CON BOTÓN\nSe cierra sola a los 4 segundos");
            float x = AnchoPasillo;
            var pivote = Vacio("Puerta_Corredera", raiz, new Vector3(x, 0f, z + 0.5f), RotMarco(1f));
            Cubo("Hoja", pivote, new Vector3(0.5f, AltoPuerta * 0.5f - 0.01f, 0f), new Vector3(0.98f, AltoPuerta - 0.03f, 0.05f), mMetal);
            Cubo("Franja", pivote, new Vector3(0.5f, 1.2f, -0.03f), new Vector3(0.98f, 0.08f, 0.01f), mAmarillo, false);
            var p = pivote.gameObject.AddComponent<Puerta>();
            Set(p, "tipo", TipoPuerta.Corredera);
            Set(p, "direccionLocal", Vector3.left);
            Set(p, "distancia", 1f);
            Set(p, "cierreAutomatico", 4f);
            Set(p, "permitirArrastre", false);
            Set(p, "habilitado", false); // solo se abre con el botón

            var bt = Vacio("Boton", raiz, new Vector3(x - GrosorMuro * 0.5f, 1.2f, z - 0.9f), RotMarco(1f));
            Cubo("Caja", bt, new Vector3(0f, 0f, -0.01f), new Vector3(0.1f, 0.1f, 0.02f), mNegro, false);
            var pieza = Primitiva(PrimitiveType.Cylinder, "Pieza", bt, new Vector3(0f, 0f, -0.025f), new Vector3(0.05f, 0.01f, 0.05f), Quaternion.Euler(90f, 0f, 0f), mRojo);
            var boton = bt.gameObject.AddComponent<Boton>();
            Set(boton, "textoAccion", "Pulsar botón");
            Set(boton, "pieza", pieza.transform);
            Set(boton, "direccionPulsado", Vector3.up); // el cilindro está girado: su +Y local apunta hacia el muro
            Set(boton, "indicador", pieza.GetComponent<Renderer>());
            UnityEventTools.AddPersistentListener(boton.alPulsar, new UnityAction(p.Abrir));
        }

        static Palanca PalancaEn(Transform raiz, string nombre, Vector3 pos, Quaternion rot, int posInicial)
        {
            var t = Vacio(nombre, raiz, pos, rot);
            Cubo("Base", t, new Vector3(0f, 0f, -0.015f), new Vector3(0.12f, 0.2f, 0.03f), mNegro, false);
            var mango = Vacio("Mango", t, new Vector3(0f, 0f, -0.035f), Quaternion.identity);
            Cubo("Barra", mango, new Vector3(0f, 0f, -0.13f), new Vector3(0.025f, 0.025f, 0.26f), mMetal);
            Primitiva(PrimitiveType.Sphere, "Pomo", mango, new Vector3(0f, 0f, -0.27f), Vector3.one * 0.05f, Quaternion.identity, mRojo);
            var pal = t.gameObject.AddComponent<Palanca>();
            Set(pal, "textoAccion", "Accionar palanca");
            Set(pal, "mango", mango);
            Set(pal, "ejeLocal", Vector3.right);
            Set(pal, "anguloMinimo", -45f);
            Set(pal, "anguloMaximo", 45f);
            Set(pal, "posicionInicial", posInicial);
            return pal;
        }

        static void EstacionPalancas(Transform raiz, float z)
        {
            Cartel(raiz, 1f, z, "7. REJA CON DOS PALANCAS\nSube las dos para abrirla");
            float x = AnchoPasillo;
            var pivote = Vacio("Reja", raiz, new Vector3(x, 0f, z + 0.5f), RotMarco(1f));
            for (int i = 0; i < 6; i++)
                Cubo("Barrote", pivote, new Vector3(0.08f + i * 0.168f, AltoPuerta * 0.5f, 0f), new Vector3(0.03f, AltoPuerta - 0.02f, 0.03f), mMetal);
            Cubo("Travesano", pivote, new Vector3(0.5f, 0.3f, 0f), new Vector3(0.98f, 0.05f, 0.04f), mMetal);
            Cubo("Travesano", pivote, new Vector3(0.5f, 1.8f, 0f), new Vector3(0.98f, 0.05f, 0.04f), mMetal);
            var reja = pivote.gameObject.AddComponent<Puerta>();
            Set(reja, "tipo", TipoPuerta.Corredera);
            Set(reja, "direccionLocal", Vector3.up);
            Set(reja, "distancia", 1.9f);
            Set(reja, "duracion", 1.6f);
            Set(reja, "permitirArrastre", false);
            Set(reja, "habilitado", false);

            float xp = x - GrosorMuro * 0.5f;
            var a = PalancaEn(raiz, "Palanca_A", new Vector3(xp, 1.3f, z - 1.2f), RotMarco(1f), 0);
            var b = PalancaEn(raiz, "Palanca_B", new Vector3(xp, 1.3f, z + 1.2f), RotMarco(1f), 0);

            var comb = new GameObject("Combinador_Reja").AddComponent<CombinadorSenales>();
            comb.transform.SetParent(raiz, false);
            AnadirALista(comb, "fuentes", a);
            AnadirALista(comb, "fuentes", b);
            Set(comb, "modo", ModoCombinacion.Todas);
            UnityEventTools.AddPersistentListener(comb.alActivar, new UnityAction(reja.Abrir));
            UnityEventTools.AddPersistentListener(comb.alDesactivar, new UnityAction(reja.Cerrar));
        }

        static Interruptor InterruptorEn(Transform raiz, string nombre, Vector3 pos, Quaternion rot, CircuitoLuz circuito)
        {
            var t = Vacio(nombre, raiz, pos, rot);
            Cubo("Placa", t, new Vector3(0f, 0f, -0.005f), new Vector3(0.08f, 0.12f, 0.01f), Mat("Blanco", Color.white));
            var tecla = Vacio("Tecla", t, new Vector3(0f, 0f, -0.012f), Quaternion.identity);
            Cubo("Visual", tecla, Vector3.zero, new Vector3(0.035f, 0.06f, 0.012f), mPared);
            var it = t.gameObject.AddComponent<Interruptor>();
            Set(it, "textoAccion", "Interruptor");
            Set(it, "circuito", circuito);
            Set(it, "tecla", tecla);
            return it;
        }

        static void EstacionLuces(Transform raiz, float z)
        {
            Cartel(raiz, 1f, z, "8. LUCES CONMUTADAS\nDos interruptores (fuera y dentro) y una palanca de corriente");
            PuertaBisagra(raiz, "Puerta_Luces", 1f, z, 1f, mMadera, -95f);

            var habit = new GameObject("Circuito_Habitacion").transform;
            habit.SetParent(raiz, false);
            habit.localPosition = new Vector3(AnchoPasillo + 1.5f, AltoMuro - 0.15f, z);
            var lamp = Primitiva(PrimitiveType.Sphere, "Lampara", habit, Vector3.zero, new Vector3(0.3f, 0.15f, 0.3f), Quaternion.identity, mLampara, false);
            var luzT = Vacio("Luz", habit, new Vector3(0f, -0.2f, 0f), Quaternion.identity);
            var luz = luzT.gameObject.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.range = 7f;
            luz.intensity = 2.5f;
            luz.color = new Color(1f, 0.93f, 0.8f);
            luz.shadows = LightShadows.Soft;
            var circuito = habit.gameObject.AddComponent<CircuitoLuz>();
            AnadirALista(circuito, "luces", luz);
            AnadirALista(circuito, "lamparas", lamp.GetComponent<Renderer>());
            Set(circuito, "encendidoInicial", false);
            Set(circuito, "parpadeoAlEncender", true);

            float xf = AnchoPasillo - GrosorMuro * 0.5f;   // cara del pasillo
            float xd = AnchoPasillo + GrosorMuro * 0.5f;   // cara de la habitación
            InterruptorEn(raiz, "Interruptor_Fuera", new Vector3(xf, 1.2f, z - 0.8f), RotMarco(1f), circuito);
            InterruptorEn(raiz, "Interruptor_Dentro", new Vector3(xd, 1.2f, z - 0.8f), Quaternion.Euler(0f, -90f, 0f), circuito);

            var corriente = PalancaEn(raiz, "Palanca_Corriente", new Vector3(xf, 1.3f, z + 1.4f), RotMarco(1f), 1);
            UnityEventTools.AddPersistentListener(corriente.alCambiarEstado, new UnityAction<bool>(circuito.EstablecerCorriente));
            Texto("Rotulo", raiz, new Vector3(xf - 0.01f, 1.62f, z + 1.4f), RotMarco(1f), "CORRIENTE", 0.012f, Color.black);
        }

        static void EstacionPuertaDoble(Transform raiz, float z)
        {
            Cartel(raiz, 1f, z, "9. PUERTA DOBLE CON SEGURO\nPulsa el botón para quitar el seguro");
            float x = AnchoPasillo;
            var izq = Vacio("PuertaDoble_A", raiz, new Vector3(x, 0f, z + 0.6f), RotMarco(1f));
            Cubo("Hoja", izq, new Vector3(0.3f, AltoPuerta * 0.5f - 0.01f, 0f), new Vector3(0.59f, AltoPuerta - 0.03f, 0.05f), mAzul);
            var der = Vacio("PuertaDoble_B", raiz, new Vector3(x, 0f, z - 0.6f), RotMarco(1f));
            Cubo("Hoja", der, new Vector3(-0.3f, AltoPuerta * 0.5f - 0.01f, 0f), new Vector3(0.59f, AltoPuerta - 0.03f, 0.05f), mAzul);
            var pa = izq.gameObject.AddComponent<Puerta>();
            var pb = der.gameObject.AddComponent<Puerta>();
            Set(pa, "anguloApertura", -95f);
            Set(pb, "anguloApertura", 95f);
            Set(pa, "textoAccion", "Abrir");
            Set(pb, "textoAccion", "Abrir");
            AnadirALista(pa, "vinculadas", pb);
            AnadirALista(pb, "vinculadas", pa);

            var bt = Vacio("Boton_Seguro", raiz, new Vector3(x - GrosorMuro * 0.5f, 1.2f, z - 1.1f), RotMarco(1f));
            Cubo("Caja", bt, new Vector3(0f, 0f, -0.01f), new Vector3(0.1f, 0.1f, 0.02f), mNegro, false);
            var pieza = Primitiva(PrimitiveType.Cylinder, "Pieza", bt, new Vector3(0f, 0f, -0.025f), new Vector3(0.05f, 0.01f, 0.05f), Quaternion.Euler(90f, 0f, 0f), mVerde);
            var boton = bt.gameObject.AddComponent<Boton>();
            Set(boton, "modo", ModoBoton.Conmutador);
            Set(boton, "textoAccion", "Quitar / poner el seguro");
            Set(boton, "pieza", pieza.transform);
            Set(boton, "direccionPulsado", Vector3.up);
            Set(boton, "indicador", pieza.GetComponent<Renderer>());

            var bloqueo = bt.gameObject.AddComponent<BloqueoPorSenal>();
            Set(bloqueo, "fuente", boton);
            Set(bloqueo, "motivo", "Tiene el seguro puesto. Busca el botón.");
            AnadirALista(pa, "bloqueos", bloqueo);
            AnadirALista(pb, "bloqueos", bloqueo);
        }

        static void EstacionArmario(Transform raiz, float z)
        {
            Cartel(raiz, 1f, z, "10. PUERTA LIBRE\nClic para abrir o cerrar · mantén y arrastra para empujar o tirar");
            PuertaBisagra(raiz, "Puerta_Libre", 1f, z, 1f, mVerde, 95f);
        }

        static void CrearJugador()
        {
            var j = new GameObject("Jugador");
            j.transform.position = new Vector3(0f, 0.05f, 2f);
            var cc = j.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            j.AddComponent<Llavero>();

            var cam = new GameObject("Camara");
            cam.tag = "MainCamera";
            cam.transform.SetParent(j.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var c = cam.AddComponent<Camera>();
            c.nearClipPlane = 0.02f;
            cam.AddComponent<AudioListener>();
            cam.AddComponent<Interactor>();
            cam.AddComponent<InteraccionHUD>();

            var ctrl = j.AddComponent<JugadorPrimeraPersonaDemo>();
            Set(ctrl, "camara", cam.transform);
        }
    }
}
