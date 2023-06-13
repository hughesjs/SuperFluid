namespace SuperFluid.Internal.Model;

internal record FluidApiState
{
	public FluidApiState(List<FluidApiMethod> transitions)
	{
		Name = $"ICan{transitions.Select(t => t.Name).Aggregate((a, b) => $"{a}Or{b}")}";
		CanTransitionTo = transitions;
	}

	internal string               Name            { get; init; }
	internal List<FluidApiMethod> CanTransitionTo { get; init; } = new();
}
