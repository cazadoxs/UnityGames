using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Uso: dotnet run --project Tools/DotNet/SyntaxCheck -- Estandartes/Assets/_Project/Code
var root = args.Length > 0 ? args[0] : "Estandartes/Assets/_Project/Code";
var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: new[] { "UNITY_5_3_OR_NEWER", "UNITY_EDITOR", "UNITY_INCLUDE_TESTS" });
int files = 0, errors = 0;
foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(f => f))
{
    files++;
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), options, file);
    foreach (var d in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    {
        Console.WriteLine($"{Path.GetRelativePath(root, file)}{d.Location.GetLineSpan().StartLinePosition}: {d.Id} {d.GetMessage()}");
        errors++;
    }
}
Console.WriteLine($"{files} archivos, {errors} errores de sintaxis");
return errors == 0 ? 0 : 1;
