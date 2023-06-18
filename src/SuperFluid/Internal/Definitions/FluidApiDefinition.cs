using System.Collections.Generic;
using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Name}")]
internal record FluidApiDefinition
{
	public string                   Name         { get; set; }
	public string                   Namespace    { get; set; }
	public FluidApiMethodDefinition InitialState { get; set; }
	public List<FluidApiMethodDefinition> Methods      { get; set; }
}
