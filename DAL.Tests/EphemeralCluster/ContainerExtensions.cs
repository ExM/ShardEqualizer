using System.Text;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Xunit;

namespace ShardEqualizer.DAL.EphemeralCluster;

public static class ContainerExtensions
{
    public static async Task<ExecResult> ExecMongoScript(this IContainer container, string scriptContent, CancellationToken token)
    {
        var scriptFilePath = string.Join("/", string.Empty, "tmp", Guid.NewGuid().ToString("D"), Path.GetRandomFileName());

        await container.CopyAsync(Encoding.Default.GetBytes(scriptContent), scriptFilePath, Unix.FileMode644, token)
            .ConfigureAwait(false);

        var command = new[]
        {
            "mongosh",
            "--quiet",
            "--eval",
            $"load('{scriptFilePath}')",
        };

        return await container.ExecAsync(command, token)
            .ConfigureAwait(false);
    }
    
    public static string GetMongoConnectionString(this IContainer container)
    {
        var endpoint = new UriBuilder("mongodb", container.Hostname, container.GetMappedPublicPort(ShardClusterFixture.MongoDbPort));
        endpoint.Query = "?directConnection=true";
        return endpoint.ToString();
    }
    
    public static async Task<string> SafeExecScript(this ShardClusterFixture clusterFixture, string script)
    {
        var res = await clusterFixture.ExecScript(
            $"try{{{script}}}catch(e){{throw e;}}", 
            TestContext.Current.CancellationToken);
        res.ExitCode.Should().Be(0, res.Stderr);
        return res.Stdout;
    }
    
    public static async Task<string> SetCustomChunkSize(this ShardClusterFixture clusterFixture, int sizeInMb)
    {
        var script = 
            $"db.getSiblingDB(\"config\").settings.updateOne({{ _id: \"chunksize\" }}, {{ $set: {{ _id: \"chunksize\", value: {sizeInMb} }} }}, {{ upsert: true }});";

        return await clusterFixture.SafeExecScript(script);
    }
    
    public static async Task<string> UnsetCustomChunkSize(this ShardClusterFixture clusterFixture)
    {
        var script = 
            $"db.getSiblingDB(\"config\").settings.deleteOne({{ _id: \"chunksize\" }});";

        return await clusterFixture.SafeExecScript(script);
    }
    
    public static async Task AddShardToZone(this ShardClusterFixture clusterFixture, string shardName, string zone)
    {
        await clusterFixture.SafeExecScript(
            $$"""
              db.getSiblingDB("admin").adminCommand({
                  addShardToZone : "{{shardName}}",
                  zone : "{{zone}}"
              })
              """);
    }
    
    
}