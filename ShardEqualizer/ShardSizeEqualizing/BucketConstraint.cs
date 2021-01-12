namespace ShardEqualizer.ShardSizeEqualizing
{
	public class BucketConstraint
	{
		public BucketSolve Bucket { get; }
		public ConstraintType Type { get; }
		public long Bound { get; }

		public BucketConstraint(BucketSolve bucket, ConstraintType constraintType, long bound)
		{
			Bucket = bucket;
			Type = constraintType;
			Bound = bound;
		}

		public bool IsActive => Type == ConstraintType.Min 
			? Bucket.TargetSize <= Bound + 1 
			: Bucket.TargetSize >= Bound - 1;

		public override string ToString()
		{
			return $"collection {Bucket.Collection} from {Bucket.Shard} {TypeAsText} {Bound.ByteSize()}";
		}

		public string TypeAsText => Type == ConstraintType.Min ? ">=" : "<=";

		public enum ConstraintType
		{
			Max,
			Min
		}
	}
}