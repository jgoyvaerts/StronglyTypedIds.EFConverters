using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StronglyTypedIds.EFConverters;

[Generator]
public class EfCoreValueConverterGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization needed for now
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var structsToProcess = new List<INamedTypeSymbol>();
        
        // Process all assemblies
        foreach (var metadataReference in context.Compilation.References)
        {
            // context.AddSource($"{metadataReference}.cs", SourceText.From("source", Encoding.UTF8));

            var assemblySymbol = context.Compilation.GetAssemblyOrModuleSymbol(metadataReference) as IAssemblySymbol;
            if (assemblySymbol == null)
                continue;

            foreach (var type in assemblySymbol.GlobalNamespace.GetNamespaceTypes())
            {
                if (type.GetAttributes().Any(IsStronglyTypedIdAttribute))
                {
                    structsToProcess.Add(type);
                }
            }
        }

        foreach (var structSymbol in structsToProcess)
        {
            var namespaceName = structSymbol.ContainingNamespace.ToDisplayString();
            var structName = structSymbol.Name;
            //get the type of the value of the struct by using reflection and looking at the type of the .Value property
            var structType = structSymbol.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == "Value")?.Type;
            var structTypeAsString = structType!.SpecialType switch
            {
                SpecialType.System_String => "string",
                SpecialType.System_Int32 => "int",
                _ => structType.Name
            };

            var source = GenerateEfCoreValueConverter(namespaceName, structName, structTypeAsString);
            context.AddSource($"{structName}_EfCoreValueConverter.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        var extensionsSource = GenerateDbContextExtensionMethod(structsToProcess);
        context.AddSource($"DbContextExtensions.g.cs", SourceText.From(extensionsSource, Encoding.UTF8));
    }

    private bool IsStronglyTypedIdAttribute(AttributeData attributeData)
    {
        return attributeData.AttributeClass?.Name == "StronglyTypedIdAttribute" || (attributeData.AttributeClass?.Name == "GeneratedCodeAttribute" &&
                                                                                    attributeData.ConstructorArguments[0].Value!.ToString()!
                                                                                        .Equals("StronglyTypedId",
                                                                                            StringComparison.InvariantCultureIgnoreCase));
    }

    private string GenerateDbContextExtensionMethod(List<INamedTypeSymbol> structSymbols)
    {
        var uniqueNameSpaces = structSymbols.Select(s => s.ContainingNamespace.ToDisplayString()).Distinct().ToList();
        var usingStatements = uniqueNameSpaces.Count > 0
            ? string.Join("\r\n", uniqueNameSpaces.Select(ns => $"using {ns};"))
            : string.Empty;
        return $@"#nullable enable
using System;
using Microsoft.EntityFrameworkCore;
{usingStatements}

namespace StronglyTypedIds.EFConverters
{{
    public static class DbContextExtensions
    {{
        public static void UseStronglyTypedIdConverters(this ModelConfigurationBuilder modelBuilder)
        {{
            {string.Join("\n", structSymbols.Select(s => $"modelBuilder.Properties<{s.Name}>().HaveConversion<{s.Name}Converter>();"))}
        }}
    }}
}}
";
    }

    private string GenerateEfCoreValueConverter(string namespaceName, string structName, string structType)
    {
        return $@"
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using {namespaceName};

namespace StronglyTypedIds.EFConverters
{{
    public class {structName}Converter : ValueConverter<{structName}, {structType}>
    {{
        public {structName}Converter() : this(null) {{ }}
        public {structName}Converter(ConverterMappingHints? mappingHints = null)
            : base(
                id => id.Value,
                value => new {structName}(value),
                mappingHints
            ) {{ }}
    }}
}}
";
    }
}

public static class RoslynExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol typeSymbol)
            {
                yield return typeSymbol;
            }

            if (member is INamespaceSymbol nestedNamespace)
            {
                foreach (var nestedType in nestedNamespace.GetNamespaceTypes())
                {
                    yield return nestedType;
                }
            }
        }
    }
}