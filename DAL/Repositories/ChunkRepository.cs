using MongoDB.Driver;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.DAL.Repositories;

public class ChunkRepository
{
	private readonly IMongoCollection<Chunk> _coll;

	public ChunkRepository(SystemDatabases dbs)
	{
		_coll = dbs.Config.GetCollection<Chunk>("chunks");
	}
	
	public Filtered ByUuid(Guid uuid)
	{
		return new Filtered(_coll, Builders<Chunk>.Filter.Eq(c => c.Uuid, uuid));
	}

	public class Filtered
	{
		private readonly IMongoCollection<Chunk> _coll;
		private readonly FilterDefinition<Chunk> _filter;

		internal Filtered(IMongoCollection<Chunk> coll, FilterDefinition<Chunk> filter)
		{
			_coll = coll;
			_filter = filter;
		}

		public async Task<IAsyncCursor<Chunk>> Find(CancellationToken token)
		{
			return await _coll.FindAsync(_filter, new FindOptions<Chunk>()
			{
				Sort = Builders<Chunk>.Sort
					.Ascending(с => с.Uuid)
					.Ascending(c => c.Min)
			}, token);
		}

		public async Task<long> Count(CancellationToken token)
		{
			return await _coll.CountDocumentsAsync(_filter, null, token);
		}

		public Filtered From(BsonBound? from)
		{
			if (from == null)
				return this;

			return new Filtered(_coll, _filter & Builders<Chunk>.Filter.Gte(c => c.Min, from));
		}

		public Filtered To(BsonBound? to)
		{
			if (to == null)
				return this;

			return new Filtered(_coll, _filter & Builders<Chunk>.Filter.Lt(c => c.Min, to));
		}

		public Filtered NoJumbo()
		{
			return new Filtered(_coll, _filter & Builders<Chunk>.Filter.Where(c => c.Jumbo != true));
		}

		public Filtered OnlyJumbo()
		{
			return new Filtered(_coll, _filter & Builders<Chunk>.Filter.Where(c => c.Jumbo == true));
		}

		public Filtered ExcludeShards(IEnumerable<ShardIdentity> shards)
		{
			return new Filtered(_coll, _filter & Builders<Chunk>.Filter.Nin(c => c.Shard, shards));
		}

		public Filtered ByShards(IEnumerable<ShardIdentity> shards)
		{
			return new Filtered(_coll, _filter & Builders<Chunk>.Filter.In(c => c.Shard, shards));
		}
	}
}