using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.MongoCommands
{
	public class DbStatShard
	{
		[BsonElement("ok")]
		public int Ok;
		
		[BsonElement("db"), BsonRequired]
		public string Db;
		
		[BsonElement("collections"), BsonRequired]
		public long Collections;

		[BsonElement("views"), BsonRequired]
		public long Views;
		
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
		
		[BsonElement("$gleStats"), BsonRequired]
		public BsonDocument GleStats;
	}
}