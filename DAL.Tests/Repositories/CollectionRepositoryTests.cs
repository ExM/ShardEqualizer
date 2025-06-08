using FluentAssertions;
using ShardEqualizer.DAL.EphemeralCluster;
using Xunit;

namespace ShardEqualizer.DAL.Repositories;

[Collection(nameof(CommonClusterDefinition))]
public class CollectionRepositoryTests
{
    private readonly ShardClusterFixture _clusterFixture;
    private readonly CollectionRepository _repo;

    public CollectionRepositoryTests(ShardClusterFixture clusterFixture)
    {
        _clusterFixture = clusterFixture;
        _repo = new CollectionRepository(clusterFixture.SystemDatabases);
    }
    
    [Fact]
    public async Task ReadDefaultCollectionOptions()
    {
        await _clusterFixture.SafeExecScript("""db.adminCommand( { shardCollection: "CollectionRepositoryTests.ReadDefaultCollectionOptions", key: { zipcode: 1 } } )""");
        
        var items = await _repo.FindAll(CancellationToken.None);

        var item = items.Should().Contain(i => i.Id.FullName == "CollectionRepositoryTests.ReadDefaultCollectionOptions").Subject;

        item.NoBalance.Should().BeFalse();
        item.Unique.Should().BeFalse();
        item.MaxChunkSizeBytes.Should().BeNull();
    }
    
    [Fact]
    public async Task ReadCustomChunkSize()
    {
        await _clusterFixture.SafeExecScript("""db.adminCommand( { shardCollection: "CollectionRepositoryTests.ReadCustomChunkSize", key: { zipcode: 1 } } )""");
        await _clusterFixture.SafeExecScript("""db.adminCommand( { "configureCollectionBalancing":"CollectionRepositoryTests.ReadCustomChunkSize", "chunkSize": 1 } );""");
        
        var items = await _repo.FindAll(CancellationToken.None);

        var item = items.Should().Contain(i => i.Id.FullName == "CollectionRepositoryTests.ReadCustomChunkSize").Subject;

        item.NoBalance.Should().BeFalse();
        item.Unique.Should().BeFalse();
        item.MaxChunkSizeBytes.Should().Be(1024 * 1024);
    }
}

