using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;

namespace ShardEqualizer.MongoCommands
{
	public class CollStats: CommandResult
	{
		[BsonElement("ns"), BsonRequired]
		public CollectionNamespace Ns;

		[BsonElement("size"), BsonRequired]
		public long Size;

		[BsonElement("count"), BsonRequired]
		public long Count;

		[BsonElement("max"), BsonIgnoreIfNull]
		public long? Max;

		[BsonElement("maxSize"), BsonIgnoreIfNull]
		public long? MaxSize;

		[BsonElement("sleepCount"), BsonIgnoreIfNull]
		public long? SleepCount;

		[BsonElement("sleepMS"), BsonIgnoreIfNull]
		public long? SleepMS;

		[BsonElement("avgObjSize"), BsonIgnoreIfNull]
		public double? AvgObjSize;

		[BsonElement("storageSize"), BsonRequired]
		public long StorageSize;

		[BsonElement("capped"), BsonRequired]
		public bool Capped;

		[BsonElement("wiredTiger"), BsonIgnoreIfNull]
		public BsonDocument WiredTiger;

		[BsonElement("nindexes"), BsonRequired]
		public long NIndexes;

		[BsonElement("indexDetails"), BsonIgnoreIfNull]
		public BsonDocument IndexDetails;

		[BsonElement("totalIndexSize"), BsonRequired]
		public long TotalIndexSize;

		[BsonElement("indexSizes"), BsonDictionaryOptions(DictionaryRepresentation.Document), BsonRequired]
		public IReadOnlyDictionary<string, long> IndexSizes;

		[BsonElement("$gleStats"), BsonIgnoreIfNull]
		public BsonDocument GleStats;

		[BsonElement("$configServerState"), BsonIgnoreIfNull]
		public BsonDocument ConfigServerState;

		[BsonElement("lastCommittedOpTime"), BsonIgnoreIfNull]
		public BsonTimestamp LastCommittedOpTime;

		[BsonElement("totalSize")]
		public long? TotalSize;

		[BsonElement("freeStorageSize")]
		public long? FreeStorageSize;

		[BsonElement("indexBuilds")]
		public BsonArray IndexBuilds;

		[BsonElement("scaleFactor")]
		public double? ScaleFactor;

		//[BsonExtraElements]
		//public BsonDocument ExtraElements;
	}
}
