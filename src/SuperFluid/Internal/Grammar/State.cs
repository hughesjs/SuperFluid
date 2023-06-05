namespace SuperFluid.Internal.Grammar;

internal class State
{
	public required List<State> Transitions { get; init; }
}
