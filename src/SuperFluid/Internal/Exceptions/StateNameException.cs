namespace SuperFluid.Internal.Exceptions;

internal sealed class InvalidStateNameIdentifierException : InvalidOperationException
{
    public InvalidStateNameIdentifierException(string declaredName)
        : base($"The declared state name '{declaredName}' is not a valid C# identifier")
    {
        DeclaredName = declaredName;
    }

    public string DeclaredName { get; }
}

internal sealed class UnmatchedStateNameDeclarationException : InvalidOperationException
{
    public UnmatchedStateNameDeclarationException(string declaredName)
        : base($"The StateNames entry '{declaredName}' does not match any synthesised state")
    {
        DeclaredName = declaredName;
    }

    public string DeclaredName { get; }
}

internal sealed class AmbiguousStateNameDeclarationException : InvalidOperationException
{
    public AmbiguousStateNameDeclarationException(string firstName, string secondName)
        : base($"Multiple StateNames entries '{firstName}' and '{secondName}' match the same synthesised state")
    {
        FirstName  = firstName;
        SecondName = secondName;
    }

    public string FirstName  { get; }
    public string SecondName { get; }
}
