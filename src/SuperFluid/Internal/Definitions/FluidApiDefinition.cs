namespace SuperFluid.Internal.Definitions;

internal record FluidApiDefinition
{
	public string                   Name { get; init; }
	public FluidApiMethodDefinition InitialState { get; init; }
	public List<FluidApiMethodDefinition> Methods      { get; init; }
}
