using System.Collections.Immutable;

namespace SuperFluid.Internal.Model;

internal record FluidGenericArgument(string Name, ImmutableArray<string> Constraints)
{
	public FluidGenericArgument(string name, IEnumerable<string> constraints)
		: this(name, ValidateAndConvert(constraints))
	{
	}

	private static ImmutableArray<string> ValidateAndConvert(IEnumerable<string> constraints)
	{
		if (constraints is null)
			throw new ArgumentNullException(nameof(constraints));

		string[] enumeratedConstraints = (constraints as string[] ?? constraints.ToArray()).Distinct().ToArray();
		if (enumeratedConstraints.Length == 0)
			throw new ArgumentException("Generic argument must have at least one constraint", nameof(constraints));
		return ImmutableArray.Create(enumeratedConstraints);
	}
}
