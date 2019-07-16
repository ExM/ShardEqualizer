using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Accord.Math.Optimization;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.ShardSizeEqualizing;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ZoneOptimizationDescriptor: IUnShardedSizeDescriptor, ICollectionPriorityDescriptor
	{
		public ZoneOptimizationDescriptor(IEnumerable<CollectionNamespace> collections, IEnumerable<ShardIdentity> shards)
		{
			Collections = collections.ToList();
			foreach (var coll in Collections)
				_collectionPriorities.Add(coll, 1);
			
			Shards = shards.ToList();
			foreach (var shard in Shards)
				_unShardedSizes.Add(shard, 0);
			
			_bucketList = new List<Bucket>(Collections.Count * Shards.Count);
			foreach (var coll in Collections)
				foreach (var sh in Shards)
					_bucketList.Add(new Bucket(coll, sh));

			_bucketsByShardByCollection =  _bucketList.GroupBy(_ => _.Shard)
				.ToDictionary(k => k.Key, v => (IReadOnlyDictionary<CollectionNamespace, Bucket>)v.ToDictionary(_ => _.Collection, _ => _));
			
			_bucketsByShard = _bucketList.GroupBy(_ => _.Shard)
				.ToDictionary(_ => _.Key, _ => (IReadOnlyList<Bucket>) _.ToList());

			_bucketsByCollection = _bucketList.GroupBy(_ => _.Collection)
				.ToDictionary(_ => _.Key, _ => (IReadOnlyList<Bucket>) _.ToList());
			
			ShardEqualsPriority = 100;
			DeviationLimitFromAverage = 0.5;
		}

		public IBucket this[CollectionNamespace coll, ShardIdentity shard] =>
			_bucketsByShardByCollection[shard][coll];

		long IUnShardedSizeDescriptor.this[ShardIdentity shard]
		{
			get => _unShardedSizes[shard];
			set =>  _unShardedSizes[shard] = value;
		}
		
		double ICollectionPriorityDescriptor.this[CollectionNamespace coll]
		{
			get => _collectionPriorities[coll];
			set =>  _collectionPriorities[coll] = value;
		}
		
		public double ShardEqualsPriority { get; set; }
		
		/// <summary>
		/// limit of deviation from the average value of the bucket in percent
		/// </summary>
		public double DeviationLimitFromAverage { get; set; }

		public IUnShardedSizeDescriptor UnShardedSize => this;
		
		public ICollectionPriorityDescriptor CollectionPriority => this;

		public IList<IBucket> AllManagedBuckets => _bucketList.Where(_ => _.Managed).Cast<IBucket>().ToList();

		private void cleanUp()
		{
			foreach (var bucket in _bucketList)
			{
				bucket.VariableIndex = null;
				bucket.VariableFunction = null;
				bucket.TargetSize = bucket.CurrentSize;
			}

			_initialVector = null;
			_totalVariables = 0;
		}

		private void calculateTotalVariables()
		{
			foreach (var coll in Collections)
			{
				var collCount = _bucketsByCollection[coll].Count(_ => _.Managed);
				if (collCount >= 2)
					_totalVariables += collCount - 1;
			}
			
			_initialVector = new double[_totalVariables];
		}
		
		private void setVariableFunctions()
		{
			var currentVar = 0;
			foreach (var coll in Collections)
			{
				var buckets = _bucketsByCollection[coll].Where(_ => _.Managed).ToArray();
				if (!buckets.Any())
					continue;
				var firstBucket = buckets.First();

				firstBucket.VariableFunction = new LinearPolinomial(_totalVariables)
				{
					ConstantTerm = firstBucket.CurrentSize
				};
				
				foreach (var bucket in buckets.Skip(1))
				{
					firstBucket.VariableFunction.LinearTerms[currentVar] = -1;
					firstBucket.VariableFunction.ConstantTerm += bucket.CurrentSize;

					bucket.VariableIndex = currentVar;
					bucket.VariableFunction = new LinearPolinomial(_totalVariables);
					bucket.VariableFunction.LinearTerms[currentVar] = 1;

					currentVar++;
				}
			}
		}

		public ZoneOptimizationSolver BuildSolver()
		{
			cleanUp();
			calculateTotalVariables();
			setVariableFunctions();

			_unmovedSizeByShard = new Dictionary<ShardIdentity, long>(_unShardedSizes);

			foreach (var bucket in _bucketList.Where(_ => !_.Managed))
				_unmovedSizeByShard[bucket.Shard] += bucket.CurrentSize;

			var totalManagedSize = _bucketList.Where(_ => _.Managed).Select(_ => _.CurrentSize).Sum();

			var avgManagedShardSize = (double) totalManagedSize / Shards.Count;

			var targetShardSize = _unmovedSizeByShard.ToDictionary(_ => _.Key, _ => avgManagedShardSize - _.Value);

			var targetFunction = new QuadraticObjectiveFunction(
				new double[_totalVariables, _totalVariables],
				new double[_totalVariables]);

			foreach (var shard in Shards)
			{
				var buckets = _bucketsByShard[shard].Where(_ => _.Managed).ToArray();

				var sum = new LinearPolinomial(_totalVariables)
				{
					ConstantTerm = -targetShardSize[shard]
				};

				foreach (var bucket in buckets)
				{
					sum += bucket.VariableFunction;
				}

				targetFunction += sum.Square() * (ShardEqualsPriority / (avgManagedShardSize * avgManagedShardSize));
			}

			//Console.WriteLine(targetFunction.QuadraticTerms.ToCSharp());
			//Console.WriteLine(targetFunction.QuadraticTerms.Determinant());

			var avgCollectionSize = new Dictionary<CollectionNamespace, double>();

			foreach (var coll in Collections)
			{
				var buckets = _bucketsByCollection[coll].Where(_ => _.Managed).ToArray();
				var sum = buckets.Sum(_ => _.CurrentSize);

				avgCollectionSize[coll] = (double) sum / buckets.Length;
			}

			foreach (var bucket in _bucketList.Where(_ => _.Managed))
			{
				var avg = avgCollectionSize[bucket.Collection];
				
				if(avg <= 0)
					continue;

				var sum = bucket.VariableFunction + new LinearPolinomial(_totalVariables) {ConstantTerm = -avg};

				var error = sum.Square();

				targetFunction += error * (_collectionPriorities[bucket.Collection] / (avg * avg));
			}

			//Console.WriteLine(targetFunction.QuadraticTerms.ToCSharp());
			//Console.WriteLine(targetFunction.QuadraticTerms.Determinant());

			var constraints = new List<LinearConstraint>();
			var constraintDescriptions = new List<BucketConstraint>();

			var variablesAtIndices = Enumerable.Range(0, _totalVariables).ToArray();

			foreach (var bucket in _bucketList.Where(_ => _.Managed && _.VariableIndex.HasValue))
			{
				_initialVector[bucket.VariableIndex.Value] = bucket.CurrentSize;
			}

			var lowRate = 1 - DeviationLimitFromAverage;
			var highRate = 1 + DeviationLimitFromAverage;

			foreach (var bucket in _bucketList.Where(_ => _.Managed))
			{
				var avg = avgCollectionSize[bucket.Collection];

				var min = Math.Min(bucket.CurrentSize,
					Math.Max(avg * lowRate, bucket.MinSize));

				var max = Math.Max(bucket.CurrentSize, avg * highRate);

				if (max - min <= 1)
					max *= highRate;

				constraints.Add(new LinearConstraint(_totalVariables)
				{
					VariablesAtIndices = variablesAtIndices,
					CombinedAs = bucket.VariableFunction.LinearTerms,
					ShouldBe = ConstraintType.GreaterThanOrEqualTo,
					Value = min - bucket.VariableFunction.ConstantTerm,
				});
				constraintDescriptions.Add(new BucketConstraint(bucket, BucketConstraint.ConstraintType.Min, (long) Math.Round(min)));

				constraints.Add(new LinearConstraint(_totalVariables)
				{
					VariablesAtIndices = variablesAtIndices,
					CombinedAs = bucket.VariableFunction.LinearTerms,
					ShouldBe = ConstraintType.LesserThanOrEqualTo,
					Value = max - bucket.VariableFunction.ConstantTerm
				});
				constraintDescriptions.Add(new BucketConstraint(bucket, BucketConstraint.ConstraintType.Max, (long) Math.Round(max)));
			}

			var solver = new GoldfarbIdnani(targetFunction, constraints);

			return new ZoneOptimizationSolver(_bucketList.Where(_ => _.Managed), solver, _initialVector, constraintDescriptions);
		}
		
		private IReadOnlyDictionary<ShardIdentity, long> _targetShards;

		public IReadOnlyDictionary<ShardIdentity, long> TargetShards
		{
			get
			{
				if(_targetShards == null)
					_targetShards = _bucketList.GroupBy(_ => _.Shard).ToDictionary(
					_ => _.Key,
					_ => _.Select(b => b.TargetSize).Sum() + _unShardedSizes[_.Key]);
				return _targetShards;
			}
		}

		public long TargetShardMaxDeviation => TargetShards.Values.Max() - TargetShards.Values.Min();

		public IReadOnlyList<CollectionNamespace> Collections { get; }
		public IReadOnlyList<ShardIdentity> Shards { get; }

		private readonly IDictionary<ShardIdentity, long> _unShardedSizes = new Dictionary<ShardIdentity, long>();
		
		private readonly IDictionary<CollectionNamespace, double> _collectionPriorities = new Dictionary<CollectionNamespace, double>();
		
		private readonly IReadOnlyDictionary<ShardIdentity, IReadOnlyList<Bucket>> _bucketsByShard;
		private readonly IReadOnlyDictionary<ShardIdentity, IReadOnlyDictionary<CollectionNamespace, Bucket>> _bucketsByShardByCollection;
		private readonly IReadOnlyDictionary<CollectionNamespace, IReadOnlyList<Bucket>> _bucketsByCollection;
		private readonly IList<Bucket> _bucketList;
		private double[] _initialVector;
		private int _totalVariables;
		private Dictionary<ShardIdentity, long> _unmovedSizeByShard;
	}

	public interface ICollectionPriorityDescriptor
	{
		double this[CollectionNamespace coll] { get; set; }
	}
	
	public interface IUnShardedSizeDescriptor
	{
		long this[ShardIdentity shard] { get; set; }
	}
}