using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ShardEqualizer.DAL.Repositories;

public class SettingsRepository
{
	// https://www.mongodb.com/docs/manual/tutorial/modify-chunk-size-in-sharded-cluster/
	private const long DefaultChunkSize = 128; // Mb
		
	private const long Scale = 1024 * 1024; // Mb
		
	private readonly IMongoCollection<BsonDocument> _coll;

	public SettingsRepository(SystemDatabases dbs)
	{
		_coll = dbs.Config.GetCollection<BsonDocument>("settings");
	}

	public async Task<long> GetChunksize(CancellationToken token)
	{
		var model =  await _coll.Find(Builders<BsonDocument>.Filter.Eq("_id", "chunksize")).SingleOrDefaultAsync(token);
		if(model == null)
			return DefaultChunkSize * Scale;

		var typedModel = BsonSerializer.Deserialize<ChunksizeModel>(model);
		
		return typedModel.Value * Scale;
	}

	[BsonIgnoreExtraElements]
	private class ChunksizeModel
	{
		[BsonElement("value"), BsonRequired]
		public long Value { get; init; }
	}
}