using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public static class OptimalDataPartition
	{
		public static Solve Find(ZoneOptimizationDescriptor source) => Find(source, CancellationToken.None);

		public static Solve Find(ZoneOptimizationDescriptor source, CancellationToken token)
		{
			var managedBuckets = source.AllManagedBuckets;
			var managedBucketsByCollection = managedBuckets
				.GroupBy(_ => _.Collection).ToDictionary(p => p.Key, p => p.ToList());
			var managedBucketsByShard = managedBuckets
				.GroupBy(_ => _.Shard).ToDictionary(p => p.Key, p => p.ToList());

			var managedSizeByCollection = managedBucketsByCollection.ToDictionary(p => p.Key,
				p => p.Value.Sum(_ => _.CurrentSize));

			//fill unmanaged buckets
			foreach (var bucket in source.AllBuckets.Where(_ => !_.Managed))
				bucket.TargetSize = bucket.CurrentSize;

			var shardEqSolver = new SquareSolver<Bucket>();

			var minorMultiply = 0.1d / managedBuckets.Count;

			foreach (var bucket in managedBuckets)
			{ // init variables and limits
				shardEqSolver.InitVariable(bucket, bucket.CurrentSize);
				var min = bucket.MinSize;
				shardEqSolver.SetMin(bucket, min, renderMinConstraint(bucket, min));
				var max = Math.Max(managedSizeByCollection[bucket.Collection] * source.MaxBucketSize, bucket.CurrentSize);
				shardEqSolver.SetMax(bucket, max, renderMaxConstraint(bucket, (long)Math.Round(max)));

				shardEqSolver.SetObjective(Vector<Bucket>.Unit(new []{ bucket }) * (minorMultiply / bucket.CurrentSize), minorMultiply);
			}

			foreach (var (coll, buckets) in managedBucketsByCollection)
			{ // init collection size constraint
				shardEqSolver.SetEqualConstraint(
					Vector<Bucket>.Unit(buckets),
					managedSizeByCollection[coll]);
			}

			var totalSize = source.AllBuckets.Sum(_ => _.CurrentSize) + source.UnShardedSize2.Values.Sum();
			var avgShardSize = (double) totalSize / source.Shards.Count;

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

			var collectionEqSolver = new SquareSolver<Bucket>();

			foreach (var bucket in managedBuckets)
			{ // init variables and limits
				collectionEqSolver.InitVariable(bucket, bucket.TargetSize);
				var min = bucket.MinSize;
				collectionEqSolver.SetMin(bucket, min, renderMinConstraint(bucket, min));
				var max = Math.Max(managedSizeByCollection[bucket.Collection] * source.MaxBucketSize, bucket.TargetSize);
				collectionEqSolver.SetMax(bucket, max, renderMaxConstraint(bucket, (long)Math.Round(max)));
			}


			foreach (var (coll, buckets) in managedBucketsByCollection)
			{ // init collection size constraint
				collectionEqSolver.SetEqualConstraint(
					Vector<Bucket>.Unit(buckets),
					buckets.Sum(_ => _.TargetSize));
			}

			foreach (var buckets in managedBucketsByShard.Values)
			{ // init shard size constraint
				collectionEqSolver.SetEqualConstraint(
					Vector<Bucket>.Unit(buckets),
					buckets.Sum(_ => _.TargetSize));
			}

			var maxAvg = 0.0;

			foreach (var (coll, buckets) in managedBucketsByCollection)
			{ // init objective collection size
				var collPriority = source.CollectionSettings[coll].Priority;
				if(collPriority == 0)
					continue;

				var collSize = buckets.Sum(_ => _.TargetSize);
				var avg = (double) collSize / buckets.Count;

				maxAvg = Math.Max(maxAvg, avg);
			}

			foreach (var (coll, buckets) in managedBucketsByCollection)
			{ // init objective collection size
				var collPriority = source.CollectionSettings[coll].Priority;
				if(collPriority == 0)
					continue;

				var collSize = buckets.Sum(_ => _.TargetSize);
				var avg = (double) collSize / buckets.Count;

				foreach (var bucket in buckets)
				{
					var v = new Vector<Bucket>() {[bucket] = collPriority * maxAvg / avg};
					collectionEqSolver.SetObjective(v, collPriority * maxAvg );
				}
			}

			if (!collectionEqSolver.Find(token))
				return new Solve(false);

			foreach (var bucket in managedBuckets)
			{ // read solution
				bucket.TargetSize = (long) Math.Round(collectionEqSolver.GetSolution(bucket));
			}

			return new Solve(true, collectionEqSolver.ActiveConstraints);
		}

		private static string renderMaxConstraint(Bucket bucket, long max)
		{
			return $"collection {bucket.Collection} from {bucket.Shard} <= {max.ByteSize()}";
		}

		private static string renderMinConstraint(Bucket bucket, long min)
		{
			return $"collection {bucket.Collection} from {bucket.Shard} >= {min.ByteSize()}";
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
			public IList<string> ActiveConstraints { get; set; }
		}

		public class Constraint
		{

		}
	}
}
