namespace SuperFluid.Internal.Model;

internal record FluidApiModel
{
	public required string              Name         { get; init; }
	public required FluidApiMethod       InitialMethod { get; init; }
	public required List<FluidApiMethod> States       { get; init; } = new();
}
