using System.Collections.Immutable;
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

        // Order arguments so that defaults are last (required by C# method signatures).
        // An ImmutableArray preserves this ordering; a HashSet would not.
        FluidApiArgument[] enumeratedArgs = args as FluidApiArgument[] ?? args.ToArray();
        Arguments = [..enumeratedArgs.Where(a => a.DefaultValue is null), ..enumeratedArgs.Where(a => a.DefaultValue is not null)];
    }

    internal string Name { get; init; }

    internal string? ReturnType { get; init; }
    internal HashSet<FluidApiMethod> CanTransitionTo { get; init; } = [];

    internal ImmutableArray<FluidApiArgument> Arguments { get; init; } = [];

    internal ImmutableArray<FluidGenericArgument> GenericArguments { get; init; } = [];
}
