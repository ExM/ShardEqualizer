using ShardEqualizer.DAL.EphemeralCluster;
using Xunit;

namespace ShardEqualizer.DAL;

[CollectionDefinition(nameof(ChangeGlobalChunkSizeDefinition), DisableParallelization = true)]
public class ChangeGlobalChunkSizeDefinition: ICollectionFixture<ShardClusterFixture>
{
}