using MongoDB.Driver;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.DAL.Repositories;

public class TagRangeRepository
{
	private readonly IMongoCollection<TagRange> _coll;

	public TagRangeRepository(SystemDatabases dbs)
	{
		_coll = dbs.Config.GetCollection<TagRange>("tags");
	}

	public async Task<IReadOnlyList<TagRange>> Get(CollectionNamespace ns, CancellationToken token)
	{
		return await _coll
			.Find(t => t.Namespace == ns)
			.Sort(Builders<TagRange>.Sort.Ascending(t => t.Min))
			.ToListAsync(token);
	}
}