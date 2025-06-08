using MongoDB.Bson;
using MongoDB.Driver;
using ShardEqualizer.DAL.EphemeralCluster;
using Xunit;

namespace ShardEqualizer.DAL;

[Collection(nameof(CommonClusterDefinition))]
public class ConnectToCluster
{
	private readonly ShardClusterFixture _clusterFixture;

	public ConnectToCluster(ShardClusterFixture clusterFixture)
	{
		_clusterFixture = clusterFixture;
	}
	
	[Fact]
	public async Task ListShards()
	{
		var client = new MongoClient(_clusterFixture.GetConnectionString());
		
		var cmd = new BsonDocument
		{
			{ "listShards", 1 }
		};

		var result = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(cmd, null, TestContext.Current.CancellationToken);
		
		TestContext.Current.TestOutputHelper!.Write(result.ToJson());
	}

	[Fact]
	public async Task GetShardMap()
	{
		const string scriptContent = "db.getSiblingDB(\"admin\").runCommand(\"getShardMap\");";

		var execResult = await _clusterFixture.ExecScript(scriptContent, TestContext.Current.CancellationToken)
			.ConfigureAwait(true);


		Assert.True(0L.Equals(execResult.ExitCode), execResult.Stderr);
		Assert.Empty(execResult.Stderr);
		
		TestContext.Current.TestOutputHelper!.WriteLine(execResult.Stdout);
	}
}