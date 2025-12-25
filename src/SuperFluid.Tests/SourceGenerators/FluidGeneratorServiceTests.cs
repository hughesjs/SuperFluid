using SuperFluid.Internal.Parsers;
using SuperFluid.Internal.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SuperFluid.Tests.SourceGenerators;

public class FluidGeneratorServiceTests
{
	private readonly FluidGeneratorService _sut;
	private readonly string _rawYml = File.ReadAllText("DemoApiDefinition.fluid.yml");

	public FluidGeneratorServiceTests()
	{
		IDeserializer deserializer = new DeserializerBuilder()
			.WithNamingConvention(NullNamingConvention.Instance)
			.Build();
		_sut = new FluidGeneratorService(deserializer, new FluidApiDefinitionParser());
	}

	[Fact]
	public void GenerateReturnsSuccessForValidYaml()
	{
		GenerationResult result = _sut.Generate(_rawYml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		result.GeneratedFiles.ShouldNotBeNull();
		result.GeneratedFiles.Count.ShouldBe(5);
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedFiles["ICanLockOrEnter.fluid.g.cs"].ShouldBe(CanLockOrEnterSource);
		result.GeneratedFiles["ICarActor.fluid.g.cs"].ShouldBe(CarActorSource);
	}

	[Fact]
	public void GenerateReportsSF0001ForInvalidYamlSyntax()
	{
		string invalidYaml = "Name: [this is invalid syntax";

		GenerationResult result = _sut.Generate(invalidYaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.GeneratedFiles.ShouldBeNull();
		result.Diagnostics.ShouldNotBeEmpty();
		result.Diagnostics[0].Id.ShouldBe("SF0001");
		result.Diagnostics[0].GetMessage().ShouldContain("YAML syntax error");
	}

	[Fact]
	public void GenerateReportsSF0002ForMissingNamespace()
	{
		string yaml = """
		              Name: "TestActor"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: []
		              Methods: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0002");
	}

	[Fact]
	public void GenerateReportsSF0002ForMissingName()
	{
		string yaml = """
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: []
		              Methods: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0002");
	}

	[Fact]
	public void GenerateReportsSF0004ForEmptyYaml()
	{
		GenerationResult result = _sut.Generate("", "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0004");
	}

	[Fact]
	public void GenerateReportsSF0004ForWhitespaceOnlyYaml()
	{
		GenerationResult result = _sut.Generate("   \n\t  \n", "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0004");
	}

	[Fact]
	public void GenerateReportsSF0005ForNonExistentTransition()
	{
		string yaml = """
		              Name: "TestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: ["NonExistentMethod"]
		              Methods: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0005");
		result.Diagnostics[0].GetMessage().ShouldContain("NonExistentMethod");
	}

	[Fact]
	public void GenerateReportsSF0006ForDuplicateMethodNames()
	{
		string yaml = """
		              Name: "TestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: ["DoSomething"]
		              Methods:
		                - Name: "DoSomething"
		                  CanTransitionTo: []
		                - Name: "DoSomething"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0006");
		result.Diagnostics[0].GetMessage().ShouldContain("DoSomething");
	}

	[Fact]
	public void GenerateReportsSF0007ForEmptyConstraints()
	{
		string yaml = """
		              Name: "TestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: []
		                GenericArguments:
		                  - Name: "T"
		                    Constraints: []
		              Methods: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0007");
	}

	[Fact]
	public void GenerateReportsSF0009ForNoStatesGenerated()
	{
		string yaml = """
		              Name: "TestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: []
		              Methods: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0009");
	}

	[Fact]
	public void GenerateReportsSF0010ForInvalidName()
	{
		string yaml = """
		              Name: "My Invalid Name"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: []
		              Methods: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0010");
		result.Diagnostics[0].GetMessage().ShouldContain("My Invalid Name");
	}

	[Fact]
	public void GenerateReportsSF0010ForInvalidNamespace()
	{
		string yaml = """
		              Name: "TestActor"
		              Namespace: "123Invalid"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: []
		              Methods: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0010");
	}

	[Fact]
	public void GenerateReportsSF0010ForInvalidMethodName()
	{
		string yaml = """
		              Name: "TestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: []
		              Methods:
		                - Name: "Do-Something"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0010");
	}

	private const string CanLockOrEnterSource = """
	                                            namespace SuperFluid.Tests.Cars;

	                                            public interface ICanLockOrEnter
	                                            {
	                                            	public ICanUnlock Lock();
	                                            	public ICanStartOrExit Enter();
	                                            }
	                                            """;

	private const string CarActorSource = """
	                                      namespace SuperFluid.Tests.Cars;

	                                      public interface ICarActor: ICanLockOrEnter,ICanUnlock,ICanStartOrExit,ICanStopOrBuild
	                                      {
	                                      	public static abstract ICanUnlock Initialize();
	                                      }
	                                      """;
}
