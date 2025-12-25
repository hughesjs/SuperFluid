using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SuperFluid.Internal.SourceGenerators;
using SuperFluid.Tests.TestHelpers;

namespace SuperFluid.Tests.SourceGenerators;

public class FluidApiSourceGeneratorTests
{
	[Fact]
	public void GeneratorProducesExpectedOutputForValidYaml()
	{
		string yamlContent = File.ReadAllText("DemoApiDefinition.fluid.yml");
		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText(
			"DemoApiDefinition.fluid.yml",
			yamlContent);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		runResult.Results.Length.ShouldBe(1);
		ImmutableArray<GeneratedSourceResult> generatedSources = runResult.Results[0].GeneratedSources;
		generatedSources.IsDefault.ShouldBeFalse("GeneratedSources should be initialized");
		generatedSources.Length.ShouldBe(5);

		generatedSources.ShouldContain(r => r.HintName == "ICanLockOrEnter.fluid.g.cs");
		generatedSources.ShouldContain(r => r.HintName == "ICarActor.fluid.g.cs");
	}

	[Fact]
	public void GeneratedCodeCompilesWithoutErrors()
	{
		string yamlContent = File.ReadAllText("DemoApiDefinition.fluid.yml");
		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText(
			"DemoApiDefinition.fluid.yml",
			yamlContent);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out Compilation outputCompilation,
			out _);

		Diagnostic[] errors = outputCompilation.GetDiagnostics()
			.Where(d => d.Severity == DiagnosticSeverity.Error)
			.ToArray();

		errors.ShouldBeEmpty();
	}

	[Fact]
	public void GeneratorHandlesMultipleYamlFiles()
	{
		string yaml1 = """
			Name: "IFirstActor"
			Namespace: "Test"
			InitialState:
			  Name: "Start"
			  CanTransitionTo: []
			Methods: []
			""";
		string yaml2 = """
			Name: "ISecondActor"
			Namespace: "Test"
			InitialState:
			  Name: "Begin"
			  CanTransitionTo: []
			Methods: []
			""";

		AdditionalText file1 = CompilationHelper.CreateAdditionalText("First.fluid.yml", yaml1);
		AdditionalText file2 = CompilationHelper.CreateAdditionalText("Second.fluid.yml", yaml2);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([file1, file2]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		runResult.Results[0].GeneratedSources.Length.ShouldBe(2);

		ImmutableArray<GeneratedSourceResult> sources = runResult.Results[0].GeneratedSources;
		sources.ShouldContain(s => s.HintName.Contains("IFirstActor"));
		sources.ShouldContain(s => s.HintName.Contains("ISecondActor"));
	}

	[Fact]
	public void GeneratorIgnoresNonFluidYamlFiles()
	{
		string yamlContent = """
			Name: "IShouldBeIgnored"
			Namespace: "Test"
			InitialState:
			  Name: "Start"
			  CanTransitionTo: []
			Methods: []
			""";

		AdditionalText regularYaml = CompilationHelper.CreateAdditionalText("config.yml", yamlContent);
		AdditionalText textFile = CompilationHelper.CreateAdditionalText("data.txt", yamlContent);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([regularYaml, textFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		runResult.Results[0].GeneratedSources.Length.ShouldBe(0);
	}

	[Fact]
	public void GeneratorCachesOutputWhenYamlContentUnchanged()
	{
		string yamlContent = """
			Name: "ITestActor"
			Namespace: "Test"
			InitialState:
			  Name: "Start"
			  CanTransitionTo: []
			Methods: []
			""";

		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText("Test.fluid.yml", yamlContent);
		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriverOptions options = new(
			disabledOutputs: IncrementalGeneratorOutputKind.None,
			trackIncrementalGeneratorSteps: true);

		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			generators: [generator.AsSourceGenerator()],
			driverOptions: options);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver = driver.RunGenerators(compilation);

		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult secondRun = driver.GetRunResult();

		ImmutableArray<(object Value, IncrementalStepRunReason Reason)> trackedSteps = [
            ..secondRun.Results[0]
                .TrackedSteps["YamlContent"]
                .SelectMany(step => step.Outputs)
        ];

		trackedSteps.ShouldAllBe(step => step.Reason == IncrementalStepRunReason.Cached);
	}
}
