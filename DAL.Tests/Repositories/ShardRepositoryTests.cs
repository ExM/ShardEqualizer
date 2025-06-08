using FluentAssertions;
using ShardEqualizer.DAL.EphemeralCluster;
using ShardEqualizer.DAL.Models;
using Xunit;

namespace ShardEqualizer.DAL.Repositories;

[Collection(nameof(CommonClusterDefinition))]
public class ShardRepositoryTests
{
    private readonly ShardClusterFixture _clusterFixture;
    private readonly ShardRepository _repo;

    public ShardRepositoryTests(ShardClusterFixture clusterFixture)
    {
        _clusterFixture = clusterFixture;
        _repo = new ShardRepository(clusterFixture.SystemDatabases);
    }
	
    [Fact]
    public async Task GetAll()
    {
        var allShards = await _repo.GetAll(CancellationToken.None);

        allShards.Should().HaveCount(3);
    }
    
    [Fact]
    public async Task StandardShard()
    {
        var allShards = await _repo.GetAll(CancellationToken.None);

        var alphaShard = allShards.Should().ContainSingle(s => (string)s.Id == "alpha").Subject;

        alphaShard.Tags.Should().BeNull();
        alphaShard.State.Should().Be(ShardState.Aware);
    }
    
    [Fact]
    public async Task TaggedShard()
    {
        await _clusterFixture.AddShardToZone("bravo", "anyTag");
        await _clusterFixture.AddShardToZone("bravo", "bravo");
        
        var allShards = await _repo.GetAll(CancellationToken.None);

        var alphaShard = allShards.Should().ContainSingle(s => (string)s.Id == "bravo").Subject;

        alphaShard.Tags.Should().BeEquivalentTo([new TagIdentity("anyTag"), new TagIdentity("bravo")]) ;
        alphaShard.State.Should().Be(ShardState.Aware);
    }
}