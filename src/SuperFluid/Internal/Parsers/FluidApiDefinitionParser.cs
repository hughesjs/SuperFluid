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

		List<FluidApiState> states       = GetMinimalStates(methods, initialMethod);
		FluidApiState       initialState = states.Single(s => s.MethodTransitions.Keys.Contains(initialMethod));

		FluidApiModel model = new()
							  {
								  Name          = _definition.Name,
								  Namespace     = _definition.Namespace,
								  InitialMethod = initialMethod,
								  Methods       = methods,
								  InitialState  = initialState,
								  States        = states
							  };

		return model;
	}

	private List<FluidApiState> GetMinimalStates(List<FluidApiMethod> methods, FluidApiMethod initialMethod)
	{
		List<HashSet<FluidApiMethod>> transitionSets = methods.Select(m => m.CanTransitionTo)
															  .Distinct(new HashSetSetEqualityComparer<FluidApiMethod>())
															  .ToList();
		
		Dictionary<HashSet<FluidApiMethod>, FluidApiState> transitionSetStateDict = new(new HashSetSetEqualityComparer<FluidApiMethod>());
		foreach (HashSet<FluidApiMethod> transitionSet in transitionSets)
		{
			FindOrCreateState(transitionSet, transitionSetStateDict);
		}
		
		// Add in the initial method, it won't be found initially as nothing transitions into it, might be worth revisiting this
		transitionSetStateDict[initialMethod.CanTransitionTo].MethodTransitions.Add(initialMethod, FindOrCreateState(initialMethod.CanTransitionTo, transitionSetStateDict));

		List<FluidApiState> states = transitionSetStateDict.Values.ToList();
		return states;
	}

	private FluidApiState FindOrCreateState(HashSet<FluidApiMethod> transitionSet, Dictionary<HashSet<FluidApiMethod>, FluidApiState> transitionSetStateDict)
	{
		if (transitionSetStateDict.TryGetValue(transitionSet, out FluidApiState? state))
		{
			return state;
		}

		FluidApiState newState = new(new());
		transitionSetStateDict.Add(transitionSet, newState);

		foreach (FluidApiMethod method in transitionSet)
		{
			FluidApiState destinationState = FindOrCreateState(method.CanTransitionTo, transitionSetStateDict);
			newState.MethodTransitions.Add(method, destinationState);
		}
		
		return newState;
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
