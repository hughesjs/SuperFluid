using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.EqualityComparers;
using SuperFluid.Internal.Model;

namespace SuperFluid.Internal.Parsers;

internal class FluidApiDefinitionParser
{
	public FluidApiModel Parse(FluidApiDefinition definition)
	{
		List<FluidApiMethod> methods = GetMethods(definition, out FluidApiMethod initialMethod);
		List<FluidApiState>  states  = GetMinimalStates(methods, initialMethod, out FluidApiState initialState);

		FluidApiModel model = new()
							  {
								  Name                         = definition.Name,
								  Namespace                    = definition.Namespace,
								  InitialMethod                = initialMethod,
								  Methods                      = methods,
								  InitializerMethodReturnState = initialState,
								  States                       = states
							  };

		return model;
	}

	private List<FluidApiMethod> GetMethods(FluidApiDefinition definition, out FluidApiMethod initialMethod)
	{
		Dictionary<FluidApiMethodDefinition, FluidApiMethod> methodDict = new();

		IEnumerable<FluidApiMethodDefinition> allMethods = definition.Methods.Append(definition.InitialState);
		foreach (FluidApiMethodDefinition method in allMethods)
		{
			FindOrCreateMethod(definition, method, methodDict);
		}

		initialMethod = FindOrCreateMethod(definition, definition.InitialState, methodDict);

		return methodDict.Values.ToList();
	}

	private List<FluidApiState> GetMinimalStates(List<FluidApiMethod> methods, FluidApiMethod initialMethod, out FluidApiState initializerReturnState)
	{
		List<HashSet<FluidApiMethod>> transitionSets = methods
													  .Select(m => m.CanTransitionTo)
													  .Distinct(new HashSetSetEqualityComparer<FluidApiMethod>())
													  .ToList();

		Dictionary<HashSet<FluidApiMethod>, FluidApiState> transitionSetStateDict = new(new HashSetSetEqualityComparer<FluidApiMethod>());
		foreach (HashSet<FluidApiMethod> transitionSet in transitionSets)
		{
			FindOrCreateState(transitionSet, transitionSetStateDict);
		}

		initializerReturnState = FindOrCreateState(initialMethod.CanTransitionTo, transitionSetStateDict);

		List<FluidApiState> states = transitionSetStateDict.Values.Where(s => s.MethodTransitions.Count > 0).ToList();
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


	private FluidApiMethod FindOrCreateMethod(FluidApiDefinition definition, FluidApiMethodDefinition method, Dictionary<FluidApiMethodDefinition, FluidApiMethod> stateDict)
	{
		if (stateDict.TryGetValue(method, out FluidApiMethod? state))
		{
			return state;
		}
		
		List<FluidApiArgument> args = method.Arguments.Select(a => new FluidApiArgument(a.Name, a.Type)).ToList();
		
		FluidApiMethod newMethod = new(method.Name, method.ReturnType, Array.Empty<FluidApiMethod>(), args);
		stateDict.Add(method, newMethod);

		List<FluidApiMethodDefinition> transitionDefinitions = method.CanTransitionTo.Select(m => definition.Methods.Single(d => d.Name == m)).ToList();
		List<FluidApiMethod>           transitionMethods     = transitionDefinitions.Select(td => FindOrCreateMethod(definition, td, stateDict)).ToList();

		transitionMethods.ForEach(t => newMethod.CanTransitionTo.Add(t));

		return newMethod;
	}
}
