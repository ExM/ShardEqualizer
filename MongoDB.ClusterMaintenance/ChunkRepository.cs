using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class ChunkRepository
	{
		private IMongoCollection<ChunkInfo> _coll;

		public ChunkRepository(IMongoClient client)
		{
			_coll = client
				.GetDatabase("config")
				.GetCollection<ChunkInfo>("chunks");
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
	}
}