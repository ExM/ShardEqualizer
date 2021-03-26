using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ShardEqualizer.Models;
using ShardEqualizer.MongoCommands;

namespace ShardEqualizer
{
	public class ChunkSizeService
	{
		private readonly IMongoClient _mongoClient;
		private readonly ShardedCollectionService _shardedCollectionService;

		private IReadOnlyDictionary<CollectionNamespace, ShardedCollectionInfo> _shCollsMap = null;

		public ChunkSizeService(IMongoClient mongoClient, ShardedCollectionService shardedCollectionService)
		{
			_mongoClient = mongoClient;
			_shardedCollectionService = shardedCollectionService;
		}

		public async Task<long> Get(CollectionNamespace ns, BsonBound min, BsonBound max, CancellationToken token)
		{
			if (_shCollsMap == null)
				_shCollsMap = await _shardedCollectionService.Get(token);

			var collInfo = _shCollsMap[ns];
			var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
			var result = await db.Datasize(ns, collInfo.Key, min, max, false, token);
			return result.Size;
		}
	}
}
