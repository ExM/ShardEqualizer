using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.ClusterMaintenance.Models;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance.MongoCommands
{
	public class CollStatsResult: CollStats
	{
		[BsonElement("primary"), BsonIgnoreIfNull]
		public ShardIdentity Primary;
		
		[BsonElement("nchunks"), BsonIgnoreIfNull]
		public long? NChunks;
		
		[BsonElement("sharded"), BsonRequired]
		public bool Sharded;
		
		[BsonElement("shards"), BsonDictionaryOptions(DictionaryRepresentation.Document), BsonIgnoreIfNull]
		public IReadOnlyDictionary<ShardIdentity, CollStats> Shards;
		
		[BsonElement("max"), BsonIgnoreIfNull]
		public long? Max;
			
		[BsonElement("maxSize"), BsonIgnoreIfNull]
		public long? MaxSize;
		
		[BsonElement("sleepCount"), BsonIgnoreIfNull]
		public long? SleepCount;
		
		[BsonElement("sleepMS"), BsonIgnoreIfNull]
		public long? SleepMS;
		
		//[BsonExtraElements]
		//public BsonDocument ExtraElements;
	}
}