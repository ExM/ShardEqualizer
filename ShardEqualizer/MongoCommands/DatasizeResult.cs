﻿using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.MongoCommands
{
	public class DatasizeResult : CommandResult
	{
		[BsonElement("size"), BsonIgnoreIfNull]
		public long Size { get; private set; }
		
		[BsonElement("numObjects"), BsonIgnoreIfNull]
		public long NumObjects { get; private set; }
	}
}
