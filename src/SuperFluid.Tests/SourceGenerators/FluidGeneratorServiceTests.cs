using SuperFluid.Internal.Parsers;
using SuperFluid.Internal.Services;

namespace SuperFluid.Tests.SourceGenerators;

public class FluidGeneratorServiceTests
{
	private readonly FluidGeneratorService _sut = new(new FluidApiDefinitionParser());
	private readonly string _rawYml = File.ReadAllText("DemoApiDefinition.fluid.yml");

	[Fact]
	public void GenerateReturnsSuccessForValidYaml()
	{
		GenerationResult result = _sut.Generate(_rawYml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		result.GeneratedFiles.ShouldNotBeNull();
		result.GeneratedFiles.Count.ShouldBe(5);
		result.Diagnostics.ShouldBeEmpty();
		result.GeneratedFiles["ICanEnterOrLock.fluid.g.cs"].ShouldBe(CanEnterOrLockSource);
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

	// -------------------------------------------------------------------------
	// Tiered state naming tests
	// -------------------------------------------------------------------------

	[Fact]
	public void Tier1NamingProducesShortFormForSmallStates()
	{
		// The demo YAML produces states like ICanEnterOrLock which are ≤ 60 chars — ensure unchanged
		GenerationResult result = _sut.Generate(_rawYml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		result.GeneratedFiles!.ShouldContainKey("ICanEnterOrLock.fluid.g.cs");
		result.GeneratedFiles!.ShouldContainKey("ICanExitOrStart.fluid.g.cs");
		result.GeneratedFiles!.ShouldContainKey("ICanUnlock.fluid.g.cs");
	}

	[Fact]
	public void Tier2NamingProducesTruncatedFormWhenTier1ExceedsLimit()
	{
		// Construct a state with 8 methods whose Tier-1 name exceeds 60 chars.
		// Alphabetical order: ActionAlpha, ActionBeta, ActionDelta, ActionEpsilon, ActionEta, ActionGamma, ActionTheta, ActionZeta
		// Tier-2 name: ICanActionAlphaOrActionBetaOr6More
		string yaml = """
		              Name: "ILargeActor"
		              Namespace: "Test.Large"
		              InitialState:
		                Name: "Begin"
		                CanTransitionTo:
		                  - "ActionAlpha"
		                  - "ActionBeta"
		                  - "ActionDelta"
		                  - "ActionEpsilon"
		                  - "ActionEta"
		                  - "ActionGamma"
		                  - "ActionTheta"
		                  - "ActionZeta"
		              Methods:
		                - Name: "ActionAlpha"
		                  CanTransitionTo: []
		                - Name: "ActionBeta"
		                  CanTransitionTo: []
		                - Name: "ActionDelta"
		                  CanTransitionTo: []
		                - Name: "ActionEpsilon"
		                  CanTransitionTo: []
		                - Name: "ActionEta"
		                  CanTransitionTo: []
		                - Name: "ActionGamma"
		                  CanTransitionTo: []
		                - Name: "ActionTheta"
		                  CanTransitionTo: []
		                - Name: "ActionZeta"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();

		// Verify the Tier-1 name would have been too long
		string tier1Name = "ICanActionAlphaOrActionBetaOrActionDeltaOrActionEpsilonOrActionEtaOrActionGammaOrActionThetaOrActionZeta";
		tier1Name.Length.ShouldBeGreaterThan(60);

		// Assert the Tier-2 truncated name is used instead
		result.GeneratedFiles!.ShouldContainKey("ICanActionAlphaOrActionBetaOr6More.fluid.g.cs");
		result.GeneratedFiles!.ShouldNotContainKey($"{tier1Name}.fluid.g.cs");
	}

	[Fact]
	public void Tier2CollisionPromotesBothStatesToTier3()
	{
		// Two states share the same first two alphabetical methods (Aardvark, Baboon) but differ in the rest.
		// Both should be promoted to Tier 3 bucket names rather than getting the same Tier-2 name.
		// State 1 (reachable via MethodA): {Aardvark, Baboon, Cheetah, Donkey, Elephant, Flamingo, Gorilla, Hippo}
		// State 2 (reachable via MethodB): {Aardvark, Baboon, Iguana, Jaguar, Kangaroo, Lion, Meerkat, Numbat}
		// Both produce Tier-2: ICanAardvarkOrBaboonOr6More → collision → both get Tier-3 names
		string yaml = """
		              Name: "ICollisionActor"
		              Namespace: "Test.Collision"
		              InitialState:
		                Name: "Begin"
		                CanTransitionTo:
		                  - "MethodA"
		                  - "MethodB"
		              Methods:
		                - Name: "MethodA"
		                  CanTransitionTo:
		                    - "Aardvark"
		                    - "Baboon"
		                    - "Cheetah"
		                    - "Donkey"
		                    - "Elephant"
		                    - "Flamingo"
		                    - "Gorilla"
		                    - "Hippo"
		                - Name: "MethodB"
		                  CanTransitionTo:
		                    - "Aardvark"
		                    - "Baboon"
		                    - "Iguana"
		                    - "Jaguar"
		                    - "Kangaroo"
		                    - "Lion"
		                    - "Meerkat"
		                    - "Numbat"
		                - Name: "Aardvark"
		                  CanTransitionTo: []
		                - Name: "Baboon"
		                  CanTransitionTo: []
		                - Name: "Cheetah"
		                  CanTransitionTo: []
		                - Name: "Donkey"
		                  CanTransitionTo: []
		                - Name: "Elephant"
		                  CanTransitionTo: []
		                - Name: "Flamingo"
		                  CanTransitionTo: []
		                - Name: "Gorilla"
		                  CanTransitionTo: []
		                - Name: "Hippo"
		                  CanTransitionTo: []
		                - Name: "Iguana"
		                  CanTransitionTo: []
		                - Name: "Jaguar"
		                  CanTransitionTo: []
		                - Name: "Kangaroo"
		                  CanTransitionTo: []
		                - Name: "Lion"
		                  CanTransitionTo: []
		                - Name: "Meerkat"
		                  CanTransitionTo: []
		                - Name: "Numbat"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();

		// Neither state should have the colliding Tier-2 name
		result.GeneratedFiles!.ShouldNotContainKey("ICanAardvarkOrBaboonOr6More.fluid.g.cs");

		// Both states should have Tier-3 bucket names
		string[] tier3Keys = result.GeneratedFiles!.Keys.Where(k => k.Contains("ICanDoManyCollisionActorState")).ToArray();
		tier3Keys.Length.ShouldBe(2, "Both colliding states should receive Tier-3 names");
	}

	[Fact]
	public void Tier3HashIsDeterministicAcrossRuns()
	{
		// Verify that running the generator twice on the same input yields identical filenames.
		// This specifically tests determinism of the SHA-256 hash used in Tier-3 naming.
		string yaml = """
		              Name: "ICollisionActor"
		              Namespace: "Test.Collision"
		              InitialState:
		                Name: "Begin"
		                CanTransitionTo:
		                  - "MethodA"
		                  - "MethodB"
		              Methods:
		                - Name: "MethodA"
		                  CanTransitionTo:
		                    - "Aardvark"
		                    - "Baboon"
		                    - "Cheetah"
		                    - "Donkey"
		                    - "Elephant"
		                    - "Flamingo"
		                    - "Gorilla"
		                    - "Hippo"
		                - Name: "MethodB"
		                  CanTransitionTo:
		                    - "Aardvark"
		                    - "Baboon"
		                    - "Iguana"
		                    - "Jaguar"
		                    - "Kangaroo"
		                    - "Lion"
		                    - "Meerkat"
		                    - "Numbat"
		                - Name: "Aardvark"
		                  CanTransitionTo: []
		                - Name: "Baboon"
		                  CanTransitionTo: []
		                - Name: "Cheetah"
		                  CanTransitionTo: []
		                - Name: "Donkey"
		                  CanTransitionTo: []
		                - Name: "Elephant"
		                  CanTransitionTo: []
		                - Name: "Flamingo"
		                  CanTransitionTo: []
		                - Name: "Gorilla"
		                  CanTransitionTo: []
		                - Name: "Hippo"
		                  CanTransitionTo: []
		                - Name: "Iguana"
		                  CanTransitionTo: []
		                - Name: "Jaguar"
		                  CanTransitionTo: []
		                - Name: "Kangaroo"
		                  CanTransitionTo: []
		                - Name: "Lion"
		                  CanTransitionTo: []
		                - Name: "Meerkat"
		                  CanTransitionTo: []
		                - Name: "Numbat"
		                  CanTransitionTo: []
		              """;

		GenerationResult firstRun  = _sut.Generate(yaml, "test.fluid.yml");
		GenerationResult secondRun = _sut.Generate(yaml, "test.fluid.yml");

		firstRun.IsSuccess.ShouldBeTrue();
		secondRun.IsSuccess.ShouldBeTrue();

		string[] firstKeys  = firstRun.GeneratedFiles!.Keys.OrderBy(k => k).ToArray();
		string[] secondKeys = secondRun.GeneratedFiles!.Keys.OrderBy(k => k).ToArray();

		firstKeys.ShouldBeEquivalentTo(secondKeys, "Tier-3 names must be deterministic across runs");
	}

	[Fact]
	public void Tier4UserOverrideAppliesUserDeclaredStateName()
	{
		string yaml = """
		              Name: "ICarActor"
		              Namespace: "SuperFluid.Tests.Cars"
		              InitialState:
		                Name: "Initialize"
		                CanTransitionTo:
		                  - "Unlock"
		              StateNames:
		                - Name: "ICarUnlocked"
		                  Transitions:
		                    - "Enter"
		                    - "Lock"
		              Methods:
		                - Name: "Unlock"
		                  CanTransitionTo:
		                    - "Lock"
		                    - "Enter"
		                - Name: "Lock"
		                  CanTransitionTo:
		                    - "Unlock"
		                - Name: "Enter"
		                  CanTransitionTo:
		                    - "Exit"
		                - Name: "Exit"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		result.GeneratedFiles!.ShouldContainKey("ICarUnlocked.fluid.g.cs", "User-declared state name should be used");

		// The compound interface's base list should include the user-declared name
		string compoundSource = result.GeneratedFiles!["ICarActor.fluid.g.cs"];
		compoundSource.ShouldContain("ICarUnlocked");
	}

	[Fact]
	public void Tier4UnmatchedStateNameDeclarationReportsSF0014Warning()
	{
		// The Transitions list references real methods (both Lock and Enter exist in the state machine)
		// but the exact combination {Lock} alone doesn't match any synthesised state.
		// The only state reachable from Initialize has transitions {Lock, Enter}; declaring {Lock} alone
		// produces no match, triggering SF0014.
		string yaml = """
		              Name: "ICarActor"
		              Namespace: "SuperFluid.Tests.Cars"
		              InitialState:
		                Name: "Initialize"
		                CanTransitionTo:
		                  - "Unlock"
		              StateNames:
		                - Name: "IObsoleteStateName"
		                  Transitions:
		                    - "Lock"
		              Methods:
		                - Name: "Unlock"
		                  CanTransitionTo:
		                    - "Lock"
		                    - "Enter"
		                - Name: "Lock"
		                  CanTransitionTo: []
		                - Name: "Enter"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		// SF0014 is a warning; generation should still succeed for the rest
		result.IsSuccess.ShouldBeTrue();
		result.Diagnostics.ShouldNotBeEmpty();
		result.Diagnostics[0].Id.ShouldBe("SF0014");
	}

	[Fact]
	public void Tier4InvalidStateNameIdentifierReportsSF0015Error()
	{
		string yaml = """
		              Name: "ICarActor"
		              Namespace: "SuperFluid.Tests.Cars"
		              InitialState:
		                Name: "Initialize"
		                CanTransitionTo:
		                  - "Unlock"
		              StateNames:
		                - Name: "Invalid Name With Spaces"
		                  Transitions:
		                    - "Unlock"
		              Methods:
		                - Name: "Unlock"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0015");
		result.Diagnostics[0].GetMessage().ShouldContain("Invalid Name With Spaces");
	}

	[Fact]
	public void Tier4AmbiguousStateNameDeclarationReportsSF0016Error()
	{
		// Both StateNames entries declare different names for the same synthesised state {Enter}.
		// The state reachable after Unlock has transition set {Enter}.
		// Two entries both claim it → SF0016 ambiguity error.
		string yaml = """
		              Name: "ICarActor"
		              Namespace: "SuperFluid.Tests.Cars"
		              InitialState:
		                Name: "Initialize"
		                CanTransitionTo:
		                  - "Unlock"
		              StateNames:
		                - Name: "IFirstName"
		                  Transitions:
		                    - "Enter"
		                - Name: "ISecondName"
		                  Transitions:
		                    - "Enter"
		              Methods:
		                - Name: "Unlock"
		                  CanTransitionTo:
		                    - "Enter"
		                - Name: "Enter"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeFalse();
		result.Diagnostics[0].Id.ShouldBe("SF0016");
	}

	// -------------------------------------------------------------------------
	// XML documentation tests
	// -------------------------------------------------------------------------

	[Fact]
	public void GenerateEmitsXmlDocOnCompoundInterfaceWhenDescriptionProvided()
	{
		string yaml = """
		              Name: "ITestActor"
		              Namespace: "Test"
		              Description: "My car actor"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: ["DoSomething"]
		              Methods:
		                - Name: "DoSomething"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		string compoundSource = result.GeneratedFiles!["ITestActor.fluid.g.cs"];
		compoundSource.ShouldContain("/// <summary>");
		compoundSource.ShouldContain("/// My car actor");
		compoundSource.ShouldContain("/// </summary>");
		int docIndex = compoundSource.IndexOf("/// <summary>", StringComparison.Ordinal);
		int interfaceIndex = compoundSource.IndexOf("public interface ITestActor", StringComparison.Ordinal);
		docIndex.ShouldBeLessThan(interfaceIndex);
	}

	[Fact]
	public void GenerateEmitsXmlDocOnMethodWhenDescriptionProvided()
	{
		string yaml = """
		              Name: "ITestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: ["Lock"]
		              Methods:
		                - Name: "Lock"
		                  Description: "Locks the car"
		                  CanTransitionTo: ["Start"]
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		string lockStateSource = result.GeneratedFiles!.Values.First(s => s.Contains("public ICanStart Lock()"));
		lockStateSource.ShouldContain("/// <summary>");
		lockStateSource.ShouldContain("/// Locks the car");
		lockStateSource.ShouldContain("/// </summary>");
		int docIndex = lockStateSource.IndexOf("/// <summary>", StringComparison.Ordinal);
		int methodIndex = lockStateSource.IndexOf("public ICanStart Lock()", StringComparison.Ordinal);
		docIndex.ShouldBeLessThan(methodIndex);
	}

	[Fact]
	public void GenerateEmitsXmlDocOnInitialMethodInCompoundInterfaceWhenDescriptionProvided()
	{
		string yaml = """
		              Name: "ITestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                Description: "Initialises the actor"
		                CanTransitionTo: ["DoSomething"]
		              Methods:
		                - Name: "DoSomething"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		string compoundSource = result.GeneratedFiles!["ITestActor.fluid.g.cs"];
		compoundSource.ShouldContain("/// <summary>");
		compoundSource.ShouldContain("/// Initialises the actor");
		compoundSource.ShouldContain("/// </summary>");
		int docIndex = compoundSource.IndexOf("/// <summary>", StringComparison.Ordinal);
		int methodIndex = compoundSource.IndexOf("public static abstract", StringComparison.Ordinal);
		docIndex.ShouldBeLessThan(methodIndex);
	}

	[Fact]
	public void GenerateEmitsMultiLineXmlDocWhenDescriptionContainsNewlines()
	{
		string yaml = "Name: \"ITestActor\"\nNamespace: \"Test\"\nDescription: \"Line one\\nLine two\"\nInitialState:\n  Name: \"Start\"\n  CanTransitionTo: [\"DoSomething\"]\nMethods:\n  - Name: \"DoSomething\"\n    CanTransitionTo: []";

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		string compoundSource = result.GeneratedFiles!["ITestActor.fluid.g.cs"];
		compoundSource.ShouldContain("/// Line one");
		compoundSource.ShouldContain("/// Line two");
	}

	[Fact]
	public void GenerateEscapesXmlSpecialCharactersInDescription()
	{
		string yaml = """
		              Name: "ITestActor"
		              Namespace: "Test"
		              Description: "Returns List<T> of items & more"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: ["DoSomething"]
		              Methods:
		                - Name: "DoSomething"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		string compoundSource = result.GeneratedFiles!["ITestActor.fluid.g.cs"];
		compoundSource.ShouldContain("Returns List&lt;T&gt; of items &amp; more");
		compoundSource.ShouldNotContain("List<T>");
		compoundSource.ShouldNotContain("items & more");
	}

	[Fact]
	public void GenerateProducesNoXmlDocLinesWhenDescriptionIsAbsent()
	{
		string yaml = """
		              Name: "ITestActor"
		              Namespace: "Test"
		              InitialState:
		                Name: "Start"
		                CanTransitionTo: ["DoSomething"]
		              Methods:
		                - Name: "DoSomething"
		                  CanTransitionTo: []
		              """;

		GenerationResult result = _sut.Generate(yaml, "test.fluid.yml");

		result.IsSuccess.ShouldBeTrue();
		foreach (string source in result.GeneratedFiles!.Values)
		{
			source.ShouldNotContain("///");
		}
	}

	private const string CanEnterOrLockSource = """
	                                            namespace SuperFluid.Tests.Cars;

	                                            public interface ICanEnterOrLock
	                                            {
	                                            	public ICanExitOrStart Enter();
	                                            	public ICanUnlock Lock();
	                                            }
	                                            """;

	private const string CarActorSource = """
	                                      namespace SuperFluid.Tests.Cars;

	                                      public interface ICarActor: ICanBuildOrStop,ICanEnterOrLock,ICanExitOrStart,ICanUnlock
	                                      {
	                                      	public static abstract ICanUnlock Initialize();
	                                      }
	                                      """;
}
