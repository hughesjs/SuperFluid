using SuperFluid.Internal.EqualityComparers;

namespace SuperFluid.Tests.EqualityComparers;

public class HashSetSetEqualityComparerTests
{
	private readonly HashSetSetEqualityComparer<string> _stringComparer = new();
	private readonly HashSetSetEqualityComparer<int> _intComparer = new();

	[Fact]
	public void SameInstanceReturnsTrue()
	{
		HashSet<string> set = ["a", "b", "c"];

		bool result = _stringComparer.Equals(set, set);

		result.ShouldBeTrue();
	}

	[Fact]
	public void DifferentInstancesWithSameContentsReturnsTrue()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["a", "b", "c"];

		bool result = _stringComparer.Equals(set1, set2);

		result.ShouldBeTrue();
	}

	[Fact]
	public void DifferentInstancesSameContentsDifferentOrderReturnsTrue()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["c", "a", "b"];

		bool result = _stringComparer.Equals(set1, set2);

		result.ShouldBeTrue();
	}

	[Fact]
	public void DifferentContentsReturnsFalse()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["x", "y", "z"];

		bool result = _stringComparer.Equals(set1, set2);

		result.ShouldBeFalse();
	}

	[Fact]
	public void DifferentSizesReturnsFalse()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["a", "b"];

		bool result = _stringComparer.Equals(set1, set2);

		result.ShouldBeFalse();
	}

	[Fact]
	public void BothNullReturnsFalse()
	{
		bool result = _stringComparer.Equals(null, null);

		result.ShouldBeFalse();
	}

	[Fact]
	public void FirstNullReturnsFalse()
	{
		HashSet<string> set = ["a", "b", "c"];

		bool result = _stringComparer.Equals(null, set);

		result.ShouldBeFalse();
	}

	[Fact]
	public void SecondNullReturnsFalse()
	{
		HashSet<string> set = ["a", "b", "c"];

		bool result = _stringComparer.Equals(set, null);

		result.ShouldBeFalse();
	}

	[Fact]
	public void BothEmptySetsReturnsTrue()
	{
		HashSet<string> set1 = [];
		HashSet<string> set2 = [];

		bool result = _stringComparer.Equals(set1, set2);

		result.ShouldBeTrue();
	}

	[Fact]
	public void SameSetProducesSameHashCode()
	{
		HashSet<string> set = ["a", "b", "c"];

		int hash1 = _stringComparer.GetHashCode(set);
		int hash2 = _stringComparer.GetHashCode(set);

		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void DifferentInstancesSameContentsProduceSameHashCode()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["a", "b", "c"];

		int hash1 = _stringComparer.GetHashCode(set1);
		int hash2 = _stringComparer.GetHashCode(set2);

		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void SameContentsDifferentOrderProducesSameHashCode()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["c", "a", "b"];
		HashSet<string> set3 = ["b", "c", "a"];

		int hash1 = _stringComparer.GetHashCode(set1);
		int hash2 = _stringComparer.GetHashCode(set2);
		int hash3 = _stringComparer.GetHashCode(set3);

		hash1.ShouldBe(hash2);
		hash2.ShouldBe(hash3);
	}

	[Fact]
	public void DifferentContentsProducesDifferentHashCode()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["x", "y", "z"];

		int hash1 = _stringComparer.GetHashCode(set1);
		int hash2 = _stringComparer.GetHashCode(set2);

		hash1.ShouldNotBe(hash2);
	}

	[Fact]
	public void NullHashCodeReturnsZero()
	{
		int hash = _stringComparer.GetHashCode(null);

		hash.ShouldBe(0);
	}

	[Fact]
	public void EmptySetProducesConsistentHashCode()
	{
		HashSet<string> set1 = [];
		HashSet<string> set2 = [];

		int hash1 = _stringComparer.GetHashCode(set1);
		int hash2 = _stringComparer.GetHashCode(set2);

		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void SingleElementProducesConsistentHashCode()
	{
		HashSet<string> set1 = ["single"];
		HashSet<string> set2 = ["single"];

		int hash1 = _stringComparer.GetHashCode(set1);
		int hash2 = _stringComparer.GetHashCode(set2);

		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void IntegerSetsProduceConsistentHashCode()
	{
		HashSet<int> set1 = [1, 2, 3, 4, 5];
		HashSet<int> set2 = [5, 4, 3, 2, 1];

		int hash1 = _intComparer.GetHashCode(set1);
		int hash2 = _intComparer.GetHashCode(set2);

		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void SubsetsProduceDifferentHashes()
	{
		HashSet<string> set1 = ["a", "b", "c"];
		HashSet<string> set2 = ["a", "b"];

		int hash1 = _stringComparer.GetHashCode(set1);
		int hash2 = _stringComparer.GetHashCode(set2);

		hash1.ShouldNotBe(hash2);
	}

	[Fact]
	public void LargeSetsProduceConsistentHashCode()
	{
		HashSet<int> set1 = Enumerable.Range(1, 100).ToHashSet();
		HashSet<int> set2 = Enumerable.Range(1, 100).Reverse().ToHashSet();

		int hash1 = _intComparer.GetHashCode(set1);
		int hash2 = _intComparer.GetHashCode(set2);

		hash1.ShouldBe(hash2);
	}

	[Fact]
	public void CanBeUsedAsDictionaryKey()
	{
		var comparer = new HashSetSetEqualityComparer<string>();
		var dictionary = new Dictionary<HashSet<string>, string>(comparer);
		HashSet<string> key1 = ["a", "b", "c"];
		HashSet<string> key2 = ["c", "a", "b"];

		dictionary[key1] = "value";
		string? result = dictionary.TryGetValue(key2, out string? value) ? value : null;

		result.ShouldBe("value");
	}

	[Fact]
	public void RemovesDuplicateSetsWhenUsedWithDistinct()
	{
		var comparer = new HashSetSetEqualityComparer<string>();
		List<HashSet<string>> sets =
		[
			["a", "b"],
			["b", "a"],
			["c", "d"],
			["a", "b"],
			["c", "d"]
		];

		var distinct = sets.Distinct(comparer).ToList();

		distinct.Count.ShouldBe(2);
	}
}
