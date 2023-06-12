namespace SuperFluid.Internal.Model;

internal record FluidApiState
{
	public FluidApiState(string name)
	{
		Name = name;
	}

	internal string              Name            { get; init; }
	internal List<FluidApiState> CanTransitionTo { get; init; } = new();
}
