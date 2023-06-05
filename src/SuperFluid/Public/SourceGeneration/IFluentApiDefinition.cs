namespace SuperFluid.Public.SourceGeneration;

public interface IFluentApiDefinition
{
    protected string BuilderClassName { get; }
    protected static abstract void DefineApi();
}