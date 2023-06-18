using System.Collections.Generic;
using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiModel
{
	public string              Name         { get; set; }
	public string              Namespace         { get; set; }
	public FluidApiMethod       InitialMethod { get; set; }
	
	// Might actually be able to remove this
	public List<FluidApiMethod> Methods { get; set; } = new();

	public FluidApiState InitializerMethodReturnState { get; set; }
	public List<FluidApiState> States { get; set; } = new();
}
