namespace SuperFluid.Internal.Model;

internal record FluidApiModel
{
	public required string              Name         { get; init; }
	public required FluidApiState       InitialState { get; init; }
	public required List<FluidApiState> States       { get; init; } = new();
}
