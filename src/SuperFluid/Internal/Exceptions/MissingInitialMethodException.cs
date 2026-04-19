namespace SuperFluid.Internal.Exceptions;

internal sealed class MissingInitialMethodException : InvalidOperationException
{
	public MissingInitialMethodException(string grammarInterfaceName)
		: base($"Grammar interface '{grammarInterfaceName}' has no method decorated with [Initial]")
	{
		GrammarInterfaceName = grammarInterfaceName;
	}

	public string GrammarInterfaceName { get; }
}
