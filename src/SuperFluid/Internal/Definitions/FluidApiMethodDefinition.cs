namespace SuperFluid.Internal.Definitions;

internal record FluidApiMethodDefinition
{
	public required  string Name { get; init; }
	public required List<string> AvailableFrom { get; init; } = new();
}
