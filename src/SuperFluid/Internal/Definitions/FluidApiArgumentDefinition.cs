using System.Diagnostics;

namespace SuperFluid.Internal.Definitions;

[DebuggerDisplay("{Type} {Name}")]
internal record FluidApiArgumentDefinition
{
	public string Type { get; set; }
	public string Name { get; set; }
}
