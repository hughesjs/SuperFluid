namespace SuperFluid.Internal.EqualityComparers;

internal class HashSetSetEqualityComparer<T> : IEqualityComparer<HashSet<T>> where T: notnull
{
	public bool Equals(HashSet<T>? x, HashSet<T>? y)
	{
		return x is not null && y is not null && x.SetEquals(y);
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
