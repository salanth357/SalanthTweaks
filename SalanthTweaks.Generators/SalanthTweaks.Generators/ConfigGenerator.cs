using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace SalanthTweaks.Generators;

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class ConfigGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "SalanthTweaks.Attributes.TweakConfigAttribute",
            predicate: static (syntaxNode, _) => syntaxNode is PropertyDeclarationSyntax,
            transform: (ctx, _) =>
            {
                var containingClass = ctx.TargetSymbol.ContainingType;
                var displayFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
                    SymbolDisplayGlobalNamespaceStyle.Omitted);
                var m = new Model(
                    Namespace: containingClass.ContainingNamespace?.ToDisplayString(displayFormat) ?? string.Empty,
                    ClassName: containingClass.Name,
                    PropertyName: ctx.TargetSymbol.Name,
                    TypeName: (ctx.TargetSymbol as IPropertySymbol)?.Type.ToDisplayString(displayFormat) ?? string.Empty
                );
                return m;
            });
        
        context.RegisterSourceOutput(pipeline, GenerateCode);
    }
    private static void GenerateCode(SourceProductionContext context, Model model)
    {
        var sourceText = SourceText.From($$"""
using System.IO;
using Dalamud.Plugin; 
using Newtonsoft.Json;
using SalanthTweaks.Config;

namespace {{model.Namespace}};
partial class {{model.ClassName}}
{
    private string ConfigPath => Path.Combine(Service.Get<IDalamudPluginInterface>().GetPluginConfigDirectory(), GetType().Name + ".json");

    public void SaveConfig() => File.WriteAllText(ConfigPath, JsonConvert.SerializeObject({{model.PropertyName}}, Formatting.Indented));

    public void LoadConfig() 
    {
        if (File.Exists(ConfigPath)) {
            {{model.PropertyName}} = JsonConvert.DeserializeObject<{{model.TypeName}}>(File.ReadAllText(ConfigPath));
        } else {
            {{model.PropertyName}} = new();
        }
        if ({{model.PropertyName}}.Update()) SaveConfig();
    }
}
""", Encoding.UTF8);
        // Add the source code to the compilation.
        context.AddSource($"{model.ClassName}.g.cs", sourceText);
    }

    private record Model(string Namespace, string ClassName, string PropertyName, string TypeName)
    {
        public string Namespace { get; } = Namespace;
        public string ClassName { get; } = ClassName;
        public string PropertyName { get; } = PropertyName;
        public string TypeName { get; } = TypeName;
    }
}