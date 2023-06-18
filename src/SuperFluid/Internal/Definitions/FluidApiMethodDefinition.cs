using System.Collections.Generic;
using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethodDefinition
{
	public string       Name            { get; set; }
	public string?      ReturnType      { get; set; }
	public List<string> CanTransitionTo { get; set; } = new();
	
	public List<FluidApiArgumentDefinition> Arguments { get; set; } = new();
}
