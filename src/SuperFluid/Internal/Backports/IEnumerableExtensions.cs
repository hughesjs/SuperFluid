using System;
using System.Collections.Generic;

namespace SuperFluid.Internal.Backports;

// ReSharper disable once InconsistentNaming
public static class IEnumerableExtensions
{
	[Obsolete("In the .NET framework and in NET core this method is available, " +
			  "however can't use it in .NET standard yet. When it's added, please remove this method")]
	public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null) => new HashSet<T>(source, comparer);
}
