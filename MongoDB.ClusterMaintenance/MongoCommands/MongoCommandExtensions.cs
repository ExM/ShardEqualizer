using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.MongoCommands
{
	public static class MongoCommandExtensions
	{
		public static Task<DatasizeResult> Datasize(this IMongoDatabase db, ShardedCollectionInfo collInfo, Chunk chunk, CancellationToken token)
		{
			return db.Datasize(collInfo.Id, collInfo.Key, chunk.Min, chunk.Max, token);
		}
		
		public static async Task<DatasizeResult> Datasize(this IMongoDatabase db, CollectionNamespace ns, BsonDocument key, BsonDocument min, BsonDocument max, CancellationToken token)
		{
			var cmd = new BsonDocument
			{
				{ "datasize", ns.FullName },
				{ "keyPattern", key },
				{ "min", min },
				{ "max", max }
			};

			var result = await db.RunCommandAsync<DatasizeResult>(cmd, ReadPreference.SecondaryPreferred, token);
			result.EnsureSuccess();
			return result;
		}
		
		public static async Task<DbStatsResult> DbStats(this IMongoDatabase db, int scale, CancellationToken token)
		{
			var cmd =  new BsonDocument
			{
				{ "dbStats", 1 },
				{ "scale", scale},
			};
			
			var result = await db.RunCommandAsync<DbStatsResult>(cmd, ReadPreference.SecondaryPreferred, token);
			result.EnsureSuccess();
			return result;
		}
		
		public static async Task<CollStatsResult> CollStats(this IMongoDatabase db, string collectionName, int scale, CancellationToken token)
		{
			var cmd =  new BsonDocument
			{
				{ "collStats", collectionName },
				{ "scale", scale},
			};
			
			var result = await db.RunCommandAsync<CollStatsResult>(cmd, ReadPreference.SecondaryPreferred, token);
			result.EnsureSuccess();
			return result;
		}
	}
}