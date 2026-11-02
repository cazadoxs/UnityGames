using System.IO;
using Mecanicas.Eventos.Demo;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mecanicas.Eventos.EditorTools
{
    /// <summary>
    /// Construye la escena de demostración con un pasillo de 8 estaciones, una por tipo de evento.
    /// Menú: Herramientas > Mecánicas > Eventos > Crear escena de demo.
    /// Todo lo que necesita (materiales, sonidos, animación, prefab) se genera en Demo/Generado,
    /// así que no depende de ningún asset externo.
    /// </summary>
    public static class CreadorEscenaDemoEventos
    {
        private const float Separacion = 10f;

        private static string carpeta;
        private static Material matSuelo, matPared, matZona, matRojo, matAzul, matVerde, matAmarillo, matGris;

        [MenuItem("Herramientas/Mecánicas/Eventos/Crear escena de demo", false, 0)]
        public static void Crear()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            carpeta = CarpetaGenerado();
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CrearMateriales();
            CrearEntorno();
            CrearJugador();

            // Subtítulos en la escena para poder cambiar su aspecto desde el Inspector.
            new GameObject("Subtitulos (Eventos)").AddComponent<SubtitulosSimples>();

            AudioClip pitido = CrearWav("Pitido", GenerarPitido());
            AudioClip explosion = CrearWav("Explosion", GenerarExplosion());

            Estacion1Animacion(Z(0));
            Estacion2Aparecer(Z(1));
            Estacion3MirarRecorrido(Z(2));
            Estacion4MirarGolpe(Z(3));
            Estacion5Temblor(Z(4));
            Estacion6Dialogo(Z(5));
            Estacion7Sonido(Z(6), pitido);
            Estacion8TodoJunto(Z(7), explosion);

            string ruta = carpeta + "/DemoEventos.unity";
            EditorSceneManager.SaveScene(escena, ruta);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Eventos] Escena de demo creada en {ruta}. Pulsa Play y recorre el pasillo.");
        }

        private static float Z(int i) => 8f + i * Separacion;

        // ---------------------------------------------------------------- Estaciones

        private static void Estacion1Animacion(float z)
        {
            GameObject zona = CrearZona("1 · Animación", z, "Una puerta se abre con un Animator");
            Ajustar(zona.GetComponent<ZonaEvento>(), "repeticion", (int)ModoRepeticion.UnaVez);

            GameObject puerta = Cubo("Puerta", new Vector3(-4.6f, 1.5f, z + 3f), new Vector3(0.3f, 3f, 2.4f), matAzul);
            Cubo("Marco", new Vector3(-4.9f, 1.6f, z + 3f), new Vector3(0.2f, 3.2f, 2.8f), matGris);

            Animator anim = puerta.AddComponent<Animator>();
            anim.runtimeAnimatorController = CrearControladorPuerta(puerta.transform.localPosition);

            AccionAnimacion a = zona.AddComponent<AccionAnimacion>();
            Ajustar(a, "animator", anim);
            Ajustar(a, "tipo", (int)TipoAnimacion.Trigger);
            Ajustar(a, "parametro", "Abrir");
            Registrar(zona, a);
        }

        private static void Estacion2Aparecer(float z)
        {
            GameObject zona = CrearZona("2 · Aparecer objeto", z, "Aparece un objeto que estaba oculto\ny se crea otro desde un prefab");
            Ajustar(zona.GetComponent<ZonaEvento>(), "ejecucion", (int)ModoEjecucion.Paralelo);

            Cubo("Pedestal", new Vector3(4f, 0.5f, z + 3f), new Vector3(1.2f, 1f, 1.2f), matGris);
            GameObject premio = Cubo("Objeto oculto", new Vector3(4f, 1.5f, z + 3f), Vector3.one * 0.6f, matAmarillo);
            premio.SetActive(false);

            AccionObjeto activar = zona.AddComponent<AccionObjeto>();
            Ajustar(activar, "modo", (int)ModoObjeto.ActivarExistente);
            Ajustar(activar, "objetivo", premio);
            Ajustar(activar, "quitarTras", 4f);

            GameObject punto = new GameObject("Punto de aparición");
            punto.transform.position = new Vector3(-3.5f, 2.5f, z + 3f);

            AccionObjeto instanciar = zona.AddComponent<AccionObjeto>();
            Ajustar(instanciar, "modo", (int)ModoObjeto.InstanciarPrefab);
            Ajustar(instanciar, "prefab", CrearPrefabBola());
            Ajustar(instanciar, "puntoAparicion", punto.transform);
            Ajustar(instanciar, "quitarTras", 5f);

            Registrar(zona, activar, instanciar);
        }

        private static void Estacion3MirarRecorrido(float z)
        {
            GameObject zona = CrearZona("3 · Mirar con recorrido", z, "La cámara gira despacio hacia la columna roja\ny vuelve");
            GameObject columna = Cubo("Columna roja", new Vector3(8f, 2.5f, z + 1f), new Vector3(1f, 5f, 1f), matRojo);

            AccionMirarCamara a = zona.AddComponent<AccionMirarCamara>();
            a.Mirada.objetivo = columna.transform;
            a.Mirada.desplazamiento = new Vector3(0f, -0.5f, 0f);
            a.Mirada.duracionIda = 1.2f;
            a.Mirada.tiempoMirando = 1.5f;
            a.Mirada.alTerminar = FinMirada.Volver;
            a.Mirada.duracionVuelta = 0.8f;
            EditorUtility.SetDirty(a);
            Registrar(zona, a);
        }

        private static void Estacion4MirarGolpe(float z)
        {
            GameObject zona = CrearZona("4 · Mirar de golpe", z, "La cámara mira de golpe a la lámpara\ny se queda mirando hacia allí");
            Cubo("Poste", new Vector3(-7f, 2.5f, z + 4f), new Vector3(0.3f, 5f, 0.3f), matGris);
            GameObject lampara = Cubo("Lámpara", new Vector3(-7f, 5.2f, z + 4f), Vector3.one * 0.7f, matAmarillo);

            AccionMirarCamara a = zona.AddComponent<AccionMirarCamara>();
            a.Mirada.objetivo = lampara.transform;
            a.Mirada.duracionIda = 0f;
            a.Mirada.tiempoMirando = 1f;
            a.Mirada.alTerminar = FinMirada.Mantener;
            EditorUtility.SetDirty(a);
            Registrar(zona, a);
        }

        private static void Estacion5Temblor(float z)
        {
            GameObject zona = CrearZona("5 · Temblor de cámara", z, "Terremoto suave de 2 segundos");
            AccionTemblorCamara a = zona.AddComponent<AccionTemblorCamara>();
            a.Temblor.intensidad = 0.8f;
            a.Temblor.duracion = 2f;
            a.Temblor.frecuencia = 12f;
            EditorUtility.SetDirty(a);
            Registrar(zona, a);
        }

        private static void Estacion6Dialogo(float z)
        {
            GameObject zona = CrearZona("6 · Diálogo", z, "Subtítulos simples (sin sistema de diálogos)\nEspacio o clic para pasar");
            AccionDialogo a = zona.AddComponent<AccionDialogo>();
            a.Lineas.Clear();
            a.Lineas.Add(new LineaDialogo { hablante = "Guardia", texto = "Alto. Esta zona está cerrada al público." });
            a.Lineas.Add(new LineaDialogo { hablante = "Guardia", texto = "Bueno... pasa, pero no toques nada." });
            EditorUtility.SetDirty(a);
            Registrar(zona, a);
        }

        private static void Estacion7Sonido(float z, AudioClip pitido)
        {
            GameObject zona = CrearZona("7 · Sonido", z, "Suena un pitido 3D en el altavoz verde");
            GameObject altavoz = Cubo("Altavoz", new Vector3(4.5f, 1f, z + 2f), new Vector3(0.8f, 1.2f, 0.8f), matVerde);

            AccionSonido a = zona.AddComponent<AccionSonido>();
            Ajustar(a, "clips", new Object[] { pitido });
            Ajustar(a, "posicion", (int)PosicionSonido.EnPunto);
            Ajustar(a, "punto", altavoz.transform);
            Registrar(zona, a);
        }

        private static void Estacion8TodoJunto(float z, AudioClip explosion)
        {
            GameObject zona = CrearZona("8 · Todo junto", z, "Explosión: sonido + temblor + objeto + mirada + diálogo\n(se puede repetir cada 4 s)");
            Ajustar(zona.GetComponent<ZonaEvento>(), "enfriamiento", 4f);

            GameObject fuego = Cubo("Fuego", new Vector3(5f, 1f, z + 6f), Vector3.one * 2f, matRojo);
            fuego.SetActive(false);

            AccionSonido sonido = zona.AddComponent<AccionSonido>();
            Ajustar(sonido, "clips", new Object[] { explosion });
            Ajustar(sonido, "posicion", (int)PosicionSonido.EnPunto);
            Ajustar(sonido, "punto", fuego.transform);
            Ajustar(sonido, "esperarAQueTermine", false);

            AccionTemblorCamara temblor = zona.AddComponent<AccionTemblorCamara>();
            temblor.Temblor.intensidad = 1.6f;
            temblor.Temblor.duracion = 0.9f;
            Ajustar(temblor, "esperarAQueTermine", false);

            AccionObjeto aparecer = zona.AddComponent<AccionObjeto>();
            Ajustar(aparecer, "modo", (int)ModoObjeto.ActivarExistente);
            Ajustar(aparecer, "objetivo", fuego);
            Ajustar(aparecer, "quitarTras", 3.5f);

            AccionMirarCamara mirar = zona.AddComponent<AccionMirarCamara>();
            mirar.Mirada.objetivo = fuego.transform;
            mirar.Mirada.duracionIda = 0.35f;
            mirar.Mirada.tiempoMirando = 1.2f;
            EditorUtility.SetDirty(mirar);
            Ajustar(mirar, "retraso", 0.2f);
            Ajustar(mirar, "esperarAQueTermine", false);

            AccionDialogo dialogo = zona.AddComponent<AccionDialogo>();
            dialogo.Lineas.Clear();
            dialogo.Lineas.Add(new LineaDialogo { hablante = "Tú", texto = "¡¿Qué ha sido eso?!", duracion = 2f });
            EditorUtility.SetDirty(dialogo);
            Ajustar(dialogo, "retraso", 0.6f);

            Registrar(zona, sonido, temblor, aparecer, mirar, dialogo);
        }

        // ---------------------------------------------------------------- Piezas

        private static GameObject CrearZona(string titulo, float z, string descripcion)
        {
            GameObject go = new GameObject("Zona " + titulo);
            go.transform.position = new Vector3(0f, 1.5f, z);
            BoxCollider c = go.AddComponent<BoxCollider>();
            c.size = new Vector3(19f, 3f, 2f);
            c.isTrigger = true;

            ZonaEvento zona = go.AddComponent<ZonaEvento>();
            Ajustar(zona, "repeticion", (int)ModoRepeticion.CadaVez);
            Ajustar(zona, "enfriamiento", 1.5f);
            Ajustar(zona, "mostrarMensajes", true);

            // Franja visible en el suelo para saber dónde está la zona.
            GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marca.name = "Marca en el suelo";
            Object.DestroyImmediate(marca.GetComponent<Collider>());
            marca.transform.SetParent(go.transform, false);
            marca.transform.localPosition = new Vector3(0f, -1.49f, 0f);
            marca.transform.localScale = new Vector3(19f, 0.02f, 2f);
            marca.GetComponent<Renderer>().sharedMaterial = matZona;

            Texto(titulo, new Vector3(0f, 3.6f, z + 1f), 0.16f, Color.white);
            Texto(descripcion, new Vector3(0f, 3.0f, z + 1f), 0.08f, new Color(0.85f, 0.85f, 0.85f));
            return go;
        }

        private static void Registrar(GameObject zonaGO, params AccionEvento[] acciones)
        {
            ZonaEvento zona = zonaGO.GetComponent<ZonaEvento>();
            zona.Acciones.Clear();
            zona.Acciones.AddRange(acciones);
            EditorUtility.SetDirty(zona);
        }

        private static void CrearEntorno()
        {
            GameObject luz = new GameObject("Luz");
            Light l = luz.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            l.shadows = LightShadows.Soft;
            luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            float largo = Z(7) + 16f;
            Cubo("Suelo", new Vector3(0f, -0.25f, largo * 0.5f - 6f), new Vector3(20f, 0.5f, largo + 12f), matSuelo);
            Cubo("Pared izquierda", new Vector3(-10f, 2f, largo * 0.5f - 6f), new Vector3(0.5f, 4f, largo + 12f), matPared);
            Cubo("Pared derecha", new Vector3(10f, 2f, largo * 0.5f - 6f), new Vector3(0.5f, 4f, largo + 12f), matPared);
            Cubo("Pared fondo", new Vector3(0f, 2f, largo), new Vector3(20f, 4f, 0.5f), matPared);
            Cubo("Pared inicio", new Vector3(0f, 2f, -12f), new Vector3(20f, 4f, 0.5f), matPared);

            Texto("Sistema de eventos por trigger\nWASD para moverte, ratón para mirar, Esc suelta el ratón",
                new Vector3(0f, 2.6f, 0f), 0.09f, Color.white);
        }

        private static void CrearJugador()
        {
            GameObject jugador = new GameObject("Jugador");
            jugador.tag = "Player";
            jugador.transform.position = new Vector3(0f, 0.05f, -6f);

            CharacterController cc = jugador.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            GameObject cam = new GameObject("Cámara");
            cam.tag = "MainCamera";
            cam.transform.SetParent(jugador.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            Camera c = cam.AddComponent<Camera>();
            c.nearClipPlane = 0.05f;
            c.fieldOfView = 70f;
            cam.AddComponent<AudioListener>();
            cam.AddComponent<CamaraEventos>();

            JugadorDemoPrimeraPersona control = jugador.AddComponent<JugadorDemoPrimeraPersona>();
            Ajustar(control, "camara", cam.transform);
        }

        private static GameObject Cubo(string nombre, Vector3 pos, Vector3 escala, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.position = pos;
            go.transform.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void Texto(string texto, Vector3 pos, float tamano, Color color)
        {
            GameObject go = new GameObject("Texto");
            go.transform.position = pos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = texto;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = tamano;
            tm.fontSize = 48;
            tm.color = color;

            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f != null)
            {
                tm.font = f;
                go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
            }
        }

        // ---------------------------------------------------------------- Assets generados

        private static string CarpetaGenerado()
        {
            // Busca dónde está este script para que funcione en cualquier carpeta del proyecto.
            string[] guids = AssetDatabase.FindAssets("CreadorEscenaDemoEventos t:MonoScript");
            string raiz = "Assets/Mecanicas/Eventos";
            if (guids.Length > 0)
            {
                string script = AssetDatabase.GUIDToAssetPath(guids[0]);          // .../Eventos/Editor/X.cs
                raiz = Path.GetDirectoryName(Path.GetDirectoryName(script)).Replace('\\', '/');
            }

            string demo = raiz + "/Demo";
            if (!AssetDatabase.IsValidFolder(demo)) AssetDatabase.CreateFolder(raiz, "Demo");
            string gen = demo + "/Generado";
            if (!AssetDatabase.IsValidFolder(gen)) AssetDatabase.CreateFolder(demo, "Generado");
            return gen;
        }

        private static void CrearMateriales()
        {
            // El material por defecto de las primitivas ya usa el shader correcto (Built-in, URP o HDRP).
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(tmp);

            matSuelo = Mat(baseMat, "Suelo", new Color(0.35f, 0.36f, 0.38f));
            matPared = Mat(baseMat, "Pared", new Color(0.55f, 0.55f, 0.58f));
            matZona = Mat(baseMat, "Zona", new Color(1f, 0.6f, 0.1f));
            matRojo = Mat(baseMat, "Rojo", new Color(0.85f, 0.2f, 0.15f));
            matAzul = Mat(baseMat, "Azul", new Color(0.2f, 0.4f, 0.85f));
            matVerde = Mat(baseMat, "Verde", new Color(0.2f, 0.75f, 0.3f));
            matAmarillo = Mat(baseMat, "Amarillo", new Color(1f, 0.85f, 0.2f));
            matGris = Mat(baseMat, "Gris", new Color(0.25f, 0.25f, 0.27f));
        }

        private static Material Mat(Material baseMat, string nombre, Color color)
        {
            string ruta = $"{carpeta}/M_{nombre}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m == null)
            {
                m = new Material(baseMat);
                AssetDatabase.CreateAsset(m, ruta);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject CrearPrefabBola()
        {
            string ruta = carpeta + "/Bola.prefab";
            GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "Bola";
            bola.transform.localScale = Vector3.one * 0.8f;
            bola.GetComponent<Renderer>().sharedMaterial = matVerde;
            bola.AddComponent<Rigidbody>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bola, ruta);
            Object.DestroyImmediate(bola);
            return prefab;
        }

        private static RuntimeAnimatorController CrearControladorPuerta(Vector3 cerrada)
        {
            string rutaClip = carpeta + "/Puerta_Abrir.anim";
            string rutaCtrl = carpeta + "/Puerta.controller";
            AssetDatabase.DeleteAsset(rutaClip);
            AssetDatabase.DeleteAsset(rutaCtrl);

            AnimationClip clip = new AnimationClip { name = "Puerta_Abrir" };
            clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Constant(0f, 1f, cerrada.x));
            clip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.EaseInOut(0f, cerrada.y, 1f, cerrada.y + 3.1f));
            clip.SetCurve("", typeof(Transform), "localPosition.z", AnimationCurve.Constant(0f, 1f, cerrada.z));
            AssetDatabase.CreateAsset(clip, rutaClip);

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(rutaCtrl);
            ctrl.AddParameter("Abrir", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            AnimatorState estadoCerrada = sm.AddState("Cerrada");
            AnimatorState estadoAbierta = sm.AddState("Abierta");
            estadoAbierta.motion = clip;
            sm.defaultState = estadoCerrada;

            AnimatorStateTransition t = estadoCerrada.AddTransition(estadoAbierta);
            t.AddCondition(AnimatorConditionMode.If, 0f, "Abrir");
            t.hasExitTime = false;
            t.duration = 0f;
            return ctrl;
        }

        // Sonidos sintetizados y guardados como .wav, para no depender de ningún asset.
        private static float[] GenerarPitido()
        {
            const int frecuencia = 44100;
            float[] s = new float[(int)(frecuencia * 0.35f)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)frecuencia;
                float env = Mathf.Min(1f, t * 80f) * Mathf.Exp(-t * 7f);
                s[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * env * 0.6f;
            }
            return s;
        }

        private static float[] GenerarExplosion()
        {
            const int frecuencia = 44100;
            System.Random r = new System.Random(7);
            float[] s = new float[(int)(frecuencia * 1.8f)];
            float filtrado = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)frecuencia;
                float ruido = (float)(r.NextDouble() * 2.0 - 1.0);
                float corte = Mathf.Lerp(0.5f, 0.04f, t / 1.8f); // se va apagando y volviendo grave
                filtrado += (ruido - filtrado) * corte;
                float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 2.6f);
                s[i] = Mathf.Clamp(filtrado * env * 2.2f, -1f, 1f);
            }
            return s;
        }

        private static AudioClip CrearWav(string nombre, float[] muestras)
        {
            string ruta = $"{carpeta}/{nombre}.wav";
            const int frecuencia = 44100;
            using (FileStream fs = new FileStream(Path.GetFullPath(ruta), FileMode.Create))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                int datos = muestras.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + datos);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);
                w.Write((short)1);       // PCM
                w.Write((short)1);       // mono
                w.Write(frecuencia);
                w.Write(frecuencia * 2); // bytes por segundo
                w.Write((short)2);       // bytes por muestra
                w.Write((short)16);      // bits
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(datos);
                foreach (float m in muestras)
                    w.Write((short)(Mathf.Clamp(m, -1f, 1f) * short.MaxValue));
            }
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }

        // ---------------------------------------------------------------- Utilidad

        /// <summary>Pone el valor de un campo privado serializado (como si lo cambiaras en el Inspector).</summary>
        private static void Ajustar(Object obj, string campo, object valor)
        {
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty p = so.FindProperty(campo);
            if (p == null)
            {
                Debug.LogError($"[Eventos] Campo '{campo}' no encontrado en {obj.GetType().Name}.");
                return;
            }

            switch (valor)
            {
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i: p.intValue = i; break;
                case Object[] arr:
                    p.arraySize = arr.Length;
                    for (int k = 0; k < arr.Length; k++)
                        p.GetArrayElementAtIndex(k).objectReferenceValue = arr[k];
                    break;
                case Object o: p.objectReferenceValue = o; break;
                default:
                    Debug.LogError($"[Eventos] Tipo no soportado para '{campo}'.");
                    return;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
