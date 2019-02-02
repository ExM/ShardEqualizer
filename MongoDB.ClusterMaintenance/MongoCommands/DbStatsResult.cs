using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.ClusterMaintenance.MongoCommands
{
	public class DbStatsResult : CommandResult
	{
		[BsonElement("raw"), BsonDictionaryOptions(DictionaryRepresentation.Document), BsonRequired]
		public IReadOnlyDictionary<string, DbStatShard> Raw;
		
		[BsonElement("objects"), BsonRequired]
		public long Objects;
		
		[BsonElement("avgObjSize"), BsonRequired]
		public double AvgObjSize;
		
		[BsonElement("dataSize"), BsonRequired]
		public long DataSize;
		
		[BsonElement("storageSize"), BsonRequired]
		public long StorageSize;
		
		[BsonElement("numExtents"), BsonRequired]
		public long NumExtents;
		
		[BsonElement("indexes"), BsonRequired]
		public long Indexes;
		
		[BsonElement("indexSize"), BsonRequired]
		public long IndexSize;
		
		[BsonElement("fileSize"), BsonRequired]
		public long FileSize;
		
		[BsonElement("extentFreeList"), BsonRequired]
		public BsonDocument ExtentFreeList;
	}
}