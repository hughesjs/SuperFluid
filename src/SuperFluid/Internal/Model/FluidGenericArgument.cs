using System.Collections.Immutable;

namespace SuperFluid.Internal.Model;

internal record FluidGenericArgument(string Name, ImmutableHashSet<string> Constraints)
{
	public FluidGenericArgument(string name, IEnumerable<string> constraints)
		: this(name, ValidateAndConvert(constraints))
	{
	}

	private static ImmutableHashSet<string> ValidateAndConvert(IEnumerable<string> constraints)
	{
		if (constraints is null)
			throw new ArgumentNullException(nameof(constraints));

		string[] enumeratedConstraints = constraints as string[] ?? constraints.ToArray();
		if (enumeratedConstraints.Length == 0)
			throw new ArgumentException("Generic argument must have at least one constraint", nameof(constraints));
		return ImmutableHashSet.CreateRange(enumeratedConstraints);
	}
}
