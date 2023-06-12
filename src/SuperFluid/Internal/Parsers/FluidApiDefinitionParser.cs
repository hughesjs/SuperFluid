using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.Model;

namespace SuperFluid.Internal.Parsers;

internal class FluidApiDefinitionParser
{
	private readonly FluidApiDefinition _definition;

	public FluidApiDefinitionParser(FluidApiDefinition definition)
	{
		_definition = definition;
	}

	public FluidApiModel Parse()
	{
		FluidApiState initialState = new(_definition.InitialState.Name);

		Dictionary<string, FluidApiState> stateDict = new()
													  {
														  {initialState.Name, initialState}
													  };

		foreach (FluidApiMethodDefinition method in _definition.Methods)
		{
			FindOrCreateMethod(method.Name, stateDict);
		}

		FluidApiModel model = new()
							  {
								  Name         = _definition.Name,
								  InitialState = initialState,
								  States       = stateDict.Values.ToList()
							  };

		return model;
	}

	private FluidApiState FindOrCreateMethod(string methodName, Dictionary<string, FluidApiState> stateDict)
	{
		if (stateDict.TryGetValue(methodName, out FluidApiState? state))
		{
			return state;
		}

		FluidApiState newState = new(methodName);
		stateDict.Add(newState.Name, newState);
		
		foreach (string availableFrom in _definition.Methods.Single(m => m.Name == methodName).CanTransitionTo)
		{
			FluidApiState availableState = FindOrCreateMethod(availableFrom, stateDict);
			newState.CanTransitionTo.Add(availableState);
		}
		
		return newState;
	}
}
