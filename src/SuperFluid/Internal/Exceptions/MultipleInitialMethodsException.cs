namespace SuperFluid.Internal.Exceptions;

internal sealed class MultipleInitialMethodsException : InvalidOperationException
{
	public MultipleInitialMethodsException(string grammarInterfaceName, IReadOnlyList<string> methodNames)
		: base($"Grammar interface '{grammarInterfaceName}' declares multiple methods with [Initial]: {string.Join(", ", methodNames)}. Exactly one is required.")
	{
		GrammarInterfaceName = grammarInterfaceName;
		MethodNames = methodNames;
	}

	public string GrammarInterfaceName { get; }
	public IReadOnlyList<string> MethodNames { get; }
}
