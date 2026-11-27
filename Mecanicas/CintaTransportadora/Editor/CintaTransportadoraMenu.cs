using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_6000_0_OR_NEWER
using MaterialFisico = UnityEngine.PhysicsMaterial;
#else
using MaterialFisico = UnityEngine.PhysicMaterial;
#endif

namespace Mecanicas.Cintas.Editor
{
    /// <summary>
    /// Menús del editor:
    ///  - GameObject > Mecanicas > Cinta transportadora: crea una cinta lista para usar.
    ///  - Tools > Mecanicas > Cinta transportadora > ...: materiales y escena de demo.
    /// Los materiales y la textura se guardan como assets en la subcarpeta "Generado".
    /// </summary>
    public static class CintaTransportadoraMenu
    {
        const string NombreDemo = "Mecanicas.Cintas.Demo.DemoCintaTransportadora";

        [MenuItem("GameObject/Mecanicas/Cinta transportadora", false, 10)]
        static void CrearCinta(MenuCommand comando)
        {
            var opciones = OpcionesConAssets();
            Vector3 pos = Vector3.zero;
            var vista = SceneView.lastActiveSceneView;
            if (vista != null) pos = vista.pivot;

            var cinta = CintaTransportadoraConstructor.Crear(opciones, pos, Quaternion.identity);
            if (comando.context is GameObject padre)
                GameObjectUtility.SetParentAndAlign(cinta.gameObject, padre);
            Undo.RegisterCreatedObjectUndo(cinta.gameObject, "Crear cinta transportadora");
            Selection.activeGameObject = cinta.gameObject;
        }

        [MenuItem("Tools/Mecanicas/Cinta transportadora/Generar materiales")]
        static void MenuGenerarMateriales()
        {
            OpcionesConAssets();
            EditorUtility.RevealInFinder(CarpetaGenerado());
            Debug.Log($"[CintaTransportadora] Materiales en {CarpetaGenerado()}");
        }

        [MenuItem("Tools/Mecanicas/Cinta transportadora/Crear escena demo")]
        static void CrearEscenaDemo()
        {
            var tipo = BuscarTipo(NombreDemo);
            if (tipo == null)
            {
                EditorUtility.DisplayDialog("Cinta transportadora",
                    "No encuentro el script DemoCintaTransportadora. ¿Has copiado la carpeta Demo?", "Vale");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string ruta = CarpetaRaiz() + "/Demo/CintaDemo.unity";
            if (File.Exists(ruta) && !EditorUtility.DisplayDialog("Cinta transportadora",
                    $"{ruta} ya existe. ¿Sobrescribir?", "Sobrescribir", "Cancelar"))
                return;

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Demo Cinta Transportadora");
            go.AddComponent(tipo);
            SceneManager.MoveGameObjectToScene(go, escena);
            EditorSceneManager.SaveScene(escena, ruta);
            Debug.Log($"[CintaTransportadora] Escena de demo creada en {ruta}. Dale a Play.");
        }

        // ------------------------------------------------------------------ Assets

        /// <summary>Opciones del constructor con materiales guardados como assets (para que se guarden en la escena).</summary>
        public static CintaTransportadoraConstructor.Opciones OpcionesConAssets()
        {
            string carpeta = CarpetaGenerado();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + "/CintaBanda_Textura.asset");
            if (tex == null)
            {
                tex = CintaTexturaProcedural.Crear(256, new Color(0.13f, 0.13f, 0.14f), new Color(0.95f, 0.75f, 0.1f));
                tex.name = "CintaBanda_Textura";
                AssetDatabase.CreateAsset(tex, carpeta + "/CintaBanda_Textura.asset");
            }

            var banda = CargarOCrearMaterial(carpeta + "/CintaBanda.mat", () =>
            {
                var m = CintaMateriales.Crear("CintaBanda", Color.white, 0f, 0.25f);
                m.mainTexture = tex;
                return m;
            });
            var cuerpo = CargarOCrearMaterial(carpeta + "/CintaCuerpo.mat",
                () => CintaMateriales.Crear("CintaCuerpo", new Color(0.32f, 0.36f, 0.42f), 0.55f, 0.5f));
            var rodillo = CargarOCrearMaterial(carpeta + "/CintaRodillo.mat",
                () => CintaMateriales.Crear("CintaRodillo", new Color(0.7f, 0.7f, 0.72f), 0.9f, 0.7f));

            string rutaFisico = carpeta + "/CintaSinFriccion.asset";
            var fisico = AssetDatabase.LoadAssetAtPath<MaterialFisico>(rutaFisico);
            if (fisico == null)
            {
                fisico = CintaTransportadoraConstructor.CrearMaterialSinFriccion();
                AssetDatabase.CreateAsset(fisico, rutaFisico);
            }
            AssetDatabase.SaveAssets();

            return new CintaTransportadoraConstructor.Opciones
            {
                materialBanda = banda,
                materialCuerpo = cuerpo,
                materialRodillos = rodillo,
                materialFisico = fisico
            };
        }

        static Material CargarOCrearMaterial(string ruta, System.Func<Material> crear)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            m = crear();
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        /// <summary>Carpeta donde está instalado el asset (la que contiene Runtime, Editor y Demo).</summary>
        public static string CarpetaRaiz()
        {
            foreach (var guid in AssetDatabase.FindAssets("CintaTransportadoraMenu t:MonoScript"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (!ruta.EndsWith("/CintaTransportadoraMenu.cs")) continue;
                string editor = Path.GetDirectoryName(ruta).Replace('\\', '/');
                return Path.GetDirectoryName(editor).Replace('\\', '/');
            }
            return "Assets/Mecanicas/CintaTransportadora";
        }

        static string CarpetaGenerado()
        {
            string raiz = CarpetaRaiz();
            string carpeta = raiz + "/Generado";
            if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder(raiz, "Generado");
            return carpeta;
        }

        static System.Type BuscarTipo(string nombreCompleto)
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
                if (t.FullName == nombreCompleto) return t;
            return null;
        }
    }
}
