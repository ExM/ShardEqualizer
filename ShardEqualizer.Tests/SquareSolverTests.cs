using System;
using System.Collections.Generic;
using System.Threading;
using Accord.Math;
using Accord.Math.Optimization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NLog;
using NUnit.Framework;
using ShardEqualizer.ShardSizeEqualizing;

namespace ShardEqualizer
{
	[TestFixture]
	public class SquareSolverTests
	{
		[TearDown]
		public void LogFlush()
		{
			LogManager.Flush();
		}

		[Test]
		public void EqualizeShard()
		{
			var solver = new SquareSolver<string>();

			solver.InitVariable("a", 2);
			solver.InitVariable("b", 1);
			solver.InitVariable("c", 0);
			solver.InitVariable("d", 20);
			solver.InitVariable("e", 10);
			solver.InitVariable("f", 0);

			solver.SetMin("a", 0);
			solver.SetMin("b", 0);
			solver.SetMin("c", 0);
			solver.SetMin("d", 0);
			solver.SetMin("e", 0);
			solver.SetMin("f", 0);

			solver.SetMax("a", 100);
			solver.SetMax("b", 100);
			solver.SetMax("c", 100);
			solver.SetMax("d", 100);
			solver.SetMax("e", 100);
			solver.SetMax("f", 100);


			solver.SetEqualConstraint(new Vector<string>(){ ["a"] = 1, ["b"] = 1, ["c"] = 1}, 3);
			solver.SetEqualConstraint(new Vector<string>(){ ["d"] = 1, ["e"] = 1, ["f"] = 1}, 30);

			solver.SetObjective(new Vector<string>(){ ["a"] = 1, ["d"] = 1}, 2);
			solver.SetObjective(new Vector<string>(){ ["b"] = 1, ["e"] = 1}, 2);
			solver.SetObjective(new Vector<string>(){ ["c"] = 1, ["f"] = 1}, 2);

			var state = solver.Find(CancellationToken.None);

			Assert.IsTrue(state);
			var a = solver.GetSolution("a");
			var b = solver.GetSolution("b");
			var c = solver.GetSolution("c");

			var d = solver.GetSolution("d");
			var e = solver.GetSolution("e");
			var f = solver.GetSolution("f");

			Assert.AreEqual(3, a + b + c, 0.00001);
			Assert.AreEqual(30, d + e + f, 0.00001);

			var ad = a + d;
			var be = b + e;
			var cf = c + f;

			Assert.AreEqual(be, ad, 0.00001);
			Assert.AreEqual(be, cf, 0.00001);
		}


		//2*x^2 +2* y^2 +2*x*y -20040*x -20040*y +66933600
		[Test]
		public void FailAugmentedLagrangian_Demo2()
		{
			var avg = 10000 / 3;

			var targetFunction = new QuadraticObjectiveFunction(
				new double[2, 2] {{4, 2}, {2, 4}},
				new double[2] {-20040, -20040}) {ConstantTerm = 66933600};

			Console.WriteLine("QuadraticTerms: {0}",  targetFunction.QuadraticTerms.ToCSharp());
			Console.WriteLine("LinearTerms: {0}",  targetFunction.LinearTerms.ToCSharp());
			Console.WriteLine("ConstantTerm: {0}",  targetFunction.ConstantTerm);

			var solver = new AugmentedLagrangian(targetFunction, new List<IConstraint>()) { Solution = new double[] {10000, 10} };

			Console.WriteLine($"Objective start value: {solver.Function(solver.Solution)}");
			Console.WriteLine($"Objective gradient: {solver.Gradient(solver.Solution).ToCSharp()}");

			var solveResult = solver.Minimize();

			Console.WriteLine($"Objective end value: {solver.Function(solver.Solution)}");

			Console.WriteLine($"Solution: {solver.Solution.ToCSharp()}");

			Assert.IsTrue(solveResult);
			Assert.Less(solver.Solution[0], 5000);
		}

