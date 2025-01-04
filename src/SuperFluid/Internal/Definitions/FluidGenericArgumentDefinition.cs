namespace SuperFluid.Internal.Definitions;

using System.Diagnostics;

[DebuggerDisplay("{Name}")]
internal record FluidGenericArgumentDefinition
{
    public required List<string> Constraints { get; init; }
    public required string Name { get; init; }
}
