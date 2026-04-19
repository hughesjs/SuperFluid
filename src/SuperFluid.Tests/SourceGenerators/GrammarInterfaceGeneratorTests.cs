using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Parsers;
using SuperFluid.Internal.Services;
using SuperFluid.Internal.SourceGenerators;
using SuperFluid.Tests.TestHelpers;

namespace SuperFluid.Tests.SourceGenerators;

/// <summary>
/// Smoke tests for the C# attribute-based grammar front-end wired into the incremental generator.
/// The generator's post-initialisation output injects the SuperFluid attribute declarations into
/// every consuming compilation, so test sources do NOT need to pre-declare them.
/// </summary>
public class GrammarInterfaceGeneratorTests
{
    // A minimal two-method grammar that produces one state interface and one compound interface.
    private const string CarActorGrammarSource = """
        using SuperFluid;
        namespace Cars
        {
            [FluidApiGrammar]
            internal interface ICarActorGrammar
            {
                [Initial, TransitionsTo(nameof(Unlock))]
                void Initialize();

                [TransitionsTo]
                void Unlock();
            }
        }
        """;

    // -------------------------------------------------------------------------
    // Helper — run the generator over a grammar-interface compilation
    // -------------------------------------------------------------------------

    private static GeneratorDriverRunResult RunGeneratorOverGrammarSource(string grammarSource)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilationWithGrammarSource(grammarSource);
        FluidApiSourceGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    // -------------------------------------------------------------------------
    // Test 1: at least one .fluid.g.cs source is produced
    // -------------------------------------------------------------------------

    [Fact]
    public void GrammarInterfaceProducesGeneratedOutput()
    {
        GeneratorDriverRunResult runResult = RunGeneratorOverGrammarSource(CarActorGrammarSource);

        ImmutableArray<GeneratedSourceResult> sources = runResult.Results[0].GeneratedSources;

        // Expect the ambient attributes source plus at least one grammar-driven output
        sources.ShouldContain(s => s.HintName.EndsWith(".fluid.g.cs"));
    }

    // -------------------------------------------------------------------------
    // Test 2: compound interface is emitted
    // -------------------------------------------------------------------------

    [Fact]
    public void GrammarInterfaceGeneratesCompoundInterface()
    {
        GeneratorDriverRunResult runResult = RunGeneratorOverGrammarSource(CarActorGrammarSource);

        ImmutableArray<GeneratedSourceResult> sources = runResult.Results[0].GeneratedSources;

        // GrammarInterfaceReader strips "Grammar" suffix → Name = "ICarActor"
        sources.ShouldContain(s => s.HintName == "ICarActor.fluid.g.cs",
            "Expected compound interface ICarActor.fluid.g.cs to be generated from ICarActorGrammar");
    }

    // -------------------------------------------------------------------------
    // Test 3: at least one state interface is emitted
    // -------------------------------------------------------------------------

    [Fact]
    public void GrammarInterfaceGeneratesStateInterfaces()
    {
        GeneratorDriverRunResult runResult = RunGeneratorOverGrammarSource(CarActorGrammarSource);

        ImmutableArray<GeneratedSourceResult> sources = runResult.Results[0].GeneratedSources;

        // The two-method grammar produces at least one state interface (e.g. ICanUnlock.fluid.g.cs)
        sources.ShouldContain(s => s.HintName.EndsWith(".fluid.g.cs") && s.HintName != "ICarActor.fluid.g.cs",
            "Expected at least one state interface .fluid.g.cs to be generated");
    }

    // -------------------------------------------------------------------------
    // Test 4: SF0010 flows through the shared pipeline when given an invalid name
    //
    // C# enforces valid identifiers on grammar interface method names, so we cannot
    // trigger SF0010 end-to-end through the generator for a grammar interface.  Instead
    // we exercise the shared Generate(FluidApiDefinition, string) overload directly —
    // this confirms the diagnostic pipeline is connected and returns SF0010 for an
    // invalid identifier, regardless of the source front-end that produced the DTO.
    // -------------------------------------------------------------------------

