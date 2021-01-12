using System.Collections.Generic;
using NUnit.Framework;

namespace MongoDB.ClusterMaintenance
{
	[TestFixture]
	public class CalcPercentilesTests
	{
		[Test]
		public void Demo()
		{
			var values = new long[] { 1, 3, 3, 4, 5, 9, 10, 10 };
			var bounds = new List<double> {0, .1, .5, .75, .85, .9, .95, 1};

			var percentiles = values.CalcPercentiles(bounds);
			
			Assert.That(percentiles, Is.EquivalentTo(new []{ 1, 3, 5, 10, 10, 10, 10, 10}));
		}
	}
}