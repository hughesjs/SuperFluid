using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.EqualityComparers;
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

		List<FluidApiState> states       = GetMinimalStates(methods);
		FluidApiState       initialState = states.Single(s => s.CanTransitionTo.SequenceEqual(initialMethod.CanTransitionTo));

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

	private List<FluidApiState> GetMinimalStates(List<FluidApiMethod> methods)
	{
		IEnumerable<HashSet<FluidApiMethod>> transitionSets = methods.Select(m => m.CanTransitionTo)
																	 .Distinct(new HashSetSetEqualityComparer<FluidApiMethod>());

		List<FluidApiState> states = new();
		foreach (HashSet<FluidApiMethod> transitionSet in transitionSets)
		{
			IEnumerable<FluidApiMethod> methodsWithTransitionSet = methods.Where(m => m.CanTransitionTo.SetEquals(transitionSet));
			FluidApiState               newState                 = new(transitionSet, methodsWithTransitionSet);
			states.Add(newState);
		}


		return states;
	}

	private FluidApiMethod FindOrCreateMethod(FluidApiMethodDefinition method, Dictionary<FluidApiMethodDefinition, FluidApiMethod> stateDict)
	{
		if (stateDict.TryGetValue(method, out FluidApiMethod? state))
		{
			return state;
		}
		FluidApiMethod newMethod = new(method.Name, ArraySegment<FluidApiMethod>.Empty);
		stateDict.Add(method, newMethod);

		List<FluidApiMethodDefinition> transitionDefinitions = method.CanTransitionTo.Select(m => _definition.Methods.Single(d => d.Name == m)).ToList();
		List<FluidApiMethod>           transitionMethods     = transitionDefinitions.Select(td => FindOrCreateMethod(td, stateDict)).ToList();

		transitionMethods.ForEach(t => newMethod.CanTransitionTo.Add(t));

		return newMethod;
	}
}
