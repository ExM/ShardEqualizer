using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer
{
	public class CommandResult
	{
		[BsonElement("ok")]
		public int Ok { get; private set; }
		
		[BsonElement("errmsg"), BsonIgnoreIfNull]
		public string ErrorMessage { get; private set; }
		
		[BsonElement("millis"), BsonIgnoreIfDefault]
		public long Millis { get; private set; }

		public bool IsSuccess => Ok == 1;

		public void EnsureSuccess()
		{
			if (!IsSuccess)
			{
				throw new InvalidOperationException(ErrorMessage);
			}
		}
		
		[BsonElement("operationTime"), BsonIgnoreIfNull]
		public BsonTimestamp OperationTime { get; private set; }
		
		[BsonElement("$clusterTime"), BsonIgnoreIfNull]
		public BsonDocument ClusterTime { get; private set; }
	}

}