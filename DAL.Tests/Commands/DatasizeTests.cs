using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using ShardEqualizer.DAL.EphemeralCluster;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.WorkLoad;
using Xunit;

namespace ShardEqualizer.DAL.Commands;

[Collection(nameof(CommonClusterDefinition))]
public class DatasizeTests
{
    private readonly ShardClusterFixture _clusterFixture;

    public DatasizeTests(ShardClusterFixture clusterFixture)
    {
        _clusterFixture = clusterFixture;
    }
    
    [Fact]
    public async Task CalcAll()
    {
        await _clusterFixture.SafeExecScript("db.adminCommand( { shardCollection: \"DatasizeTests.CalcAll\", key: { group: 1 } } )");

        var db = _clusterFixture.Client.GetDatabase("DatasizeTests");
        var coll = db.GetCollection<ItemModel>("CalcAll");

        await coll.InsertOneAsync(ItemModel.Create(0), null, CancellationToken.None);
        await coll.InsertOneAsync(ItemModel.Create(1), null, CancellationToken.None);
        await coll.InsertOneAsync(ItemModel.Create(2), null, CancellationToken.None);

        var result = await db.Datasize(
            new CollectionNamespace("DatasizeTests", "CalcAll"),
            new BsonDocument() { { "group", 1 } },
            BsonBound.Parse("""{ "group": MinKey }"""),
            BsonBound.Parse("""{ "group": MaxKey }"""),
            false,
            CancellationToken.None);

        result.NumObjects.Should().Be(3);
        result.Size.Should().Be(909);
    }
    
    [Fact]
    public async Task WithWrongKey()
    {
        await _clusterFixture.SafeExecScript("db.adminCommand( { shardCollection: \"DatasizeTests.WithWrongKey\", key: { group: 1 } } )");

        var db = _clusterFixture.Client.GetDatabase("DatasizeTests");
        var coll = db.GetCollection<ItemModel>("WithWrongKey");

        await coll.InsertOneAsync(ItemModel.Create(0), null, CancellationToken.None);

        await Assert.ThrowsAsync<MongoCommandException>(
            () => db.Datasize(
                new CollectionNamespace("DatasizeTests", "WithWrongKey"),
                new BsonDocument() { { "wrongKey", 1 } },
                BsonBound.Parse("""{ "wrongKey": MaxKey }"""),
                BsonBound.Parse("""{ "wrongKey": MinKey }"""),
                false,
                CancellationToken.None));
    }
}