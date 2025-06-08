using MongoDB.Bson.Serialization.Attributes;

namespace ShardEqualizer.DAL.Models;

[BsonIgnoreExtraElements]
public class Shard
{
	[BsonId]
	public required ShardIdentity Id { get; init; }

	[BsonElement("host"), BsonRequired]
	public required string Host { get; init; }

	[BsonElement("state"), BsonRequired]
	public required ShardState State { get; init; }

	[BsonElement("tags"), BsonIgnoreIfNull]
	public IReadOnlyList<TagIdentity>? Tags { get; init; }

	public bool HaveTag(TagIdentity tag) => Tags is not null && Tags.Contains(tag);
}