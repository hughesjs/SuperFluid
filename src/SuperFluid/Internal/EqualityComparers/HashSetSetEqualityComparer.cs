namespace SuperFluid.Internal.EqualityComparers;

public class HashSetSetEqualityComparer<T> : IEqualityComparer<HashSet<T>> where T: notnull
{
	public bool Equals(HashSet<T>? x, HashSet<T>? y)
	{
		return !ReferenceEquals(x, null) && !ReferenceEquals(y, null) && x.SetEquals(y);
	}

	public int GetHashCode(HashSet<T>? set)
	{
		if (set is null) return 0;
		
		HashCode hashCode = new();

		foreach (T? code in set.OrderBy(c => c.GetHashCode()))
		{
			hashCode.Add(code);
		}

		return hashCode.ToHashCode();
	}    
} 
