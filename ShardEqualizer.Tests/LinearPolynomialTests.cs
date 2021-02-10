using System.Collections.Generic;
using Accord.Math;
using Accord.Math.Optimization;
using NUnit.Framework;
using ShardEqualizer.ShardSizeEqualizing;

namespace ShardEqualizer
{
	[TestFixture]
	public class LinearPolynomialTests
	{
		[Test]
		public void Demo()
		{
			var p0 = new Dictionary<string, double>()
			{
				["x1"] = 3,
				["x2"] = 5
			};

			var p1 = new LinearPolynomial2<string>()
			{
				Constant = 5,
				["x1"] = 3,
				["x2"] = 5
			};



		}
	}
}
