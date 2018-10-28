using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ChunkRepository
	{
		private readonly IMongoCollection<ChunkInfo> _coll;

		public ChunkRepository(IMongoClient client)
		{
			_coll = client
				.GetDatabase("config")
				.GetCollection<ChunkInfo>("chunks");
		}

		public Task<IAsyncCursor<ChunkInfo>> Find(CollectionNamespace ns)
		{
			return _coll.FindAsync(_ => _.Namespace == ns.FullName);
		}

		public Task<long> Count(CollectionNamespace ns)
		{
			return _coll.CountDocumentsAsync(_ => _.Namespace == ns.FullName);
		}

		public Task<IAsyncCursor<ChunkInfo>> Find(string ns, IList<string> shardNames)
		{
			var filter = Builders<ChunkInfo>.Filter.Eq(_ => _.Namespace, ns);
			if(shardNames.Any())
				filter &= Builders<ChunkInfo>.Filter.In(_ => _.Shard, shardNames);
			
			return _coll.FindAsync(filter);
		}

		public Task<long> Count(string ns, IList<string> shardNames)
		{
			var filter = Builders<ChunkInfo>.Filter.Eq(_ => _.Namespace, ns);
			if(shardNames.Any())
				filter &= Builders<ChunkInfo>.Filter.In(_ => _.Shard, shardNames);
			
			return _coll.CountDocumentsAsync(filter);
		}

		public Filtered ByNamespace(CollectionNamespace ns)
		{
			return new Filtered(_coll, Builders<ChunkInfo>.Filter.Eq(_ => _.Namespace, ns.FullName));
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
					Sort = Builders<ChunkInfo>.Sort.Ascending(_ => _.Id)
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
					_filter & Builders<ChunkInfo>.Filter.Gte(_ => _.Id, id));
			}
			
			public Filtered ChunkTo(string id)
			{
				if (string.IsNullOrWhiteSpace(id))
					return this;
				
				return new Filtered(_coll,
					_filter & Builders<ChunkInfo>.Filter.Lte(_ => _.Id, id));
			}
		}
	}
}