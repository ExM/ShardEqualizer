using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using ShardEqualizer.DAL.Commands;
using ShardEqualizer.DAL.Models;
using ShardEqualizer.ShardedClusterViews;

namespace ShardEqualizer.ConfigServices
{
	public class ChunkSizeService
	{
		private readonly IMongoClient _mongoClient;
		private readonly ShardedCollectionInfoView _shardedCollectionInfoView;

		private IReadOnlyDictionary<CollectionNamespace, ShardedCollectionInfo> _shCollsMap = null;

		public ChunkSizeService(IMongoClient mongoClient, ShardedCollectionInfoView shardedCollectionInfoView)
		{
			_mongoClient = mongoClient;
			_shardedCollectionInfoView = shardedCollectionInfoView;
		}

		public async Task<long> Get(CollectionNamespace ns, BsonBound min, BsonBound max, CancellationToken token)
		{
			if (_shCollsMap == null)
				_shCollsMap = await _shardedCollectionInfoView.Get(token);

			var collInfo = _shCollsMap[ns];
			var db = _mongoClient.GetDatabase(ns.DatabaseNamespace.DatabaseName);
			var result = await db.Datasize(ns, collInfo.Key, min, max, false, token);
			return result.Size;
		}
	}
}
