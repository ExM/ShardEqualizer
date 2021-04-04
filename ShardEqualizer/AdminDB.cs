using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using ShardEqualizer.MongoCommands;

namespace ShardEqualizer
{
	public class AdminDB : IAdminDB
	{
		private readonly IMongoDatabase _db;

		public AdminDB(IMongoClient client)
		{
			_db = client.GetDatabase(DatabaseNamespace.Admin.DatabaseName);
		}

		public async Task MoveChunk(CollectionNamespace ns, BsonDocument point, string targetShard, CancellationToken token)
		{
			var cmd =  new BsonDocument
			{
				{ "moveChunk", ns.FullName },
				{ "find", point },
				{ "to", targetShard }
			};

			var result = await _db.RunCommandAsync<CommandResult>(cmd, null, token);
			result.EnsureSuccess();
		}

		public async Task MergeChunks(CollectionNamespace ns, BsonDocument leftBound, BsonDocument rightBound, CancellationToken token)
		{
			var cmd = new BsonDocument
			{
				{ "mergeChunks", ns.FullName },
				{ "bounds", new BsonArray() { leftBound, rightBound }},
			};

			var result = await _db.RunCommandAsync<CommandResult>(cmd, null, token);
			result.EnsureSuccess();
		}
	}
}
