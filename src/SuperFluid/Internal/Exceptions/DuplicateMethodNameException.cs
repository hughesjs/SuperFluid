namespace SuperFluid.Internal.Exceptions;

internal sealed class DuplicateMethodNameException : InvalidOperationException
{
	public DuplicateMethodNameException(string methodName)
		: base($"Duplicate method name '{methodName}' found")
	{
		MethodName = methodName;
	}

	public string MethodName { get; }
}
