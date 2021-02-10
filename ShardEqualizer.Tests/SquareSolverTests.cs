using System;
using System.Threading;
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
