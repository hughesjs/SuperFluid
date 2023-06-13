using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethod
{
	public FluidApiMethod(string name, string? returnType, IEnumerable<FluidApiMethod> transitions)
	{
		Name            = name;
		ReturnType      = returnType;
		CanTransitionTo = transitions.ToHashSet();
	}

	internal string              Name            { get; init; }
	
	internal string? ReturnType { get; init; }
	internal HashSet<FluidApiMethod> CanTransitionTo { get; init; } = new();
}
