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
		private readonly List<string> _constraintDescriptions;

		public ZoneOptimizationSolver(IEnumerable<Bucket> managedBuckets, GoldfarbIdnani solver, double[] initialVector,
			List<string> constraintDescriptions)
		{
			_buckets = managedBuckets.ToList();
			_solver = solver;
			_initialVector = initialVector;
			_constraintDescriptions = constraintDescriptions;
		}

		public List<string> ActiveConstraint =>
			_solver.ActiveConstraints.Select(_ => _constraintDescriptions[_]).ToList();
	}
}