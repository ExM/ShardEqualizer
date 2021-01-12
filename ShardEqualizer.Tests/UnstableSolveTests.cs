using System;
using System.IO;
using System.Linq;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.ClusterMaintenance
{
	[TestFixture]
	public class UnstableSolveTests
	{
		[TestCase("dump1.js")]
		[TestCase("dump2.js")]
		[TestCase("dump3.js")]
		[TestCase("conditionDump_20200928_0750.js")]
		public void SmartcatCluster(string dumpFile)
		{
			var text = File.ReadAllText(
				Path.Combine(TestContext.CurrentContext.TestDirectory, "conditionSamples", "SmartcatCluster", dumpFile));

			var zoneOpt = ZoneOptimizationDescriptor.Deserialize(text);

			var solve = ZoneOptimizationSolve.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			foreach (var pair in solve.TargetShards)
			{
				var source = zoneOpt.AllBuckets.Where(_ => _.Shard == pair.Key).Sum(_ => _.CurrentSize) + zoneOpt.UnShardedSize[pair.Key];

				Console.WriteLine($"{pair.Key}\t{zoneOpt.UnShardedSize[pair.Key].ByteSize()}\t{source.ByteSize()}\t{pair.Value.ByteSize()}");
			}

			var sh3 = zoneOpt.Shards[3];
			var sh6 = zoneOpt.Shards[6];

			var checkedColls = zoneOpt.Collections.Where(
				_ => _.FullName != "d.W" &&
				     _.FullName != "d.T"
			);

			Assert.Multiple(() =>
			{
				foreach (var coll in checkedColls)
				{
					var ts3 = solve[coll, sh3].TargetSize;
					var ts6 = solve[coll, sh6].TargetSize;

					var delta = (double) Math.Abs(ts3 - ts6) / Math.Max(ts3, ts6);

					Assert.That(delta, Is.LessThan(0.0001), $"excess delta in {coll}");
				}
			});
		}
	}
}
