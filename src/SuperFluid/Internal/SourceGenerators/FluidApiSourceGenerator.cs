using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Diagnostics;
using SuperFluid.Internal.Parsers;
using SuperFluid.Internal.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuperFluid.Internal.SourceGenerators;

[Generator]
internal class FluidApiSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //SpinWait.SpinUntil(() => Debugger.IsAttached); // Manually attach debugger here

        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("SuperFluid.Attributes.g.cs",
                SourceText.From(AttributeDefinitions.Source, Encoding.UTF8)));

        // ---- YAML path ----
        IncrementalValuesProvider<AdditionalText> extraTexts = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".fluid.yml", StringComparison.OrdinalIgnoreCase));
        IncrementalValuesProvider<(string Name, string Content)> namesAndContents = extraTexts
            .Select((text, cancellationToken)
                => (Name: Path.GetFileNameWithoutExtension(text.Path),
                    Content: text.GetText(cancellationToken)?.ToString() ?? string.Empty))
            .WithTrackingName("YamlContent");

        context.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();
            FluidGeneratorService generatorService = new(deserializer, new FluidApiDefinitionParser());

            GenerationResult result = generatorService.Generate(
                nameAndContent.Content,
                nameAndContent.Name);

            // Report all diagnostics
            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }

            // Add generated sources if successful
            if (result.IsSuccess && result.GeneratedFiles is not null)
            {
                foreach (KeyValuePair<string, string> kvp in result.GeneratedFiles)
                {
                    spc.AddSource(kvp.Key, SourceText.From(kvp.Value, Encoding.UTF8));
                }
            }
        });

        // ---- Grammar-interface path ----
        IncrementalValuesProvider<INamedTypeSymbol> grammarInterfaces = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "SuperFluid.FluidApiGrammarAttribute",
                predicate: (node, _) => node is InterfaceDeclarationSyntax,
                transform: (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);

        context.RegisterSourceOutput(grammarInterfaces, (spc, grammarSymbol) =>
        {
            GrammarInterfaceReader reader = new();
            FluidApiDefinition definition = reader.Read(grammarSymbol);
            FluidGeneratorService generatorService = new(new FluidApiDefinitionParser());
            GenerationResult result = generatorService.Generate(definition, grammarSymbol.ToDisplayString());

            foreach (Diagnostic diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }

            if (result.IsSuccess && result.GeneratedFiles is not null)
            {
                foreach (KeyValuePair<string, string> kvp in result.GeneratedFiles)
                {
                    spc.AddSource(kvp.Key, SourceText.From(kvp.Value, Encoding.UTF8));
                }
            }
        });

        // ---- SF0012: report when neither YAML files nor grammar interfaces are present ----
        IncrementalValueProvider<bool> hasAnyYaml = context.AdditionalTextsProvider
            .Where(f => f.Path.EndsWith(".fluid.yml", StringComparison.OrdinalIgnoreCase))
            .Collect()
            .Select((files, _) => files.Length > 0);

        IncrementalValueProvider<bool> hasAnyGrammarInterface = grammarInterfaces
            .Collect()
            .Select((symbols, _) => symbols.Length > 0);

        IncrementalValueProvider<(bool Yaml, bool Grammar)> combined = hasAnyYaml.Combine(hasAnyGrammarInterface);

        context.RegisterSourceOutput(combined, (spc, tuple) =>
        {
            if (!tuple.Yaml && !tuple.Grammar)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NoFluidYamlFilesFound,
                    Location.None));
            }
        });
    }
}