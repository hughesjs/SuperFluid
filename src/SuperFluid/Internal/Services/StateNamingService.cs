using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using SuperFluid.Internal.Definitions;
using SuperFluid.Internal.EqualityComparers;
using SuperFluid.Internal.Exceptions;
using SuperFluid.Internal.Model;

namespace SuperFluid.Internal.Services;

/// <summary>
/// Computes state interface names using a four-tier scheme:
///   Tier 1 — full short form  (ICan{M1}Or{M2}...),     if ≤ 60 chars
///   Tier 2 — truncated form   (ICan{M1}Or{M2}OrNMore), if no collision with another state in the same model
///   Tier 3 — bucket/hash form (ICanDoMany{Actor}State{hash6})
///   Tier 4 — user override    (name from StateNames: in YAML)
/// </summary>
internal static class StateNamingService
{
	private const int ShortFormMaxLength  = 60;
	private const int FullFormHardCeiling = 200;

	/// <summary>
	/// Computes a name for each <see cref="FluidApiState"/> in <paramref name="states"/>.
	/// Applies user overrides first, then auto-tiers.
	/// Throws typed exceptions for invalid user declarations which the caller converts to diagnostics.
	/// </summary>
	/// <returns>A tuple of (name lookup, SF0014 warning names).</returns>
	public static (Dictionary<FluidApiState, string> Names, List<string> UnmatchedWarnings) AssignNames(
		List<FluidApiState> states,
		FluidApiDefinition  definition)
	{
		Dictionary<FluidApiState, string> names             = new();
		List<string>                      unmatchedWarnings = new();
		HashSet<FluidApiState>            userNamedStates   = new();

		if (definition.StateNames is { Count: > 0 })
		{
			// Build a lookup from transition-set → FluidApiState using the existing comparer
			Dictionary<HashSet<FluidApiMethod>, FluidApiState> transitionMap = BuildTransitionMap(states);

			// Validate all C# identifiers up-front before applying any overrides (SF0015 — error, abort)
			foreach (StateNameDefinition entry in definition.StateNames)
			{
				if (!SyntaxFacts.IsValidIdentifier(entry.Name))
				{
					throw new InvalidStateNameIdentifierException(entry.Name);
				}
			}

			// Track which synthesised state has been claimed to detect SF0016
			Dictionary<FluidApiState, string> claimedBy = new();

			foreach (StateNameDefinition entry in definition.StateNames)
			{
				HashSet<FluidApiMethod> requestedTransitionSet = ResolveTransitions(entry.Transitions, states);

				if (!transitionMap.TryGetValue(requestedTransitionSet, out FluidApiState? matchedState))
				{
					// SF0014 — warning, not fatal
					unmatchedWarnings.Add(entry.Name);
					continue;
				}

				// SF0016 — two entries claim the same synthesised state
				if (claimedBy.TryGetValue(matchedState, out string? previousName))
				{
					throw new AmbiguousStateNameDeclarationException(previousName, entry.Name);
				}

				claimedBy[matchedState] = entry.Name;
				userNamedStates.Add(matchedState);
				names[matchedState] = entry.Name;
			}
		}

		List<FluidApiState> unnamedStates = states.FindAll(s => !userNamedStates.Contains(s));
		AssignAutoNames(unnamedStates, definition, names);

		return (names, unmatchedWarnings);
	}

	private static void AssignAutoNames(
		List<FluidApiState>               states,
		FluidApiDefinition                definition,
		Dictionary<FluidApiState, string> names)
	{
		if (states.Count == 0)
		{
			return;
		}

		string actorName = StripLeadingI(definition.Name);

		// Compute the Tier-1 full name for every unnamed state
		List<(FluidApiState State, string Tier1Name)> tier1Results = new();
		foreach (FluidApiState state in states)
		{
			tier1Results.Add((state, ComputeTier1Name(state)));
		}

		// States whose Tier-1 name fits within the short-form limit use Tier 1 directly
		List<(FluidApiState State, string Tier1Name)> needTiering = new();
		foreach ((FluidApiState state, string tier1Name) in tier1Results)
		{
			if (tier1Name.Length <= ShortFormMaxLength)
			{
				names[state] = tier1Name;
			}
			else
			{
				needTiering.Add((state, tier1Name));
			}
		}

		if (needTiering.Count == 0)
		{
			return;
		}

		// Compute Tier-2 candidates and find collisions within this model
		List<(FluidApiState State, string Tier2Name)> tier2Candidates = new();
		foreach ((FluidApiState state, _) in needTiering)
		{
			tier2Candidates.Add((state, ComputeTier2Name(state)));
		}

		HashSet<string> collidingTier2Names = FindCollisions(tier2Candidates);

		foreach ((FluidApiState state, string tier2Name) in tier2Candidates)
		{
			string tier1Name = ComputeTier1Name(state);

			if (tier1Name.Length > FullFormHardCeiling)
			{
				// Exceeds the hard ceiling — skip straight to Tier 3
				names[state] = ComputeTier3Name(state, actorName);
			}
			else if (!collidingTier2Names.Contains(tier2Name))
			{
				// Tier-2 name is unique within this model — safe to use
				names[state] = tier2Name;
			}
			else
			{
				// Tier-2 collision — promote to Tier 3
				names[state] = ComputeTier3Name(state, actorName);
			}
		}
	}

