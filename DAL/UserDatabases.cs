using MongoDB.Driver;

namespace ShardEqualizer.DAL;

public class UserDatabases
{
    private readonly IMongoClient _mongoClient;

    public UserDatabases(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public IMongoDatabase Get(string name)
    {
        return _mongoClient.GetDatabase(name);
    }
}
