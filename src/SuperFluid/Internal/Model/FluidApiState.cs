using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("State with {MethodTransitions.Count} transitions")]
internal record FluidApiState
{
	internal Dictionary<FluidApiMethod, FluidApiState> MethodTransitions { get; init; } = new();

	public FluidApiState(Dictionary<FluidApiMethod, FluidApiState> methodTransitions)
	{
		MethodTransitions = methodTransitions;
	}
}
