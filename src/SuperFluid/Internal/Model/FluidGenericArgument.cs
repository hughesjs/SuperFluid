using System.Collections.Immutable;
using SuperFluid.Internal.Exceptions;

namespace SuperFluid.Internal.Model;

internal record FluidGenericArgument(string Name, ImmutableArray<string> Constraints)
{
	public FluidGenericArgument(string name, IEnumerable<string> constraints)
		: this(name, ValidateAndConvert(name, constraints))
	{
	}

	private static ImmutableArray<string> ValidateAndConvert(string name, IEnumerable<string> constraints)
	{
		if (constraints is null)
			throw new ArgumentNullException(nameof(constraints));

		string[] enumeratedConstraints = (constraints as string[] ?? constraints.ToArray()).Distinct().ToArray();
		if (enumeratedConstraints.Length == 0)
			throw new EmptyConstraintsException(name);
		return ImmutableArray.Create(enumeratedConstraints);
	}
}
