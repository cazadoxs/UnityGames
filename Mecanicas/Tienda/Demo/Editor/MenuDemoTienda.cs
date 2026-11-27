using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mecanicas.Tienda.Demo.Editor
{
    /// <summary>Menú para crear la escena de demostración. Se va con la carpeta Demo si la borras.</summary>
    public static class MenuDemoTienda
    {
        [MenuItem("Herramientas/Mecánicas/Tienda/Crear escena de demostración", false, 0)]
        static void CrearEscenaDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Tienda Demo (se monta al dar a Play)").AddComponent<TiendaDemo>();

            string carpeta = CarpetaDelAsset();
            string ruta = AssetDatabase.GenerateUniqueAssetPath(carpeta + "/TiendaDemo.unity");
            EditorSceneManager.SaveScene(escena, ruta);
            Debug.Log("[Tienda] Escena demo creada en " + ruta + ". Dale a Play.");
        }

        /// <summary>Carpeta Demo de este asset si está dentro de Assets; si no (instalado como paquete), Assets/.</summary>
        static string CarpetaDelAsset()
        {
            foreach (var guid in AssetDatabase.FindAssets("TiendaDemo t:MonoScript"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (ruta.StartsWith("Assets/") && Path.GetFileName(ruta) == "TiendaDemo.cs")
                    return Path.GetDirectoryName(ruta).Replace('\\', '/');
            }
            return "Assets";
        }
    }
}
