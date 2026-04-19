using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using SuperFluid.Internal.SourceGenerators;
using SuperFluid.Tests.TestHelpers;

namespace SuperFluid.Tests.SourceGenerators;

/// <summary>
/// Proves that a grammar-interface declaration and a YAML declaration of the same state machine
/// produce byte-identical generated output from the shared parser/code-emission pipeline.
/// </summary>
public class GrammarYamlParityTests
{
    // The full car actor grammar expressed as a [FluidApiGrammar] interface.
    // This is the exact semantic equivalent of DemoApiDefinition.fluid.yml.
    // Notes on argument ordering:
    //   C# requires that optional parameters follow mandatory parameters, so
    //   Start<T,X> declares (int speed, string direction = "Forward", bool hotwire = false).
    //   The parser reorders the YAML's [direction(opt), speed(req), hotwire(opt)] into the
    //   same [speed, direction, hotwire] sequence — so both paths produce identical output.
    private const string FullCarActorGrammarSource = """
        using SuperFluid;
        namespace SuperFluid.Tests.Cars
        {
            [FluidApiGrammar]
            internal interface ICarActorGrammar
            {
                [Initial, TransitionsTo(nameof(Unlock))]
                void Initialize();

                [TransitionsTo(nameof(Lock), nameof(Enter))]
                void Unlock();

                [TransitionsTo(nameof(Unlock))]
                void Lock();

                [TransitionsTo(nameof(Start), nameof(Exit))]
                void Enter();

                [TransitionsTo(nameof(Lock), nameof(Enter))]
                void Exit();

                [TransitionsTo(nameof(Stop), nameof(Build))]
                void Start<T, X>(int speed, string direction = "Forward", bool hotwire = false)
                    where T : class, INumber
                    where X : notnull;

                [TransitionsTo(nameof(Start), nameof(Exit))]
                void Stop();

                [ReturnType(typeof(string)), TransitionsTo]
                string Build(string color);
            }
        }
        """;

    private static GeneratorDriverRunResult RunGeneratorOverYaml(string yamlContent)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation();
        FluidApiSourceGenerator generator = new();
        AdditionalText yamlFile = CompilationHelper.CreateAdditionalText("DemoApiDefinition.fluid.yml", yamlContent);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts([yamlFile]);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    private static GeneratorDriverRunResult RunGeneratorOverGrammarSource(string grammarSource)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilationWithGrammarSource(grammarSource);
        FluidApiSourceGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    /// <summary>
    /// Collects the grammar-driven generated sources (all sources except the ambient attributes
    /// file) from a generator run result, keyed by hint name.
    /// </summary>
    private static Dictionary<string, string> CollectGrammarOutputs(GeneratorDriverRunResult runResult)
    {
        return runResult.Results[0].GeneratedSources
            .Where(s => s.HintName != "SuperFluid.Attributes.g.cs")
            .ToDictionary(s => s.HintName, s => s.SourceText.ToString());
    }

    // Parity test

    [Fact]
    public void GrammarInterfaceDeclarationProducesIdenticalOutputToEquivalentYaml()
    {
        string yamlContent = File.ReadAllText("DemoApiDefinition.fluid.yml");

        GeneratorDriverRunResult yamlResult    = RunGeneratorOverYaml(yamlContent);
        GeneratorDriverRunResult grammarResult = RunGeneratorOverGrammarSource(FullCarActorGrammarSource);

        // Neither path should report diagnostics (confirms both are valid inputs)
        Diagnostic[] yamlDiagnostics = yamlResult.Results[0].Diagnostics
            .Where(d => d.Id.StartsWith("SF"))
            .ToArray();
        Diagnostic[] grammarDiagnostics = grammarResult.Results[0].Diagnostics
            .Where(d => d.Id.StartsWith("SF"))
            .ToArray();

        yamlDiagnostics.ShouldBeEmpty("YAML path reported unexpected diagnostics");
        grammarDiagnostics.ShouldBeEmpty("Grammar-interface path reported unexpected diagnostics");

        Dictionary<string, string> yamlOutputs    = CollectGrammarOutputs(yamlResult);
        Dictionary<string, string> grammarOutputs = CollectGrammarOutputs(grammarResult);

        // Same set of hint names
        yamlOutputs.Keys.OrderBy(k => k).ShouldBe(
            grammarOutputs.Keys.OrderBy(k => k),
            "The set of generated hint names must be identical for both front-ends");

        // Byte-identical source text for each hint name
        foreach (string hintName in yamlOutputs.Keys)
        {
            string yamlSource    = yamlOutputs[hintName];
            string grammarSource = grammarOutputs[hintName];

            grammarSource.ShouldBe(yamlSource,
                $"Source text for '{hintName}' differs between the YAML and grammar-interface paths");
        }
    }

