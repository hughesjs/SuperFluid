namespace SuperFluid.Internal.Model;

internal record FluidApiState
{
	public FluidApiState(string name)
	{
		Name = name;
	}

	internal string Name { get; set; }
	internal List<FluidApiState> AvailableFrom { get; set; } = new();
}
