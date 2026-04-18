namespace SuperFluid.Internal.EqualityComparers;

internal class HashSetSetEqualityComparer<T> : IEqualityComparer<HashSet<T>> where T: notnull
{
	public bool Equals(HashSet<T>? x, HashSet<T>? y)
	{
		if (x is null && y is null) return true;
		if (x is null || y is null) return false;
		return x.SetEquals(y);
	}

	public int GetHashCode(HashSet<T>? set)
	{
		if (set is null) return 0;

		// XOR is commutative and associative, so the result is independent of
		// iteration order. Seed with Count so sets of different sizes whose
		// elements happen to XOR to the same value still hash differently.
		int hash = set.Count;
		foreach (T item in set)
		{
			hash ^= EqualityComparer<T>.Default.GetHashCode(item);
		}

		return hash;
	}
} 
