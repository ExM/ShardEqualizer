using System;
using System.Collections.Generic;
using System.Linq;
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

			_bucketList = Collections.SelectMany(coll => Shards.Select(sh => new Bucket(coll, sh))).ToList();

			_bucketsByShardByCollection =  _bucketList.GroupBy(_ => _.Shard)
				.ToDictionary(k => k.Key, v => (IReadOnlyDictionary<CollectionNamespace, Bucket>)v.ToDictionary(_ => _.Collection, _ => _));
			
			_bucketsByShard = _bucketList.GroupBy(_ => _.Shard)
				.ToDictionary(_ => _.Key, _ => (IReadOnlyList<Bucket>) _.ToList());
			
			ShardEqualsPriority = 100;
			DeviationLimitFromAverage = 0.5;
		}

		public Bucket this[CollectionNamespace coll, ShardIdentity shard] =>
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

		public IReadOnlyList<Bucket> AllManagedBuckets => _bucketList.Where(_ => _.Managed).ToList();

		public IReadOnlyList<Bucket> AllBuckets => _bucketList;

		public IReadOnlyDictionary<ShardIdentity, long> UnmovedSizeByShard =>
			_bucketsByShard.ToDictionary(
				p => p.Key,
				p => _unShardedSizes[p.Key] + p.Value.Where(b => !b.Managed).Sum(b => b.CurrentSize));

		public long TotalManagedSize => AllManagedBuckets.Select(_ => _.CurrentSize).Sum();
		
		public IReadOnlyList<CollectionNamespace> Collections { get; }
		public IReadOnlyList<ShardIdentity> Shards { get; }

		private readonly IDictionary<ShardIdentity, long> _unShardedSizes = new Dictionary<ShardIdentity, long>();
		
		private readonly IDictionary<CollectionNamespace, double> _collectionPriorities = new Dictionary<CollectionNamespace, double>();
		
		private readonly IReadOnlyDictionary<ShardIdentity, IReadOnlyList<Bucket>> _bucketsByShard;
		private readonly IReadOnlyDictionary<ShardIdentity, IReadOnlyDictionary<CollectionNamespace, Bucket>> _bucketsByShardByCollection;
		private readonly IReadOnlyList<Bucket> _bucketList;
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