namespace SuperFluid.Internal.Definitions;

internal record FluidApiMethodDefinition
{
	public string Name { get; init; }
	public List<string> AvailableFrom { get; init; }
}
