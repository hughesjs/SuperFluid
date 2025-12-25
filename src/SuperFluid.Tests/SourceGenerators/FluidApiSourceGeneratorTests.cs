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

		// No diagnostics reported
		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id.StartsWith("SF"))
			.ToArray();
		diagnostics.ShouldBeEmpty();
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
			  CanTransitionTo: ["DoSomething"]
			Methods:
			  - Name: "DoSomething"
			    CanTransitionTo: []
			""";
		string yaml2 = """
			Name: "ISecondActor"
			Namespace: "Test"
			InitialState:
			  Name: "Begin"
			  CanTransitionTo: ["DoOther"]
			Methods:
			  - Name: "DoOther"
			    CanTransitionTo: []
			""";

		AdditionalText file1 = CompilationHelper.CreateAdditionalText("First.fluid.yml", yaml1);
		AdditionalText file2 = CompilationHelper.CreateAdditionalText("Second.fluid.yml", yaml2);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([file1, file2]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		runResult.Results[0].GeneratedSources.Length.ShouldBe(4);

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

	[Fact]
	public void GeneratorReportsSF0001Diagnostic()
	{
		string invalidYaml = "Name: [unclosed bracket";
		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText(
			"Invalid.fluid.yml",
			invalidYaml);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		runResult.Results[0].GeneratedSources.Length.ShouldBe(0);

		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id == "SF0001")
			.ToArray();

		diagnostics.ShouldNotBeEmpty();
		diagnostics[0].Severity.ShouldBe(DiagnosticSeverity.Error);
	}

	[Fact]
	public void GeneratorReportsSF0002Diagnostic()
	{
		string yamlMissingField = """
			Name: "TestActor"
			InitialState:
			  Name: "Start"
			  CanTransitionTo: []
			Methods: []
			""";

		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText(
			"MissingField.fluid.yml",
			yamlMissingField);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id == "SF0002")
			.ToArray();

		diagnostics.ShouldNotBeEmpty();
	}

	[Fact]
	public void GeneratorReportsSF0004Diagnostic()
	{
		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText(
			"Empty.fluid.yml",
			"");

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id == "SF0004")
			.ToArray();

		diagnostics.ShouldNotBeEmpty();
	}

	[Fact]
	public void GeneratorReportsSF0005Diagnostic()
	{
		string yaml = """
			Name: "TestActor"
			Namespace: "Test"
			InitialState:
			  Name: "Start"
			  CanTransitionTo: ["InvalidMethod"]
			Methods: []
			""";

		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText(
			"InvalidTransition.fluid.yml",
			yaml);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id == "SF0005")
			.ToArray();

		diagnostics.ShouldNotBeEmpty();
	}

	[Fact]
	public void GeneratorReportsSF0010Diagnostic()
	{
		string yaml = """
			Name: "Invalid Name With Spaces"
			Namespace: "Test"
			InitialState:
			  Name: "Start"
			  CanTransitionTo: []
			Methods: []
			""";

		AdditionalText yamlFile = CompilationHelper.CreateAdditionalText(
			"InvalidIdentifier.fluid.yml",
			yaml);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([yamlFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id == "SF0010")
			.ToArray();

		diagnostics.ShouldNotBeEmpty();
	}

	[Fact]
	public void GeneratorReportsSF0012WhenNoFilesFound()
	{
		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		// Don't add any additional texts
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id == "SF0012")
			.ToArray();

		diagnostics.ShouldNotBeEmpty();
		diagnostics[0].Severity.ShouldBe(DiagnosticSeverity.Info);
	}

	[Fact]
	public void GeneratorProcessesMultipleFilesIndependently()
	{
		string validYaml = """
			Name: "ValidActor"
			Namespace: "Test"
			InitialState:
			  Name: "Start"
			  CanTransitionTo: ["DoSomething"]
			Methods:
			  - Name: "DoSomething"
			    CanTransitionTo: []
			""";

		string invalidYaml = "Name: [invalid";

		AdditionalText validFile = CompilationHelper.CreateAdditionalText(
			"Valid.fluid.yml",
			validYaml);
		AdditionalText invalidFile = CompilationHelper.CreateAdditionalText(
			"Invalid.fluid.yml",
			invalidYaml);

		CSharpCompilation compilation = CompilationHelper.CreateCompilation();
		FluidApiSourceGenerator generator = new();

		GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
		driver = driver.AddAdditionalTexts([validFile, invalidFile]);
		driver = driver.RunGenerators(compilation);

		GeneratorDriverRunResult runResult = driver.GetRunResult();

		// Should have some generated sources from valid file
		runResult.Results[0].GeneratedSources.Length.ShouldBeGreaterThan(0);

		// Should have diagnostic from invalid file
		Diagnostic[] diagnostics = runResult.Results[0].Diagnostics
			.Where(d => d.Id.StartsWith("SF"))
			.ToArray();
		diagnostics.ShouldNotBeEmpty();
	}
}
