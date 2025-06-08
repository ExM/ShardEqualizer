using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;
using Xunit;

namespace ShardEqualizer.DAL.EphemeralCluster;

public class ShardClusterFixture : IAsyncLifetime
{
    public const string MongoDbImage = "mongo:8.0.10";

    public const ushort MongoDbPort = 27017;
    
    private readonly INetwork _network;
    
    private readonly IContainer _routerH0;
    private readonly IContainer _configRsH0;
    private readonly IContainer _alphaRsH0;
    private readonly IContainer _bravoRsH0;
    private readonly IContainer _charlieRsH0;
    
    public ShardClusterFixture()
    {
        _network = new NetworkBuilder()
            .WithName("mongoTestClusterNetwork")
            .Build();

        _configRsH0 = BuildConfigReplica(_network, "config").Build();
        _alphaRsH0 = BuildShardReplica(_network, "alpha").Build();
        _bravoRsH0 = BuildShardReplica(_network, "bravo").Build();
        _charlieRsH0 = BuildShardReplica(_network, "charlie").Build();
        
        _routerH0 = BuildRouter(_network)
            .DependsOn(_configRsH0)
            .DependsOn(_alphaRsH0)
            .DependsOn(_bravoRsH0)
            .DependsOn(_charlieRsH0)
            .Build();
        
        _lazyClient = new Lazy<IMongoClient>(() => new MongoClient(GetConnectionString()));
    }

    private static ContainerBuilder BuildShardReplica(INetwork network, string replicaSetName)
    {
        var hostName = replicaSetName + "-h0";
        
        return new ContainerBuilder()
            .WithImage(MongoDbImage)
            .WithNetwork(network)
            .WithHostname(hostName)
            .WithPortBinding(MongoDbPort, true)
            .WithCommand("--port", "27017", "--shardsvr", "--replSet", replicaSetName, "--oplogSize", "16", "--bind_ip_all")
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitInitiateReplicaSet(hostName, replicaSetName)));
    }
    
    private static ContainerBuilder BuildConfigReplica(INetwork network, string replicaSetName)
    {
        var hostName = replicaSetName + "-h0";
        
        return new ContainerBuilder()
            .WithImage(MongoDbImage)
            .WithNetwork(network)
            .WithHostname(hostName)
            .WithPortBinding(MongoDbPort, true)
            .WithCommand("--port", "27017", "--configsvr", "--replSet", replicaSetName, "--oplogSize", "16", "--bind_ip_all")
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitInitiateConfigReplicaSet(hostName, replicaSetName)));
    }
    
    private static ContainerBuilder BuildRouter(INetwork network)
    {
        return new ContainerBuilder()
            .WithImage(MongoDbImage)
            .WithNetwork(network)
            .WithHostname("router")
            .WithPortBinding(MongoDbPort, true)
            .WithEntrypoint("mongos")
            .WithCommand("--port", "27017", "--configdb", "config/config-h0:27017", "--bind_ip_all")
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new WaitIndicateReadiness()));
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(
            _alphaRsH0.StartAsync(),
            _bravoRsH0.StartAsync(),
            _charlieRsH0.StartAsync(),
            _configRsH0.StartAsync()).ConfigureAwait(false);
        
        await _routerH0.StartAsync().ConfigureAwait(false);

        await _routerH0.ExecMongoScript("sh.addShard(\"alpha/alpha-h0:27017\")", CancellationToken.None);
        await _routerH0.ExecMongoScript("sh.addShard(\"bravo/bravo-h0:27017\")", CancellationToken.None);
        await _routerH0.ExecMongoScript("sh.addShard(\"charlie/charlie-h0:27017\")", CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await _routerH0.DisposeAsync().ConfigureAwait(false);
        
        await Task.WhenAll(
            _alphaRsH0.DisposeAsync().AsTask(),
            _bravoRsH0.DisposeAsync().AsTask(),
            _charlieRsH0.DisposeAsync().AsTask(),
            _configRsH0.DisposeAsync().AsTask()).ConfigureAwait(false);
        
        await _network.DisposeAsync().ConfigureAwait(false);
    }

    public string GetConnectionString() 
        => _routerH0.GetMongoConnectionString();

    public Task<ExecResult> ExecScript(string scriptContent, CancellationToken ct = default)
        => _routerH0.ExecMongoScript(scriptContent, ct);

    private readonly Lazy<IMongoClient> _lazyClient;
    
    public IMongoClient Client => _lazyClient.Value;
    
    public SystemDatabases SystemDatabases => new SystemDatabases(Client);
    
    private sealed class WaitInitiateReplicaSet : IWaitUntil
    {
        private readonly string _scriptContent;

        public WaitInitiateReplicaSet(string hostName, string replicaSetName)
        {
            _scriptContent = $"try{{rs.status()}}catch(e){{rs.initiate({{_id:'{replicaSetName}',members:[{{_id:0,host:'{hostName}:27017'}}]}});throw e;}}";
        }

        public async Task<bool> UntilAsync(IContainer container)
        {
            var execResult = await container.ExecMongoScript(_scriptContent, CancellationToken.None).ConfigureAwait(false);
            
            return execResult.ExitCode == 0;
        }
    }
    
    private sealed class WaitInitiateConfigReplicaSet : IWaitUntil
    {
        private readonly string _scriptContent;

        public WaitInitiateConfigReplicaSet(string hostName, string replicaSetName)
        {
            _scriptContent = $"try{{rs.status()}}catch(e){{rs.initiate({{_id:'{replicaSetName}',configsvr:true,members:[{{_id:0,host:'{hostName}:27017'}}]}});throw e;}}";
        }

        public async Task<bool> UntilAsync(IContainer container)
        {
            var execResult = await container.ExecMongoScript(_scriptContent, CancellationToken.None).ConfigureAwait(false);
            
            return execResult.ExitCode == 0;
        }
    }
    
    private sealed class WaitIndicateReadiness : IWaitUntil
    {
        private static readonly string[] LineEndings = { "\r\n", "\n" };

        public async Task<bool> UntilAsync(IContainer container)
        {
            var (stdout, stderr) = await container.GetLogsAsync(since: container.StoppedTime, timestampsEnabled: false)
                .ConfigureAwait(false);

            return stdout.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries)
                .Concat(stderr.Split(LineEndings, StringSplitOptions.RemoveEmptyEntries))
                .Count(line => line.Contains("Waiting for connections")) == 1;
        }
    }
}