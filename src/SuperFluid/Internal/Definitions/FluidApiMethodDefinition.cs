using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethodDefinition
{
	public required string Name { get; init; }
	public required List<string> CanTransitionTo { get; init; } = new();
}
