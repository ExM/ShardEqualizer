using System;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public static class BucketExtensions
	{
		public static void Init(this Bucket bucket, Action<Bucket> action)
		{
			action(bucket);
		}
		
		public static void BlockSizeReduction(this Bucket bucket)
		{
			bucket.MinSize = bucket.CurrentSize;
		}
	}
}