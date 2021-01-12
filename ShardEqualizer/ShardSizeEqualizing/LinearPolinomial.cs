using System;
using Accord.Math;
using Accord.Math.Optimization;

namespace ShardEqualizer.ShardSizeEqualizing
{
	public class LinearPolinomial
	{
		private LinearPolinomial()
		{
		}
	
		public LinearPolinomial(int numberOfVariables)
		{
			NumberOfVariables = numberOfVariables;
			LinearTerms = new double[numberOfVariables];
		}
		
		public double Function(double[] input)
		{
			return input.Dot(LinearTerms) + ConstantTerm;
		}

		public int NumberOfVariables { get; private set; }

		public double[] LinearTerms { get; private set; }

		public double ConstantTerm { get; set; }
		
		public static LinearPolinomial operator +(
			LinearPolinomial a,
			LinearPolinomial b)
		{
			if (a.LinearTerms.Length != b.LinearTerms.Length)
				throw new ArgumentException();

			return new LinearPolinomial()
			{
				NumberOfVariables = a.NumberOfVariables,
				LinearTerms = Elementwise.Add(a.LinearTerms, b.LinearTerms),
				ConstantTerm = a.ConstantTerm + b.ConstantTerm
			};
		}
		
		public QuadraticObjectiveFunction Square()
		{
			var q = new double[NumberOfVariables, NumberOfVariables];
			var d = new double[NumberOfVariables];

			for (var i = 0; i < NumberOfVariables; i++)
			{
				var ti = LinearTerms[i];
				q[i, i] = 2 * ti * ti;
				d[i] = 2 * ti * ConstantTerm;
				for (var j = i + 1; j < NumberOfVariables; j++)
				{
					var s = 2 * ti * LinearTerms[j];
					q[i, j] = s;
					q[j, i] = s;
				}
			}

			return new QuadraticObjectiveFunction(q, d)
			{
				ConstantTerm = ConstantTerm * ConstantTerm
			};
		}
	}
}