using System.Diagnostics;

namespace SuperFluid.Internal.Model;

[DebuggerDisplay("{Name}")]
internal record FluidApiMethod
{
    public FluidApiMethod(string name, string? returnType, IEnumerable<FluidApiMethod> transitions, IEnumerable<FluidApiArgument> args,
        IEnumerable<FluidGenericArgument> genericArgs)
    {
        Name = name;
        ReturnType = returnType;
        CanTransitionTo = [..transitions];
        GenericArguments = [..genericArgs];

        // We need to order the arguments so that the ones with defaults are last
        FluidApiArgument[] enumeratedArgs = args as FluidApiArgument[] ?? args.ToArray();
        var orderedArgs = enumeratedArgs.Where(a => a.DefaultValue is null).Concat(enumeratedArgs.Where(a => a.DefaultValue is not null)).ToList();
        Arguments = [..orderedArgs];
    }

    internal string Name { get; init; }

    internal string? ReturnType { get; init; }
    internal HashSet<FluidApiMethod> CanTransitionTo { get; init; } = new();

    internal HashSet<FluidApiArgument> Arguments { get; init; } = new();

    internal HashSet<FluidGenericArgument> GenericArguments { get; init; } = new();
}