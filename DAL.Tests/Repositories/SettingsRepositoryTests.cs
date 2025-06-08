using FluentAssertions;
using ShardEqualizer.DAL.EphemeralCluster;
using Xunit;

namespace ShardEqualizer.DAL.Repositories;

[Collection(nameof(ChangeGlobalChunkSizeDefinition))]
public class SettingsRepositoryTests
{
    private readonly ShardClusterFixture _clusterFixture;
    private readonly SettingsRepository _repo;

    public SettingsRepositoryTests(ShardClusterFixture clusterFixture)
    {
        _clusterFixture = clusterFixture;
        _repo = new SettingsRepository(clusterFixture.SystemDatabases);
    }
    
    [Fact]
    public async Task ReadDefaultChunksize()
    {
        await _clusterFixture.UnsetCustomChunkSize();
        
        var size = await _repo.GetChunksize(CancellationToken.None);

        size.Should().Be(128 * 1024 * 1024);
    }
    
    [Fact]
    public async Task ReadCustomChunksize()
    {
        await _clusterFixture.SetCustomChunkSize(130);
        
        try
        {
            var size = await _repo.GetChunksize(CancellationToken.None);
            size.Should().Be(130L * 1024 * 1024);
        }
        finally
        {
            await _clusterFixture.UnsetCustomChunkSize();
        }
    }
}

