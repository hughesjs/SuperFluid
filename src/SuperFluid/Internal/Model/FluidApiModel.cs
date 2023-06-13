using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiModel
{
	public required string              Name         { get; init; }
	public required string              Namespace         { get; init; }
	public required FluidApiMethod       InitialMethod { get; init; }
	
	// Might actually be able to remove this
	public required List<FluidApiMethod> Methods       { get; init; } = new();

	public required FluidApiState InitializerMethodReturnState { get; init; }
	public required List<FluidApiState> States { get; init; } = new();
}
