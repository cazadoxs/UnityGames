using System;
using System.IO;
using Estandartes.Core.Content;
using Estandartes.Core.Maps;
using Estandartes.Core.Mods;
using Estandartes.Core.Strategy;
using NJsonSchema;
using NJsonSchema.NewtonsoftJson.Generation;

// Uso: dotnet run --project Tools/DotNet/SchemaGen -- <carpeta de salida>
//      dotnet run --project Tools/DotNet/SchemaGen -- validate <carpeta de esquemas> <carpeta de un mod>
if (args.Length == 3 && args[0] == "validate") return Validate(args[1], args[2]);
var output = args.Length > 0 ? args[0] : "Docs/Modding/schemas";
Directory.CreateDirectory(output);
var settings = new NewtonsoftJsonSchemaGeneratorSettings
{
    SerializerSettings = Json.Settings,
    SchemaType = SchemaType.JsonSchema,
    FlattenInheritanceHierarchy = true,
    DefaultReferenceTypeNullHandling = NJsonSchema.Generation.ReferenceTypeNullHandling.NotNull,
};
void Gen<T>(string name)
{
    var schema = NewtonsoftJsonSchemaGenerator.FromType<T>(settings);
    schema.Title = name;
    File.WriteAllText(Path.Combine(output, name + ".schema.json"), schema.ToJson() + "\n");
    Console.WriteLine($"{name}.schema.json");
}
Gen<ModManifest>("mod");
Gen<UnitDefinition>("unit");
Gen<WeaponDefinition>("weapon");
Gen<SiegeEngineDefinition>("siege");
Gen<FactionDefinition>("faction");
Gen<DoctrineDefinition>("doctrine");
Gen<PersonalityDefinition>("personality");
Gen<FormationDefinition>("formation");
Gen<MoraleTableDefinition>("morale");
Gen<CurveDefinition>("curve");
Gen<HtnDomainDefinition>("htn");
Gen<ScenarioDefinition>("scenario");
Gen<MapManifest>("map-manifest");
return 0;

static int Validate(string schemas, string mod)
{
    var map = new System.Collections.Generic.Dictionary<string, string>
    {
        ["data/units"] = "unit", ["data/weapons"] = "weapon", ["data/siege"] = "siege", ["data/factions"] = "faction",
        ["data/doctrines"] = "doctrine", ["data/personalities"] = "personality", ["data/formations"] = "formation",
        ["data/morale"] = "morale", ["data/curves"] = "curve", ["data/htn"] = "htn", ["scenarios"] = "scenario",
    };
    int errors = 0, files = 0;
    var manifest = JsonSchema.FromFileAsync(Path.Combine(schemas, "mod.schema.json")).Result;
    foreach (var e in manifest.Validate(File.ReadAllText(Path.Combine(mod, "mod.json")))) { Console.WriteLine($"mod.json: {e}"); errors++; }
    foreach (var kv in map)
    {
        var dir = Path.Combine(mod, kv.Key);
        if (!Directory.Exists(dir)) continue;
        var schema = JsonSchema.FromFileAsync(Path.Combine(schemas, kv.Value + ".schema.json")).Result;
        foreach (var f in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
        {
            if (f.EndsWith(".patch.json")) continue;
            files++;
            foreach (var e in schema.Validate(File.ReadAllText(f))) { Console.WriteLine($"{Path.GetRelativePath(mod, f)}: {e}"); errors++; }
        }
    }
    Console.WriteLine($"{files} archivos validados, {errors} errores");
    return errors == 0 ? 0 : 1;
}
