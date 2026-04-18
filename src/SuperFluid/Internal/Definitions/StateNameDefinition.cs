namespace SuperFluid.Internal.Definitions;

internal record StateNameDefinition
{
    public required string       Name        { get; init; }
    public List<string>          Transitions { get; init; } = [];
}
