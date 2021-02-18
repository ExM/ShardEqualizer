using System;
using System.Linq;
using System.Threading;
using MongoDB.Driver;
using NUnit.Framework;
using ShardEqualizer.Models;
using ShardEqualizer.ShardSizeEqualizing;

namespace ShardEqualizer
{
	[TestFixture]
	public class OptimalDataPartitionTests
	{
		[Test]
		public void SizeSolverDemo()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC, _cD, _cE},
				new []{_sA, _sB, _sC, _sD});

			zoneOpt.UnShardedSize[_sA] = 100;
			zoneOpt.UnShardedSize[_sB] = 20;
			zoneOpt.UnShardedSize[_sD] = 30;

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =  600; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = 2000; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize = 2000; b.Managed = true;});
			zoneOpt[_cA, _sD].Init(b => { b.CurrentSize = 1230; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize =  100; b.Managed = true;});
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 4520; b.Managed = true;});
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize =   30; b.Managed = true;});
			zoneOpt[_cB, _sD].Init(b => { b.CurrentSize = 2330; b.Managed = true;});

			zoneOpt[_cC, _sA].Init(b => { b.CurrentSize =  100; b.Managed = true;});
			zoneOpt[_cC, _sB].Init(b => { b.CurrentSize =  100; b.Managed = true;});
			zoneOpt[_cC, _sC].Init(b => { b.CurrentSize =    0; b.Managed = true;});
			zoneOpt[_cC, _sD].Init(b => { b.CurrentSize =   30; b.Managed = true;});

			zoneOpt[_cD, _sA].Init(b => { b.CurrentSize =  100; b.Managed = true;});
			zoneOpt[_cD, _sB].Init(b => { b.CurrentSize =  200; b.Managed = true;});
			zoneOpt[_cD, _sC].Init(b => { b.CurrentSize =  500; b.Managed = true;});
			zoneOpt[_cD, _sD].Init(b => { b.CurrentSize =  300; b.Managed = true;});

			zoneOpt[_cE, _sA].Init(b => { b.CurrentSize =   10; b.Managed = true;});
			zoneOpt[_cE, _sB].Init(b => { b.CurrentSize =   20; b.Managed = true;});
			zoneOpt[_cE, _sC].Init(b => { b.CurrentSize =   20; b.Managed = true;});
			zoneOpt[_cE, _sD].Init(b => { b.CurrentSize =   30; b.Managed = false;});

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);
		}

		[Test]
		public void BlockSizeReduction()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =    0; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = 2000; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize = 2000; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize = 3000; b.Managed = true; });
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 4100; b.Managed = true; b.BlockSizeReduction(); });
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize = 2000; b.Managed = true; });

			zoneOpt[_cC, _sA].Init(b => { b.CurrentSize =  500; b.Managed = true; });
			zoneOpt[_cC, _sB].Init(b => { b.CurrentSize =  500; b.Managed = true; });
			zoneOpt[_cC, _sC].Init(b => { b.CurrentSize =  500; b.Managed = true; });

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sA].TargetSize, Is.EqualTo(0).Within(1));
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(429).Within(1));
				Assert.That(zoneOpt[_cA, _sC].TargetSize, Is.EqualTo(3571).Within(1));

				Assert.That(zoneOpt[_cB, _sA].TargetSize, Is.EqualTo(4238).Within(1));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(4100).Within(1));
				Assert.That(zoneOpt[_cB, _sC].TargetSize, Is.EqualTo(761).Within(1));

				Assert.That(zoneOpt[_cC, _sA].TargetSize, Is.EqualTo(628).Within(1));
				Assert.That(zoneOpt[_cC, _sB].TargetSize, Is.EqualTo(338).Within(1));
				Assert.That(zoneOpt[_cC, _sC].TargetSize, Is.EqualTo(534).Within(1));

				Assert.That(solve.ActiveConstraints.Count, Is.EqualTo(1));
			});
		}

		[Test]
		public void WithoutUnShardCompensation()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =    0; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = 2000; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize = 2000; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize = 3000; b.Managed = true; });
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 4000; b.Managed = true; });
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize = 2000; b.Managed = true; });

			zoneOpt[_cC, _sA].Init(b => { b.CurrentSize =  600; b.Managed = true; });
			zoneOpt[_cC, _sB].Init(b => { b.CurrentSize =  500; b.Managed = true; });
			zoneOpt[_cC, _sC].Init(b => { b.CurrentSize =  400; b.Managed = true; });

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);
			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(2000).Within(1));
				Assert.That(zoneOpt[_cA, _sC].TargetSize, Is.EqualTo(2000).Within(1));

				Assert.That(zoneOpt[_cB, _sA].TargetSize, Is.EqualTo(4298).Within(1));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(2351).Within(1));
				Assert.That(zoneOpt[_cB, _sC].TargetSize, Is.EqualTo(2351).Within(1));

				Assert.That(zoneOpt[_cC, _sA].TargetSize, Is.EqualTo(535).Within(1));
				Assert.That(zoneOpt[_cC, _sB].TargetSize, Is.EqualTo(482).Within(1));
				Assert.That(zoneOpt[_cC, _sC].TargetSize, Is.EqualTo(482).Within(1));

				Assert.That(solve.ActiveConstraints, Is.Empty);
			});
		}

		[Test]
		public void NoManagedCollection()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =    0; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = 2000; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize = 2000; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize = 3000; b.Managed = true; });
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 3000; b.Managed = true; });
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize = 3000; b.Managed = true; });

			zoneOpt[_cC, _sA].Init(b => { b.CurrentSize =  500; b.Managed = false; });
			zoneOpt[_cC, _sB].Init(b => { b.CurrentSize =  500; b.Managed = false; });
			zoneOpt[_cC, _sC].Init(b => { b.CurrentSize =  500; b.Managed = false; });

			zoneOpt.ShardEqualsPriority = 1;

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(2333));
			Assert.That(zoneOpt[_cC, _sA].TargetSize, Is.EqualTo(500));
			Assert.That(zoneOpt[_cC, _sB].TargetSize, Is.EqualTo(500));
			Assert.That(zoneOpt[_cC, _sC].TargetSize, Is.EqualTo(500));
		}

		[Test]
		public void ExcessConstraints()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =    0; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = 1000; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize = 3000; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize = 3000; b.Managed = true; b.BlockSizeReduction(); });
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 3000; b.Managed = true; b.BlockSizeReduction(); });
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize = 1000; b.Managed = true; b.BlockSizeReduction(); });

			zoneOpt[_cC, _sA].Init(b => { b.CurrentSize = 1000; b.Managed = true; });
			zoneOpt[_cC, _sB].Init(b => { b.CurrentSize =  100; b.Managed = true; });
			zoneOpt[_cC, _sC].Init(b => { b.CurrentSize =  100; b.Managed = true; });

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(1039));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(3000));
				Assert.That(zoneOpt[_cC, _sA].TargetSize, Is.EqualTo(1067));
				Assert.That(zoneOpt[_cC, _sB].TargetSize, Is.EqualTo(28));

				Assert.That(solve.ActiveConstraints, Is.EquivalentTo(new []
				{
					"collection d.collB from shA >= 2.93 Kb",
					"collection d.collB from shB >= 2.93 Kb",
					"collection d.collB from shC >= 0.98 Kb"
				}));
			});
		}

		[Test]
		public void SolveStability(
			[Values(100, 1000, 2000, 3500)] int bucketAB,
			[Values(10, 1000, 2000, 3000, 5500)] int bucketBA,
			[Values(10, 1000, 2000, 3000, 3200)] int bucketBB,
			[Values(10, 50, 125, 432, 499)] int bucketCA)
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =    0; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = bucketAB; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize = 4000 - bucketAB; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize = bucketBA; b.Managed = true; });
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = bucketBB; b.Managed = true; });
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize = 9000 - bucketBA - bucketBB; b.Managed = true; });

			zoneOpt[_cC, _sA].Init(b => { b.CurrentSize =  bucketCA; b.Managed = true; });
			zoneOpt[_cC, _sB].Init(b => { b.CurrentSize =  500; b.Managed = true; });
			zoneOpt[_cC, _sC].Init(b => { b.CurrentSize =  1000 - bucketCA; b.Managed = true; });

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			var targetShards = zoneOpt.TargetShards;

			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(2000).Within(1));

				Assert.That(zoneOpt[_cB, _sA].TargetSize, Is.EqualTo(4297).Within(1));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(2351).Within(1));

				Assert.That(zoneOpt[_cC, _sA].TargetSize, Is.EqualTo(536).Within(1));

				Assert.That(targetShards[_sA], Is.EqualTo(4833).Within(1));
				Assert.That(targetShards[_sB], Is.EqualTo(4833).Within(1));
				Assert.That(targetShards[_sC], Is.EqualTo(4833).Within(1));
			});
		}

		[Test]
		public void SolveStabilityWithBlockedSizeReduction(
			[Values(100, 1000, 2000, 3500)] int bucketAB,
			[Values(10, 1000, 2000, 3000, 5500)] int bucketBA,
			[Values(10, 50, 125, 432, 499)] int bucketCA,
			[Values(10, 50, 125, 432, 499)] int bucketCB,
			[Values(0.1, 0.25, 0.5, 0.75, 0.99)] double pathPercent)
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =    0; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = bucketAB; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize = 4000 - bucketAB; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize = bucketBA; b.Managed = true; });
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 4500; b.Managed = true; b.BlockSizeReduction(); });
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize = 6000 - bucketBA; b.Managed = true; });

			zoneOpt[_cC, _sA].Init(b => { b.CurrentSize =  bucketCA; b.Managed = true; });
			zoneOpt[_cC, _sB].Init(b => { b.CurrentSize =  bucketCB; b.Managed = true; });
			zoneOpt[_cC, _sC].Init(b => { b.CurrentSize =  1500 - bucketCA - bucketCB; b.Managed = true; });

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);
			var expectedTargets = zoneOpt.AllManagedBuckets.Select(_ => _.TargetSize).ToList();

			foreach (var b in zoneOpt.AllManagedBuckets)
			{
				b.CurrentSize = b.CurrentSize + (long)Math.Round(pathPercent * (zoneOpt[b.Collection, b.Shard].TargetSize - b.CurrentSize));
			}

			var solve2 = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve2.IsSuccess);

			var actualTargets = zoneOpt.AllManagedBuckets.Select(_ => _.TargetSize);

			foreach (var pair in actualTargets.Zip(expectedTargets, (actual, expected) => new {actual, expected}))
			{
				Assert.That(pair.actual, Is.EqualTo(pair.expected).Within(2));
			}
		}

		[TestCase(0,     new [] {4666, 2666, 2666})]
		[TestCase(1,     new [] {4637, 2681, 2681})]
		[TestCase(10,    new [] {3744, 3128, 3128})]
		[TestCase(100,   new [] {3741, 3129, 3129})]
		public void CollectionEqualsPriority_WithLockedBucket(double collectionEqualsPriority, int[] expectedCollBuckets)
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB, _cC},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].OnSize(   0).OnLocked();
			zoneOpt[_cA, _sB].OnSize(2000).OnManaged();
			zoneOpt[_cA, _sC].OnSize(2000).OnManaged();

			zoneOpt[_cB, _sA].OnSize(5000).OnManaged();
			zoneOpt[_cB, _sB].OnSize(4000).OnManaged();
			zoneOpt[_cB, _sC].OnSize(1000).OnManaged();

			zoneOpt[_cC, _sA].OnSize( 500).OnManaged();
			zoneOpt[_cC, _sB].OnSize( 500).OnManaged();
			zoneOpt[_cC, _sC].OnSize( 500).OnManaged();

			zoneOpt.CollectionSettings[ _cB].Priority = collectionEqualsPriority;

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cB, _sA].TargetSize, Is.EqualTo(expectedCollBuckets[0]).Within(1));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(expectedCollBuckets[1]).Within(1));
				Assert.That(zoneOpt[_cB, _sC].TargetSize, Is.EqualTo(expectedCollBuckets[2]).Within(1));
			});
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(10)]
		[TestCase(100)]
		public void CollectionEqualsPriority_WithoutLocks(double collectionEqualsPriority)
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].OnSize(30).OnManaged();
			zoneOpt[_cA, _sB].OnSize(30).OnManaged();
			zoneOpt[_cA, _sC].OnSize(30).OnManaged();

			zoneOpt[_cB, _sA].OnSize(5000).OnManaged();
			zoneOpt[_cB, _sB].OnSize(3000).OnManaged();
			zoneOpt[_cB, _sC].OnSize(1000).OnManaged();

			zoneOpt.CollectionSettings[ _cB].Priority = collectionEqualsPriority;

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			Assert.That(new []
			{
				zoneOpt[_cA, _sA].TargetSize,
				zoneOpt[_cA, _sB].TargetSize,
				zoneOpt[_cA, _sC].TargetSize
			}, Is.EquivalentTo(new [] {30, 30, 30}));

			Assert.That(new []
			{
				zoneOpt[_cB, _sA].TargetSize,
				zoneOpt[_cB, _sB].TargetSize,
				zoneOpt[_cB, _sC].TargetSize
			}, Is.EquivalentTo(new [] {3000, 3000, 3000}));
		}

		[Test]
		public void Trivial()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB},
				new []{_sA, _sB});

			zoneOpt.UnShardedSize[_sA] = 0;
			zoneOpt.UnShardedSize[_sB] = 0;

			zoneOpt[_cA, _sA].OnSize(1000).OnManaged();
			zoneOpt[_cA, _sB].OnSize(3000).OnManaged();
			zoneOpt[_cB, _sA].OnSize(3000).OnManaged();
			zoneOpt[_cB, _sB].OnSize(1000).OnManaged();

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);
			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sA].TargetSize, Is.EqualTo(2000));
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(2000));
				Assert.That(zoneOpt[_cB, _sA].TargetSize, Is.EqualTo(2000));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(2000));
			});
		}

		[Test]
		public void Trivial_new()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB},
				new []{_sA, _sB});

			zoneOpt.UnShardedSize[_sA] = 0;
			zoneOpt.UnShardedSize[_sB] = 0;

			zoneOpt[_cA, _sA].Init(b =>
			{
				b.CurrentSize = 2500;
				b.Managed = true;
			});
			zoneOpt[_cA, _sB].Init(b =>
			{
				b.CurrentSize = 1500;
				b.Managed = true;
			});
			zoneOpt[_cB, _sA].Init(b =>
			{
				b.CurrentSize = 1000;
				b.Managed = true;
			});
			zoneOpt[_cB, _sB].Init(b =>
			{
				b.CurrentSize = 3000;
				b.Managed = true;
			});

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);
			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sA].TargetSize, Is.EqualTo(2000));
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(2000));
				Assert.That(zoneOpt[_cB, _sA].TargetSize, Is.EqualTo(2000));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(2000));
			});
		}

		[Test]
		public void Trivial_unManaged()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB},
				new []{_sA, _sB});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =  500; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize = 1000; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize = 1000; b.Managed = true;});
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 1000; b.Managed = true;});

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);

			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sA].TargetSize, Is.EqualTo(500));
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(1000));
				Assert.That(zoneOpt[_cB, _sA].TargetSize, Is.EqualTo(1250));
				Assert.That(zoneOpt[_cB, _sB].TargetSize, Is.EqualTo(750));
			});
		}

		[Test]
		public void unManaged()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA, _cB},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].Init(b => { b.CurrentSize =  10; b.Managed = false;});
			zoneOpt[_cA, _sB].Init(b => { b.CurrentSize =  10; b.Managed = true;});
			zoneOpt[_cA, _sC].Init(b => { b.CurrentSize =  10; b.Managed = true;});

			zoneOpt[_cB, _sA].Init(b => { b.CurrentSize =  10; b.Managed = true;});
			zoneOpt[_cB, _sB].Init(b => { b.CurrentSize = 1000; b.Managed = true;});
			zoneOpt[_cB, _sC].Init(b => { b.CurrentSize =  10; b.Managed = true;});

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);
		}

		[Test]
		public void ExcessImbalance()
		{
			var zoneOpt = new ZoneOptimizationDescriptor(
				new []{_cA},
				new []{_sA, _sB, _sC});

			zoneOpt[_cA, _sA].OnSize(   10).OnManaged();
			zoneOpt[_cA, _sB].OnSize(10000).OnManaged();
			zoneOpt[_cA, _sC].OnSize(   10).OnManaged();

			var solve = OptimalDataPartition.Find(zoneOpt);

			Assert.IsTrue(solve.IsSuccess);
			Assert.Multiple(() =>
			{
				Assert.That(zoneOpt[_cA, _sA].TargetSize, Is.EqualTo(3340).Within(1));
				Assert.That(zoneOpt[_cA, _sB].TargetSize, Is.EqualTo(3340).Within(1));
				Assert.That(zoneOpt[_cA, _sC].TargetSize, Is.EqualTo(3340).Within(1));
			});
		}

		private static readonly ShardIdentity _sA = new ShardIdentity("shA");
		private static readonly ShardIdentity _sB = new ShardIdentity("shB");
		private static readonly ShardIdentity _sC = new ShardIdentity("shC");
		private static readonly ShardIdentity _sD = new ShardIdentity("shD");

		private static readonly CollectionNamespace _cA = new CollectionNamespace("d", "collA");
		private static readonly CollectionNamespace _cB = new CollectionNamespace("d", "collB");
		private static readonly CollectionNamespace _cC = new CollectionNamespace("d", "collC");
		private static readonly CollectionNamespace _cD = new CollectionNamespace("d", "collD");
		private static readonly CollectionNamespace _cE = new CollectionNamespace("d", "collE");
	}
}
