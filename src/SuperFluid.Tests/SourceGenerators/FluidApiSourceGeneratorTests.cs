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
}
