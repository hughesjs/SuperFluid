namespace SuperFluid.Internal.Model;

internal record FluidApiModel
{
	public string        Name         { get; init; }
	public FluidApiState InitialState { get; init; }
	public List<FluidApiState> States       { get; init; }
}
