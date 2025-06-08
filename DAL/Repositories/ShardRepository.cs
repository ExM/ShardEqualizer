using MongoDB.Driver;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.DAL.Repositories;

public class ShardRepository
{
	private readonly IMongoCollection<Shard> _coll;

	public ShardRepository(SystemDatabases dbs)
	{
		_coll = dbs.Config.GetCollection<Shard>("shards");
	}

	public async Task<IReadOnlyCollection<Shard>> GetAll(CancellationToken token)
	{
		return await _coll.Find(Builders<Shard>.Filter.Empty).ToListAsync(token);
	}
}