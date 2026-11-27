using System.IO;
using Mecanicas.MiradaNPC.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mecanicas.MiradaNPC.Editor
{
    /// <summary>Menú para generar la escena de demostración y para añadir la mecánica a un NPC seleccionado.</summary>
    public static class CreadorEscenaDemoMirada
    {
        const string Menu = "Herramientas/Mecánicas/Mirada NPC/";

        [MenuItem(Menu + "Crear escena demo", priority = 0)]
        public static void CrearEscenaDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string carpetaDemo = CarpetaDemo();
            string carpetaMateriales = carpetaDemo + "/Materiales";
            if (!AssetDatabase.IsValidFolder(carpetaMateriales)) AssetDatabase.CreateFolder(carpetaDemo, "Materiales");

            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var camaraPorDefecto = GameObject.Find("Main Camera");
            if (camaraPorDefecto != null) Object.DestroyImmediate(camaraPorDefecto);

            ConstructorDemoMirada.Construir(m =>
            {
                string ruta = $"{carpetaMateriales}/{m.name}.mat";
                var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (existente != null) return existente;
                AssetDatabase.CreateAsset(m, ruta);
                return m;
            });

            string rutaEscena = carpetaDemo + "/MiradaNPC_Demo.unity";
            EditorSceneManager.SaveScene(escena, rutaEscena);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mirada NPC] Escena demo creada en {rutaEscena}. Dale a Play.");
        }

        [MenuItem(Menu + "Añadir a los objetos seleccionados", priority = 20)]
        public static void AnadirASeleccion()
        {
            foreach (var go in Selection.gameObjects)
            {
                var npc = go.GetComponent<SeguimientoMiradaNPC>();
                if (npc == null) npc = Undo.AddComponent<SeguimientoMiradaNPC>(go);
                Undo.RecordObject(npc, "Autoconfigurar mirada");
                npc.AutoConfigurar();
                EditorUtility.SetDirty(npc);
                Debug.Log($"[Mirada NPC] {go.name}: {npc.cadena.Count} huesos de cadena y {npc.ojos.Count} ojos detectados.", go);
            }
        }

        [MenuItem(Menu + "Añadir a los objetos seleccionados", true)]
        static bool ValidarAnadir() => Selection.gameObjects.Length > 0;

        [MenuItem(Menu + "Marcar cámara principal como objetivo", priority = 21)]
        public static void MarcarCamara()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("Mirada NPC", "No hay ninguna cámara con la etiqueta MainCamera en la escena.", "Vale");
                return;
            }
            if (cam.GetComponent<ObjetivoMirada>() == null) Undo.AddComponent<ObjetivoMirada>(cam.gameObject);
            Selection.activeGameObject = cam.gameObject;
        }

        /// <summary>Carpeta Demo de esta mecánica, esté donde esté copiada dentro de Assets (o como paquete).</summary>
        static string CarpetaDemo()
        {
            var guids = AssetDatabase.FindAssets("ConstructorDemoMirada t:MonoScript");
            foreach (var g in guids)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(g);           // .../Demo/Scripts/ConstructorDemoMirada.cs
                string demo = Path.GetDirectoryName(Path.GetDirectoryName(ruta))?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(demo) && demo.StartsWith("Assets")) return demo;
            }
            // Si está instalado como paquete (solo lectura), guardamos la demo dentro de Assets.
            if (!AssetDatabase.IsValidFolder("Assets/MiradaNPC_Demo")) AssetDatabase.CreateFolder("Assets", "MiradaNPC_Demo");
            return "Assets/MiradaNPC_Demo";
        }
    }
}
