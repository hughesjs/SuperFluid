using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Name}")]
internal record FluidApiDefinition
{
	public required string                   Name         { get; init; }
	public required FluidApiMethodDefinition InitialState { get; init; }
	public required List<FluidApiMethodDefinition> Methods      { get; init; }
}
