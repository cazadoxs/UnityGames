using System;
using System.Collections.Generic;

namespace Estandartes.Core.Mods
{
    [Serializable]
    public class ModDependency
    {
        public string Id;
        public string Version = "*";
    }

    /// <summary>Contenido de <c>mod.json</c> (Vol. V §4.2).</summary>
    [Serializable]
    public class ModManifest
    {
        public string Id;
        public string Name;
        public string Version;
        public string GameVersion = "*";
        public List<string> Authors = new List<string>();
        public string Description;
        public List<ModDependency> Dependencies = new List<ModDependency>();
        public List<string> LoadAfter = new List<string>();
        public List<string> Content = new List<string>();
        public string Era;
        public List<string> Tags = new List<string>();
        public bool ChangesRules;

        public const int MaxIdLength = 64;

        /// <summary>Valida el manifiesto. Devuelve los errores encontrados.</summary>
        public List<string> Validate(SemVer gameVersion)
        {
            var errors = new List<string>();
            if (!IsValidModId(Id)) errors.Add($"id '{Id}' no válido (minúsculas, dígitos y '_', máx. {MaxIdLength})");
            if (!SemVer.TryParse(Version, out _)) errors.Add($"version '{Version}' no válida");
            if (!VersionRange.TryParse(GameVersion, out var range, out var err)) errors.Add($"gameVersion: {err}");
            else if (!range.Satisfies(gameVersion)) errors.Add($"no es compatible con la versión del juego {gameVersion} (requiere {GameVersion})");
            foreach (var d in Dependencies ?? new List<ModDependency>())
            {
                if (!IsValidModId(d.Id)) errors.Add($"dependencia con id no válido '{d.Id}'");
                if (!VersionRange.TryParse(d.Version, out _, out var e2)) errors.Add($"dependencia '{d.Id}': {e2}");
            }
            return errors;
        }

        public static bool IsValidModId(string id) =>
            !string.IsNullOrEmpty(id) && id.Length <= MaxIdLength && Estandartes.Core.Content.ContentId.IsValidPart(id);
    }
}
