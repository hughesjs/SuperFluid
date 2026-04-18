using System;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.EqualityComparers;
using SuperFluid.Internal.Exceptions;
using SuperFluid.Internal.Model;
using SuperFluid.Internal.Services;

namespace SuperFluid.Internal.Parsers;

internal class FluidApiDefinitionParser
{
    public FluidApiModel Parse(FluidApiDefinition definition)
    {
        if (definition is null)
            throw new ArgumentNullException(nameof(definition));
        if (definition.Methods is null)
            throw new ArgumentNullException(nameof(definition.Methods));
        if (definition.InitialState is null)
            throw new ArgumentNullException(nameof(definition.InitialState));

        List<FluidApiMethod> methods = GetMethods(definition, out FluidApiMethod initialMethod);
        List<FluidApiState>  states  = GetMinimalStates(methods, initialMethod, out FluidApiState initialState);

        // Assign state names using the tiered naming scheme (may throw for SF0015/SF0016)
        (Dictionary<FluidApiState, string> stateNames, List<string> unmatchedWarnings) = StateNamingService.AssignNames(states, definition);

        // Empty terminal states are filtered out of `states` but may still appear as destination
        // references inside non-terminal states' MethodTransitions (or as the initial return state).
        // Register them in the name map so GenerateMethodSource's
        // `method.ReturnType ?? model.StateNames[state]` fallback works.
        foreach (FluidApiState state in states)
        {
            foreach (FluidApiState destination in state.MethodTransitions.Values)
            {
                if (!stateNames.ContainsKey(destination))
                {
                    stateNames[destination] = "Terminating State";
                }
            }
        }

        if (!stateNames.ContainsKey(initialState))
        {
            stateNames[initialState] = "Terminating State";
        }

        FluidApiModel model = new()
                              {
                                  Name                         = definition.Name,
                                  Namespace                    = definition.Namespace,
                                  Description                  = definition.Description,
                                  InitialMethod                = initialMethod,
                                  Methods                      = methods,
                                  InitializerMethodReturnState = initialState,
                                  States                       = states,
                                  StateNames                   = stateNames,
                                  UnmatchedStateNameWarnings   = unmatchedWarnings
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

		List<FluidApiArgument> args = method.Arguments.Select(a => new FluidApiArgument(a.Name, a.Type, a.DefaultValue)).ToList();

		List<FluidGenericArgument> genericArgs = method.GenericArguments.Select(a => new FluidGenericArgument(a.Name, a.Constraints)).ToList();

		FluidApiMethod newMethod = new(method.Name, method.ReturnType, method.Description, [], args, genericArgs);
		stateDict.Add(method, newMethod);

		List<FluidApiMethodDefinition> transitionDefinitions = method.CanTransitionTo.Select(m => FindMethodByName(definition, m, method.Name)).ToList();
		List<FluidApiMethod>           transitionMethods     = transitionDefinitions.Select(td => FindOrCreateMethod(definition, td, stateDict)).ToList();

		transitionMethods.ForEach(t => newMethod.CanTransitionTo.Add(t));

		return newMethod;
	}

	private FluidApiMethodDefinition FindMethodByName(FluidApiDefinition definition, string methodName, string referencingMethod)
	{
		IEnumerable<FluidApiMethodDefinition> allMethods = definition.Methods.Append(definition.InitialState);
		FluidApiMethodDefinition[] matches = allMethods
			.Where(d => d.Name == methodName)
			.ToArray();

		if (matches.Length == 0)
		{
			throw new MethodNotFoundException(referencingMethod, methodName);
		}

		if (matches.Length > 1)
		{
			throw new DuplicateMethodNameException(methodName);
		}

		return matches[0];
	}

}
