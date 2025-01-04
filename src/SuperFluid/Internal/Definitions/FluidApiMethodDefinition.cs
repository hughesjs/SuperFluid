using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethodDefinition
{
	public required string Name { get; init; }
	public string? ReturnType { get; init; }
	public List<string> CanTransitionTo { get; init; } = new();
	
	public List<FluidApiArgumentDefinition> Arguments { get; init; } = new();
	
	public List<string> GenericArguments { get; init; } = new();
}
