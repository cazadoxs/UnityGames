using System;
using System.IO;

namespace Estandartes.Tests
{
    /// <summary>Rutas del proyecto válidas tanto en Unity como en las pruebas de .NET fuera de Unity.</summary>
    public static class TestPaths
    {
        public static string StreamingAssets
        {
            get
            {
#if UNITY_5_3_OR_NEWER
                return UnityEngine.Application.streamingAssetsPath;
#else
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Estandartes", "Assets"))) dir = dir.Parent;
                if (dir == null) throw new DirectoryNotFoundException("No se encuentra la carpeta del proyecto Estandartes");
                return Path.Combine(dir.FullName, "Estandartes", "Assets", "StreamingAssets");
#endif
            }
        }

        public static string BaseMod => Path.Combine(StreamingAssets, "Mods", "estandartes");
    }
}
