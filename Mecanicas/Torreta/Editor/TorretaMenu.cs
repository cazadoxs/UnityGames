using System.IO;
using Mecanicas.Torretas.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mecanicas.Torretas.Editor
{
    /// <summary>Menús para crear torretas, la escena de demostración y prefabs de ejemplo.</summary>
    public static class TorretaMenu
    {
        const string MenuGameObject = "GameObject/Mecánicas/Torreta/";
        const string MenuHerramientas = "Herramientas/Mecánicas/Torreta/";

        // ------------------------------------------------------------------ Crear en la escena
        [MenuItem(MenuGameObject + "Fija", false, 10)]
        static void CrearFija(MenuCommand c) => CrearEnEscena(ModoApuntado.Fija, c);

        [MenuItem(MenuGameObject + "Giratoria (un eje)", false, 11)]
        static void CrearUnEje(MenuCommand c) => CrearEnEscena(ModoApuntado.UnEje, c);

        [MenuItem(MenuGameObject + "Todos los ángulos", false, 12)]
        static void CrearTodos(MenuCommand c) => CrearEnEscena(ModoApuntado.TodosLosAngulos, c);

        static void CrearEnEscena(ModoApuntado modo, MenuCommand c)
        {
            var padre = (c.context as GameObject) != null ? ((GameObject)c.context).transform : null;
            Vector3 pos = Vector3.zero;
            if (padre == null && SceneView.lastActiveSceneView != null) pos = SceneView.lastActiveSceneView.pivot;
            pos.y = 0f;

            var t = FabricaTorretas.Crear(modo, pos, Quaternion.identity, padre);
            GuardarMateriales(t.gameObject, CarpetaGenerada());
            Undo.RegisterCreatedObjectUndo(t.gameObject, "Crear torreta");
            Selection.activeGameObject = t.gameObject;
        }

        // ------------------------------------------------------------------ Escena de demostración
        [MenuItem(MenuHerramientas + "Crear escena de demostración", false, 1)]
        static void CrearEscenaDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Demo Torretas");
            var demo = go.AddComponent<DemoTorretas>();
            demo.Construir();
            demo.construirAlIniciar = false;

            string carpeta = CarpetaGenerada();
            GuardarMateriales(go, carpeta);
            string ruta = Path.Combine(CarpetaRaiz(), "Demo", "DemoTorretas.unity").Replace('\\', '/');
            EditorSceneManager.SaveScene(escena, ruta);
            AssetDatabase.Refresh();
            Debug.Log($"[Torreta] Escena de demostración guardada en {ruta}. Dale a Play.");
        }

        // ------------------------------------------------------------------ Prefabs
        [MenuItem(MenuHerramientas + "Guardar torretas de ejemplo como prefabs", false, 2)]
        static void CrearPrefabs()
        {
            string carpeta = Path.Combine(CarpetaRaiz(), "Prefabs").Replace('\\', '/');
            CrearCarpeta(carpeta);
            string mats = CarpetaGenerada();
            foreach (ModoApuntado modo in System.Enum.GetValues(typeof(ModoApuntado)))
            {
                var t = FabricaTorretas.Crear(modo, Vector3.zero, Quaternion.identity);
                GuardarMateriales(t.gameObject, mats);
                string ruta = $"{carpeta}/Torreta {modo}.prefab";
                PrefabUtility.SaveAsPrefabAsset(t.gameObject, ruta);
                Object.DestroyImmediate(t.gameObject);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Torreta] Prefabs guardados en {carpeta}");
        }

        // ------------------------------------------------------------------ Utilidades
        /// <summary>Carpeta donde está instalado el sistema (busca este script, así funciona aunque lo muevas).</summary>
        static string CarpetaRaiz()
        {
            foreach (var guid in AssetDatabase.FindAssets("TorretaMenu t:MonoScript"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/Editor/TorretaMenu.cs")) return p.Substring(0, p.Length - "/Editor/TorretaMenu.cs".Length);
            }
            return "Assets/Mecanicas/Torreta";
        }

        static string CarpetaGenerada()
        {
            string c = CarpetaRaiz() + "/Generado";
            CrearCarpeta(c);
            return c;
        }

        static void CrearCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;
            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            CrearCarpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }

        /// <summary>
        /// Los materiales creados por código solo viven en memoria; para que una escena o un prefab
        /// los conserve hay que guardarlos como archivos .mat.
        /// </summary>
        static void GuardarMateriales(GameObject raiz, string carpeta)
        {
            foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || EditorUtility.IsPersistent(m)) continue; // ya es un archivo (o un recurso interno de Unity)
                    string nombre = string.Join("_", m.name.Split(Path.GetInvalidFileNameChars()));
                    string ruta = $"{carpeta}/{nombre}.mat";
                    var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                    if (existente != null)
                    {
                        EditorUtility.CopySerialized(m, existente);
                        Reemplazar(raiz, m, existente);
                    }
                    else AssetDatabase.CreateAsset(m, ruta);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static void Reemplazar(GameObject raiz, Material viejo, Material nuevo)
        {
            foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool cambio = false;
                for (int i = 0; i < mats.Length; i++) if (mats[i] == viejo) { mats[i] = nuevo; cambio = true; }
                if (cambio) r.sharedMaterials = mats;
            }
        }
    }
}
