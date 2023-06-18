using System.Collections.Generic;
using System.Diagnostics;
using SuperFluid.Internal.Backports;

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

	internal string Name { get; set; }

	internal string?                 ReturnType      { get; set; }
	internal HashSet<FluidApiMethod> CanTransitionTo { get; set; } = new();

	internal HashSet<FluidApiArgument> Arguments { get; set; } = new();
}

