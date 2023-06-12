using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Model;
using SuperFluid.Internal.Parsers;

namespace SuperFluid.Tests.Parsers;

public class FluidApiDefinitionParserTests
{
	private FluidApiMethodDefinition _init = new()
											 {
												 Name = "Initialize"
											 };

	private FluidApiMethodDefinition _unlock = new()
											   {
												   Name = "Unlock",
												   AvailableFrom = new()
																   {
																	   "Initialize",
																	   "Lock"
																   }
											   };

	private FluidApiMethodDefinition _lock = new()
											 {
												 Name = "Lock",
												 AvailableFrom = new()
																 {
																	 "Unlock",
																	 "Exit"
																 }
											 };
	
	private FluidApiMethodDefinition _enter = new()
											  {
												  Name = "Enter",
												  AvailableFrom = new()
																  {
																	  "Unlock",
																	  "Exit"
																  }
											  };

	private FluidApiMethodDefinition _exit = new()
											 {
												 Name = "Exit",
												 AvailableFrom = new()
																 {
																	 "Enter",
																	 "Stop"
																 }
											 };
	
	private FluidApiMethodDefinition _start = new()
											 {
												 Name = "Start",
												 AvailableFrom = new()
																 {
																	 "Enter"
																 }
											 };
	
	private FluidApiMethodDefinition _stop = new()
											 {
												 Name = "Stop",
												 AvailableFrom = new()
																 {
																	 "Start"
																 }
											 };

	private FluidApiMethodDefinition _dropDead = new()
												 {
													 Name = "DropDead",
													 AvailableFrom = new()
																	 {
																		 "Initialize"
																	 }
												 };

	[Fact]
	public void CanDeserializeSimpleCase()
	{
		FluidApiDefinition definition = new()
										{
											Name         = "Simple",
											InitialState = "Initialize",
											Methods      = new()
														   {
															   _dropDead
														   }
										};
		
		FluidApiDefinitionParser parser = new(definition);
		
		FluidApiModel model = parser.Parse();

		FluidApiState initState = model.InitialState;
		initState.Name.ShouldBe("Initialize");
		initState.AvailableFrom.ShouldBeEmpty();
		model.States.First().ShouldBe(initState);

		FluidApiState deadState = model.States.Single(s => s.Name == "DropDead");
		deadState.Name.ShouldBe("DropDead");
		deadState.AvailableFrom.Count.ShouldBe(1);
		deadState.AvailableFrom.ShouldContain(initState);
	}

	[Fact]
	public void CanDeserializeComplexCase()
	{
		FluidApiDefinition definition = new()
										{
											Name         = "Simple",
											InitialState = "Initialize",
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
		initState.AvailableFrom.ShouldBeEmpty();
		model.States.First().ShouldBe(initState);
		
		FluidApiState lockState = model.States.Single(s => s.Name == "Lock");
		FluidApiState unlockState = model.States.Single(s => s.Name == "Unlock");
		FluidApiState enterState = model.States.Single(s => s.Name == "Enter");
		FluidApiState exitState = model.States.Single(s => s.Name == "Exit");
		FluidApiState startState = model.States.Single(s => s.Name == "Start");
		FluidApiState stopState = model.States.Single(s => s.Name == "Stop");
		
		lockState.AvailableFrom.ShouldBeEquivalentTo(new List<FluidApiState>{ unlockState, exitState });
		unlockState.AvailableFrom.ShouldBeEquivalentTo(new List<FluidApiState> { initState, lockState });
		enterState.AvailableFrom.ShouldBeEquivalentTo(new List<FluidApiState> { unlockState, exitState });
		exitState.AvailableFrom.ShouldBeEquivalentTo(new List<FluidApiState> { enterState, stopState });
		startState.AvailableFrom.ShouldBeEquivalentTo(new List<FluidApiState> { enterState });
		stopState.AvailableFrom.ShouldBeEquivalentTo(new List<FluidApiState> { startState });
		
	}
}
