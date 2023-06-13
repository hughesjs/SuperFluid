using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiState
{
	internal string                                    Name              => MethodTransitions.Count == 0 ? "Terminating State" : $"ICan{MethodTransitions.Keys.Select(t => t.Name).Aggregate((a, b) => $"{a}Or{b}")}";
	internal Dictionary<FluidApiMethod, FluidApiState> MethodTransitions { get; init; } = new();

	public FluidApiState(Dictionary<FluidApiMethod, FluidApiState> methodTransitions)
	{
		MethodTransitions = methodTransitions;
	}

}
