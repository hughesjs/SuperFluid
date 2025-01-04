namespace SuperFluid.Internal.Model;

public class FluidGenericArgument
{
    public FluidGenericArgument(string name, IEnumerable<string> constraints)
    {
        var enumeratedConstraints = constraints as string[] ?? constraints.ToArray();
        if (enumeratedConstraints.Length == 0)
        {
            throw new ArgumentException("Generic argument must have at least one constraint");
        }
        
        Name = name;
        Constraints = [..enumeratedConstraints];
    }

    internal  HashSet<string> Constraints { get; init; }
    internal  string Name { get; init; }
}