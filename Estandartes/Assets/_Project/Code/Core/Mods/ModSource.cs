using System;
using System.Collections.Generic;
using System.IO;

namespace Estandartes.Core.Mods
{
    /// <summary>Acceso de solo lectura a los archivos de un mod (carpeta, zip o memoria).</summary>
    public interface IModSource
    {
        string Origin { get; }
        bool Exists(string relativePath);
        string ReadText(string relativePath);
        /// <summary>Archivos (rutas relativas con '/') dentro de una carpeta relativa, recursivo.</summary>
        IEnumerable<string> ListFiles(string relativeDirectory, string extension);
    }

    /// <summary>Mod en una carpeta del disco (carpeta <c>Mods/</c>, StreamingAssets o Workshop).</summary>
    public sealed class DirectoryModSource : IModSource
    {
        private readonly string _root;
        public string Origin => _root;

        public DirectoryModSource(string root)
        {
            _root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, '/') + Path.DirectorySeparatorChar;
        }

        private string Resolve(string relativePath)
        {
            string full = Path.GetFullPath(Path.Combine(_root, relativePath));
            // Seguridad (Vol. V §7): ninguna ruta puede salir de la carpeta del mod.
            if (!full.StartsWith(_root, StringComparison.Ordinal))
                throw new UnauthorizedAccessException($"Ruta fuera del mod: {relativePath}");
            return full;
        }

        public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));
        public string ReadText(string relativePath) => File.ReadAllText(Resolve(relativePath));

        public IEnumerable<string> ListFiles(string relativeDirectory, string extension)
        {
            string dir = Resolve(relativeDirectory);
            if (!Directory.Exists(dir)) yield break;
            var files = Directory.GetFiles(dir, "*" + extension, SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal); // orden determinista
            foreach (var f in files)
                yield return f.Substring(_root.Length).TrimStart(Path.DirectorySeparatorChar, '/').Replace('\\', '/');
        }
    }

    /// <summary>Mod en memoria (pruebas y contenido generado).</summary>
    public sealed class MemoryModSource : IModSource
    {
        private readonly SortedDictionary<string, string> _files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        public string Origin { get; }

        public MemoryModSource(string origin) { Origin = origin; }

        public MemoryModSource Add(string path, string text) { _files[path] = text; return this; }

        public bool Exists(string relativePath) => _files.ContainsKey(relativePath);
        public string ReadText(string relativePath) => _files[relativePath];

        public IEnumerable<string> ListFiles(string relativeDirectory, string extension)
        {
            string prefix = relativeDirectory.TrimEnd('/') + "/";
            foreach (var k in _files.Keys)
                if (k.StartsWith(prefix, StringComparison.Ordinal) && k.EndsWith(extension, StringComparison.Ordinal))
                    yield return k;
        }
    }
}
