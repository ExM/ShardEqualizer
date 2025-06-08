using MongoDB.Driver;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.DAL.Repositories;

public class CollectionRepository
{
	private readonly IMongoCollection<ShardedCollectionInfo> _coll;

	public CollectionRepository(SystemDatabases dbs)
	{
		_coll = dbs.Config.GetCollection<ShardedCollectionInfo>("collections");
	}

	public async Task<IReadOnlyList<ShardedCollectionInfo>> FindAll(CancellationToken token)
	{
		var result = await _coll.Find(Builders<ShardedCollectionInfo>.Filter.Empty).ToListAsync(token);
			
		result.RemoveAll(c => string.Equals(c.Id.DatabaseNamespace.DatabaseName, "config", StringComparison.Ordinal));

		return result;
	}
}