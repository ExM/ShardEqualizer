using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public static class OptimalDataPartition
	{
		public static Solve Find(ZoneOptimizationDescriptor source, CancellationToken token)
		{
			var managedBuckets = source.AllManagedBuckets;
			var managedBucketsByCollection = managedBuckets
				.GroupBy(_ => _.Collection).ToDictionary(p => p.Key, p => p.ToList());
			var managedBucketsByShard = managedBuckets
				.GroupBy(_ => _.Shard).ToDictionary(p => p.Key, p => p.ToList());

			var managedSizeByCollection = managedBucketsByCollection.ToDictionary(p => p.Key,
				p => p.Value.Sum(_ => _.CurrentSize));

			var avgCollectionSize = managedBucketsByCollection.ToDictionary(p => p.Key,
				p => (double) managedSizeByCollection[p.Key] / p.Value.Count);

			var shardEqSolver = new SquareSolver<Bucket>();

			foreach (var bucket in managedBuckets)
			{ // init variables and limits
				shardEqSolver.InitVariable(bucket, bucket.CurrentSize);
				shardEqSolver.SetMin(bucket, bucket.MinSize);
				shardEqSolver.SetMax(bucket, Math.Max(avgCollectionSize[bucket.Collection] * 2, bucket.CurrentSize));
			}

			foreach (var (coll, buckets) in managedBucketsByCollection)
			{ // init collection size constraint
				shardEqSolver.SetEqualConstraint(
					Vector<Bucket>.Unit(buckets),
					managedSizeByCollection[coll]);
			}

			var totalSize = source.AllBuckets.Sum(_ => _.CurrentSize) + source.UnShardedSize2.Values.Sum();
			var avgShardSize = (double) totalSize / source.Shards.Count;

			var avgManagedShardSize = (double) source.TotalManagedSize / source.Shards.Count;
			var targetShardSize = source.UnmovedSizeByShard.ToDictionary(_ => _.Key, _ => avgManagedShardSize - _.Value);

			foreach (var (shard, buckets) in managedBucketsByShard)
			{ // init objective shard size
				var unManagedSize = source.AllBuckets
					.Where(_ => !_.Managed && _.Shard == shard)
					.Sum(_ => _.CurrentSize);

				var target = avgShardSize - source.UnShardedSize2[shard] - unManagedSize;

				shardEqSolver.SetObjective(Vector<Bucket>.Unit(buckets), target);
			}

			if (!shardEqSolver.Find(token))
				return new Solve(false);

			foreach (var bucket in managedBuckets)
			{ // read solution
				bucket.TargetSize = (long) Math.Round(shardEqSolver.GetSolution(bucket));
			}

			return new Solve(true, shardEqSolver.ActiveConstraints);
		}

		public class Solve
		{
			public Solve(bool isSuccess)
			{
				IsSuccess = isSuccess;
				ActiveConstraints = new List<string>();
			}
			public Solve(bool isSuccess, IList<string> activeConstraints)
			{
				IsSuccess = isSuccess;
				ActiveConstraints = activeConstraints;
			}

			public bool IsSuccess { get; }
			public IList<string> ActiveConstraints { get; }
		}
	}
}
