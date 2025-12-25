using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethodDefinition
{
	public required string Name { get; init; }
	public string? ReturnType { get; init; }
	public List<string> CanTransitionTo { get; init; } = [];
	
	public List<FluidApiArgumentDefinition> Arguments { get; init; } = [];
	
	public List<FluidGenericArgumentDefinition> GenericArguments { get; init; } = [];
}
