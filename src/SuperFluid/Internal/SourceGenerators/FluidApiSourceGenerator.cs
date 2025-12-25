using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SuperFluid.Internal.Comparers;
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

        IncrementalValuesProvider<AdditionalText> extraTexts = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".fluid.yml", StringComparison.OrdinalIgnoreCase));
        IncrementalValuesProvider<(string Name, string Content)> namesAndContents = extraTexts
            .Select((text, cancellationToken)
                => (Name: Path.GetFileNameWithoutExtension(text.Path),
                    Content: text.GetText(cancellationToken)?.ToString() ?? string.Empty))
            .WithComparer(new YamlContentComparer())
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

        // Report SF0012 if no .fluid.yml files found
        IncrementalValueProvider<bool> hasAnyFiles = context.AdditionalTextsProvider
            .Where(f => f.Path.EndsWith(".fluid.yml", StringComparison.OrdinalIgnoreCase))
            .Collect()
            .Select((files, _) => files.Length > 0);

        context.RegisterSourceOutput(hasAnyFiles, (spc, hasFiles) =>
        {
            if (!hasFiles)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NoFluidYamlFilesFound,
                    Location.None));
            }
        });
    }
}