using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Type} {Name}")]
internal record FluidApiArgument(string Name, string Type, string? DefaultValue);
