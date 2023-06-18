using System.Collections.Generic;
using System.Linq;

namespace SuperFluid.Internal.EqualityComparers;

public class HashSetSetEqualityComparer<T> : IEqualityComparer<HashSet<T>> where T: notnull
{
	public bool Equals(HashSet<T>? x, HashSet<T>? y)
	{
		return !ReferenceEquals(x, null) && !ReferenceEquals(y, null) && x.SetEquals(y);
	}

	public int GetHashCode(HashSet<T>? set)
	{
		int hashCode = 0;

		if (set is not null)
		{
			hashCode = set.Aggregate(hashCode, (current, t) => current ^ set.Comparer.GetHashCode(t) & 0x7FFFFFFF);
		}

		return hashCode;
	}    
} 
