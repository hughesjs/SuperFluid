using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Diagnostics;
using SuperFluid.Internal.Exceptions;
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

        IncrementalValuesProvider<AdditionalText> extraTexts = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".fluid.yml", StringComparison.OrdinalIgnoreCase));
        IncrementalValuesProvider<(string Name, string Content)> namesAndContents = extraTexts
            .Select((text, cancellationToken)
                => (Name: Path.GetFileNameWithoutExtension(text.Path),
                    Content: text.GetText(cancellationToken)?.ToString() ?? string.Empty))
            .WithTrackingName("YamlContent");

        context.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
        {
            FluidGeneratorService generatorService = new(new FluidApiDefinitionParser());

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

        IncrementalValuesProvider<INamedTypeSymbol> grammarInterfaces = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "SuperFluid.FluidApiGrammarAttribute",
                predicate: (node, _) => node is InterfaceDeclarationSyntax,
                transform: (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);

        context.RegisterSourceOutput(grammarInterfaces, (spc, grammarSymbol) =>
        {
            FluidApiDefinition definition;
            try
            {
                GrammarInterfaceReader reader = new();
                definition = reader.Read(grammarSymbol);
            }
            catch (MissingInitialMethodException ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MissingInitialMethod,
                    Location.None,
                    ex.GrammarInterfaceName));
                return;
            }
            catch (MultipleInitialMethodsException ex)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MultipleInitialMethods,
                    Location.None,
                    ex.GrammarInterfaceName,
                    string.Join(", ", ex.MethodNames)));
                return;
            }
            catch (Exception ex)
            {
                // Include the full ToString (with stack trace) rather than just ex.Message so
                // unexpected Roslyn-symbol edge cases surface enough detail to be debugged from
                // the diagnostic output alone. SF0011 is an error-severity catch-all so the
                // verbosity is appropriate.
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnexpectedGenerationError,
                    Location.None,
                    ex.ToString()));
                return;
            }

            FluidGeneratorService grammarService = new(new FluidApiDefinitionParser());
            GenerationResult result = grammarService.Generate(definition, grammarSymbol.ToDisplayString());

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

        // Extract actor names from the YAML pipeline by doing a lightweight deserialisation of just
        // the Name field. If deserialisation fails the entry is skipped (SF0001/SF0004 will already
        // have been reported by the main YAML output stage above).
        // NOTE (V1): when a collision is detected via SF0017, both the YAML and grammar-interface
        // generation paths will still fire and will each attempt to add their output under the same
        // hint name. This will surface as a duplicate-hint build error if left uncorrected. A future
        // improvement could suppress emission on detected collision, but SF0017 is sufficient for V1.
        IncrementalValueProvider<ImmutableArray<string>> yamlActorNames = namesAndContents
            .Select((nameAndContent, _) => ExtractYamlActorName(nameAndContent.Content))
            .Where(name => name is not null)
            .Select((name, _) => name!)
            .Collect();

        IncrementalValueProvider<ImmutableArray<string>> grammarActorNames = grammarInterfaces
            .Select((sym, _) => GrammarInterfaceReader.DeriveActorName(sym.Name))
            .Collect();

        IncrementalValueProvider<(ImmutableArray<string> YamlNames, ImmutableArray<string> GrammarNames)> collisionInput =
            yamlActorNames.Combine(grammarActorNames);

        context.RegisterSourceOutput(collisionInput, (spc, pair) =>
        {
            // Use a HashSet for O(1) lookup of YAML actor names
            System.Collections.Generic.HashSet<string> yamlSet = new(pair.YamlNames, StringComparer.Ordinal);

            foreach (string grammarName in pair.GrammarNames)
            {
                if (yamlSet.Contains(grammarName))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateActorDeclaration,
                        Location.None,
                        grammarName));
                }
            }
        });

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

    // Dedicated deserialiser for the SF0017 actor-name sniff: IgnoreUnmatchedProperties lets us
    // read only the Name field without binding the rest of the document. Shared across invocations
    // because deserialiser instances are immutable after construction.
    private static readonly IDeserializer ActorNameDeserialiser = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Performs a lightweight deserialisation of YAML content to extract just the actor name (the
    /// <c>Name</c> field at the root level). Returns <c>null</c> if the content is empty or cannot
    /// be deserialised — in those cases the main YAML pipeline will already report SF0001/SF0004.
    /// </summary>
    private static string? ExtractYamlActorName(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
        {
            return null;
        }

        try
        {
            YamlNameStub? stub = ActorNameDeserialiser.Deserialize<YamlNameStub>(yamlContent);
            return stub?.Name;
        }
        catch
        {
            // Deserialisation failure is handled by the main YAML pipeline — skip here
            return null;
        }
    }

    /// <summary>Minimal YAML stub used by <see cref="ExtractYamlActorName"/> to read just the actor name.</summary>
    private sealed class YamlNameStub
    {
        public string? Name { get; set; }
    }
}
