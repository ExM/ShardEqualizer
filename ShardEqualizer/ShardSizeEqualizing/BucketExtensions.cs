using System;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public static class BucketExtensions
	{
		public static void Init(this Bucket bucket, Action<Bucket> action)
		{
			action(bucket);
		}

		public static Bucket OnSize(this Bucket bucket, long size)
		{
			bucket.CurrentSize = size;
			return bucket;
		}

		public static Bucket OnManaged(this Bucket bucket)
		{
			bucket.Managed = true;
			return bucket;
		}

		public static Bucket OnLocked(this Bucket bucket)
		{
			bucket.Managed = false;
			return bucket;
		}

		public static Bucket BlockSizeReduction(this Bucket bucket)
		{
			bucket.MinSize = bucket.CurrentSize;
			return bucket;
		}
	}
}
