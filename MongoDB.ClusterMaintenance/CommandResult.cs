using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.ClusterMaintenance
{
	public class CommandResult
	{
		[BsonElement("ok")]
		public int Ok { get; private set; }

		public bool IsSuccess => Ok == 1;

		public void EnsureSuccess()
		{
			if (!IsSuccess)
			{
				throw new InvalidOperationException("no success result");
			}
		}
	}

}