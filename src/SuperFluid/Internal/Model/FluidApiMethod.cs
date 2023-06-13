namespace SuperFluid.Internal.Model;

internal record FluidApiMethod
{
	public FluidApiMethod(string name, List<FluidApiMethod> transitions)
	{
		Name = name;
		CanTransitionTo = transitions;
	}

	internal string              Name            { get; init; }
	internal List<FluidApiMethod> CanTransitionTo { get; init; } = new();
}
