using ShardEqualizer.DAL.EphemeralCluster;
using Xunit;

namespace ShardEqualizer.DAL;

[CollectionDefinition(nameof(CommonClusterDefinition))]
public class CommonClusterDefinition : ICollectionFixture<ShardClusterFixture>
{
}