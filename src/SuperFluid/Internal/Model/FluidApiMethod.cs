using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethod
{
	public FluidApiMethod(string name, string? returnType, IEnumerable<FluidApiMethod> transitions, IEnumerable<FluidApiArgument> args)
	{
		Name            = name;
		ReturnType      = returnType;
		Arguments       = args.ToHashSet();
		CanTransitionTo = transitions.ToHashSet();
	}

	internal string Name { get; init; }

	internal string?                 ReturnType      { get; init; }
	internal HashSet<FluidApiMethod> CanTransitionTo { get; init; } = new();

	internal HashSet<FluidApiArgument> Arguments { get; init; } = new();
}

