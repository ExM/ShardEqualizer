using System;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public class Bucket: IBucket
	{
		public Bucket(CollectionNamespace collection, ShardIdentity shard)
		{
			Shard = shard;
			Collection = collection;
			CurrentSize = 0;
			Managed = false;
			EnableSizeReduction = true;
		}

		public ShardIdentity Shard { get; }
		public CollectionNamespace Collection { get; }
		public long CurrentSize { get; set; }
		public bool Managed { get; set; }
		public bool EnableSizeReduction { get; set; }

		public int? VariableIndex { get; set; }
		public LinearPolinomial VariableFunction { get; set; }

		public long TargetSize { get; set; }

		public long Delta => TargetSize - CurrentSize;
	}

	public interface IBucket
	{
		long CurrentSize { get; set; }
		bool Managed { get; set; }
		bool EnableSizeReduction { get; set; }
		long TargetSize { get; }
	}

	public static class BucketExtensions
	{
		public static void Init(this IBucket bucket, Action<IBucket> action)
		{
			action(bucket);
		}
	}
	
}