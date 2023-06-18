using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Type} {Name}")]
internal class FluidApiArgument
{
	public FluidApiArgument(string argName, string argType)
	{
		Name = argName;
		Type = argType;
	}

	public string Type { get; set; }
	public string Name { get; set; }
}
