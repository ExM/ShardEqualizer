using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShardEqualizer.Models;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public partial class ZoneOptimizationDescriptor
	{
		public static ZoneOptimizationDescriptor Deserialize(string text)
		{
			var container = JsonConvert.DeserializeObject<Container>(text);

			var result = new ZoneOptimizationDescriptor(
				container.Collections.Select(_ => CollectionNamespace.FromFullName(_.Ns)),
				container.Shards.Select(_ => new ShardIdentity(_.Name)));

			result.ShardEqualsPriority = container.ShardEqualsPriority;
			result.DeviationLimitFromAverage = container.DeviationLimitFromAverage;
			result.MaxBucketSize = container.MaxBucketSize;

			foreach (var collDesc in container.Collections)
			{
				var collSettings = result.CollectionSettings[CollectionNamespace.FromFullName(collDesc.Ns)];
				collSettings.Priority = collDesc.Priority;
			}

			foreach (var shardDesc in container.Shards)
			{
				var shardId = new ShardIdentity(shardDesc.Name);
				result.UnShardedSize[shardId] = shardDesc.UnShardedSize;
			}

			foreach (var bucketDesc in container.Buckets)
			{
				var collNs = CollectionNamespace.FromFullName(bucketDesc.Collection);
				var shardId = new ShardIdentity(bucketDesc.Shard);
				var bucket = result[collNs, shardId];

				bucket.Managed = bucketDesc.Managed;
				bucket.CurrentSize = bucketDesc.Size;
				bucket.MinSize = bucketDesc.Min;
			}

			return result;
		}

		public string Serialize()
		{
			var container = new Container()
			{
				ShardEqualsPriority = ShardEqualsPriority,
				DeviationLimitFromAverage = DeviationLimitFromAverage,
				MaxBucketSize = MaxBucketSize,
				Collections = Collections.Select(_ => new CollectionDescriptor()
				{
					Ns = _.FullName,
					Priority = CollectionSettings[_].Priority,
				}).ToArray(),
				Shards = Shards.Select(_ => new ShardDescriptor()
				{
					Name = _.ToString(),
					UnShardedSize = UnShardedSize[_]
				}).ToArray(),
				Buckets = AllBuckets.Select(_ => new BucketDescriptor()
				{
					Collection = _.Collection.FullName,
					Shard = _.Shard.ToString(),
					Size = _.CurrentSize,
					Managed = _.Managed,
					Min = _.MinSize
				}).ToArray()
			};

			return JObject.FromObject(container).ToString();
		}

		private class Container
		{
			public double ShardEqualsPriority;
			public double DeviationLimitFromAverage;
			public CollectionDescriptor[] Collections;
			public ShardDescriptor[] Shards;
			public BucketDescriptor[] Buckets;
			public double MaxBucketSize { get; set; }
		}

		private class BucketDescriptor
		{
			public string Collection;
			public string Shard;
			public long Size;
			public bool Managed;
			public long Min;
		}

		private class CollectionDescriptor
		{
			public string Ns;
			public double Priority;
		}

		private class ShardDescriptor
		{
			public string Name;
			public long UnShardedSize;
		}
	}
}
