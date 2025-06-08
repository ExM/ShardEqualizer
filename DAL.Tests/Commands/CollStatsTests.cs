using FluentAssertions;
using ShardEqualizer.DAL.EphemeralCluster;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.WorkLoad;
using Xunit;

namespace ShardEqualizer.DAL.Commands;

[Collection(nameof(CommonClusterDefinition))]
public class CollStatsTests
{
    private readonly ShardClusterFixture _clusterFixture;

    public CollStatsTests(ShardClusterFixture clusterFixture)
    {
        _clusterFixture = clusterFixture;
    }
    
    [Fact]
    public async Task EmptyCollection()
    {
        var db = _clusterFixture.Client.GetDatabase("CollStatsTests");

        var result = await db.CollStats("EmptyCollection", 1, CancellationToken.None);

        result.Primary.Should().NotBeNull();
        result.Sharded.Should().BeFalse();
        result.Size.Should().Be(0);
        result.Count.Should().Be(0);
        result.StorageSize.Should().Be(0);
        result.TotalIndexSize.Should().Be(0);
        var p = result.Shards.Should().ContainSingle().Subject;
        p.Key.Should().Be(result.Primary!.Value);
        p.Value.Count.Should().Be(0);
        p.Value.Size.Should().Be(0);
        p.Value.StorageSize.Should().Be(0);
        p.Value.TotalIndexSize.Should().Be(0);
    }
    
    [Fact]
    public async Task UnshardedCollection()
    {
        var db = _clusterFixture.Client.GetDatabase("CollStatsTests");
        
        var coll = db.GetCollection<ItemModel>("UnshardedCollection");

        await coll.InsertOneAsync(ItemModel.Create(0), null, CancellationToken.None);
        await coll.InsertOneAsync(ItemModel.Create(1), null, CancellationToken.None);
        await coll.InsertOneAsync(ItemModel.Create(2), null, CancellationToken.None);

        var result = await db.CollStats("UnshardedCollection", 1, CancellationToken.None);

        var expectedCount = 3;
        var expectedSize = 909;
        
        result.Primary.Should().NotBeNull();
        result.Sharded.Should().BeFalse();
        result.Size.Should().Be(expectedSize);
        result.Count.Should().Be(expectedCount);
        result.StorageSize.Should().BeGreaterThan(expectedSize);
        result.TotalIndexSize.Should().BeGreaterThan(0);
        var p = result.Shards.Should().ContainSingle().Subject;
        p.Key.Should().Be(result.Primary!.Value);
        p.Value.Count.Should().Be(expectedCount);
        p.Value.Size.Should().Be(expectedSize);
        p.Value.StorageSize.Should().BeGreaterThan(expectedSize);
        p.Value.TotalIndexSize.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task ShardedCollection()
    {
        await _clusterFixture.AddShardToZone("bravo", "bravo");
        await _clusterFixture.AddShardToZone("charlie", "charlie");
        
        await _clusterFixture.SafeExecScript("db.adminCommand( { shardCollection: \"CollStatsTests.ShardedCollection\", key: { group: 1 } } )");

        await _clusterFixture.SafeExecScript(
            """sh.addTagRange( "CollStatsTests.ShardedCollection", { "group" : NumberInt(0) }, { "group" : NumberInt(10) }, "bravo");""");
        await _clusterFixture.SafeExecScript(
            """sh.addTagRange( "CollStatsTests.ShardedCollection", { "group" : NumberInt(10) }, { "group" : NumberInt(20) }, "charlie");""");
        
        await _clusterFixture.SafeExecScript(
            """sh.splitAt( "CollStatsTests.ShardedCollection", { "group" : NumberInt(10) } )""");
        
        await _clusterFixture.SafeExecScript(
            """sh.moveChunk("CollStatsTests.ShardedCollection", { "group": NumberInt(0) }, "bravo")""");
        await _clusterFixture.SafeExecScript(
            """sh.moveChunk("CollStatsTests.ShardedCollection", { "group": NumberInt(10) }, "charlie")""");
        
        var db = _clusterFixture.Client.GetDatabase("CollStatsTests");
        
        var coll = db.GetCollection<ItemModel>("ShardedCollection");

        await coll.InsertOneAsync(ItemModel.Create(1), null, CancellationToken.None);
        await coll.InsertOneAsync(ItemModel.Create(2), null, CancellationToken.None);
        await coll.InsertOneAsync(ItemModel.Create(3), null, CancellationToken.None);
        
        await coll.InsertOneAsync(ItemModel.Create(11), null, CancellationToken.None);
        await coll.InsertOneAsync(ItemModel.Create(12), null, CancellationToken.None);

        var result = await db.CollStats("ShardedCollection", 1, CancellationToken.None);

        var expectedCountWithBravo = 3;
        var expectedSizeWithBravo = 909;
        
        var expectedCountWithCharlie = 2;
        var expectedSizeWithCharlie = 606;
        
        result.Primary.Should().BeNull();
        result.Sharded.Should().BeTrue();
        result.Size.Should().Be(expectedSizeWithBravo + expectedSizeWithCharlie);
        result.Count.Should().Be(expectedCountWithBravo + expectedCountWithCharlie);

        result.Shards.Should().HaveCount(2);

        var bravoStats = result.Shards[new ShardIdentity("bravo")];
        
        bravoStats.Count.Should().Be(expectedCountWithBravo);
        bravoStats.Size.Should().Be(expectedSizeWithBravo);
        
        var charlieStats = result.Shards[new ShardIdentity("charlie")];
        
        charlieStats.Count.Should().Be(expectedCountWithCharlie);
        charlieStats.Size.Should().Be(expectedSizeWithCharlie);
    }
}