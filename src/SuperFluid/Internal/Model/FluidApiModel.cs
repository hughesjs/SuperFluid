using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiModel
{
    public required string               Name                         { get; init; }
    public required string               Namespace                    { get; init; }
    public required FluidApiMethod       InitialMethod                { get; init; }

    // Might actually be able to remove this
    public required List<FluidApiMethod> Methods                      { get; init; } = [];

    public required FluidApiState        InitializerMethodReturnState { get; init; }
    public required List<FluidApiState>  States                       { get; init; } = [];

    /// <summary>
    /// Lookup from <see cref="FluidApiState"/> to its generated interface name.
    /// Single source of truth for state names. Keyed by reference — the record-generated equality
    /// on <see cref="FluidApiState"/> compares its only field (a mutable <see cref="Dictionary{TKey,TValue}"/>)
    /// by reference equality, so two distinct <see cref="FluidApiState"/> instances remain distinct keys.
    /// </summary>
    public Dictionary<FluidApiState, string> StateNames                 { get; init; } = new();

    /// <summary>
    /// SF0014 warnings: names declared in StateNames: that did not match any synthesised state.
    /// </summary>
    public List<string>                  UnmatchedStateNameWarnings   { get; init; } = [];
}
