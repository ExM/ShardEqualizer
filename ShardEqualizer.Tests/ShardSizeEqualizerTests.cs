using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.ShardedClusterViews.Models;
using ShardEqualizer.ShardSizeEqualizing;

namespace ShardEqualizer;

[TestFixture]
public class ShardSizeEqualizerTests
{
	[Test]
	public async Task Demo()
	{
		Shard CreateTestShard(string name) => new Shard()
		{
			Id = new ShardIdentity("s" + name),
			Host = "s" + name,
			Tags = [new TagIdentity("t" + name)],
			State = ShardState.Aware,
		};
			
		var shards = new List<Shard>()
		{
			CreateTestShard("A"),
			CreateTestShard("B"),
			CreateTestShard("C"),
			CreateTestShard("S")
		};

		var collStatsByShards = new Dictionary<ShardIdentity, ShardCollectionStatistics>()
		{
			{shards[0].Id, GetCollStat(150)},
			{shards[1].Id, GetCollStat(150)},
			{shards[2].Id, GetCollStat(350)},
			{shards[3].Id, GetCollStat(100)},
		};

		var tagRanges = new List<TagRange>()
		{
			CreateTagRange("tA", testBound(0), testBound(100)),
			CreateTagRange("tB", testBound(100), testBound(200)),
			CreateTagRange("tC", testBound(200), testBound(500)),
		};

		var testNS = tagRanges[0].Namespace;
		var chunks = new List<ChunkInfo>();

		for (var i = 0; i < 10; i++)
		{
			chunks.Add(new ChunkInfo()
			{
				Min = testBound(i * 10),
				Max = testBound((i + 1) * 10),
				Shard = new ShardIdentity("sA"),
				Jumbo = false
			});
		}
		for (var i = 10; i < 20; i++)
		{
			chunks.Add(new ChunkInfo()
			{
				Min = testBound(i * 10),
				Max = testBound((i + 1) * 10),
				Shard = new ShardIdentity("sB"),
				Jumbo = false
			});
		}
		for (var i = 20; i < 50; i++)
		{
			chunks.Add(new ChunkInfo()
			{
				Min = testBound(i * 10),
				Max = testBound((i + 1) * 10),
				Shard = new ShardIdentity("sC"),
				Jumbo = false
			});
		}

		var chunkColl = new ChunkCollection(chunks, (ch) => Task.FromResult<long>(10));

		var targetSize = new Dictionary<TagIdentity, long>()
		{
			{tagRanges[0].Tag, 200},
			{tagRanges[1].Tag, 200},
			{tagRanges[2].Tag, 200},
		};

		var shardByTag = ShardTagCollator.Collate(shards, tagRanges.Select(x => x.Tag));

		var equalizer = new ShardSizeEqualizer(shardByTag, collStatsByShards, tagRanges, targetSize, chunkColl);

		var round = 0;
		while(!(await equalizer.Equalize()).IsSuccess)
		{
			if (equalizer.CurrentSizeDeviation < 3)
				break;

			round++;
		}
	}

	private TagRange CreateTagRange(string tagName, BsonBound min, BsonBound max)
	{
		return new TagRange()
		{
			Tag = new TagIdentity(tagName),
			Min = min,
			Max = max,
			Namespace = new CollectionNamespace("test", "test"),
			Id = ObjectId.GenerateNewId()
		};
	}

	private BsonBound testBound(int x)
	{
		return (BsonBound)new BsonDocument("x", x);
	}
	
	private ShardCollectionStatistics GetCollStat(long size)
	{
		return new ShardCollectionStatistics()
		{
			Size = size,
			StorageSize = size,
			TotalIndexSize = 0
		};
	}

}