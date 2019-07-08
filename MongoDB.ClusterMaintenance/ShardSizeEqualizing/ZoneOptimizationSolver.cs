using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Accord.Math.Optimization;

namespace MongoDB.ClusterMaintenance.ShardSizeEqualizing
{
	public class ZoneOptimizationSolver
	{
		public bool Find() => Find(CancellationToken.None);

		public bool Find(CancellationToken token)
		{
			_solver.Token = token;
			var result = _solver.Minimize(_initialVector);
			if (!result)
				return false;
			
			foreach (var bucket in _buckets)
				bucket.TargetSize = (long) Math.Round(bucket.VariableFunction.Function(_solver.Solution));

			return true;
		}

		public double Error => Math.Sqrt(Math.Abs(_solver.Value));

		private readonly IList<Bucket> _buckets;
		private readonly GoldfarbIdnani _solver;
		private readonly double[] _initialVector;
		private readonly List<BucketConstraint> _constraintDescriptions;

		public ZoneOptimizationSolver(IEnumerable<Bucket> managedBuckets, GoldfarbIdnani solver, double[] initialVector,
			List<BucketConstraint> constraintDescriptions)
		{
			_buckets = managedBuckets.ToList();
			_solver = solver;
			_initialVector = initialVector;
			_constraintDescriptions = constraintDescriptions;
		}

		public List<BucketConstraint> ActiveConstraints => _constraintDescriptions.Where(_ => _.IsActive).ToList();
	}

	public class BucketConstraint
	{
		public Bucket Bucket { get; }
		public ConstraintType Type { get; }
		public long Bound { get; }

		public BucketConstraint(Bucket bucket, ConstraintType constraintType, long bound)
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

		public string TypeAsText => Type == ConstraintType.Min ? "≥" : "≤";

		public enum ConstraintType
		{
			Max,
			Min
		}
	}
}