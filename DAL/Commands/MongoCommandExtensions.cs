using MongoDB.Bson;
using MongoDB.Driver;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.DAL.Commands
{
	public static class MongoCommandExtensions
	{
		public static Task<DatasizeResult> Datasize(this IMongoDatabase db, ShardedCollectionInfo collInfo, Chunk chunk, bool estimate, CancellationToken token)
		{
			return db.Datasize(collInfo.Id, collInfo.Key, chunk.Min, chunk.Max, estimate, token);
		}

		public static async Task<DatasizeResult> Datasize(this IMongoDatabase db, CollectionNamespace ns, BsonDocument key, BsonBound min, BsonBound max, bool estimate, CancellationToken token)
		{
			var cmd = new BsonDocument
			{
				{ "datasize", ns.FullName },
				{ "keyPattern", key },
				{ "min", (BsonDocument)min },
				{ "max", (BsonDocument)max },
				{ "estimate", estimate}
			};

			return await db.RunCommandAsync<DatasizeResult>(cmd, ReadPreference.SecondaryPreferred, token);
		}

		public static async Task<CollStatsSummary> CollStats(this IMongoDatabase db, string collectionName, int scale, CancellationToken token)
		{
			var cmd =  new BsonDocument
			{
				{ "collStats", collectionName },
				{ "scale", scale},
			};

			return await db.RunCommandAsync<CollStatsSummary>(cmd, ReadPreference.SecondaryPreferred, token);
		}
	}
}
