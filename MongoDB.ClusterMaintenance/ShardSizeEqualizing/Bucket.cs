using System;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public class Bucket: IBucket
	{
		private long _minSize;

		public Bucket(CollectionNamespace collection, ShardIdentity shard)
		{
			Shard = shard;
			Collection = collection;
			CurrentSize = 0;
			Managed = false;
			MinSize = 0;
		}

		public ShardIdentity Shard { get; }
		public CollectionNamespace Collection { get; }
		public long CurrentSize { get; set; }
		public bool Managed { get; set; }

		public void BlockSizeReduction()
		{
			MinSize = CurrentSize;
		}

		public long MinSize
		{
			get => _minSize;
			set => _minSize = value < 0 ? 0 : value;
		}

		public int? VariableIndex { get; set; }
		public LinearPolinomial VariableFunction { get; set; }

		public long TargetSize { get; set; }

		public long Delta => TargetSize - CurrentSize;
	}

	public interface IBucket
	{
		long CurrentSize { get; set; }
		bool Managed { get; set; }
		void BlockSizeReduction();
		long MinSize { get; set; }
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