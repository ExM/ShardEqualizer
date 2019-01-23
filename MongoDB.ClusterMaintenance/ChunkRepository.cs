using System;
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
			return new Filtered(_coll, Task.FromResult(Builders<ChunkInfo>.Filter.Eq(_ => _.Namespace, ns)));
		}
		
		public class Filtered
		{
			private readonly IMongoCollection<ChunkInfo> _coll;
			private readonly Task<FilterDefinition<ChunkInfo>> _filterResolver;

			internal Filtered(IMongoCollection<ChunkInfo> coll, Task<FilterDefinition<ChunkInfo>> filterResolver)
			{
				_coll = coll;
				_filterResolver = filterResolver;
			}

			public async Task<IAsyncCursor<ChunkInfo>> Find()
			{
				return await _coll.FindAsync(await _filterResolver, new FindOptions<ChunkInfo>()
				{
					Sort = Builders<ChunkInfo>.Sort
						.Ascending(_ => _.Namespace)
						.Ascending(_ => _.Min)
				});
			}

			public async Task<long> Count()
			{
				return await _coll.CountDocumentsAsync(await _filterResolver);
			}
			
			public Filtered ChunkFrom(string id)
			{
				if (string.IsNullOrWhiteSpace(id))
					return this;

				return new Filtered(_coll, resolveChunkFromFilter(id));
			}

			private async Task<FilterDefinition<ChunkInfo>> resolveChunkFromFilter(string chunkId)
			{
				var chunkInfo = await _coll.Find(_ => _.Id == chunkId).SingleOrDefaultAsync();
				if(chunkInfo == null)
					throw new ArgumentException($"chunk {chunkId} not found");

				return await _filterResolver & Builders<ChunkInfo>.Filter.Gte(_ => _.Min, chunkInfo.Max);
			}
			
			public Filtered ChunkTo(string id)
			{
				if (string.IsNullOrWhiteSpace(id))
					return this;
				
				return new Filtered(_coll, resolveChunkToFilter(id));
			}
			
			private async Task<FilterDefinition<ChunkInfo>> resolveChunkToFilter(string chunkId)
			{
				var chunkInfo = await _coll.Find(_ => _.Id == chunkId).SingleOrDefaultAsync();
				if(chunkInfo == null)
					throw new ArgumentException($"chunk {chunkId} not found");

				return await _filterResolver & Builders<ChunkInfo>.Filter.Lt(_ => _.Min, chunkInfo.Min);
			}
		}
	}
}