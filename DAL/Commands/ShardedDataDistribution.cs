using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ShardEqualizer.DAL.Models;

namespace ShardEqualizer.DAL.Commands;

[BsonIgnoreExtraElements]
public class ShardedDataDistributionCollection
{
	[BsonElement("ns"), BsonRequired]
	public required CollectionNamespace Ns { get; init; }

	[BsonElement("shards"), BsonRequired]
	public required ShardedDataDistribution[] Shards { get; init; }
}

[BsonIgnoreExtraElements]
public class ShardedDataDistribution
{
	[BsonElement("shardName"), BsonRequired]
	public required ShardIdentity ShardName { get; init; }
	
	[BsonElement("numOrphanedDocs"), BsonRequired]
	public required long NumOrphanedDocs { get; init; }

	[BsonElement("numOwnedDocuments"), BsonRequired]
	public required long NumOwnedDocuments { get; init; }

	[BsonElement("ownedSizeBytes"), BsonRequired]
	public required long OwnedSizeBytes { get; init; }

	[BsonElement("orphanedSizeBytes"), BsonRequired]
	public required long OrphanedSizeBytes { get; init; }
}