    // SF0017: actor name collision between YAML and grammar-interface declarations

    [Fact]
    public void CollisionBetweenYamlAndGrammarReportsSF0017()
    {
        // ICarActorGrammar strips "Grammar" suffix → actor name "ICarActor"
        string grammarSource = """
            using SuperFluid;
            namespace Test
            {
                [FluidApiGrammar]
                internal interface ICarActorGrammar
                {
                    [Initial, TransitionsTo]
                    void Initialize();
                }
            }
            """;

        // YAML also declares actor "ICarActor"
        string yaml = """
            Name: "ICarActor"
            Namespace: "Test"
            InitialState:
              Name: "Initialize"
              CanTransitionTo: []
            Methods: []
            """;

        CSharpCompilation compilation = CompilationHelper.CreateCompilationWithGrammarSource(grammarSource);
        FluidApiSourceGenerator generator = new();
        AdditionalText yamlFile = CompilationHelper.CreateAdditionalText("CarActor.fluid.yml", yaml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts([yamlFile]);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Diagnostic[] sf0017 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0017")
            .ToArray();

        sf0017.ShouldNotBeEmpty("SF0017 should be reported when the same actor name is declared in both YAML and a [FluidApiGrammar] interface");
        sf0017[0].Severity.ShouldBe(DiagnosticSeverity.Error);
        sf0017[0].GetMessage().ShouldContain("ICarActor");
    }

    [Fact]
    public void NoCollisionWhenActorNamesDiffer()
    {
        // Grammar declares IBoatActor (actor name "IBoatActor")
        string grammarSource = """
            using SuperFluid;
            namespace Test
            {
                [FluidApiGrammar]
                internal interface IBoatActorGrammar
                {
                    [Initial, TransitionsTo]
                    void Launch();
                }
            }
            """;

        // YAML declares a different actor "ICarActor"
        string yaml = """
            Name: "ICarActor"
            Namespace: "Test"
            InitialState:
              Name: "Initialize"
              CanTransitionTo: []
            Methods: []
            """;

        CSharpCompilation compilation = CompilationHelper.CreateCompilationWithGrammarSource(grammarSource);
        FluidApiSourceGenerator generator = new();
        AdditionalText yamlFile = CompilationHelper.CreateAdditionalText("CarActor.fluid.yml", yaml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts([yamlFile]);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Diagnostic[] sf0017 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0017")
            .ToArray();

        sf0017.ShouldBeEmpty("SF0017 should not be reported when actor names differ between YAML and grammar-interface");
    }

    [Fact]
    public void NoCollisionWhenOnlyYamlPresent()
    {
        string yaml = """
            Name: "ICarActor"
            Namespace: "Test"
            InitialState:
              Name: "Initialize"
              CanTransitionTo: []
            Methods: []
            """;

        CSharpCompilation compilation = CompilationHelper.CreateCompilation();
        FluidApiSourceGenerator generator = new();
        AdditionalText yamlFile = CompilationHelper.CreateAdditionalText("CarActor.fluid.yml", yaml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts([yamlFile]);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Diagnostic[] sf0017 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0017")
            .ToArray();

        sf0017.ShouldBeEmpty("SF0017 should not be reported when only YAML is present");
    }

    [Fact]
    public void NoCollisionWhenOnlyGrammarInterfacePresent()
    {
        string grammarSource = """
            using SuperFluid;
            namespace Test
            {
                [FluidApiGrammar]
                internal interface ICarActorGrammar
                {
                    [Initial, TransitionsTo]
                    void Initialize();
                }
            }
            """;

        CSharpCompilation compilation = CompilationHelper.CreateCompilationWithGrammarSource(grammarSource);
        FluidApiSourceGenerator generator = new();
        // No YAML additional text added

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Diagnostic[] sf0017 = runResult.Results[0].Diagnostics
            .Where(d => d.Id == "SF0017")
            .ToArray();

        sf0017.ShouldBeEmpty("SF0017 should not be reported when only a grammar interface is present");
    }
}
