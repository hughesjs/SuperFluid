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
		Dictionary<FluidApiMethodDefinition, FluidApiMethod> stateDict = new();

		foreach (FluidApiMethodDefinition method in _definition.Methods.Append(_definition.InitialState))
		{
			FindOrCreateMethod(method, stateDict);
		}

		List<FluidApiMethod> methods       = stateDict.Values.ToList();
		FluidApiMethod       initialMethod = FindOrCreateMethod(_definition.InitialState, stateDict);

		List<List<FluidApiMethod>> minimalTransitionSets = GetMinimalTransitionSets(methods);
		List<FluidApiState>        states                = minimalTransitionSets.Select(ts => new FluidApiState(ts)).ToList();
		FluidApiState              initialState          = states.Single(s => s.CanTransitionTo.SequenceEqual(initialMethod.CanTransitionTo));

		FluidApiModel model = new()
							  {
								  Name          = _definition.Name,
								  InitialMethod = initialMethod,
								  Methods       = methods,
								  InitialState  = initialState,
								  States        = states
							  };

		return model;
	}

	private List<List<FluidApiMethod>> GetMinimalTransitionSets(List<FluidApiMethod> methods)
	{
		List<List<FluidApiMethod>> transitionSets = methods.Select(m => m.CanTransitionTo)
														   .DistinctBy(transitions => string.Join(',', transitions.Select(t => t.Name).Order()))
														   .ToList();
		return transitionSets;
	}

	private FluidApiMethod FindOrCreateMethod(FluidApiMethodDefinition method, Dictionary<FluidApiMethodDefinition, FluidApiMethod> stateDict)
	{
		if (stateDict.TryGetValue(method, out FluidApiMethod? state))
		{
			return state;
		}
		FluidApiMethod newMethod = new(method.Name, new());
		stateDict.Add(method, newMethod);

		List<FluidApiMethodDefinition> transitionDefinitions = method.CanTransitionTo.Select(m => _definition.Methods.Single(d => d.Name == m)).ToList();
		List<FluidApiMethod>           transitionMethods     = transitionDefinitions.Select(td => FindOrCreateMethod(td, stateDict)).ToList();

		newMethod.CanTransitionTo.AddRange(transitionMethods);
		return newMethod;
	}
}
