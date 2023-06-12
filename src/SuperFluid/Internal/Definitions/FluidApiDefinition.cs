namespace SuperFluid.Internal.Definitions;

internal record FluidApiDefinition
{
	public required string                   Name         { get; init; }
	public required FluidApiMethodDefinition InitialState { get; init; }
	public required List<FluidApiMethodDefinition> Methods      { get; init; }
}
