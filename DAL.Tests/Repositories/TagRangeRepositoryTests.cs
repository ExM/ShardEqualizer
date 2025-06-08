using FluentAssertions;
using MongoDB.Driver;
using ShardEqualizer.DAL.EphemeralCluster;
using ShardEqualizer.DAL.Models;
using Xunit;

namespace ShardEqualizer.DAL.Repositories;

[Collection(nameof(CommonClusterDefinition))]
public class TagRangeRepositoryTests
{
    private readonly ShardClusterFixture _clusterFixture;
    private readonly TagRangeRepository _repo;

    public TagRangeRepositoryTests(ShardClusterFixture clusterFixture)
    {
        _clusterFixture = clusterFixture;
        _repo = new TagRangeRepository(clusterFixture.SystemDatabases);
    }
    
    [Fact]
    public async Task ReadSingleTag()
    {
        await _clusterFixture.AddShardToZone("charlie", "charlie");

        await _clusterFixture.SafeExecScript(
            """
            db.adminCommand(
            {
                shardCollection: "TagRangeRepositoryTests.ReadSingleTag",
                key: { group: 1 }
            });
            """);

        var script = 
        """
        db.getSiblingDB("admin").runCommand({
            updateZoneKeyRange : "TagRangeRepositoryTests.ReadSingleTag",
            min : { group : 0 },
            max : { group : 10 },
            zone : "charlie"
        });
        """;

        await _clusterFixture.SafeExecScript(script);
        
        var items = await _repo.Get(CollectionNamespace.FromFullName("TagRangeRepositoryTests.ReadSingleTag"), CancellationToken.None);

        var tagZone = items.Should().ContainSingle().Subject;

        tagZone.Tag.Should().Be(new TagIdentity("charlie"));
        tagZone.Min.Should().Be(BsonBound.Parse($"{{ \"group\" : 0 }}"));
        tagZone.Max.Should().Be(BsonBound.Parse($"{{ \"group\" : 10 }}"));
    }
}

