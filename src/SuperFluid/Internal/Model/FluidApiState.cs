using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiState
{
	internal string                  Name            { get; init; }
	internal HashSet<FluidApiMethod> CanTransitionTo { get; init; } = new();
	internal HashSet<FluidApiMethod> Methods         { get; init; } = new();

	public FluidApiState(IEnumerable<FluidApiMethod> transitions, IEnumerable<FluidApiMethod> methods)
	{
		Methods        = methods.ToHashSet();
		Name            = $"ICan{methods.Select(t => t.Name).Aggregate((a, b) => $"{a}Or{b}")}";
		CanTransitionTo = transitions.ToHashSet();
	}

}
