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
																	 "Initialize",
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
		
		model.InitialState.Name.ShouldBe("Initialize");
		model.States.Single().Name.ShouldBe("DropDead");
		model.States.Single().AvailableFrom.Single().Name.ShouldBe("Initialize");
	}
}
