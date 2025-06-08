using FluentAssertions;
using ShardEqualizer.DAL.EphemeralCluster;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.DAL.WorkLoad;
using Xunit;

namespace ShardEqualizer.DAL.Repositories;

[Collection(nameof(CommonClusterDefinition))]
public class ChunkRepositoryTests
{
    private readonly ShardClusterFixture _clusterFixture;
    private readonly ChunkRepository _repo;
    private readonly CollectionRepository _collRepo;

    public ChunkRepositoryTests(ShardClusterFixture clusterFixture)
    {
        _clusterFixture = clusterFixture;
        
        _collRepo = new CollectionRepository(clusterFixture.SystemDatabases);
        _repo = new ChunkRepository(clusterFixture.SystemDatabases);
    }
    
    [Fact]
    public async Task ReadDefaultChunk()
    {
        await _clusterFixture.SafeExecScript("db.adminCommand( { shardCollection: \"ChunkRepositoryTests.ReadDefaultChunk\", key: { group: 1 } } )");
        
        var coll = _clusterFixture.Client.GetDatabase("ChunkRepositoryTests").GetCollection<ItemModel>("ReadDefaultChunk");

        await coll.InsertOneAsync(ItemModel.Create(0), null, CancellationToken.None);

        var collInfo = (await _collRepo.FindAll(CancellationToken.None)).Should()
            .ContainSingle(c => c.Id.FullName == "ChunkRepositoryTests.ReadDefaultChunk").Subject;
        
        var items = await _repo
            .ByUuid(collInfo.Uuid)
            .Find(CancellationToken.None)
            .ToList(CancellationToken.None);

        var chunk = items.Should().ContainSingle().Subject;

        chunk.Min.Should().Be(BsonBound.Parse($"{{ \"group\" : MinKey }}"));
        chunk.Max.Should().Be(BsonBound.Parse($"{{ \"group\" : MaxKey }}"));
    }
}

