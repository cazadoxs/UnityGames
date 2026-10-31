// PENDIENTE DE COMPILAR EN UNITY. Carga el juego base y los mods al arrancar (Vol. V §2 y §4.5).
using System.Collections.Generic;
using System.IO;
using Estandartes.Core.Mods;
using UnityEngine;

namespace Estandartes.Game
{
    /// <summary>
    /// Carga el contenido: primero StreamingAssets/Mods (juego base incluido) y después la carpeta de mods del usuario
    /// (persistentDataPath/Mods). Las suscripciones de Workshop se añadirán en la fase 8.
    /// </summary>
    public class ContentBootstrap : MonoBehaviour
    {
        public static LoadResult Content { get; private set; }

        [Tooltip("Orden de carga elegido por el jugador (ids de mod).")]
        public List<string> UserOrder = new List<string>();

        private void Awake()
        {
            if (Content != null) return;
            var sources = new List<IModSource>();
            AddFolder(Path.Combine(Application.streamingAssetsPath, "Mods"), sources);
            AddFolder(Path.Combine(Application.persistentDataPath, "Mods"), sources);
            Content = GameContent.CreateLoader().Load(sources, GameContent.GameVersion, UserOrder);
            foreach (var m in Content.Messages)
            {
                if (m.Severity == LoadSeverity.Error) Debug.LogError(m.ToString());
                else Debug.Log(m.ToString());
            }
            Debug.Log($"Contenido cargado: {Content.Registry.Count} elementos de {Content.Loaded.Count} paquetes; desactivados: {Content.Disabled.Count}");
            DontDestroyOnLoad(gameObject);
        }

        private static void AddFolder(string root, List<IModSource> sources)
        {
            if (!Directory.Exists(root)) return;
            foreach (var dir in Directory.GetDirectories(root))
                if (File.Exists(Path.Combine(dir, "mod.json"))) sources.Add(new DirectoryModSource(dir));
        }
    }
}
