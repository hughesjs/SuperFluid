namespace SuperFluid.Internal.Definitions;

internal record FluidApiDefinition
{
	public string                         Name         { get; init; }
	public string                         InitialState { get; init; }
	public List<FluidApiMethodDefinition> Methods      { get; init; }
}