		[Test]
		public void FailAugmentedLagrangian_Demo()
		{
			var solver = new SquareSolver<string>();

			solver.InitVariable("a", 10);
			solver.InitVariable("b", 10000);
			solver.InitVariable("c", 10);

			solver.SetEqualConstraint(Vector<string>.Unit(new []{"a", "b", "c"}), 10020);

			var avg = (double) 10020 / 3;

			solver.SetObjective(Vector<string>.Unit(new []{"a"}), avg);
			solver.SetObjective(Vector<string>.Unit(new []{"b"}), avg);
			solver.SetObjective(Vector<string>.Unit(new []{"c"}), avg);

			var state = solver.Find(CancellationToken.None);

			Assert.IsTrue(state);

			Assert.That(solver.GetSolution("a"), Is.GreaterThan(10));
		}

		[Test]
		public void EqualizeShardWithActiveConstraints()
		{
			var solver = new SquareSolver<string>();

			solver.InitVariable("a", 2);
			solver.InitVariable("b", 1);
			solver.InitVariable("c", 0);
			solver.InitVariable("d", 20);
			solver.InitVariable("e", 10);
			solver.InitVariable("f", 0);

			solver.SetMin("a", 0);
			solver.SetMin("b", 0);
			solver.SetMin("c", 0);
			solver.SetMin("d", 0);
			solver.SetMin("e", 0);
			solver.SetMin("f", 0);

			solver.SetMax("a", 100);
			solver.SetMax("b", 100);
			solver.SetMax("c", 100);
			solver.SetMax("d", 100);
			solver.SetMax("e", 100);
			solver.SetMax("f", 5);


			solver.SetEqualConstraint(new Vector<string>(){ ["a"] = 1, ["b"] = 1, ["c"] = 1}, 3);
			solver.SetEqualConstraint(new Vector<string>(){ ["d"] = 1, ["e"] = 1, ["f"] = 1}, 30);

			solver.SetObjective(new Vector<string>(){ ["a"] = 1, ["d"] = 1}, 2);
			solver.SetObjective(new Vector<string>(){ ["b"] = 1, ["e"] = 1}, 2);
			solver.SetObjective(new Vector<string>(){ ["c"] = 1, ["f"] = 1}, 2);

			var state = solver.Find(CancellationToken.None);

			Assert.IsTrue(state);
			var a = solver.GetSolution("a");
			var b = solver.GetSolution("b");
			var c = solver.GetSolution("c");

			var d = solver.GetSolution("d");
			var e = solver.GetSolution("e");
			var f = solver.GetSolution("f");

			Assert.AreEqual(3, a + b + c, 0.00001);
			Assert.AreEqual(30, d + e + f, 0.00001);

			var ad = a + d;
			var be = b + e;
			var cf = c + f;

			Assert.AreEqual(be, ad, 0.00001);
			Assert.Less(cf, be);
			CollectionAssert.AreEquivalent(new [] {"0 <= [a]", "0 <= [b]", "[f] <= 5"}, solver.ActiveConstraints);
		}

		[TestCase(10, 5)]
		[TestCase(5, 2.5)]
		[TestCase(1, 0.5)]
		[TestCase(0, 0)]
		[TestCase(-1, -0.5)]
		[TestCase(-5, -2.5)]
		[TestCase(-10, -5)]
		public void Trivial(double start, double end)
		{
			var solver = new SquareSolver<string>();

			solver.InitVariable("a", start);
			solver.InitVariable("b", 1);

			solver.SetMin("a", -100);
			solver.SetMin("b", -100);

			solver.SetMax("a", 100);
			solver.SetMax("b", 100);

			solver.SetObjective(new Vector<string>(){ ["a"] = 1, ["b"] = 1}, 1);

			var state = solver.Find(CancellationToken.None);

			Assert.IsTrue(state);

			var a = solver.GetSolution("a");
			var b = solver.GetSolution("b");

			Assert.AreEqual(1, a + b, 0.00001);
			Assert.AreEqual(end, a, 0.00001);
		}
	}
}
