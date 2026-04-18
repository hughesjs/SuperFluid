namespace SuperFluid.Internal.Exceptions;

internal sealed class MethodNotFoundException : InvalidOperationException
{
	public MethodNotFoundException(string referencingMethod, string missingMethod)
		: base($"Method '{missingMethod}' referenced in CanTransitionTo by '{referencingMethod}' does not exist")
	{
		ReferencingMethod = referencingMethod;
		MissingMethod = missingMethod;
	}

	public string ReferencingMethod { get; }
	public string MissingMethod { get; }
}
