using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Type} {Name}")]
internal record FluidApiArgumentDefinition
{
	public required string Type { get; init; }
	public required string Name { get; init; }
	
	public string? DefaultValue { get; init; }
}
