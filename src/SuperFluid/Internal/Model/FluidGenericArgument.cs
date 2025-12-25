namespace SuperFluid.Internal.Model;

internal record FluidGenericArgument(string Name, HashSet<string> Constraints)
{
	public FluidGenericArgument(string name, IEnumerable<string> constraints)
		: this(name, ValidateAndConvert(constraints))
	{
	}

	private static HashSet<string> ValidateAndConvert(IEnumerable<string> constraints)
	{
		string[] enumeratedConstraints = constraints as string[] ?? constraints.ToArray();
		if (enumeratedConstraints.Length == 0)
			throw new ArgumentException("Generic argument must have at least one constraint");
		return [..enumeratedConstraints];
	}
}
