using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.ClusterMaintenance.Serialization;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public class AdminDB
	{
		private readonly IMongoClient _client;
		private readonly IMongoDatabase _db;

		public AdminDB(IMongoClient client)
		{
			_client = client;
			_db = _client.GetDatabase(DatabaseNamespace.Admin.DatabaseName);
		}

		public async Task MoveChunk(CollectionNamespace ns, BsonDocument point, string targetShard, CancellationToken token)
		{
			var cmd = AdminCommand.MoveChunk(ns, point, targetShard);
			var result = await _db.RunCommandAsync<CommandResult>(cmd, null, token);
			result.EnsureSuccess();
		}
		
		public async Task MergeChunks(CollectionNamespace ns, BsonDocument leftBound, BsonDocument rightBound, CancellationToken token)
		{
			var cmd = AdminCommand.MergeChunks(ns, leftBound, rightBound);
			var result = await _db.RunCommandAsync<CommandResult>(cmd, null, token);
			result.EnsureSuccess();
		}
	}
}