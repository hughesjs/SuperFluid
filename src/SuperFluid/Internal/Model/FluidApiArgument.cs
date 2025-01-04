using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Type} {Name}")]
internal class FluidApiArgument
{
	public FluidApiArgument(string argName, string argType, string? defaultValue)
	{
		Name = argName;
		Type = argType;
		DefaultValue = defaultValue;
	}

	public string Type { get; init; }
	public string Name { get; init; }
	
	public string? DefaultValue { get; init; }
}
