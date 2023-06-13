using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Model;
using SuperFluid.Internal.Parsers;

namespace SuperFluid.Tests.Parsers;

public class FluidApiDefinitionParserTests
{
	private readonly FluidApiMethodDefinition _init = new()
													  {
														  Name = "Initialize",
														  CanTransitionTo = new()
																			{
																				"Unlock"
																			}
													  };
	private readonly FluidApiMethodDefinition _initSimple = new()
															{
																Name = "Initialize",
																CanTransitionTo = new()
																				  {
																					  "DropDead"
																				  }
															};

	private readonly FluidApiMethodDefinition _unlock = new()
														{
															Name = "Unlock",
															CanTransitionTo = new()
																			  {
																				  "Enter",
																				  "Lock"
																			  }
														};

	private readonly FluidApiMethodDefinition _lock = new()
													  {
														  Name = "Lock",
														  CanTransitionTo = new()
																			{
																				"Unlock"
																			}
													  };

	private readonly FluidApiMethodDefinition _enter = new()
													   {
														   Name = "Enter",
														   CanTransitionTo = new()
																			 {
																				 "Start",
																				 "Exit"
																			 }
													   };

	private readonly FluidApiMethodDefinition _exit = new()
													  {
														  Name = "Exit",
														  CanTransitionTo = new()
																			{
																				"Lock",
																				"Enter"
																			}
													  };

	private readonly FluidApiMethodDefinition _start = new()
													   {
														   Name = "Start",
														   CanTransitionTo = new()
																			 {
																				 "Stop"
																			 }
													   };

	private readonly FluidApiMethodDefinition _stop = new()
													  {
														  Name = "Stop",
														  CanTransitionTo = new()
																			{
																				"Start",
																				"Exit"
																			}
													  };

	private readonly FluidApiMethodDefinition _dropDead = new()
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
											InitialState = _initSimple,
											Methods = new()
													  {
														  _dropDead
													  }
										};

		FluidApiDefinitionParser parser = new(definition);

		FluidApiModel model = parser.Parse();

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
											InitialState = _init,
											Methods = new()
													  {
														  _lock,
														  _unlock,
														  _enter,
														  _exit,
														  _start,
														  _stop
													  }
										};

		FluidApiDefinitionParser parser = new(definition);

		FluidApiModel model = parser.Parse();

		FluidApiMethod lockMethod   = model.Methods.Single(s => s.Name == _lock.Name);
		FluidApiMethod unlockMethod = model.Methods.Single(s => s.Name == _unlock.Name);
		FluidApiMethod enterMethod  = model.Methods.Single(s => s.Name == _enter.Name);
		FluidApiMethod exitMethod   = model.Methods.Single(s => s.Name == _exit.Name);
		FluidApiMethod startMethod  = model.Methods.Single(s => s.Name == _start.Name);
		FluidApiMethod stopMethod   = model.Methods.Single(s => s.Name == _stop.Name);

		FluidApiMethod initMethod = model.InitialMethod;
		initMethod.Name.ShouldBe(_init.Name);
		model.Methods.ShouldContain(initMethod);

		initMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {unlockMethod});
		lockMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {unlockMethod});
		unlockMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {enterMethod, lockMethod});
		enterMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {startMethod, exitMethod});
		exitMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {enterMethod, lockMethod});
		startMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {stopMethod});
		stopMethod.CanTransitionTo.ShouldBeEquivalentTo(new HashSet<FluidApiMethod> {startMethod, exitMethod});
	}

}
