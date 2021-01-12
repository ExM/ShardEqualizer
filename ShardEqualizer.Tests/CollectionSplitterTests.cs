using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ShardEqualizer
{
	[TestFixture]
	public class CollectionSplitterTests
	{
		[TestCaseSource(nameof(splitCases))]
		public void Split(int count, int numberOfParts, int[] partCounts)
		{
			var coll = Enumerable.Range(0, count).ToList();

			var parts = coll.Split(numberOfParts).ToList();
			
			CollectionAssert.AreEquivalent(partCounts, parts.Select(_ => _.Count));
		}

		private static IEnumerable<TestCaseData> splitCases()
		{
			yield return new TestCaseData(10, 10, new []{1,1,1,1,1,1,1,1,1,1});
			yield return new TestCaseData(5, 6, new []{1,1,1,1,1,0});
			yield return new TestCaseData(10, 2, new []{5,5});
			yield return new TestCaseData(10, 3, new []{4,3,3});
			yield return new TestCaseData(14, 3, new []{5,5,4});
			yield return new TestCaseData(1047, 6, new []{175, 175, 175, 174, 174, 174});
		}
	}
}