// PENDIENTE DE COMPILAR EN UNITY. Herramientas de editor para el contenido.
using System.IO;
using Estandartes.Core.Mods;
using UnityEditor;
using UnityEngine;

namespace Estandartes.Tools
{
    public static class ContentMenu
    {
        [MenuItem("Estandartes/Validar contenido base y mods")]
        public static void Validate()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Mods");
            var sources = new System.Collections.Generic.List<IModSource>();
            foreach (var dir in Directory.GetDirectories(root)) sources.Add(new DirectoryModSource(dir));
            var r = GameContent.CreateLoader().Load(sources, GameContent.GameVersion);
            int errors = 0;
            foreach (var m in r.Messages)
            {
                if (m.Severity == LoadSeverity.Error) { Debug.LogError(m.ToString()); errors++; }
                else Debug.Log(m.ToString());
            }
            EditorUtility.DisplayDialog("Estandartes", errors == 0
                ? $"Contenido correcto: {r.Registry.Count} elementos."
                : $"{errors} errores. Revisa la consola.", "Aceptar");
        }

        /// <summary>Punto de entrada para modo batch: Unity -batchmode -executeMethod Estandartes.Tools.ContentMenu.ValidateBatch -quit</summary>
        public static void ValidateBatch()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Mods");
            var sources = new System.Collections.Generic.List<IModSource>();
            foreach (var dir in Directory.GetDirectories(root)) sources.Add(new DirectoryModSource(dir));
            var r = GameContent.CreateLoader().Load(sources, GameContent.GameVersion);
            foreach (var m in r.Messages) Debug.Log(m.ToString());
            EditorApplication.Exit(r.Disabled.Count == 0 ? 0 : 1);
        }
    }
}
