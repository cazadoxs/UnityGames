using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Estandartes.Core.Mods;

namespace Estandartes.Core.Maps
{
    /// <summary>
    /// Paquete de mapa <c>.estmap</c> (Vol. V §3.12): zip con manifiesto, datos de juego y escenarios.
    /// Las alturas, texturas y datos horneados viajan como binarios dentro del mismo zip.
    /// </summary>
    public sealed class MapPackage
    {
        public const int CurrentFormatVersion = 1;
        public const long MaxEntryBytes = 256L * 1024 * 1024;
        public const int MaxEntries = 4096;

        public static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ".json", ".r16", ".png", ".ktx2", ".bin", ".glb", ".ogg", ".wav" };

        public MapManifest Manifest = new MapManifest();
        public List<ZoneData> Zones = new List<ZoneData>();
        public Objectives.ObjectiveGraph Objectives = new Objectives.ObjectiveGraph();
        public List<AiHint> AiHints = new List<AiHint>();
        public List<NavLinkData> NavLinks = new List<NavLinkData>();
        public List<ScenarioEvent> Events = new List<ScenarioEvent>();
        public List<ScenarioDefinition> Scenarios = new List<ScenarioDefinition>();
        /// <summary>Otros archivos (alturas, splatmaps, vegetación…), por ruta interna.</summary>
        public Dictionary<string, byte[]> Binaries = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        public void Save(Stream output)
        {
            using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            void Text(string path, object obj)
            {
                var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
                using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                w.Write(Json.Serialize(obj));
            }
            Text("manifest.json", Manifest);
            Text("gameplay/zones.json", Zones);
            Text("gameplay/objectives.json", Objectives);
            Text("gameplay/ai_hints.json", AiHints);
            Text("gameplay/nav_links.json", NavLinks);
            Text("gameplay/events.json", Events);
            foreach (var s in Scenarios)
                Text($"scenarios/{Content.ContentId.Parse(s.Id).Name}.json", s);
            foreach (var kv in Binaries)
            {
                var e = zip.CreateEntry(kv.Key, CompressionLevel.Optimal);
                using var st = e.Open();
                st.Write(kv.Value, 0, kv.Value.Length);
            }
        }

        /// <summary>Carga con comprobaciones de seguridad (Vol. V §7): rutas, extensiones, tamaños y número de entradas.</summary>
        public static MapPackage Load(Stream input, List<string> errors)
        {
            var pkg = new MapPackage();
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
            if (zip.Entries.Count > MaxEntries) { errors.Add("demasiados archivos en el paquete"); return null; }
            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in zip.Entries)
            {
                string path = entry.FullName.Replace('\\', '/');
                if (path.EndsWith("/")) continue;
                if (!IsSafePath(path)) { errors.Add($"ruta no permitida '{path}'"); return null; }
                if (!AllowedExtensions.Contains(Path.GetExtension(path))) { errors.Add($"tipo de archivo no permitido '{path}'"); continue; }
                if (entry.Length > MaxEntryBytes) { errors.Add($"archivo demasiado grande '{path}'"); return null; }
                using var s = entry.Open();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) texts[path] = Encoding.UTF8.GetString(ms.ToArray());
                else pkg.Binaries[path] = ms.ToArray();
            }
            T Read<T>(string p, T fallback)
            {
                if (!texts.TryGetValue(p, out var t)) return fallback;
                try { return Json.Deserialize<T>(t); }
                catch (Exception e) { errors.Add($"{p}: {e.Message}"); return fallback; }
            }
            pkg.Manifest = Read<MapManifest>("manifest.json", null);
            if (pkg.Manifest == null) { errors.Add("falta manifest.json"); return null; }
            if (pkg.Manifest.FormatVersion > CurrentFormatVersion) { errors.Add($"formato {pkg.Manifest.FormatVersion} más nuevo que el del juego ({CurrentFormatVersion})"); return null; }
            MapMigrations.Apply(pkg.Manifest.FormatVersion, texts);
            pkg.Zones = Read("gameplay/zones.json", new List<ZoneData>());
            pkg.Objectives = Read("gameplay/objectives.json", new Objectives.ObjectiveGraph());
            pkg.AiHints = Read("gameplay/ai_hints.json", new List<AiHint>());
            pkg.NavLinks = Read("gameplay/nav_links.json", new List<NavLinkData>());
            pkg.Events = Read("gameplay/events.json", new List<ScenarioEvent>());
            foreach (var kv in texts)
                if (kv.Key.StartsWith("scenarios/", StringComparison.Ordinal))
                {
                    var s = Read<ScenarioDefinition>(kv.Key, null);
                    if (s != null) pkg.Scenarios.Add(s);
                }
            pkg.Manifest.FormatVersion = CurrentFormatVersion;
            return pkg;
        }

        public static bool IsSafePath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.StartsWith("/") || path.Contains(":")) return false;
            foreach (var part in path.Split('/')) if (part == ".." || part == ".") return false;
            return true;
        }

        /// <summary>Validación previa a publicar (Vol. V §3.10): grafo, zonas, escenarios.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>(Objectives.Validate());
            if (!Mods.ModManifest.IsValidModId(Manifest.Id)) errors.Add($"id de mapa '{Manifest.Id}' no válido");
            bool deploy0 = false, deploy1 = false;
            foreach (var z in Zones)
            {
                if (z.Polygon.Count < 3) errors.Add($"zona '{z.Id}' con menos de 3 vértices");
                if (z.Type == ZoneType.Deployment) { if (z.Owner == 0) deploy0 = true; if (z.Owner == 1) deploy1 = true; }
            }
            if (!deploy0 || !deploy1) errors.Add("cada bando necesita una zona de despliegue");
            foreach (var s in Scenarios) foreach (var e in s.Validate()) errors.Add($"escenario '{s.Id}': {e}");
            return errors;
        }
    }

    /// <summary>Migraciones del formato de mapa entre versiones (se amplía con cada versión nueva).</summary>
    public static class MapMigrations
    {
        public static void Apply(int fromVersion, Dictionary<string, string> texts)
        {
            // Versión 1: formato inicial, sin migraciones todavía.
        }
    }
}
