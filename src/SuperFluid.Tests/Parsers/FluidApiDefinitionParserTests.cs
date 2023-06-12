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
															  Name = "DropDead",
															  CanTransitionTo = new()
														  };

	[Fact]
	public void CanDeserializeSimpleCase()
	{
		FluidApiDefinition definition = new()
										{
											Name         = "Simple",
											InitialState = _init,
											Methods      = new()
														   {
															   _dropDead
														   }
										};
		
		FluidApiDefinitionParser parser = new(definition);
		
		FluidApiModel model = parser.Parse();

		FluidApiState initState = model.InitialState;
		initState.Name.ShouldBe("Initialize");
		initState.CanTransitionTo.ShouldBeEmpty();
		model.States.First().ShouldBe(initState);

		FluidApiState deadState = model.States.Single(s => s.Name == "DropDead");
		deadState.Name.ShouldBe("DropDead");
		deadState.CanTransitionTo.ShouldBeEmpty();
	}

	[Fact]
	public void CanDeserializeComplexCase()
	{
		FluidApiDefinition definition = new()
										{
											Name         = "Simple",
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

		FluidApiState initState = model.InitialState;
		initState.Name.ShouldBe("Initialize");
		initState.CanTransitionTo.ShouldBeEmpty();
		model.States.First().ShouldBe(initState);
		
		FluidApiState lockState = model.States.Single(s => s.Name == "Lock");
		FluidApiState unlockState = model.States.Single(s => s.Name == "Unlock");
		FluidApiState enterState = model.States.Single(s => s.Name == "Enter");
		FluidApiState exitState = model.States.Single(s => s.Name == "Exit");
		FluidApiState startState = model.States.Single(s => s.Name == "Start");
		FluidApiState stopState = model.States.Single(s => s.Name == "Stop");
		
		lockState.CanTransitionTo.ShouldBeEquivalentTo(new List<FluidApiState>{ unlockState });
		unlockState.CanTransitionTo.ShouldBeEquivalentTo(new List<FluidApiState> { enterState, lockState });
		enterState.CanTransitionTo.ShouldBeEquivalentTo(new List<FluidApiState> { startState, exitState });
		exitState.CanTransitionTo.ShouldBeEquivalentTo(new List<FluidApiState> { enterState, lockState });
		startState.CanTransitionTo.ShouldBeEquivalentTo(new List<FluidApiState> { stopState });
		stopState.CanTransitionTo.ShouldBeEquivalentTo(new List<FluidApiState> { startState, exitState });
	}

}
