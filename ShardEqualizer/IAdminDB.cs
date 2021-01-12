using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.ClusterMaintenance
{
	public interface IAdminDB
	{
		Task MoveChunk(CollectionNamespace ns, BsonDocument point, string targetShard, CancellationToken token);
		Task MergeChunks(CollectionNamespace ns, BsonDocument leftBound, BsonDocument rightBound, CancellationToken token);
	}
}