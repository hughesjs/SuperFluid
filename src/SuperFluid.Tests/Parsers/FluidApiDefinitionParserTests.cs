using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Model;
using SuperFluid.Internal.Parsers;

namespace SuperFluid.Tests.Parsers;

public class FluidApiDefinitionParserTests
{
	private static readonly FluidApiArgumentDefinition Speed = new()
														 {
															 Name = "speed",
															 Type = "int"
														 };
	
	private static readonly FluidApiMethodDefinition Init = new()
															 {
																 Name = "Initialize",
																 CanTransitionTo = new()
																				   {
																					   "Unlock"
																				   }
															 };
	
	private static readonly FluidApiMethodDefinition InitSimple = new()
																   {
																	   Name = "Initialize",
																	   CanTransitionTo = new()
																						 {
																							 "DropDead"
																						 }
																   };

	private static readonly FluidApiMethodDefinition Unlock = new()
															   {
																   Name = "Unlock",
																   CanTransitionTo = new()
																					 {
																						 "Enter",
																						 "Lock"
																					 }
															   };

	private static readonly FluidApiMethodDefinition Lock = new()
															 {
																 Name = "Lock",
																 CanTransitionTo = new()
																				   {
																					   "Unlock"
																				   }
															 };

	private static readonly FluidApiMethodDefinition Enter = new()
															  {
																  Name = "Enter",
																  CanTransitionTo = new()
																					{
																						"Start",
																						"Exit"
																					}
															  };

	private static readonly FluidApiMethodDefinition Exit = new()
															 {
																 Name = "Exit",
																 CanTransitionTo = new()
																				   {
																					   "Lock",
																					   "Enter"
																				   }
															 };

	private static readonly FluidApiMethodDefinition Start = new()
															  {
																  Name = "Start",
																  CanTransitionTo = new()
																					{
																						"Stop"
																					},
																  Arguments = new()
																			  {
																				  Speed
																			  }
															  };

	private static readonly FluidApiMethodDefinition Stop = new()
															 {
																 Name = "Stop",
																 CanTransitionTo = new()
																				   {
																					   "Start",
																					   "Exit"
																				   }
															 };

	private static readonly FluidApiMethodDefinition DropDead = new()
																 {
																	 Name            = "DropDead",
																	 CanTransitionTo = new()
																 };

	[Fact]
	public void CanDeserializeSimpleCase()
	{
		FluidApiDefinition definition = new()
										{
											Name         = "Simple",
											Namespace    = "Simple.Test",
											InitialState = InitSimple,
											Methods = new()
													  {
														  DropDead
													  }
										};

		FluidApiDefinitionParser parser = new();
		FluidApiModel            model  = parser.Parse(definition);

		FluidApiMethod deadMethod = model.Methods.Single(s => s.Name == "DropDead");
		deadMethod.Name.ShouldBe("DropDead");
		deadMethod.CanTransitionTo.ShouldBeEmpty();

		FluidApiMethod initMethod = model.InitialMethod;
		initMethod.Name.ShouldBe("Initialize");
		initMethod.CanTransitionTo.ShouldContain(deadMethod);

		model.Methods.ShouldContain(initMethod);
	}

	[Fact]
	public void CanDeserializeComplexCase()
	{
		FluidApiDefinition definition = new()
										{
											Name         = "Simple",
											Namespace    = "Simple.Test",
											InitialState = Init,
											Methods = new()
													  {
														  Lock,
														  Unlock,
														  Enter,
														  Exit,
														  Start,
														  Stop
													  }
										};

		FluidApiDefinitionParser parser = new();
		FluidApiModel            model  = parser.Parse(definition);

		FluidApiMethod lockMethod   = model.Methods.Single(s => s.Name == Lock.Name);
		FluidApiMethod unlockMethod = model.Methods.Single(s => s.Name == Unlock.Name);
		FluidApiMethod enterMethod  = model.Methods.Single(s => s.Name == Enter.Name);
		FluidApiMethod exitMethod   = model.Methods.Single(s => s.Name == Exit.Name);
		FluidApiMethod startMethod  = model.Methods.Single(s => s.Name == Start.Name);
		FluidApiMethod stopMethod   = model.Methods.Single(s => s.Name == Stop.Name);

		FluidApiMethod initMethod = model.InitialMethod;
		initMethod.Name.ShouldBe(Init.Name);
		model.Methods.ShouldContain(initMethod);

		initMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {unlockMethod});
		lockMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {unlockMethod});
		unlockMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {enterMethod, lockMethod});
		enterMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {startMethod, exitMethod});
		exitMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {enterMethod, lockMethod});
		startMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {stopMethod});
		stopMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {startMethod, exitMethod});

		startMethod.Arguments.First().Name.ShouldBe("speed");
		startMethod.Arguments.First().Type.ShouldBe("int");
	}
}
