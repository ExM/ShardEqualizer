using System;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.ClusterMaintenance.ShardSizeEqualizing;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.ClusterMaintenance
{
	[TestFixture]
	public class ZoneOptimizationDescriptorSerializeTests
	{
		[Test]
		public void SerializeDemo()
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

			var text = zoneOpt.Serialize();
			Console.WriteLine(zoneOpt.Serialize());

			var zoneOpt2 = ZoneOptimizationDescriptor.Deserialize(text);
			
			Assert.AreEqual(zoneOpt[_cB, _sB].CurrentSize, zoneOpt2[_cB, _sB].CurrentSize);
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