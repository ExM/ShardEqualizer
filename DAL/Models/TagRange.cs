using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ShardEqualizer.DAL.Models;

public class TagRange
{
	[BsonId]
	public required BsonValue Id { get; init; }

	[BsonElement("ns"), BsonRequired]
	public required CollectionNamespace Namespace { get; init; }

	[BsonElement("min"), BsonRequired]
	public required BsonBound Min { get; init; }

	[BsonElement("max"), BsonRequired]
	public required BsonBound Max { get; init; }

	[BsonElement("tag"), BsonRequired]
	public required TagIdentity Tag { get; init; }
}