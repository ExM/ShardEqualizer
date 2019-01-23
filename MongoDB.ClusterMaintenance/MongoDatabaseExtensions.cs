using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public static class MongoDatabaseExtensions
	{
		public static Task<DatasizeResult> Datasize(this IMongoDatabase db, ShardedCollectionInfo collInfo, ChunkInfo chunk, CancellationToken token)
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
	}
}