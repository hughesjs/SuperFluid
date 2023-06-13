using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethod
{
	public FluidApiMethod(string name, IEnumerable<FluidApiMethod> transitions)
	{
		Name = name;
		CanTransitionTo = transitions.ToHashSet();
	}

	internal string              Name            { get; init; }
	internal HashSet<FluidApiMethod> CanTransitionTo { get; init; } = new();
}