	private static string ComputeTier1Name(FluidApiState state)
	{
		string joined = state.MethodTransitions.Keys
							 .Select(m => m.Name)
							 .OrderBy(n => n, StringComparer.Ordinal)
							 .Aggregate((a, b) => $"{a}Or{b}");
		return $"ICan{joined}";
	}

	private static string ComputeTier2Name(FluidApiState state)
	{
		List<string> sorted = state.MethodTransitions.Keys
								   .Select(m => m.Name)
								   .OrderBy(n => n, StringComparer.Ordinal)
								   .ToList();

		string first  = sorted[0];
		string second = sorted.Count > 1 ? sorted[1] : string.Empty;
		int    rest   = sorted.Count - 2;

		if (sorted.Count <= 2)
		{
			// Tier 2 should only be called for states with > 2 methods;
			// fall back gracefully
			return $"ICan{first}" + (second.Length > 0 ? $"Or{second}" : string.Empty);
		}

		return $"ICan{first}Or{second}Or{rest}More";
	}

	private static string ComputeTier3Name(FluidApiState state, string actorName)
	{
		string hash6 = ComputeHash6(state);
		return $"ICanDoMany{actorName}State{hash6}";
	}

	private static string ComputeHash6(FluidApiState state)
	{
		string combined = state.MethodTransitions.Keys
							   .Select(m => m.Name)
							   .OrderBy(n => n, StringComparer.Ordinal)
							   .Aggregate((a, b) => $"{a}|{b}");

		byte[] inputBytes = Encoding.UTF8.GetBytes(combined);
		byte[] hashBytes;

		// SHA256.HashData is not available on netstandard2.0; use SHA256.Create().ComputeHash()
		using (SHA256 sha256 = SHA256.Create())
		{
			hashBytes = sha256.ComputeHash(inputBytes);
		}

		// Convert.ToHexString is not available on netstandard2.0; use BitConverter
		string hex = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
		return hex.Substring(0, 6);
	}

	private static HashSet<string> FindCollisions(List<(FluidApiState State, string Tier2Name)> candidates)
	{
		HashSet<string> seen      = new(StringComparer.Ordinal);
		HashSet<string> colliding = new(StringComparer.Ordinal);

		foreach ((_, string name) in candidates)
		{
			if (!seen.Add(name))
			{
				colliding.Add(name);
			}
		}

		return colliding;
	}

	private static string StripLeadingI(string name)
	{
		if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
		{
			return name.Substring(1);
		}

		return name;
	}

	/// <summary>
	/// Builds a map from transition-set → <see cref="FluidApiState"/> for all supplied states.
	/// </summary>
	private static Dictionary<HashSet<FluidApiMethod>, FluidApiState> BuildTransitionMap(List<FluidApiState> states)
	{
		HashSetSetEqualityComparer<FluidApiMethod>         comparer = new();
		Dictionary<HashSet<FluidApiMethod>, FluidApiState> map      = new(comparer);

		foreach (FluidApiState state in states)
		{
			HashSet<FluidApiMethod> transitionSet = new(state.MethodTransitions.Keys);
			if (!map.ContainsKey(transitionSet))
			{
				map[transitionSet] = state;
			}
		}

		return map;
	}

	/// <summary>
	/// Resolves a list of method name strings to the corresponding <see cref="FluidApiMethod"/> instances,
	/// returning a <see cref="HashSet{T}"/> suitable for matching against state transition sets.
	/// Throws <see cref="MethodNotFoundException"/> if a name is not found.
	/// </summary>
	private static HashSet<FluidApiMethod> ResolveTransitions(List<string> methodNames, List<FluidApiState> states)
	{
		// Build a flat lookup of all known methods across all states
		Dictionary<string, FluidApiMethod> allMethods = new(StringComparer.Ordinal);
		foreach (FluidApiState state in states)
		{
			foreach (FluidApiMethod method in state.MethodTransitions.Keys)
			{
				if (!allMethods.ContainsKey(method.Name))
				{
					allMethods[method.Name] = method;
				}
			}
		}

		HashSet<FluidApiMethod> result = new();
		foreach (string name in methodNames)
		{
			if (!allMethods.TryGetValue(name, out FluidApiMethod? method))
			{
				throw new MethodNotFoundException("StateNames", name);
			}

			result.Add(method);
		}

		return result;
	}
}
