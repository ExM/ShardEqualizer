using MongoDB.Driver;

namespace ShardEqualizer.DAL;

public class SystemDatabases
{
    public SystemDatabases(IMongoClient mongoClient)
    {
        Admin = mongoClient.GetDatabase("admin");
        Config = mongoClient.GetDatabase("config");
    }

    public IMongoDatabase Admin { get; }
    public IMongoDatabase Config { get; }
}
