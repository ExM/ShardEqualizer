using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ChunkRepository
	{
		private readonly IMongoCollection<ChunkInfo> _coll;

		internal ChunkRepository(IMongoDatabase db)
		{
			_coll = db.GetCollection<ChunkInfo>("chunks");
		}
		
		public Task<ChunkInfo> Find(string id)
		{
			return _coll.Find(_ => _.Id == id).SingleOrDefaultAsync();
		}

		public Filtered ByNamespace(CollectionNamespace ns)
		{
			return new Filtered(_coll, Builders<ChunkInfo>.Filter.Eq(_ => _.Namespace, ns));
		}
		
		public class Filtered
		{
			private readonly IMongoCollection<ChunkInfo> _coll;
			private readonly FilterDefinition<ChunkInfo> _filter;

			internal Filtered(IMongoCollection<ChunkInfo> coll, FilterDefinition<ChunkInfo> filter)
			{
				_coll = coll;
				_filter = filter;
			}

			public Task<IAsyncCursor<ChunkInfo>> Find()
			{
				return _coll.FindAsync(_filter, new FindOptions<ChunkInfo>()
				{
					Sort = Builders<ChunkInfo>.Sort
						.Ascending(_ => _.Namespace)
						.Ascending(_ => _.Min)
				});
			}

			public Task<long> Count()
			{
				return _coll.CountDocumentsAsync(_filter);
			}
			
			public Filtered ByShards(IList<string> shardNames)
			{
				if (!shardNames.Any())
					return this;
				
				return new Filtered(_coll,
					_filter & Builders<ChunkInfo>.Filter.In(_ => _.Shard, shardNames));
			}
			
			public Filtered ChunkFrom(string id)
			{
				if (string.IsNullOrWhiteSpace(id))
					return this;
				
				return new Filtered(_coll,
					_filter & Builders<ChunkInfo>.Filter.Gt(_ => _.Id, id));
			}
			
			public Filtered ChunkTo(string id)
			{
				if (string.IsNullOrWhiteSpace(id))
					return this;
				
				return new Filtered(_coll,
					_filter & Builders<ChunkInfo>.Filter.Lt(_ => _.Id, id));
			}
		}
	}
}