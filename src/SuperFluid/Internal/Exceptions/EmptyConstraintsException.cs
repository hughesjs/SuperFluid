namespace SuperFluid.Internal.Exceptions;

internal sealed class EmptyConstraintsException : InvalidOperationException
{
	public EmptyConstraintsException(string genericArgumentName)
		: base($"Generic argument '{genericArgumentName}' must have at least one constraint")
	{
		GenericArgumentName = genericArgumentName;
	}

	public string GenericArgumentName { get; }
}