    [Fact]
    public void GrammarInterfaceInvalidIdentifierReportsSF0010()
    {
        FluidApiDefinition definition = new()
        {
            Name = "Invalid Name With Spaces",
            Namespace = "Test",
            InitialState = new() { Name = "Start", CanTransitionTo = [] },
            Methods = []
        };

        FluidGeneratorService service = new(new FluidApiDefinitionParser());
        GenerationResult result = service.Generate(definition, "ITestGrammar");

        result.IsSuccess.ShouldBeFalse();
        result.Diagnostics.ShouldContain(d => d.Id == "SF0010");
    }

    // -------------------------------------------------------------------------
    // Test 5: SF0012 is NOT reported when a grammar interface is present (no YAML)
    // -------------------------------------------------------------------------

    [Fact]
    public void SF0012NotReportedWhenGrammarInterfacePresent()
    {
        GeneratorDriverRunResult runResult = RunGeneratorOverGrammarSource(CarActorGrammarSource);

        Diagnostic[] sf0012 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0012")
            .ToArray();

        sf0012.ShouldBeEmpty("SF0012 should not be reported when a [FluidApiGrammar] interface is present");
    }

    // -------------------------------------------------------------------------
    // Test 6: SF0012 is NOT reported when only YAML is present (regression check)
    // -------------------------------------------------------------------------

    [Fact]
    public void SF0012NotReportedWhenYamlPresent()
    {
        string yaml = """
            Name: "ITestActor"
            Namespace: "Test"
            InitialState:
              Name: "Start"
              CanTransitionTo: []
            Methods: []
            """;

        CSharpCompilation compilation = CompilationHelper.CreateCompilation();
        FluidApiSourceGenerator generator = new();
        AdditionalText yamlFile = CompilationHelper.CreateAdditionalText("Test.fluid.yml", yaml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts([yamlFile]);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Diagnostic[] sf0012 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0012")
            .ToArray();

        sf0012.ShouldBeEmpty("SF0012 should not be reported when a .fluid.yml file is present");
    }

    // -------------------------------------------------------------------------
    // Test 7: SF0012 IS reported when neither YAML nor grammar interfaces are present
    // -------------------------------------------------------------------------

    [Fact]
    public void SF0012ReportedWhenNeitherPresent()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation();
        FluidApiSourceGenerator generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        // No additional texts; no grammar interface in the compilation
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Diagnostic[] sf0012 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0012")
            .ToArray();

        sf0012.ShouldNotBeEmpty("SF0012 should be reported when no grammar sources are found");
        sf0012[0].Severity.ShouldBe(DiagnosticSeverity.Info);
    }

    // -------------------------------------------------------------------------
    // Test 8: SF0018 reported (not thrown) when grammar interface has no [Initial] method
    // -------------------------------------------------------------------------

    [Fact]
    public void GrammarInterfaceWithoutInitialMethodReportsSF0018()
    {
        string grammar = @"
using SuperFluid;
namespace Test
{
    [FluidApiGrammar]
    internal interface IOrphanGrammar
    {
        [TransitionsTo(nameof(B))]
        void A();

        [TransitionsTo]
        void B();
    }
}";

        CSharpCompilation compilation = CompilationHelper.CreateCompilationWithGrammarSource(grammar);
        FluidApiSourceGenerator generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Diagnostic[] sf0018 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0018")
            .ToArray();

        sf0018.ShouldNotBeEmpty("SF0018 should be reported when a grammar interface is missing [Initial]");
        sf0018[0].Severity.ShouldBe(DiagnosticSeverity.Error);
        sf0018[0].GetMessage().ShouldContain("IOrphanGrammar");

        // Generator exception (CS8032-style) must NOT be present — the reader's throw must be
        // converted to a diagnostic, not escape the generator.
        Diagnostic[] generatorFailures = runResult.Results[0].Diagnostics
            .Where(d => d.Id.StartsWith("CS8", System.StringComparison.Ordinal))
            .ToArray();
        generatorFailures.ShouldBeEmpty("Reader exception must be converted to a diagnostic, not escape the generator");
    }
}
