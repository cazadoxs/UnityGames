using System;
using System.Collections.Generic;
using Estandartes.Core.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Estandartes.Core.Mods
{
    public enum LoadSeverity { Info, Warning, Error }

    public sealed class LoadMessage
    {
        public LoadSeverity Severity;
        public string ModId;
        public string File;
        public string Text;
        public override string ToString() => $"[{Severity}] {ModId}{(File != null ? "/" + File : "")}: {Text}";
    }

    public sealed class LoadedMod
    {
        public ModManifest Manifest;
        public IModSource Source;
        public bool ChangesRules;
        public int Additions, Replacements, Patches;
    }

    public sealed class LoadResult
    {
        public readonly ContentRegistry Registry = new ContentRegistry();
        public readonly List<LoadedMod> Loaded = new List<LoadedMod>();
        public readonly Dictionary<string, string> Disabled = new Dictionary<string, string>();
        public readonly List<LoadMessage> Messages = new List<LoadMessage>();
        public readonly Dictionary<string, Dictionary<string, string>> Localization = new Dictionary<string, Dictionary<string, string>>();

        public bool AnyRuleChanges
        {
            get
            {
                foreach (var m in Loaded) if (m.ChangesRules && m.Manifest.Id != ModLoader.BaseModId) return true;
                return false;
            }
        }
    }

    /// <summary>
    /// Cargador de contenido: el juego base y los mods pasan por aquí (Vol. V §2 y §4.5).
    /// Descubrir → validar manifiestos → dependencias y orden → leer y validar datos → aplicar.
    /// Un mod con errores se desactiva entero sin afectar al resto.
    /// </summary>
    public sealed class ModLoader
    {
        public const string BaseModId = "estandartes";

        /// <summary>Carpeta del mod → tipo de definición.</summary>
        private readonly Dictionary<string, Type> _folders = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly List<IReferenceValidator> _validators = new List<IReferenceValidator>();

        /// <summary>Tipos que cambian las reglas de juego (Vol. V §5).</summary>
        private readonly HashSet<Type> _ruleTypes = new HashSet<Type>
        {
            typeof(UnitDefinition), typeof(WeaponDefinition), typeof(SiegeEngineDefinition), typeof(DoctrineDefinition),
            typeof(PersonalityDefinition), typeof(FormationDefinition), typeof(MoraleTableDefinition), typeof(CurveDefinition),
        };

        public ModLoader()
        {
            RegisterFolder("data/units", typeof(UnitDefinition));
            RegisterFolder("data/weapons", typeof(WeaponDefinition));
            RegisterFolder("data/siege", typeof(SiegeEngineDefinition));
            RegisterFolder("data/factions", typeof(FactionDefinition));
            RegisterFolder("data/doctrines", typeof(DoctrineDefinition));
            RegisterFolder("data/personalities", typeof(PersonalityDefinition));
            RegisterFolder("data/formations", typeof(FormationDefinition));
            RegisterFolder("data/morale", typeof(MoraleTableDefinition));
            RegisterFolder("data/curves", typeof(CurveDefinition));
            _validators.Add(new CoreReferenceValidator());
        }

        public void RegisterFolder(string folder, Type type, bool changesRules = false)
        {
            if (!typeof(Definition).IsAssignableFrom(type)) throw new ArgumentException("Debe heredar de Definition", nameof(type));
            _folders[folder] = type;
            if (changesRules) _ruleTypes.Add(type);
        }

        public void AddValidator(IReferenceValidator v) => _validators.Add(v);

        public LoadResult Load(IEnumerable<IModSource> sources, SemVer gameVersion, IList<string> userOrder = null)
        {
            var result = new LoadResult();
            var candidates = new Dictionary<string, (ModManifest manifest, IModSource source)>(StringComparer.Ordinal);

            // 1. Manifiestos
            foreach (var src in sources)
            {
                ModManifest m;
                try
                {
                    if (!src.Exists("mod.json")) { Error(result, src.Origin, "mod.json", "falta el manifiesto"); continue; }
                    var obj = JObject.Parse(src.ReadText("mod.json"));
                    obj.Remove("$schema");
                    m = obj.ToObject<ModManifest>(Json.Serializer);
                }
                catch (Exception e) { Error(result, src.Origin, "mod.json", "manifiesto ilegible: " + e.Message); continue; }

                var errors = m.Validate(gameVersion);
                if (errors.Count > 0)
                {
                    foreach (var e in errors) Error(result, m.Id ?? src.Origin, "mod.json", e);
                    if (m.Id != null) result.Disabled[m.Id] = errors[0];
                    continue;
                }
                if (candidates.ContainsKey(m.Id)) { Error(result, m.Id, "mod.json", "id duplicado; se ignora la segunda copia"); continue; }
                candidates[m.Id] = (m, src);
            }

            // 2. Dependencias (en cascada) y orden topológico
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var id in new List<string>(candidates.Keys))
                {
                    var m = candidates[id].manifest;
                    foreach (var dep in m.Dependencies)
                    {
                        string reason = null;
                        if (!candidates.TryGetValue(dep.Id, out var d)) reason = $"falta la dependencia '{dep.Id}'";
                        else if (!VersionRange.Parse(dep.Version).Satisfies(SemVer.Parse(d.manifest.Version)))
                            reason = $"la dependencia '{dep.Id}' {d.manifest.Version} no cumple '{dep.Version}'";
                        if (reason != null)
                        {
                            Error(result, id, "mod.json", reason);
                            result.Disabled[id] = reason;
                            candidates.Remove(id);
                            changed = true;
                            break;
                        }
                    }
                }
            }

            var order = TopologicalOrder(candidates, userOrder, result);

            // 3. Carga mod a mod con zona de preparación: solo se aplica si no hay errores
            foreach (var id in order)
            {
                var (manifest, source) = candidates[id];
                var loaded = new LoadedMod { Manifest = manifest, Source = source };
                var staged = new List<(Definition def, bool isPatch)>();
                int errorsBefore = CountErrors(result);
                var lookup = new StagedLookup(result.Registry);

                foreach (var kv in _folders)
                {
                    foreach (var file in source.ListFiles(kv.Key, ".json"))
                    {
                        try
                        {
                            string text = source.ReadText(file);
                            if (file.EndsWith(".patch.json", StringComparison.Ordinal))
                            {
                                foreach (var patch in ReadArrayOrObject(text))
                                {
                                    var def = ApplyPatch(patch, kv.Value, lookup, id, file, result);
                                    if (def != null) { staged.Add((def, true)); lookup.Stage(def); loaded.Patches++; }
                                }
                                continue;
                            }
                            foreach (var token in ReadArrayOrObject(text))
                            {
                                var def = (Definition)token.ToObject(kv.Value, Json.Serializer);
                                if (!CheckId(def, id, file, result)) continue;
                                if (lookup.Contains(def.Id)) loaded.Replacements++; else loaded.Additions++;
                                staged.Add((def, false));
                                lookup.Stage(def);
                            }
                        }
                        catch (JsonException e) { Error(result, id, file, "JSON no válido: " + e.Message); }
                        catch (ArgumentException e) { Error(result, id, file, e.Message); }
                    }
                }

                // Referencias (contra el juego ya cargado + lo preparado de este mod)
                foreach (var (def, _) in staged)
                    foreach (var v in _validators)
                        foreach (var msg in v.Validate(def, lookup))
                            Error(result, id, def.Id, msg);

                if (CountErrors(result) > errorsBefore)
                {
                    result.Disabled[id] = "errores en el contenido (ver mensajes)";
                    continue;
                }

                foreach (var (def, isPatch) in staged)
                {
                    result.Registry.Register(def, id);
                    if (_ruleTypes.Contains(def.GetType()) || isPatch) loaded.ChangesRules = true;
                }
                loaded.ChangesRules |= manifest.ChangesRules;
                LoadLocalization(source, result);
                result.Loaded.Add(loaded);
                result.Messages.Add(new LoadMessage
                {
                    Severity = LoadSeverity.Info, ModId = id,
                    Text = $"cargado: {loaded.Additions} añadidos, {loaded.Replacements} reemplazos, {loaded.Patches} parches"
                });
            }
            return result;
        }

        private static IEnumerable<JToken> ReadArrayOrObject(string text)
        {
            var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
            using var reader = new JsonTextReader(new System.IO.StringReader(text)) { MaxDepth = 64 };
            var token = JToken.ReadFrom(reader, settings);
            if (token is JArray arr) { foreach (var t in arr) yield return StripSchema(t); }
            else yield return StripSchema(token);
        }

        /// <summary>El campo <c>$schema</c> solo sirve al editor de texto: se ignora al cargar.</summary>
        private static JToken StripSchema(JToken t)
        {
            if (t is JObject o) o.Remove("$schema");
            return t;
        }

        private static bool CheckId(Definition def, string modId, string file, LoadResult result)
        {
            if (!ContentId.TryParse(def.Id, out var cid)) { Error(result, modId, file, $"id '{def.Id}' no válido"); return false; }
            if (cid.Type != def.Kind) { Error(result, modId, file, $"id '{def.Id}': se esperaba el tipo '{def.Kind}'"); return false; }
            return true;
        }

        /// <summary>Parche por campos: <c>{ "patch": id, "set": {...}, "append": { "lista": [...] } }</c> (Vol. V §4.4).</summary>
        private static Definition ApplyPatch(JToken patch, Type type, StagedLookup lookup, string modId, string file, LoadResult result)
        {
            string target = (string)patch["patch"];
            if (target == null || !lookup.TryGet(target, out var existing) || existing.GetType() != type)
            {
                Error(result, modId, file, $"el parche apunta a '{target}', que no existe o no es de este tipo");
                return null;
            }
            var obj = JObject.FromObject(existing, Json.Serializer);
            if (patch["set"] is JObject set)
            {
                foreach (var prop in set.Properties())
                {
                    if (prop.Name == "id") { Error(result, modId, file, "un parche no puede cambiar el id"); return null; }
                    if (!obj.ContainsKey(prop.Name)) { Error(result, modId, file, $"campo desconocido '{prop.Name}'"); return null; }
                    if (obj[prop.Name] is JObject inner && prop.Value is JObject innerSet) inner.Merge(innerSet);
                    else obj[prop.Name] = prop.Value.DeepClone();
                }
            }
            if (patch["append"] is JObject append)
            {
                foreach (var prop in append.Properties())
                {
                    if (!(obj[prop.Name] is JArray list) || !(prop.Value is JArray add)) { Error(result, modId, file, $"'{prop.Name}' no es una lista"); return null; }
                    foreach (var item in add) list.Add(item.DeepClone());
                }
            }
            return (Definition)obj.ToObject(type, Json.Serializer);
        }

        private static void LoadLocalization(IModSource source, LoadResult result)
        {
            foreach (var file in source.ListFiles("localization", ".json"))
            {
                string lang = System.IO.Path.GetFileNameWithoutExtension(file);
                var table = Json.Deserialize<Dictionary<string, string>>(source.ReadText(file));
                if (!result.Localization.TryGetValue(lang, out var dict))
                    result.Localization[lang] = dict = new Dictionary<string, string>();
                foreach (var kv in table) dict[kv.Key] = kv.Value;
            }
        }

        /// <summary>Kahn con desempate determinista: juego base primero, luego el orden del usuario, luego alfabético.</summary>
        private static List<string> TopologicalOrder(Dictionary<string, (ModManifest manifest, IModSource source)> mods,
                                                     IList<string> userOrder, LoadResult result)
        {
            var edges = new Dictionary<string, List<string>>();
            var indegree = new Dictionary<string, int>();
            foreach (var id in mods.Keys) { edges[id] = new List<string>(); indegree[id] = 0; }
            void Edge(string before, string after)
            {
                if (!mods.ContainsKey(before) || before == after) return;
                edges[before].Add(after);
                indegree[after]++;
            }
            foreach (var kv in mods)
            {
                foreach (var d in kv.Value.manifest.Dependencies) Edge(d.Id, kv.Key);
                foreach (var a in kv.Value.manifest.LoadAfter) Edge(a, kv.Key);
                if (kv.Key != BaseModId) Edge(BaseModId, kv.Key);
            }

            int Rank(string id)
            {
                if (id == BaseModId) return -1;
                int i = userOrder?.IndexOf(id) ?? -1;
                return i >= 0 ? i : int.MaxValue;
            }

            var ready = new List<string>();
            foreach (var kv in indegree) if (kv.Value == 0) ready.Add(kv.Key);
            var order = new List<string>();
            while (ready.Count > 0)
            {
                ready.Sort((a, b) => { int c = Rank(a).CompareTo(Rank(b)); return c != 0 ? c : string.CompareOrdinal(a, b); });
                var next = ready[0];
                ready.RemoveAt(0);
                order.Add(next);
                foreach (var after in edges[next])
                    if (--indegree[after] == 0) ready.Add(after);
            }
            foreach (var kv in indegree)
            {
                if (kv.Value > 0)
                {
                    Error(result, kv.Key, "mod.json", "ciclo en dependencias u orden de carga");
                    result.Disabled[kv.Key] = "ciclo de dependencias";
                }
            }
            return order;
        }

        private static int CountErrors(LoadResult r)
        {
            int n = 0;
            foreach (var m in r.Messages) if (m.Severity == LoadSeverity.Error) n++;
            return n;
        }

        private static void Error(LoadResult r, string mod, string file, string text) =>
            r.Messages.Add(new LoadMessage { Severity = LoadSeverity.Error, ModId = mod, File = file, Text = text });
    }

    /// <summary>Consulta de contenido que ve el registro más lo preparado por el mod en curso.</summary>
    public sealed class StagedLookup : IContentLookup
    {
        private readonly ContentRegistry _registry;
        private readonly Dictionary<string, Definition> _staged = new Dictionary<string, Definition>(StringComparer.Ordinal);

        public StagedLookup(ContentRegistry registry) { _registry = registry; }

        public void Stage(Definition def) => _staged[def.Id] = def;

        public bool Contains(string id) => _staged.ContainsKey(id) || _registry.Contains(id);

        public bool TryGet(string id, out Definition def)
        {
            if (_staged.TryGetValue(id, out def)) return true;
            return _registry.TryGet(id, out def);
        }

        public bool TryGet<T>(string id, out T def) where T : Definition
        {
            def = null;
            if (!TryGet(id, out Definition d)) return false;
            def = d as T;
            return def != null;
        }
    }

    public interface IContentLookup
    {
        bool Contains(string id);
        bool TryGet<T>(string id, out T def) where T : Definition;
    }

    /// <summary>Comprueba las referencias de una definición. Devuelve mensajes de error.</summary>
    public interface IReferenceValidator
    {
        IEnumerable<string> Validate(Definition def, IContentLookup lookup);
    }

    /// <summary>Referencias del contenido básico (unidades, facciones, doctrinas).</summary>
    public sealed class CoreReferenceValidator : IReferenceValidator
    {
        public IEnumerable<string> Validate(Definition def, IContentLookup lookup)
        {
            switch (def)
            {
                case UnitDefinition u:
                    if (u.Weapons == null || u.Weapons.Count == 0) yield return "la unidad no tiene armas";
                    else foreach (var w in u.Weapons) if (!lookup.TryGet<WeaponDefinition>(w, out _)) yield return $"arma desconocida '{w}'";
                    if (u.SquadSize < 1 || u.SquadSize > 500) yield return $"squadSize fuera de rango ({u.SquadSize})";
                    if (u.Armor.Cut < 0 || u.Armor.Cut > 1 || u.Armor.Pierce < 0 || u.Armor.Pierce > 1 || u.Armor.Blunt < 0 || u.Armor.Blunt > 1)
                        yield return "armadura fuera de [0, 1]";
                    foreach (var c in u.Capabilities) if (!Capabilities.IsKnown(c)) yield return $"capacidad desconocida '{c}'";
                    break;
                case FactionDefinition f:
                    if (f.Doctrine != null && !lookup.TryGet<DoctrineDefinition>(f.Doctrine, out _)) yield return $"doctrina desconocida '{f.Doctrine}'";
                    foreach (var u in f.Units) if (!lookup.TryGet<UnitDefinition>(u, out _)) yield return $"unidad desconocida '{u}'";
                    break;
                case DoctrineDefinition d:
                    foreach (var kv in d.Weights)
                    {
                        if (!lookup.TryGet<UnitDefinition>(kv.Key, out _) && !lookup.TryGet<SiegeEngineDefinition>(kv.Key, out _))
                            yield return $"peso para unidad desconocida '{kv.Key}'";
                        if (kv.Value < 0) yield return $"peso negativo para '{kv.Key}'";
                    }
                    break;
                case PersonalityDefinition p:
                    if (p.CasualtyAversion < 0 || p.CasualtyAversion > 3) yield return "casualtyAversion fuera de [0, 3]";
                    if (p.RiskAversion < 0 || p.RiskAversion > 3) yield return "riskAversion fuera de [0, 3]";
                    if (p.MinReserve < 0 || p.MinReserve > 0.6f) yield return "minReserve fuera de [0, 0,6]";
                    if (p.Temperature <= 0 || p.Temperature > 5) yield return "temperature fuera de (0, 5]";
                    break;
                case MoraleTableDefinition m:
                    foreach (var e in m.Events) if (Math.Abs(e.Magnitude) > 50) yield return $"magnitud de moral excesiva en '{e.Event}'";
                    break;
            }
        }
    }
}
